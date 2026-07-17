using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using Zircon.Mobile.Core.Assets;
using Zircon.Mobile.UI.Login;

namespace Zircon.Mobile.Game.World
{
    /// <summary>Draws the local player from the exact body, dye, hair and weapon layers reported by StartGame.</summary>
    public sealed class ZirconComposedLocalPlayerBehaviour : MonoBehaviour
    {
        [SerializeField] private ZirconProtocolProbeBehaviour session;
        [SerializeField] private ZirconWorldDebugRenderer worldRenderer;
        [SerializeField] private float pixelsPerUnit = 100f;

        private readonly Sprite[] bodies = new Sprite[8];
        private readonly Sprite[] overlays = new Sprite[8];
        private readonly Sprite[] hairs = new Sprite[8];
        private readonly Sprite[] weapons = new Sprite[8];
        private readonly Vector2[] bodyOffsets =
        {
            new Vector2(8,-47), new Vector2(10,-47), new Vector2(15,-48), new Vector2(8,-49),
            new Vector2(8,-49), new Vector2(13,-49), new Vector2(15,-48), new Vector2(12,-48)
        };
        private readonly Vector2[] hairOffsets =
        {
            new Vector2(20,-48), new Vector2(19,-48), new Vector2(18,-48), new Vector2(18,-49),
            new Vector2(20,-50), new Vector2(21,-49), new Vector2(21,-49), new Vector2(21,-48)
        };
        private readonly Vector2[] weaponOffsets =
        {
            new Vector2(29,-55), new Vector2(20,-44), new Vector2(7,-25), new Vector2(2,-24),
            new Vector2(-1,-20), new Vector2(-20,-27), new Vector2(-24,-37), new Vector2(-3,-52)
        };

        private Transform marker;
        private GameObject composition;
        private SpriteRenderer bodyRenderer;
        private SpriteRenderer overlayRenderer;
        private SpriteRenderer hairRenderer;
        private SpriteRenderer weaponRenderer;
        private bool loading;
        private bool ready;

        private void Update()
        {
            ZirconWorldSnapshot snapshot = session?.GetWorldSnapshot();
            if (snapshot == null || !snapshot.HasLocalPlayer || snapshot.LocalPlayer == null)
                return;

            if (!loading && !ready && ZirconPlayerAppearanceCaptureBehaviour.Current.HasValue)
                StartCoroutine(LoadSprites());
            if (!ready)
                return;

            uint objectId = snapshot.LocalPlayer.ObjectId;
            if (marker == null || !marker.name.EndsWith("_" + objectId, StringComparison.Ordinal))
                marker = FindMarker(objectId);
            if (marker == null)
                return;

            EnsureComposition();
            int direction = Mathf.Clamp(snapshot.LocalPlayer.Direction, (byte)0, (byte)7);
            ApplyDirection(direction);
        }

        private IEnumerator LoadSprites()
        {
            loading = true;
            int loaded = 0;
            for (int direction = 0; direction < 8; direction++)
            {
                int bodyIndex = 40000 + direction * 10;
                int weaponIndex = 35000 + direction * 10;
                int hairIndex = direction * 10;
                yield return Load("PlayerAppearance/M-Hum/M-Hum_" + bodyIndex.ToString("D5") + "_image.png", value => { bodies[direction] = value; loaded++; });
                yield return Load("PlayerAppearance/M-HumOverlay/M-Hum_" + bodyIndex.ToString("D5") + "_overlay.png", value => { overlays[direction] = value; loaded++; });
                yield return Load("PlayerAppearance/M-Hair/M-Hair_" + hairIndex.ToString("D5") + "_image.png", value => { hairs[direction] = value; loaded++; });
                yield return Load("PlayerAppearance/M-Weapon15/M-Weapon15_" + weaponIndex.ToString("D5") + "_image.png", value => { weapons[direction] = value; loaded++; });
            }
            ready = loaded >= 24 && bodies[6] != null && weapons[6] != null;
            loading = false;
            Debug.Log("P0 composed local player layers=" + loaded + "/32 ready=" + ready);
        }

        private IEnumerator Load(string relative, Action<Sprite> completed)
        {
            byte[] bytes = null;
            string error = null;
            yield return ZirconAssetStore.LoadBytes("Generated/Textures/" + relative, value => bytes = value, value => error = value);
            if (bytes == null || bytes.Length == 0)
            {
                Debug.LogWarning("P0 player layer missing " + relative + ": " + error);
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
            completed(Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0f, 1f), pixelsPerUnit));
        }

        private void EnsureComposition()
        {
            if (composition != null && composition.transform.parent == marker)
                return;
            if (composition != null)
                Destroy(composition);

            composition = new GameObject("P0_ComposedPlayer");
            composition.transform.SetParent(marker, false);
            var group = composition.AddComponent<SortingGroup>();
            SpriteRenderer old = marker.GetComponent<SpriteRenderer>();
            group.sortingOrder = old != null ? old.sortingOrder : 0;
            bodyRenderer = AddLayer("衣甲", 0);
            overlayRenderer = AddLayer("衣甲染色", 1);
            hairRenderer = AddLayer("头发", 2);
            weaponRenderer = AddLayer("武器", 3);
            if (old != null)
                old.enabled = false;
        }

        private SpriteRenderer AddLayer(string name, int order)
        {
            var child = new GameObject(name);
            child.transform.SetParent(composition.transform, false);
            var renderer = child.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = order;
            return renderer;
        }

        private void ApplyDirection(int direction)
        {
            bodyRenderer.sprite = bodies[direction];
            overlayRenderer.sprite = overlays[direction];
            hairRenderer.sprite = hairs[direction];
            weaponRenderer.sprite = weapons[direction];
            Position(bodyRenderer, bodyOffsets[direction]);
            Position(overlayRenderer, bodyOffsets[direction]);
            Position(hairRenderer, hairOffsets[direction]);
            Position(weaponRenderer, weaponOffsets[direction]);

            ZirconPlayerAppearanceCaptureBehaviour.Appearance appearance = ZirconPlayerAppearanceCaptureBehaviour.Current.Value;
            overlayRenderer.color = FromArgb(appearance.ArmourColour);
            hairRenderer.color = FromArgb(appearance.HairColour);
            weaponRenderer.sortingOrder = direction == 0 || direction >= 5 ? -1 : 3;
        }

        private void Position(SpriteRenderer renderer, Vector2 pixelOffset)
        {
            renderer.transform.localPosition = new Vector3(pixelOffset.x / pixelsPerUnit, -pixelOffset.y / pixelsPerUnit, 0f);
        }

        private Transform FindMarker(uint objectId)
        {
            if (worldRenderer == null)
                return null;
            string suffix = "_" + objectId;
            for (int i = 0; i < worldRenderer.transform.childCount; i++)
            {
                Transform child = worldRenderer.transform.GetChild(i);
                if (child.name.EndsWith(suffix, StringComparison.Ordinal))
                    return child;
            }
            return null;
        }

        private static Color32 FromArgb(int value)
        {
            uint argb = unchecked((uint)value);
            byte a = (byte)(argb >> 24);
            if (a == 0) a = 255;
            return new Color32((byte)(argb >> 16), (byte)(argb >> 8), (byte)argb, a);
        }
    }
}
