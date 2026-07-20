using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using Zircon.Mobile.Core.Assets;
using Zircon.Mobile.Core.Protocol;
using Zircon.Mobile.UI.Login;

namespace Zircon.Mobile.Game.World
{
    /// <summary>Composes the currently selected female warrior from her real body, hair and weapon resources.</summary>
    public sealed class ZirconFemaleWarriorPlayerBehaviour : MonoBehaviour
    {
        [SerializeField] private ZirconProtocolProbeBehaviour session;
        [SerializeField] private ZirconWorldDebugRenderer worldRenderer;
        [SerializeField] private float pixelsPerUnit = 100f;
        [SerializeField] private float standingFps = 4f;
        [SerializeField] private float movingFps = 7f;
        private const int DirectionCount = 8;
        private const int FramesPerDirection = 4;
        private readonly Sprite[,] bodies = new Sprite[DirectionCount, FramesPerDirection];
        private readonly Sprite[,] overlays = new Sprite[DirectionCount, FramesPerDirection];
        private readonly Sprite[,] hairs = new Sprite[DirectionCount, FramesPerDirection];
        private readonly Sprite[,] weapons = new Sprite[DirectionCount, FramesPerDirection];
        private readonly Vector2[,] bodyOffsets = new Vector2[DirectionCount, FramesPerDirection];
        private readonly Vector2[,] overlayOffsets = new Vector2[DirectionCount, FramesPerDirection];
        private readonly Vector2[,] hairOffsets = new Vector2[DirectionCount, FramesPerDirection];
        private readonly Vector2[,] weaponOffsets = new Vector2[DirectionCount, FramesPerDirection];
        private Transform marker;
        private GameObject composition;
        private SpriteRenderer body, overlay, hair, weapon;
        private SpriteRenderer shadow;
        private SortingGroup sortingGroup;
        private Sprite shadowSprite;
        private bool loading, ready, incompatible;
        private long observedActionSequence;
        private float actionFeedbackStarted = float.NegativeInfinity;

        private void Update()
        {
            ZirconWorldSnapshot snapshot = session?.GetWorldSnapshot();
            if (snapshot == null || !snapshot.HasLocalPlayer || snapshot.LocalPlayer == null) return;
            if (!loading && !ready && !incompatible && ZirconPlayerAppearanceCaptureBehaviour.Current.HasValue)
                StartCoroutine(LoadSprites());
            if (!ready) return;
            uint id = snapshot.LocalPlayer.ObjectId;
            if (marker == null || !marker.name.EndsWith("_" + id, StringComparison.Ordinal)) marker = FindMarker(id);
            if (marker == null) return;
            EnsureComposition();
            int direction = Mathf.Clamp(snapshot.LocalPlayer.Direction, (byte)0, (byte)7);
            float fps = snapshot.LocalPlayer.Action == ZirconMirAction.Moving ? movingFps : standingFps;
            int frame = Mathf.FloorToInt(Time.time * Mathf.Max(1f, fps)) % FramesPerDirection;
            Apply(direction, frame);
            if (snapshot.LocalPlayer.ActionSequence != observedActionSequence)
            {
                observedActionSequence = snapshot.LocalPlayer.ActionSequence;
                if (snapshot.LocalPlayer.Action == ZirconMirAction.Attack || snapshot.LocalPlayer.Action == ZirconMirAction.RangeAttack ||
                    snapshot.LocalPlayer.Action == ZirconMirAction.Spell)
                    actionFeedbackStarted = Time.time;
            }
            ApplyActionFeedback(direction, snapshot.LocalPlayer.Action);
            SpriteRenderer markerRenderer = marker.GetComponent<SpriteRenderer>();
            if (sortingGroup != null && markerRenderer != null)
                sortingGroup.sortingOrder = markerRenderer.sortingOrder;
        }

