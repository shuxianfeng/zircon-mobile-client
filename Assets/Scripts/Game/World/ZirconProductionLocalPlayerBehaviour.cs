using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Zircon.Mobile.Core.Assets;
using Zircon.Mobile.Core.Protocol;
using Zircon.Mobile.UI.Login;

namespace Zircon.Mobile.Game.World
{
    /// <summary>
    /// Composes the local player from the PC client's body, dye overlay, hair and weapon libraries.
    /// The scene asset keeps the original script GUID while this production type uses a matching file name.
    /// </summary>
    public sealed class ZirconProductionLocalPlayerBehaviour : MonoBehaviour
    {
        [SerializeField] private ZirconProtocolProbeBehaviour session;
        [SerializeField] private ZirconWorldDebugRenderer worldRenderer;
        [SerializeField] private float pixelsPerUnit = 100f;

        private const int DirectionCount = 8;
        private readonly Dictionary<int, LayerFrame> bodies = new Dictionary<int, LayerFrame>();
        private readonly Dictionary<int, LayerFrame> overlays = new Dictionary<int, LayerFrame>();
        private readonly Dictionary<int, LayerFrame> hairs = new Dictionary<int, LayerFrame>();
        private readonly Dictionary<int, LayerFrame> weapons = new Dictionary<int, LayerFrame>();

        private Transform marker;
        private GameObject composition;
        private SpriteRenderer body;
        private SpriteRenderer overlay;
        private SpriteRenderer hair;
        private SpriteRenderer weapon;
        private SpriteRenderer shadow;
        private SortingGroup sortingGroup;
        private Sprite shadowSprite;
        private bool loading;
        private bool ready;
        private bool incompatible;
        private long observedActionSequence = long.MinValue;
        private ZirconMirAction observedAction = (ZirconMirAction)byte.MaxValue;
        private float actionStartedAt;
        private string loadedAppearanceSignature;

        private sealed class LayerFrame
        {
            public Sprite Sprite;
            public Vector2 Offset;
        }

        private readonly struct AnimationSpec
        {
            public AnimationSpec(int start, int count, float fps, bool loop)
            {
                Start = start;
                Count = count;
                Fps = fps;
                Loop = loop;
            }

            public int Start { get; }
            public int Count { get; }
            public float Fps { get; }
            public bool Loop { get; }
        }

        private void Update()
        {
            ZirconWorldSnapshot snapshot = session?.GetWorldSnapshot();
            if (snapshot == null || !snapshot.HasLocalPlayer || snapshot.LocalPlayer == null)
                return;

            if (ZirconPlayerAppearanceCaptureBehaviour.Current.HasValue)
            {
                string signature = AppearanceSignature(ZirconPlayerAppearanceCaptureBehaviour.Current.Value);
                if (ready && !string.Equals(signature, loadedAppearanceSignature, StringComparison.Ordinal))
                    ResetLoadedAppearance();
                if (!loading && !ready && !incompatible)
                    StartCoroutine(LoadSprites());
            }

            if (!ready)
                return;

            uint id = snapshot.LocalPlayer.ObjectId;
            if (marker == null || !marker.name.EndsWith("_" + id, StringComparison.Ordinal))
                marker = FindMarker(id);
            if (marker == null)
                return;

            EnsureComposition();
            if (snapshot.LocalPlayer.ActionSequence != observedActionSequence ||
                snapshot.LocalPlayer.Action != observedAction)
            {
                observedActionSequence = snapshot.LocalPlayer.ActionSequence;
                observedAction = snapshot.LocalPlayer.Action;
                actionStartedAt = Time.time;
            }

            int direction = Mathf.Clamp(snapshot.LocalPlayer.Direction, (byte)0, (byte)(DirectionCount - 1));
            AnimationSpec animation = GetAnimation(snapshot.LocalPlayer.Action);
            int frame = Mathf.FloorToInt(Mathf.Max(0f, Time.time - actionStartedAt) * animation.Fps);
            frame = animation.Loop ? frame % animation.Count : Mathf.Min(frame, animation.Count - 1);
            int drawFrame = animation.Start + direction * 10 + frame;
            if (!bodies.ContainsKey(drawFrame))
            {
                AnimationSpec standing = GetAnimation(ZirconMirAction.Standing);
                drawFrame = standing.Start + direction * 10 +
                            (Mathf.FloorToInt(Time.time * standing.Fps) % standing.Count);
            }
            Apply(drawFrame, direction);

            SpriteRenderer markerRenderer = marker.GetComponent<SpriteRenderer>();
            if (sortingGroup != null && markerRenderer != null)
                sortingGroup.sortingOrder = markerRenderer.sortingOrder;
        }

