using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using Zircon.Mobile.Core.Assets;
using Zircon.Mobile.UI.Login;

namespace Zircon.Mobile.Game.World
{
    /// <summary>Composes the currently selected female warrior from her real body, hair and weapon resources.</summary>
    public sealed class ZirconFemaleWarriorPlayerBehaviour : MonoBehaviour
    {
        [SerializeField] private ZirconProtocolProbeBehaviour session;
        [SerializeField] private ZirconWorldDebugRenderer worldRenderer;
        [SerializeField] private float pixelsPerUnit = 100f;
        private readonly Sprite[] bodies = new Sprite[8];
        private readonly Sprite[] overlays = new Sprite[8];
        private readonly Sprite[] hairs = new Sprite[8];
        private readonly Sprite[] weapons = new Sprite[8];
        private readonly Vector2[] bodyOffsets = { new Vector2(-2,-48), new Vector2(-4,-48), new Vector2(6,-49), new Vector2(-3,-48), new Vector2(0,-48), new Vector2(13,-48), new Vector2(14,-48), new Vector2(14,-48) };
        private readonly Vector2[] hairOffsets = { new Vector2(18,-49), new Vector2(18,-49), new Vector2(17,-49), new Vector2(17,-50), new Vector2(18,-50), new Vector2(19,-50), new Vector2(20,-49), new Vector2(19,-49) };
        private readonly Vector2[] weaponOffsets = { new Vector2(18,-63), new Vector2(18,-54), new Vector2(2,-27), new Vector2(-2,-19), new Vector2(9,-29), new Vector2(-24,-31), new Vector2(-46,-27), new Vector2(-28,-51) };
        private Transform marker;
        private GameObject composition;
        private SpriteRenderer body, overlay, hair, weapon;
        private bool loading, ready;

        private void Update()
        {
            ZirconWorldSnapshot snapshot = session?.GetWorldSnapshot();
            if (snapshot == null || !snapshot.HasLocalPlayer || snapshot.LocalPlayer == null) return;
            if (!loading && !ready && ZirconPlayerAppearanceCaptureBehaviour.Current.HasValue)
                StartCoroutine(LoadSprites());
            if (!ready) return;
            uint id = snapshot.LocalPlayer.ObjectId;
            if (marker == null || !marker.name.EndsWith("_" + id, StringComparison.Ordinal)) marker = FindMarker(id);
            if (marker == null) return;
            EnsureComposition();
            Apply(Mathf.Clamp(snapshot.LocalPlayer.Direction, (byte)0, (byte)7));
        }

        private IEnumerator LoadSprites()
        {
            loading = true;
            ZirconPlayerAppearanceCaptureBehaviour.Appearance appearance = ZirconPlayerAppearanceCaptureBehaviour.Current.Value;
            if (appearance.Gender != 1 || appearance.CharacterClass != 0)
            {
                Debug.LogWarning("Female warrior renderer skipped incompatible character class=" + appearance.CharacterClass + " gender=" + appearance.Gender);
                loading = false;
                yield break;
            }
            int loaded = 0;
            for (int direction = 0; direction < 8; direction++)
            {
                int bodyIndex = appearance.Armour * 5000 + direction * 10;
                int weaponIndex = appearance.Weapon * 5000 + direction * 10;
                int hairIndex = direction * 10;
                yield return Load("PlayerAppearance/WM-Hum/WM-Hum_" + bodyIndex.ToString("D5") + "_image.png", value => { bodies[direction] = value; loaded++; });
                yield return Load("PlayerAppearance/WM-HumOverlay/WM-Hum_" + bodyIndex.ToString("D5") + "_overlay.png", value => { overlays[direction] = value; loaded++; });
                yield return Load("PlayerAppearance/WM-Hair/WM-Hair_" + hairIndex.ToString("D5") + "_image.png", value => { hairs[direction] = value; loaded++; });
                yield return Load("PlayerAppearance/WM-Weapon1/WM-Weapon1_" + weaponIndex.ToString("D5") + "_image.png", value => { weapons[direction] = value; loaded++; });
            }
            ready = loaded >= 24 && bodies[0] != null && weapons[0] != null;
            loading = false;
            Debug.Log("New character real appearance ready: layers=" + loaded + "/32 armour=" + appearance.Armour + " weapon=" + appearance.Weapon);
        }

        private IEnumerator Load(string relative, Action<Sprite> complete)
        {
            byte[] bytes = null;
            string error = null;
            yield return ZirconAssetStore.LoadBytes("Generated/Textures/" + relative, value => bytes = value, value => error = value);
            if (bytes == null || bytes.Length == 0) { Debug.LogWarning("Appearance layer missing " + relative + ": " + error); yield break; }
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(bytes)) { Destroy(texture); yield break; }
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            complete(Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0f, 1f), pixelsPerUnit));
        }

        private void EnsureComposition()
        {
            if (composition != null && composition.transform.parent == marker) return;
            if (composition != null) Destroy(composition);
            composition = new GameObject("新角色真实外观");
            composition.transform.SetParent(marker, false);
            SortingGroup group = composition.AddComponent<SortingGroup>();
            SpriteRenderer old = marker.GetComponent<SpriteRenderer>();
            group.sortingOrder = old != null ? old.sortingOrder : 0;
            body = Add("衣甲", 0); overlay = Add("衣甲染色", 1); hair = Add("头发", 2); weapon = Add("武器", 3);
            if (old != null) old.enabled = false;
        }

        private SpriteRenderer Add(string name, int order)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(composition.transform, false);
            SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = order;
            return renderer;
        }

        private void Apply(int direction)
        {
            body.sprite = bodies[direction]; overlay.sprite = overlays[direction]; hair.sprite = hairs[direction]; weapon.sprite = weapons[direction];
            Position(body, bodyOffsets[direction]); Position(overlay, bodyOffsets[direction]); Position(hair, hairOffsets[direction]); Position(weapon, weaponOffsets[direction]);
            ZirconPlayerAppearanceCaptureBehaviour.Appearance value = ZirconPlayerAppearanceCaptureBehaviour.Current.Value;
            overlay.color = FromArgb(value.ArmourColour); hair.color = FromArgb(value.HairColour);
            weapon.sortingOrder = direction == 0 || direction >= 5 ? -1 : 3;
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

        private static Color32 FromArgb(int value)
        {
            uint argb = unchecked((uint)value);
            byte alpha = (byte)(argb >> 24); if (alpha == 0) alpha = 255;
            return new Color32((byte)(argb >> 16), (byte)(argb >> 8), (byte)argb, alpha);
        }
    }
}
