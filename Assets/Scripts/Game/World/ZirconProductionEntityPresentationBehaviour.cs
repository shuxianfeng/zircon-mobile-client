using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Zircon.Mobile.Core.Assets;
using Zircon.Mobile.Core.Protocol;
using Zircon.Mobile.Game.Entities;
using Zircon.Mobile.UI.Login;

namespace Zircon.Mobile.Game.World
{
    /// <summary>Plays staged monster animation, spell effects, shadows and combat audio.</summary>
    [DefaultExecutionOrder(1000)]
    public sealed class ZirconProductionEntityPresentationBehaviour : MonoBehaviour
    {
        private static ZirconProductionEntityPresentationBehaviour instance;
        private const int MonsterModels = 2;
        private const int MonsterFramesPerModel = 4;
        private const int EffectFrames = 16;
        private const float PixelsPerUnit = 100f;
        private const float MonsterTargetHeight = 0.72f;
        private const bool EnableProceduralAudioFallback = false;
        private static readonly MonsterVisualSpec[] StagedMonsterSpecs =
        {
            new MonsterVisualSpec(8, "鸡", "entity.monster.mon3", 0, 0.21f, true),
            new MonsterVisualSpec(10, "鹿", "entity.monster.mon3", 1, 0.51f, true),
            new MonsterVisualSpec(11, "牛", "entity.monster.mon13", 1, 0.59f, true),
            new MonsterVisualSpec(189, "小花猪", "entity.monster.mon34", 0, 0.09f, false),
        };

        private readonly Sprite[,] monsterSprites = new Sprite[MonsterModels, MonsterFramesPerModel];
        private readonly Vector2[,] monsterOffsets = new Vector2[MonsterModels, MonsterFramesPerModel];
        private readonly Dictionary<int, Dictionary<int, MonsterVisualFrame>> stagedMonsterFrames =
            new Dictionary<int, Dictionary<int, MonsterVisualFrame>>();
        private readonly Sprite[] effectSprites = new Sprite[EffectFrames];
        private readonly Dictionary<uint, MonsterPresentation> monsters = new Dictionary<uint, MonsterPresentation>();
        private readonly Dictionary<uint, long> observedActions = new Dictionary<uint, long>();
        private readonly List<TransientEffect> transientEffects = new List<TransientEffect>();
        private readonly HashSet<uint> seenMonsterIds = new HashSet<uint>();
        private readonly HashSet<uint> loggedPetIds = new HashSet<uint>();
        private readonly List<uint> removedMonsterIds = new List<uint>();

        private ZirconProtocolProbeBehaviour session;
        private ZirconWorldDebugRenderer worldRenderer;
        private Sprite shadowSprite;
        private AudioSource audioSource;
        private AudioClip attackClip;
        private AudioClip magicClip;
        private bool loading;
        private bool ready;

        public void Configure(ZirconProtocolProbeBehaviour valueSession, ZirconWorldDebugRenderer valueWorldRenderer)
        {
            session = valueSession;
            worldRenderer = valueWorldRenderer;
        }

        private void OnEnable()
        {
            instance = this;
            // Resource chunks stay unloaded on login/character selection screens.
        }

        private void OnDisable()
        {
            if (instance == this) instance = null;
        }

        public static void PlayLocalEffectPreview()
        {
            if (instance == null || !instance.ready || instance.session == null) return;
            ZirconWorldSnapshot snapshot = instance.session.GetWorldSnapshot();
            Transform marker = snapshot?.LocalPlayer == null ? null : instance.FindMarker(snapshot.LocalPlayer.ObjectId);
            if (marker == null) return;
            instance.SpawnEffect(marker.position, -1);
            Debug.Log("P2 local skill effect preview started");
        }

        public static bool TryGetKnownMonster(int modelIndex, out string name, out float height)
        {
            foreach (MonsterVisualSpec spec in StagedMonsterSpecs)
            {
                if (spec.ModelIndex != modelIndex) continue;
                name = spec.Name;
                height = spec.Height;
                return true;
            }
            name = null;
            height = 0f;
            return false;
        }

        public static bool TryGetMonsterBodyRenderer(uint objectId, out SpriteRenderer renderer)
        {
            renderer = null;
            if (instance == null ||
                !instance.monsters.TryGetValue(objectId, out MonsterPresentation presentation) ||
                presentation?.Body == null || presentation.Body.sprite == null)
                return false;
            renderer = presentation.Body;
            return true;
        }