        private IEnumerator LoadSprites()
        {
            loading = true;
            ZirconPlayerAppearanceCaptureBehaviour.Appearance appearance = ZirconPlayerAppearanceCaptureBehaviour.Current.Value;
            if (appearance.Gender != 1)
            {
                Debug.LogWarning("Female renderer skipped incompatible gender=" + appearance.Gender);
                incompatible = true;
                loading = false;
                yield break;
            }
            ZirconRuntimeVisualCatalog catalog = null;
            string catalogError = null;
            yield return ZirconRuntimeVisualCatalog.Load(value => catalog = value, value => catalogError = value);
            if (catalog == null)
            {
                Debug.LogWarning("P2 production visual catalog unavailable: " + catalogError);
                loading = false;
                yield break;
            }

            int loaded = 0;
            int requestedBodyBase = appearance.Armour * 5000;
            int bodyBase = catalog.TryGetFrame("player.warrior.female.body", requestedBodyBase, out _)
                ? requestedBodyBase : 45000;
            bool weaponAvailable = catalog.TryGetFrame("player.warrior.female.weapon", appearance.Weapon * 5000, out _);
            for (int direction = 0; direction < DirectionCount; direction++)
            {
                for (int frame = 0; frame < FramesPerDirection; frame++)
                {
                    int bodyIndex = bodyBase + direction * 10 + frame;
                    int weaponIndex = appearance.Weapon * 5000 + direction * 10 + frame;
                    int hairIndex = direction * 10 + frame;
                    yield return Load(catalog, "player.warrior.female.body", bodyIndex,
                        (sprite, offset) => { bodies[direction, frame] = sprite; bodyOffsets[direction, frame] = offset; loaded++; });
                    yield return Load(catalog, "player.warrior.female.overlay", bodyIndex,
                        (sprite, offset) => { overlays[direction, frame] = sprite; overlayOffsets[direction, frame] = offset; loaded++; });
                    yield return Load(catalog, "player.warrior.female.hair", hairIndex,
                        (sprite, offset) => { hairs[direction, frame] = sprite; hairOffsets[direction, frame] = offset; loaded++; });
                    if (weaponAvailable)
                        yield return Load(catalog, "player.warrior.female.weapon", weaponIndex,
                            (sprite, offset) => { weapons[direction, frame] = sprite; weaponOffsets[direction, frame] = offset; loaded++; });
                }
            }
            ready = loaded >= 96 && bodies[0, 0] != null && hairs[0, 0] != null;
            loading = false;
            Debug.Log("P2 animated character appearance ready: layers=" + loaded + "/128 catalogFrames=" + catalog.FrameCount +
                      " class=" + appearance.CharacterClass + " armour=" + appearance.Armour + " weapon=" + appearance.Weapon +
                      " bodySampleFallback=" + (bodyBase != requestedBodyBase) + " weaponAvailable=" + weaponAvailable + " ready=" + ready);
        }