        private IEnumerator LoadSprites()
        {
            loading = true;
            ZirconPlayerAppearanceCaptureBehaviour.Appearance appearance =
                ZirconPlayerAppearanceCaptureBehaviour.Current.Value;
            ZirconRuntimeVisualCatalog catalog = null;
            string catalogError = null;
            yield return ZirconRuntimeVisualCatalog.Load(value => catalog = value, value => catalogError = value);
            if (catalog == null)
            {
                Debug.LogWarning("P2 production visual catalog unavailable: " + catalogError);
                loading = false;
                yield break;
            }

            string bodySet;
            string overlaySet;
            string hairSet;
            string weaponSet;
            int bodyBase;
            int hairBase = Math.Max(0, appearance.HairType - 1) * 5000;
            int weaponShape = appearance.Weapon >= 1000 ? appearance.Weapon - 1000 : appearance.Weapon;
            int weaponBase = Math.Max(0, weaponShape % 10) * 5000;
            int[] drawFrames;
            bool productionMale = appearance.Gender == 0 && appearance.CharacterClass <= 2;

            if (productionMale)
            {
                if (appearance.Armour / 11 != 0)
                {
                    Debug.LogWarning("P2-B male armour library not staged: armour=" + appearance.Armour);
                    incompatible = true;
                    loading = false;
                    yield break;
                }
                bodySet = "player.standard.male.body";
                overlaySet = "player.standard.male.overlay";
                hairSet = "player.standard.male.hair";
                weaponSet = MaleWeaponSet(weaponShape / 10);
                bodyBase = (appearance.Armour % 11) * 5000;
                drawFrames = ProductionDrawFrames();
            }
            else if (appearance.Gender == 1 && appearance.CharacterClass <= 2)
            {
                bodySet = "player.warrior.female.body";
                overlaySet = "player.warrior.female.overlay";
                hairSet = "player.warrior.female.hair";
                weaponSet = "player.warrior.female.weapon";
                int requestedBodyBase = (appearance.Armour % 11) * 5000;
                bodyBase = catalog.TryGetFrame(bodySet, requestedBodyBase, out _) ? requestedBodyBase : 45000;
                drawFrames = StandingDrawFrames();
            }
            else
            {
                Debug.LogWarning("P2-B player renderer has no staged library for class=" +
                                 appearance.CharacterClass + " gender=" + appearance.Gender);
                incompatible = true;
                loading = false;
                yield break;
            }

            bool showHair = appearance.HairType > 0;
            bool weaponLibraryMatches = productionMale
                ? !string.IsNullOrEmpty(weaponSet)
                : appearance.Weapon / 10 == 0;
            bool weaponAvailable = weaponLibraryMatches &&
                                   catalog.TryGetFrame(weaponSet, weaponBase + drawFrames[0], out _);
            int loaded = 0;
            foreach (int drawFrame in drawFrames)
            {
                int bodyIndex = bodyBase + drawFrame;
                int hairIndex = hairBase + drawFrame;
                int weaponIndex = weaponBase + drawFrame;
                yield return Load(catalog, bodySet, bodyIndex, bodies, drawFrame, () => loaded++);
                yield return Load(catalog, overlaySet, bodyIndex, overlays, drawFrame, () => loaded++);
                if (showHair)
                    yield return Load(catalog, hairSet, hairIndex, hairs, drawFrame, () => loaded++);
                if (weaponAvailable)
                    yield return Load(catalog, weaponSet, weaponIndex, weapons, drawFrame, () => loaded++);
            }

            ready = bodies.ContainsKey(0) && (!showHair || hairs.ContainsKey(0));
            incompatible = !ready;
            loadedAppearanceSignature = AppearanceSignature(appearance);
            loading = false;
            Debug.Log("P2-B local player appearance ready: loadedLayers=" + loaded +
                      " drawFrames=" + drawFrames.Length +
                      " catalogFrames=" + catalog.FrameCount +
                      " class=" + appearance.CharacterClass +
                      " gender=" + appearance.Gender +
                      " armour=" + appearance.Armour +
                      " weapon=" + appearance.Weapon +
                      " weaponAvailable=" + weaponAvailable +
                      " ready=" + ready);
        }