        private void LateUpdate()
        {
            if (session == null || worldRenderer == null)
                return;

            ZirconWorldSnapshot snapshot = session.GetWorldSnapshot();
            if (snapshot == null)
                return;

            if (!ready)
            {
                if (!loading && snapshot.HasLocalPlayer)
                    StartCoroutine(LoadPresentationAssets());
                return;
            }

            UpdateMonsters(snapshot);
            UpdateActionEffects(snapshot);
            UpdateTransientEffects();
        }

        private IEnumerator LoadPresentationAssets()
        {
            loading = true;
            ZirconRuntimeVisualCatalog catalog = null;
            string catalogError = null;
            yield return ZirconRuntimeVisualCatalog.Load(value => catalog = value, value => catalogError = value);
            if (catalog == null)
            {
                Debug.LogWarning("P2 entity presentation catalog unavailable: " + catalogError);
                loading = false;
                yield break;
            }

            int monsterLoaded = 0;
            for (int model = 0; model < MonsterModels; model++)
            for (int frame = 0; frame < MonsterFramesPerModel; frame++)
            {
                int capturedModel = model;
                int capturedFrame = frame;
                int index = model * 1000 + frame;
                yield return LoadSprite(catalog, "entity.monster.sample", index, (sprite, offset) =>
                {
                    monsterSprites[capturedModel, capturedFrame] = sprite;
                    monsterOffsets[capturedModel, capturedFrame] = offset;
                    monsterLoaded++;
                });
            }

            int effectLoaded = 0;
            for (int frame = 0; frame < EffectFrames; frame++)
            {
                int capturedFrame = frame;
                int index = frame * 2;
                yield return LoadSprite(catalog, "effect.icon.sample", index, (sprite, _) =>
                {
                    effectSprites[capturedFrame] = sprite;
                    effectLoaded++;
                });
            }

            EnsureAudio();
            ready = monsterLoaded == MonsterModels * MonsterFramesPerModel && effectLoaded == EffectFrames;
            loading = false;
            Debug.Log("P2 entity presentation ready: monsterFrames=" + monsterLoaded + "/8 effectFrames=" + effectLoaded +
                      "/16 shadows=procedural audio=formal-pending ready=" + ready);
            if (ready)
                StartCoroutine(LoadStagedMonsterFrames(catalog));
        }

        private IEnumerator LoadStagedMonsterFrames(ZirconRuntimeVisualCatalog catalog)
        {
            // Make every known creature visible before decoding hundreds of action frames.
            foreach (MonsterVisualSpec spec in StagedMonsterSpecs)
            {
                stagedMonsterFrames[spec.ModelIndex] = new Dictionary<int, MonsterVisualFrame>();
                yield return LoadStagedMonsterFrame(catalog, spec, spec.Shape * 1000);
                Debug.Log("P2 monster preview staged: " + spec.Name + " model=" + spec.ModelIndex);
            }

            // Fill directional standing frames next, then movement/combat/death.
            foreach (MonsterVisualSpec spec in StagedMonsterSpecs)
                yield return LoadStagedMonsterAction(catalog, spec, 0, 4);

            foreach (MonsterVisualSpec spec in StagedMonsterSpecs)
            {
                yield return LoadStagedMonsterAction(catalog, spec, 80, 6);
                yield return LoadStagedMonsterAction(catalog, spec, 160, 6);
                if (spec.HasDeadFrames)
                    yield return LoadStagedMonsterAction(catalog, spec, 329, 1);
                Debug.Log("P2 monster art staged: " + spec.Name + " model=" + spec.ModelIndex +
                          " frames=" + stagedMonsterFrames[spec.ModelIndex].Count + " set=" + spec.SetId);
            }
        }

        private IEnumerator LoadStagedMonsterAction(ZirconRuntimeVisualCatalog catalog,
            MonsterVisualSpec spec, int start, int count)
        {
            for (int direction = 0; direction < 8; direction++)
            for (int frame = 0; frame < count; frame++)
                yield return LoadStagedMonsterFrame(catalog, spec,
                    spec.Shape * 1000 + start + direction * 10 + frame);
        }

        private IEnumerator LoadStagedMonsterFrame(ZirconRuntimeVisualCatalog catalog,
            MonsterVisualSpec spec, int index)
        {
            Dictionary<int, MonsterVisualFrame> frames = stagedMonsterFrames[spec.ModelIndex];
            if (frames.ContainsKey(index) ||
                !catalog.TryGetFrame(spec.SetId, index, out ZirconRuntimeSpriteFrame metadata))
                yield break;
            yield return LoadSprite(catalog, spec.SetId, index, (sprite, offset) =>
                frames[index] = new MonsterVisualFrame(sprite, offset,
                    metadata.ShadowWidth, metadata.ShadowHeight,
                    metadata.ShadowOffsetX));
        }