        private IEnumerator Load(ZirconRuntimeVisualCatalog catalog, string setId, int index, Action<Sprite, Vector2> complete)
        {
            if (!catalog.TryGetFrame(setId, index, out ZirconRuntimeSpriteFrame frame))
            {
                Debug.LogWarning("P2 frame missing from catalog: " + setId + " index=" + index);
                yield break;
            }
            Sprite atlasSprite = null;
            string atlasError = null;
            yield return ZirconRuntimeAtlasStore.LoadSprite(frame, pixelsPerUnit,
                value => atlasSprite = value, value => atlasError = value);
            if (atlasSprite != null)
            {
                complete(atlasSprite, new Vector2(frame.OffsetX, frame.OffsetY));
                yield break;
            }

            byte[] bytes = null;
            string error = null;
            yield return ZirconAssetStore.LoadBytes(frame.Path, value => bytes = value, value => error = value);
            if (bytes == null || bytes.Length == 0) { Debug.LogWarning("Appearance layer missing " + frame.Path + ": " + error + " atlas=" + atlasError); yield break; }
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(bytes)) { Destroy(texture); yield break; }
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.name = setId + "_" + index;
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0f, 1f), pixelsPerUnit);
            sprite.name = texture.name;
            complete(sprite, new Vector2(frame.OffsetX, frame.OffsetY));
        }

        private void EnsureComposition()
        {
            if (composition != null && composition.transform.parent == marker) return;
            if (composition != null) Destroy(composition);
            composition = new GameObject("新角色真实外观");
            composition.transform.SetParent(marker, false);
            sortingGroup = composition.AddComponent<SortingGroup>();
            SpriteRenderer old = marker.GetComponent<SpriteRenderer>();
            sortingGroup.sortingOrder = old != null ? old.sortingOrder : 0;
            shadow = Add("动态阴影", -2);
            shadow.sprite = GetOrCreateShadowSprite();
            shadow.color = new Color(0f, 0f, 0f, 0.42f);
            shadow.transform.localPosition = new Vector3(0.18f, -0.03f, 0f);
            shadow.transform.localScale = new Vector3(0.72f, 0.34f, 1f);
            body = Add("衣甲", 0); overlay = Add("衣甲染色", 1); hair = Add("头发", 2); weapon = Add("武器", 3);
            if (old != null) old.enabled = false;
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

        private void Apply(int direction, int frame)
        {
            body.sprite = bodies[direction, frame]; overlay.sprite = overlays[direction, frame]; hair.sprite = hairs[direction, frame]; weapon.sprite = weapons[direction, frame];
            Position(body, bodyOffsets[direction, frame]); Position(overlay, overlayOffsets[direction, frame]); Position(hair, hairOffsets[direction, frame]); Position(weapon, weaponOffsets[direction, frame]);
            ZirconPlayerAppearanceCaptureBehaviour.Appearance value = ZirconPlayerAppearanceCaptureBehaviour.Current.Value;
            overlay.color = FromArgb(value.ArmourColour); hair.color = FromArgb(value.HairColour);
            weapon.sortingOrder = direction == 0 || direction >= 5 ? -1 : 3;
        }

        private void ApplyActionFeedback(int direction, ZirconMirAction action)
        {
            if (composition == null) return;
            float age = Time.time - actionFeedbackStarted;
            if (age < 0f || age > 0.24f)
            {
                composition.transform.localPosition = Vector3.zero;
                composition.transform.localScale = Vector3.one;
                return;
            }
            float pulse = Mathf.Sin(age / 0.24f * Mathf.PI);
            Vector2 delta = DirectionVector(direction);
            composition.transform.localPosition = new Vector3(delta.x, -delta.y, 0f) * (0.13f * pulse);
            composition.transform.localScale = action == ZirconMirAction.Spell
                ? Vector3.one * (1f + 0.08f * pulse) : Vector3.one;
        }

        private static Vector2 DirectionVector(int direction)
        {
            switch (direction & 7)
            {
                case 0: return new Vector2(0f, -1f); case 1: return new Vector2(0.707f, -0.707f);
                case 2: return new Vector2(1f, 0f); case 3: return new Vector2(0.707f, 0.707f);
                case 4: return new Vector2(0f, 1f); case 5: return new Vector2(-0.707f, 0.707f);
                case 6: return new Vector2(-1f, 0f); default: return new Vector2(-0.707f, -0.707f);
            }
        }

        private void Position(SpriteRenderer renderer, Vector2 offset) => renderer.transform.localPosition = new Vector3(offset.x / pixelsPerUnit, -offset.y / pixelsPerUnit, 0f);

        private Transform FindMarker(uint id)
        {
            if (worldRenderer == null) return null;
            string suffix = "_" + id;
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
            texture.name = "P2_SoftEntityShadow";
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float nx = (x + 0.5f - width * 0.5f) / (width * 0.5f);
                float ny = (y + 0.5f - height * 0.5f) / (height * 0.5f);
                float alpha = Mathf.Clamp01(1f - nx * nx - ny * ny);
                alpha = alpha * alpha * 0.9f;
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
            texture.Apply(false, true);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            shadowSprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
            return shadowSprite;
        }

        private static Color32 FromArgb(int value)
        {
            uint argb = unchecked((uint)value);
            byte alpha = (byte)(argb >> 24); if (alpha == 0) alpha = 255;
            return new Color32((byte)(argb >> 16), (byte)(argb >> 8), (byte)argb, alpha);
        }
    }
}