        private static string MaleWeaponSet(int libraryGroup)
        {
            switch (libraryGroup)
            {
                case 1:
                    return "player.standard.male.weapon2";
                case 10:
                    return "player.standard.male.weapon11";
                case 14:
                    return "player.standard.male.weapon15";
                default:
                    return null;
            }
        }

        private IEnumerator Load(
            ZirconRuntimeVisualCatalog catalog,
            string setId,
            int sourceIndex,
            Dictionary<int, LayerFrame> destination,
            int drawFrame,
            Action completed)
        {
            if (!catalog.TryGetFrame(setId, sourceIndex, out ZirconRuntimeSpriteFrame frame))
            {
                // Some legacy overlay frames are intentionally empty (most
                // notably parts of the death sequence). The base body frame
                // remains valid, so do not flood Android logs with false
                // missing-resource warnings for those optional layers.
                if (!setId.EndsWith(".overlay", StringComparison.Ordinal))
                    Debug.LogWarning("P2-B frame missing from catalog: " + setId + " index=" + sourceIndex);
                yield break;
            }

            Sprite atlasSprite = null;
            string atlasError = null;
            yield return ZirconRuntimeAtlasStore.LoadSprite(frame, pixelsPerUnit,
                value => atlasSprite = value, value => atlasError = value);
            if (atlasSprite == null)
            {
                byte[] bytes = null;
                string error = null;
                yield return ZirconAssetStore.LoadBytes(frame.Path, value => bytes = value, value => error = value);
                if (bytes == null || bytes.Length == 0)
                {
                    Debug.LogWarning("P2-B appearance layer missing " + frame.Path + ": " +
                                     error + " atlas=" + atlasError);
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
                texture.name = setId + "_" + sourceIndex;
                atlasSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0f, 1f), pixelsPerUnit);
                atlasSprite.name = texture.name;
            }

            destination[drawFrame] = new LayerFrame
            {
                Sprite = atlasSprite,
                Offset = new Vector2(frame.OffsetX, frame.OffsetY),
            };
            completed?.Invoke();
        }

        private void EnsureComposition()
        {
            if (composition != null && composition.transform.parent == marker)
                return;
            if (composition != null)
                Destroy(composition);

            composition = new GameObject("本地角色正式外观");
            composition.transform.SetParent(marker, false);
            sortingGroup = composition.AddComponent<SortingGroup>();
            SpriteRenderer old = marker.GetComponent<SpriteRenderer>();
            sortingGroup.sortingOrder = old != null ? old.sortingOrder : 0;
            shadow = Add("动态阴影", -2);
            shadow.sprite = GetOrCreateShadowSprite();
            shadow.color = new Color(0f, 0f, 0f, 0.42f);
            shadow.transform.localPosition = new Vector3(0.18f, -0.03f, 0f);
            shadow.transform.localScale = new Vector3(0.72f, 0.34f, 1f);
            body = Add("衣甲", 0);
            overlay = Add("衣甲染色", 1);
            hair = Add("头发", 2);
            weapon = Add("武器", 3);
            if (old != null)
                old.enabled = false;
        }

