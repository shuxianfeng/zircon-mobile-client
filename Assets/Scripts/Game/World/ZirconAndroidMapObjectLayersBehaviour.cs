using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zircon.Mobile.Core.Assets;
using Zircon.Mobile.UI.Login;

namespace Zircon.Mobile.Game.World
{
    public sealed class ZirconAndroidMapObjectLayersBehaviour : MonoBehaviour
    {
        [SerializeField] private ZirconProtocolProbeBehaviour session;
        [SerializeField] private ZirconMapDebugRenderer mapRenderer;
        [SerializeField] private float tileScale = 0.32f;
        [SerializeField] private float spritePixelsPerUnit = 150f;

        private readonly Dictionary<int, Sprite> sprites = new Dictionary<int, Sprite>();
        private Transform layerRoot;
        private bool loading;
        private ZirconMapManifest renderedManifest;

        private void Update()
        {
            if (loading || mapRenderer == null || mapRenderer.Manifest == null || mapRenderer.Manifest == renderedManifest)
                return;
            ZirconWorldSnapshot snapshot = session?.GetWorldSnapshot();
            if (snapshot != null && snapshot.MapIndex == 294)
                StartCoroutine(LoadAndRender(mapRenderer.Manifest));
        }

        private IEnumerator LoadAndRender(ZirconMapManifest manifest)
        {
            loading = true;
            renderedManifest = manifest;
            ClearLayers();

            var indices = new HashSet<int>();
            foreach (ZirconMapCellManifest cell in manifest.SampleCells)
            {
                if (cell.MiddleFile == 21 && cell.MiddleImage > 0)
                    indices.Add(cell.MiddleImage - 1);
                if (cell.FrontFile == 21 && cell.FrontImage > 0)
                    indices.Add(cell.FrontImage - 1);
            }

            int loaded = 0;
            foreach (int index in indices)
            {
                byte[] bytes = null;
                string file = "Dungeonsc_" + index.ToString("D5") + "_image.png";
                yield return ZirconAssetStore.LoadBytes("Generated/Textures/MapData/Wood_Dungeonsc/" + file,
                    value => bytes = value,
                    error => Debug.LogError("Map object layer missing " + file + ": " + error));
                if (bytes == null || bytes.Length == 0)
                    continue;

                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!texture.LoadImage(bytes))
                {
                    Destroy(texture);
                    continue;
                }
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.name = file;
                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0f, 0f), spritePixelsPerUnit);
                sprite.name = file;
                sprites[index] = sprite;
                loaded++;
            }

            layerRoot = new GameObject("D2401ObjectLayers").transform;
            layerRoot.SetParent(mapRenderer.transform, false);
            int middleCount = 0;
            int frontCount = 0;
            foreach (ZirconMapCellManifest cell in manifest.SampleCells)
            {
                if (cell.MiddleFile == 21 && CreateLayer(cell, cell.MiddleImage - 1, false))
                    middleCount++;
                if (cell.FrontFile == 21 && CreateLayer(cell, cell.FrontImage - 1, true))
                    frontCount++;
            }

            loading = false;
            Debug.Log("D2401 object layers: sprites=" + loaded + "/" + indices.Count +
                      " middle=" + middleCount + " front=" + frontCount);
        }

        private bool CreateLayer(ZirconMapCellManifest cell, int index, bool front)
        {
            if (index < 0 || !sprites.TryGetValue(index, out Sprite sprite) || sprite == null)
                return false;

            // PC rendering handles 48x32 and 96x64 cell-sized images in its
            // floor pass. They are omitted here because the mobile background
            // pass already owns the walkable surface; this layer restores the
            // tall walls, pillars, doors and other scene silhouettes.
            Rect rect = sprite.rect;
            if ((rect.width == 48f && rect.height == 32f) || (rect.width == 96f && rect.height == 64f))
                return false;

            GameObject item = new GameObject((front ? "Front" : "Middle") + "_" + cell.X + "_" + cell.Y);
            item.transform.SetParent(layerRoot, false);
            item.transform.localPosition = new Vector3(cell.X * tileScale, -(cell.Y + 1) * tileScale, 0f);
            item.transform.localScale = new Vector3(1f, 1.5f, 1f);
            SpriteRenderer renderer = item.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = Color.white;
            renderer.sortingOrder = -cell.Y + (front ? 1 : -1);
            return true;
        }

        private void ClearLayers()
        {
            if (layerRoot != null)
                Destroy(layerRoot.gameObject);
            layerRoot = null;
            foreach (Sprite sprite in sprites.Values)
            {
                if (sprite == null) continue;
                Texture2D texture = sprite.texture;
                Destroy(sprite);
                if (texture != null) Destroy(texture);
            }
            sprites.Clear();
        }

        private void OnDestroy() => ClearLayers();
    }
}
