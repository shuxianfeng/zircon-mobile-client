using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zircon.Mobile.Core.Assets;
using Zircon.Mobile.Core.Protocol;
using Zircon.Mobile.Game.Entities;
using Zircon.Mobile.UI.Login;

namespace Zircon.Mobile.Game.World
{
    /// <summary>Plays production-sample monster animation, spell effects, shadows and combat audio.</summary>
    public sealed class ZirconProductionEntityPresentationBehaviour : MonoBehaviour
    {
        private static ZirconProductionEntityPresentationBehaviour instance;
        private const int MonsterModels = 2;
        private const int MonsterFramesPerModel = 4;
        private const int EffectFrames = 16;
        private const float PixelsPerUnit = 100f;
        private const bool EnableProceduralAudioFallback = false;

        private readonly Sprite[,] monsterSprites = new Sprite[MonsterModels, MonsterFramesPerModel];
        private readonly Vector2[,] monsterOffsets = new Vector2[MonsterModels, MonsterFramesPerModel];
        private readonly Sprite[] effectSprites = new Sprite[EffectFrames];
        private readonly Dictionary<uint, MonsterPresentation> monsters = new Dictionary<uint, MonsterPresentation>();
        private readonly Dictionary<uint, long> observedActions = new Dictionary<uint, long>();
        private readonly List<TransientEffect> transientEffects = new List<TransientEffect>();

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

        private void Update()
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
            var seen = new HashSet<uint>();
            foreach (ZirconEntityState entity in snapshot.Entities)
            {
                if (entity == null || entity.Kind != ZirconEntityKind.Monster)
                    continue;

                if (entity.ModelIndex < 0 || entity.ModelIndex >= MonsterModels)
                    continue;

                Transform marker = FindMarker(entity.ObjectId);
                if (marker == null)
                    continue;

                seen.Add(entity.ObjectId);
                if (!monsters.TryGetValue(entity.ObjectId, out MonsterPresentation presentation) || presentation.Root == null)
                {
                    presentation = CreateMonsterPresentation(marker);
                    monsters[entity.ObjectId] = presentation;
                }

                int model = entity.ModelIndex;
                float fps = entity.Action == ZirconMirAction.Moving || entity.Action == ZirconMirAction.Attack ? 7f : 4f;
                int frame = Mathf.FloorToInt(Time.time * fps + entity.ObjectId % 17u) % MonsterFramesPerModel;
                presentation.Body.sprite = monsterSprites[model, frame];
                Vector2 offset = monsterOffsets[model, frame];
                presentation.Body.transform.localPosition = new Vector3(offset.x / PixelsPerUnit, -offset.y / PixelsPerUnit, 0f);
                presentation.Body.flipX = entity.Direction >= 5;
                presentation.Body.color = entity.Dead ? new Color(0.55f, 0.35f, 0.35f, 0.72f) : Color.white;
                presentation.Shadow.enabled = !entity.Dead;
            }

            var remove = new List<uint>();
            foreach (KeyValuePair<uint, MonsterPresentation> pair in monsters)
                if (!seen.Contains(pair.Key)) remove.Add(pair.Key);
            foreach (uint id in remove)
            {
                if (monsters[id].Root != null) Destroy(monsters[id].Root);
                monsters.Remove(id);
            }
        }

        private MonsterPresentation CreateMonsterPresentation(Transform marker)
        {
            SpriteRenderer original = marker.GetComponent<SpriteRenderer>();
            if (original != null) original.enabled = false;

            var root = new GameObject("P2_ProductionMonster");
            root.transform.SetParent(marker, false);
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
            return new MonsterPresentation(root, body, shadow);
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
            renderer.sortingOrder = 1000;
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
            string suffix = "_" + objectId;
            for (int i = 0; i < worldRenderer.transform.childCount; i++)
            {
                Transform child = worldRenderer.transform.GetChild(i);
                if (child.name.EndsWith(suffix, StringComparison.Ordinal)) return child;
            }
            return null;
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

        private sealed class MonsterPresentation
        {
            public MonsterPresentation(GameObject root, SpriteRenderer body, SpriteRenderer shadow)
            { Root = root; Body = body; Shadow = shadow; }
            public GameObject Root { get; }
            public SpriteRenderer Body { get; }
            public SpriteRenderer Shadow { get; }
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