        private SpriteRenderer Add(string name, int order)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(composition.transform, false);
            SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
            renderer.sharedMaterial = ZirconRuntimeSpriteMaterial.Shared;
            renderer.sortingOrder = order;
            return renderer;
        }

        private void Apply(int drawFrame, int direction)
        {
            ApplyLayer(body, bodies, drawFrame);
            ApplyLayer(overlay, overlays, drawFrame);
            ApplyLayer(hair, hairs, drawFrame);
            ApplyLayer(weapon, weapons, drawFrame);

            ZirconPlayerAppearanceCaptureBehaviour.Appearance value =
                ZirconPlayerAppearanceCaptureBehaviour.Current.Value;
            overlay.color = FromArgb(value.ArmourColour);
            hair.color = FromArgb(value.HairColour);
            weapon.sortingOrder = direction == 0 || direction >= 5 ? -1 : 3;
        }

        private void ApplyLayer(SpriteRenderer renderer, Dictionary<int, LayerFrame> frames, int drawFrame)
        {
            if (renderer == null)
                return;
            if (!frames.TryGetValue(drawFrame, out LayerFrame frame))
            {
                renderer.sprite = null;
                return;
            }
            renderer.sprite = frame.Sprite;
            renderer.transform.localPosition =
                new Vector3(frame.Offset.x / pixelsPerUnit, -frame.Offset.y / pixelsPerUnit, 0f);
        }

        private static AnimationSpec GetAnimation(ZirconMirAction action)
        {
            switch (action)
            {
                case ZirconMirAction.Moving:
                    return new AnimationSpec(80, 6, 10f, true);
                case ZirconMirAction.Attack:
                case ZirconMirAction.Mining:
                    return new AnimationSpec(720, 6, 10f, false);
                case ZirconMirAction.RangeAttack:
                case ZirconMirAction.Spell:
                    return new AnimationSpec(560, 5, 10f, false);
                case ZirconMirAction.Pushed:
                    return new AnimationSpec(1840, 3, 10f, false);
                case ZirconMirAction.Harvest:
                    return new AnimationSpec(480, 2, 3.33f, false);
                case ZirconMirAction.Die:
                    return new AnimationSpec(1920, 10, 10f, false);
                case ZirconMirAction.Dead:
                    return new AnimationSpec(1929, 1, 1f, false);
                default:
                    return new AnimationSpec(0, 4, 2f, true);
            }
        }

        private static int[] ProductionDrawFrames()
        {
            var values = new List<int>();
            AddAnimationFrames(values, 0, 4);
            AddAnimationFrames(values, 80, 6);
            AddAnimationFrames(values, 480, 2);
            AddAnimationFrames(values, 560, 5);
            AddAnimationFrames(values, 640, 5);
            AddAnimationFrames(values, 720, 6);
            AddAnimationFrames(values, 1840, 3);
            AddAnimationFrames(values, 1920, 10);
            return values.ToArray();
        }

        private static int[] StandingDrawFrames()
        {
            var values = new List<int>();
            AddAnimationFrames(values, 0, 4);
            return values.ToArray();
        }

        private static void AddAnimationFrames(List<int> values, int start, int count)
        {
            for (int direction = 0; direction < DirectionCount; direction++)
            for (int frame = 0; frame < count; frame++)
                values.Add(start + direction * 10 + frame);
        }

        private Transform FindMarker(uint id)
        {
            if (worldRenderer == null)
                return null;
            string suffix = "_" + id;
            for (int i = 0; i < worldRenderer.transform.childCount; i++)
            {
                Transform child = worldRenderer.transform.GetChild(i);
                if (child.name.EndsWith(suffix, StringComparison.Ordinal))
                    return child;
            }
            return null;
        }

        private Sprite GetOrCreateShadowSprite()
        {
            if (shadowSprite != null)
                return shadowSprite;
            const int width = 64;
            const int height = 32;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.name = "P2_SoftEntityShadow";
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float nx = (x + 0.5f - width * 0.5f) / (width * 0.5f);
                float ny = (y + 0.5f - height * 0.5f) / (height * 0.5f);
                float alpha = Mathf.Clamp01(1f - nx * nx - ny * ny);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * alpha * 0.9f));
            }
            texture.Apply(false, true);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            shadowSprite = Sprite.Create(texture, new Rect(0, 0, width, height),
                new Vector2(0.5f, 0.5f), pixelsPerUnit);
            return shadowSprite;
        }

        private void ResetLoadedAppearance()
        {
            if (composition != null)
                Destroy(composition);
            composition = null;
            marker = null;
            ClearFrames(bodies);
            ClearFrames(overlays);
            ClearFrames(hairs);
            ClearFrames(weapons);
            ready = false;
            incompatible = false;
            loadedAppearanceSignature = null;
        }

        private static void ClearFrames(Dictionary<int, LayerFrame> frames)
        {
            foreach (LayerFrame frame in frames.Values)
                if (frame?.Sprite != null)
                    Destroy(frame.Sprite);
            frames.Clear();
        }

        private void OnDestroy()
        {
            ResetLoadedAppearance();
            if (shadowSprite != null)
                Destroy(shadowSprite);
        }

        private static string AppearanceSignature(ZirconPlayerAppearanceCaptureBehaviour.Appearance value)
        {
            return value.CharacterClass + ":" + value.Gender + ":" + value.HairType + ":" +
                   value.Weapon + ":" + value.Armour + ":" + value.Shield + ":" + value.Helmet;
        }

        private static Color32 FromArgb(int value)
        {
            uint argb = unchecked((uint)value);
            byte alpha = (byte)(argb >> 24);
            if (alpha == 0)
                alpha = 255;
            return new Color32((byte)(argb >> 16), (byte)(argb >> 8), (byte)argb, alpha);
        }
    }
}
