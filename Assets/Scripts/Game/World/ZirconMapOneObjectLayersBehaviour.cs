using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zircon.Mobile.Core.Assets;
using Zircon.Mobile.UI.Login;

namespace Zircon.Mobile.Game.World
{
    /// <summary>Restores the middle/front house and small-object layers for map 1.</summary>
    public sealed class ZirconMapOneObjectLayersBehaviour : MonoBehaviour
    {
        [SerializeField] private ZirconProtocolProbeBehaviour session;
        [SerializeField] private ZirconMapDebugRenderer mapRenderer;
        [SerializeField] private float tileScale = 0.32f;
        [SerializeField] private float pixelsPerUnit = 150f;
        private readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        private ZirconMapManifest rendered;
        private Transform root;
        private bool loading;

        private void Update()
        {
            if (loading || mapRenderer == null || mapRenderer.Manifest == null || mapRenderer.Manifest == rendered)
                return;
            ZirconWorldSnapshot snapshot = session?.GetWorldSnapshot();
            if (snapshot != null && snapshot.MapIndex == 1)
                StartCoroutine(LoadAndRender(mapRenderer.Manifest));
        }

        private IEnumerator LoadAndRender(ZirconMapManifest manifest)
        {
            loading = true;
            rendered = manifest;
            Clear();
            var needed = new HashSet<string>();
            foreach (ZirconMapCellManifest cell in manifest.SampleCells)
            {
                AddKey(needed, cell.MiddleFile, cell.MiddleImage);
                AddKey(needed, cell.FrontFile, cell.FrontImage);
            }

            int loadedCount = 0;
            foreach (string key in needed)
            {
                string[] parts = key.Split(':');
                int fileId = int.Parse(parts[0]);
                int index = int.Parse(parts[1]);
                string folder = fileId == 4 ? "Housesc" : "SmObjectsc";
                string file = folder + "_" + index.ToString("D5") + "_image.png";
                byte[] bytes = null;
                yield return ZirconAssetStore.LoadBytes("Generated/Textures/MapData/Base_" + folder + "/" + file,
                    value => bytes = value,
                    value => Debug.LogError("Map 1 object missing " + file + ": " + value));
                if (bytes == null || bytes.Length == 0) continue;
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!texture.LoadImage(bytes)) { Destroy(texture); continue; }
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;
                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0f, 0f), pixelsPerUnit);
                sprite.name = file;
                sprites[key] = sprite;
                loadedCount++;
            }

            root = new GameObject("Map1RealObjectLayers").transform;
            root.SetParent(mapRenderer.transform, false);
            int middle = 0, front = 0;
            foreach (ZirconMapCellManifest cell in manifest.SampleCells)
            {
                if (Create(cell, cell.MiddleFile, cell.MiddleImage, false)) middle++;
                if (Create(cell, cell.FrontFile, cell.FrontImage, true)) front++;
            }
            loading = false;
            Debug.Log("Map 1 real objects ready: sprites=" + loadedCount + "/" + needed.Count + " middle=" + middle + " front=" + front);
        }

        private static void AddKey(HashSet<string> keys, int fileId, int image)
        {
            if ((fileId == 4 || fileId == 10) && image > 0)
                keys.Add(fileId + ":" + (image - 1));
        }

        private bool Create(ZirconMapCellManifest cell, int fileId, int image, bool front)
        {
            if (image <= 0 || !sprites.TryGetValue(fileId + ":" + (image - 1), out Sprite sprite)) return false;
            var go = new GameObject((front ? "Front" : "Middle") + "_" + cell.X + "_" + cell.Y);
            go.transform.SetParent(root, false);
            go.transform.localPosition = new Vector3(cell.X * tileScale, -(cell.Y + 1) * tileScale, 0f);
            go.transform.localScale = new Vector3(1f, 1.5f, 1f);
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = -cell.Y + (front ? 1 : -1);
            return true;
        }

        private void Clear()
        {
            if (root != null) Destroy(root.gameObject);
            root = null;
            foreach (Sprite sprite in sprites.Values)
            {
                if (sprite == null) continue;
                Texture texture = sprite.texture;
                Destroy(sprite);
                if (texture != null) Destroy(texture);
            }
            sprites.Clear();
        }

        private void OnDestroy() => Clear();
    }
}