        private IEnumerator LoadSprite(ZirconRuntimeVisualCatalog catalog, string setId, int index, Action<Sprite, Vector2> completed)
        {
            if (!catalog.TryGetFrame(setId, index, out ZirconRuntimeSpriteFrame frame))
                yield break;

            Sprite atlasSprite = null;
            string atlasError = null;
            yield return ZirconRuntimeAtlasStore.LoadSprite(frame, PixelsPerUnit,
                value => atlasSprite = value, value => atlasError = value);
            if (atlasSprite != null)
            {
                completed(atlasSprite, new Vector2(frame.OffsetX, frame.OffsetY));
                yield break;
            }

            byte[] bytes = null;
            string error = null;
            yield return ZirconAssetStore.LoadBytes(frame.Path, value => bytes = value, value => error = value);
            if (bytes == null || bytes.Length == 0)
            {
                Debug.LogWarning("P2 entity sprite missing " + frame.Path + ": " + error + " atlas=" + atlasError);
                yield break;
            }

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(bytes))
            {
                Destroy(texture);
                yield break;
            }
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.name = setId + "_" + index;
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0f, 1f), PixelsPerUnit);
            sprite.name = texture.name;
            completed(sprite, new Vector2(frame.OffsetX, frame.OffsetY));
        }

        private void UpdateMonsters(ZirconWorldSnapshot snapshot)
        {
            seenMonsterIds.Clear();
            foreach (ZirconEntityState entity in snapshot.Entities)
            {
                if (entity == null || entity.Kind != ZirconEntityKind.Monster)
                    continue;

                if (!string.IsNullOrWhiteSpace(entity.PetOwner) && loggedPetIds.Add(entity.ObjectId))
                    Debug.Log("P2 pet observed: object=" + entity.ObjectId + " model=" + entity.ModelIndex);

                bool sample = entity.ModelIndex >= 0 && entity.ModelIndex < MonsterModels;
                MonsterVisualSpec spec = sample ? null : FindStagedSpec(entity.ModelIndex);
                if (!sample && spec == null) continue;

                if (!worldRenderer.TryGetEntityRenderer(entity.ObjectId, out SpriteRenderer markerRenderer))
                    continue;

                MonsterVisualFrame stagedFrame = null;
                if (!sample && !TryGetStagedFrame(entity, spec, out stagedFrame)) continue;

                seenMonsterIds.Add(entity.ObjectId);
                if (!monsters.TryGetValue(entity.ObjectId, out MonsterPresentation presentation) ||
                    presentation.Root == null || presentation.MarkerRenderer != markerRenderer)
                {
                    if (presentation?.Root != null)
                        Destroy(presentation.Root);
                    presentation = CreateMonsterPresentation(markerRenderer);
                    monsters[entity.ObjectId] = presentation;
                }

                // Unknown monster models use a 0.12-scale collision marker.
                // The production body is parented to that marker for movement,
                // so cancel its diagnostic scale instead of shrinking the art.
                float markerScale = Mathf.Abs(markerRenderer.transform.localScale.x);
                presentation.Root.transform.localScale = markerScale > 0.001f
                    ? Vector3.one / markerScale
                    : Vector3.one;

                if (sample)
                {
                    int model = entity.ModelIndex;
                    float fps = entity.Action == ZirconMirAction.Moving || entity.Action == ZirconMirAction.Attack ? 7f : 4f;
                    int frame = Mathf.FloorToInt(Time.time * fps + entity.ObjectId % 17u) % MonsterFramesPerModel;
                    Sprite bodySprite = monsterSprites[model, frame];
                    presentation.Body.sprite = bodySprite;
                    float spriteHeight = bodySprite != null ? bodySprite.bounds.size.y : 0f;
                    presentation.Body.transform.localScale = spriteHeight > 0.001f
                        ? Vector3.one * (MonsterTargetHeight / spriteHeight)
                        : Vector3.one;
                    Vector2 offset = monsterOffsets[model, frame];
                    presentation.Body.transform.localPosition = new Vector3(offset.x / PixelsPerUnit, -offset.y / PixelsPerUnit, 0f);
                    presentation.Body.flipX = entity.Direction >= 5;
                }
                else
                {
                    presentation.Body.sprite = stagedFrame.Sprite;
                    presentation.Body.transform.localScale = Vector3.one;
                    presentation.Body.transform.localPosition = new Vector3(
                        stagedFrame.Offset.x / PixelsPerUnit,
                        -stagedFrame.Offset.y / PixelsPerUnit, 0f);
                    AlignGroundShadow(presentation, stagedFrame, spec);
                    // The original monster libraries include all eight directions.
                    presentation.Body.flipX = false;
                }
                presentation.Body.color = entity.Dead
                    ? new Color(0.55f, 0.35f, 0.35f, 0.72f)
                    : worldRenderer.IsSelectedObject(entity.ObjectId)
                        ? new Color(1f, 0.82f, 0.45f, 1f)
                        : Color.white;
                presentation.Shadow.enabled = !entity.Dead;
                markerRenderer.enabled = false;
                presentation.SortingGroup.sortingOrder = markerRenderer.sortingOrder;
            }

            removedMonsterIds.Clear();
            foreach (KeyValuePair<uint, MonsterPresentation> pair in monsters)
                if (!seenMonsterIds.Contains(pair.Key)) removedMonsterIds.Add(pair.Key);
            foreach (uint id in removedMonsterIds)
            {
                if (monsters[id].Root != null) Destroy(monsters[id].Root);
                monsters.Remove(id);
            }
        }

        private static MonsterVisualSpec FindStagedSpec(int modelIndex)
        {
            foreach (MonsterVisualSpec spec in StagedMonsterSpecs)
                if (spec.ModelIndex == modelIndex) return spec;
            return null;
        }

        private void AlignGroundShadow(MonsterPresentation presentation,
            MonsterVisualFrame frame, MonsterVisualSpec spec)
        {
            // This is a contact shadow, not the large directional shadow from
            // the source library (whose bitmap is not staged yet). The latter's
            // offset can put a small procedural oval beside/below the hooves.
            // Tie this oval to the actual sprite's bottom edge instead.
            float bodyCenterX = (frame.Offset.x +
                frame.Sprite.rect.width * 0.5f) / PixelsPerUnit;
            float footY = (-frame.Offset.y - frame.Sprite.rect.height) / PixelsPerUnit;
            float width = Mathf.Max(0.18f,
                frame.Sprite.rect.width / PixelsPerUnit * 1.1f);
            float height = frame.ShadowHeight > 0
                ? Mathf.Clamp(frame.ShadowHeight / PixelsPerUnit * 0.18f, 0.05f, 0.10f)
                : Mathf.Clamp(width * 0.26f, 0.05f, 0.10f);
            Vector2 sourceSize = shadowSprite.bounds.size;
            presentation.Shadow.transform.localPosition =
                new Vector3(bodyCenterX, footY + height * 0.15f, 0f);
            presentation.Shadow.transform.localScale = new Vector3(
                width / sourceSize.x, height / sourceSize.y, 1f);
            presentation.Shadow.color = new Color(0f, 0f, 0f,
                spec.ModelIndex == 189 ? 0.70f : 0.58f);
        }

        private bool TryGetStagedFrame(ZirconEntityState entity, MonsterVisualSpec spec,
            out MonsterVisualFrame frame)
        {
            frame = null;
            if (!stagedMonsterFrames.TryGetValue(spec.ModelIndex, out Dictionary<int, MonsterVisualFrame> frames))
                return false;

            int direction = Mathf.Clamp(entity.Direction, (byte)0, (byte)7);
            bool moving = worldRenderer.IsObjectMoving(entity.ObjectId);
            int start = 0;
            int count = 4;
            float fps = 4f;
            if (entity.Dead || entity.Action == ZirconMirAction.Die || entity.Action == ZirconMirAction.Dead)
            {
                if (spec.HasDeadFrames) { start = 329; count = 1; }
            }
            else if (moving)
            {
                start = 80; count = 6; fps = 8f;
            }
            else if (entity.Action == ZirconMirAction.Attack || entity.Action == ZirconMirAction.RangeAttack)
            {
                start = 160; count = 6; fps = 8f;
            }

            int animationFrame = Mathf.FloorToInt(Time.time * fps + entity.ObjectId % 17u) % count;
            int shapeBase = spec.Shape * 1000;
            int index = shapeBase + start + direction * 10 + animationFrame;
            if (frames.TryGetValue(index, out frame)) return true;
            if (frames.TryGetValue(shapeBase + direction * 10, out frame)) return true;
            return frames.TryGetValue(shapeBase, out frame);
        }

        private MonsterPresentation CreateMonsterPresentation(SpriteRenderer markerRenderer)
        {
            markerRenderer.enabled = false;

            var root = new GameObject("P2_ProductionMonster");
            root.transform.SetParent(markerRenderer.transform, false);
            SortingGroup sortingGroup = root.AddComponent<SortingGroup>();
            var shadowObject = new GameObject("Shadow");
            shadowObject.transform.SetParent(root.transform, false);
            var shadow = shadowObject.AddComponent<SpriteRenderer>();
            shadow.sprite = GetOrCreateShadowSprite();
            shadow.color = new Color(0f, 0f, 0f, 0.38f);
            shadow.sharedMaterial = ZirconRuntimeSpriteMaterial.Shared;
            shadow.sortingOrder = -1;
            shadow.transform.localPosition = new Vector3(0.12f, -0.02f, 0f);
            shadow.transform.localScale = new Vector3(0.85f, 0.38f, 1f);

            var bodyObject = new GameObject("Body");
            bodyObject.transform.SetParent(root.transform, false);
            var body = bodyObject.AddComponent<SpriteRenderer>();
            body.sharedMaterial = ZirconRuntimeSpriteMaterial.Shared;
            return new MonsterPresentation(root, body, shadow, sortingGroup, markerRenderer);
        }

        private void UpdateActionEffects(ZirconWorldSnapshot snapshot)
        {
            foreach (ZirconEntityState entity in snapshot.Entities)
            {
                if (entity == null || entity.ActionSequence <= 0)
                    continue;
                if (observedActions.TryGetValue(entity.ObjectId, out long sequence) && sequence == entity.ActionSequence)
                    continue;
                observedActions[entity.ObjectId] = entity.ActionSequence;

                if (entity.Action == ZirconMirAction.Spell)
                {
                    Transform anchor = entity.ActionTargetId != 0 ? FindMarker(entity.ActionTargetId) : null;
                    if (anchor == null) anchor = FindMarker(entity.ObjectId);
                    if (anchor != null) SpawnEffect(anchor.position, entity.ActionMagic);
                    PlayNearby(snapshot, entity, magicClip);
                }
                else if (entity.Action == ZirconMirAction.Attack || entity.Action == ZirconMirAction.RangeAttack)
                {
                    PlayNearby(snapshot, entity, attackClip);
                }
            }
        }

        private void SpawnEffect(Vector3 position, int magicType)
        {
            var effectObject = new GameObject("P2_SkillEffect_" + magicType);
            effectObject.transform.SetParent(worldRenderer.transform, false);
            effectObject.transform.position = position + new Vector3(0f, 0.35f, 0f);
            effectObject.transform.localScale = Vector3.one * 1.35f;
            var renderer = effectObject.AddComponent<SpriteRenderer>();
            renderer.sharedMaterial = ZirconRuntimeSpriteMaterial.Shared;
            // Effects intentionally render above the world; the UI uses an overlay canvas.
            renderer.sortingOrder = 32000;
            transientEffects.Add(new TransientEffect(effectObject, renderer, Time.time));
        }

        private void UpdateTransientEffects()
        {
            for (int i = transientEffects.Count - 1; i >= 0; i--)
            {
                TransientEffect effect = transientEffects[i];
                float age = Time.time - effect.Started;
                int frame = Mathf.FloorToInt(age * 12f);
                if (effect.Root == null || frame >= EffectFrames)
                {
                    if (effect.Root != null) Destroy(effect.Root);
                    transientEffects.RemoveAt(i);
                    continue;
                }
                effect.Renderer.sprite = effectSprites[frame];
                effect.Renderer.color = new Color(1f, 1f, 1f, Mathf.Clamp01(1.25f - age));
            }
        }

        private void EnsureAudio()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            audioSource.ignoreListenerPause = true;
            audioSource.volume = 0.82f;
            if (EnableProceduralAudioFallback)
            {
                RequestAndroidAudioFocus();
                attackClip = CreateTone("P2_AttackFallback", 620f, 0.16f, false);
                magicClip = CreateTone("P2_MagicFallback", 440f, 0.28f, true);
            }
            else
            {
                attackClip = null;
                magicClip = null;
                Debug.Log("P2 audio events ready; playback muted until formal Sound resources are staged.");
            }
        }

        private void PlayNearby(ZirconWorldSnapshot snapshot, ZirconEntityState entity, AudioClip clip)
        {
            if (audioSource == null || clip == null || !snapshot.HasLocalPlayer)
                return;
            int dx = entity.Location.X - snapshot.Location.X;
            int dy = entity.Location.Y - snapshot.Location.Y;
            if (dx * dx + dy * dy <= 144)
            {
                audioSource.PlayOneShot(clip);
                Debug.Log("P2 audio played: clip=" + clip.name + " entity=" + entity.ObjectId +
                          " action=" + entity.Action + " sequence=" + entity.ActionSequence);
            }
        }

        private static AudioClip CreateTone(string name, float frequency, float duration, bool sweep)
        {
            const int sampleRate = 22050;
            int count = Mathf.CeilToInt(sampleRate * duration);
            var samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)sampleRate;
                float envelope = Mathf.Sin(Mathf.PI * i / Mathf.Max(1, count - 1));
                float current = sweep ? frequency * (1f + t * 2f) : frequency;
                float fundamental = Mathf.Sin(2f * Mathf.PI * current * t);
                float harmonic = Mathf.Sin(4f * Mathf.PI * current * t) * 0.35f;
                samples[i] = (fundamental + harmonic) * envelope * 0.62f;
            }
            AudioClip clip = AudioClip.Create(name, count, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static void RequestAndroidAudioFocus()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                using (AndroidJavaObject manager = activity.Call<AndroidJavaObject>("getSystemService", "audio"))
                {
                    int result = manager.Call<int>("requestAudioFocus", new object[] { null, 3, 1 });
                    Debug.Log("P2 Android audio focus result=" + result);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("P2 Android audio focus request failed: " + ex.Message);
            }
#endif
        }

        private Transform FindMarker(uint objectId)
        {
            return worldRenderer != null &&
                   worldRenderer.TryGetEntityRenderer(objectId, out SpriteRenderer renderer)
                ? renderer.transform
                : null;
        }

        private Sprite GetOrCreateShadowSprite()
        {
            if (shadowSprite != null) return shadowSprite;
            const int width = 64;
            const int height = 32;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float nx = (x + 0.5f - width * 0.5f) / (width * 0.5f);
                float ny = (y + 0.5f - height * 0.5f) / (height * 0.5f);
                float alpha = Mathf.Clamp01(1f - nx * nx - ny * ny);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * alpha));
            }
            texture.Apply(false, true);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            shadowSprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), PixelsPerUnit);
            return shadowSprite;
        }

        private sealed class MonsterVisualSpec
        {
            public MonsterVisualSpec(int modelIndex, string name, string setId, int shape,
                float height, bool hasDeadFrames)
            {
                ModelIndex = modelIndex;
                Name = name;
                SetId = setId;
                Shape = shape;
                Height = height;
                HasDeadFrames = hasDeadFrames;
            }
            public int ModelIndex { get; }
            public string Name { get; }
            public string SetId { get; }
            public int Shape { get; }
            public float Height { get; }
            public bool HasDeadFrames { get; }
        }

        private sealed class MonsterVisualFrame
        {
            public MonsterVisualFrame(Sprite sprite, Vector2 offset,
                int shadowWidth, int shadowHeight, int shadowOffsetX)
            {
                Sprite = sprite;
                Offset = offset;
                ShadowWidth = shadowWidth;
                ShadowHeight = shadowHeight;
                ShadowOffsetX = shadowOffsetX;
            }
            public Sprite Sprite { get; }
            public Vector2 Offset { get; }
            public int ShadowWidth { get; }
            public int ShadowHeight { get; }
            public int ShadowOffsetX { get; }
        }

        private sealed class MonsterPresentation
        {
            public MonsterPresentation(
                GameObject root,
                SpriteRenderer body,
                SpriteRenderer shadow,
                SortingGroup sortingGroup,
                SpriteRenderer markerRenderer)
            {
                Root = root;
                Body = body;
                Shadow = shadow;
                SortingGroup = sortingGroup;
                MarkerRenderer = markerRenderer;
            }
            public GameObject Root { get; }
            public SpriteRenderer Body { get; }
            public SpriteRenderer Shadow { get; }
            public SortingGroup SortingGroup { get; }
            public SpriteRenderer MarkerRenderer { get; }
        }

        private sealed class TransientEffect
        {
            public TransientEffect(GameObject root, SpriteRenderer renderer, float started)
            { Root = root; Renderer = renderer; Started = started; }
            public GameObject Root { get; }
            public SpriteRenderer Renderer { get; }
            public float Started { get; }
        }
    }
}
