using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zircon.Mobile.Core.Assets;
using Zircon.Mobile.UI.Login;

namespace Zircon.Mobile.Game.World
{
    /// <summary>
    /// Restores production middle/front map layers with the source library offsets.
    /// The historical class name is retained so existing scene references remain valid.
    /// </summary>
    public sealed class ZirconMapOneObjectLayersBehaviour : MonoBehaviour
    {
        [SerializeField] private ZirconProtocolProbeBehaviour session;
        [SerializeField] private ZirconMapDebugRenderer mapRenderer;
        [SerializeField] private float tileScale = 0.32f;
        [SerializeField] private float pixelsPerUnit = 150f;

        private readonly Dictionary<string, ResourceSprite> sprites =
            new Dictionary<string, ResourceSprite>();
        private ZirconMapManifest rendered;
        private Transform root;
        private bool loading;

        [Serializable]
        private sealed class SourceFrameList
        {
            public SourceFrame[] Items;
        }

        [Serializable]
        private sealed class SourceFrame
        {
            public int Index;
            public int OffSetX;
            public int OffSetY;
        }

        private sealed class ResourceSprite
        {
            public Sprite Sprite;
            public int OffsetX;
            public int OffsetY;
        }

        private readonly struct MapDefinition
        {
            public MapDefinition(int mapIndex, string textureSourceRoot)
            {
                MapIndex = mapIndex;
                TextureSourceRoot = textureSourceRoot;
            }

            public int MapIndex { get; }
            public string TextureSourceRoot { get; }
        }

        private void Update()
        {
            if (loading || mapRenderer == null || mapRenderer.Manifest == null ||
                mapRenderer.Manifest == rendered)
                return;
            ZirconWorldSnapshot snapshot = session?.GetWorldSnapshot();
            if (snapshot != null && TryGetDefinition(snapshot.MapIndex, out MapDefinition definition))
                StartCoroutine(LoadAndRender(mapRenderer.Manifest, definition));
        }

        private IEnumerator LoadAndRender(ZirconMapManifest manifest, MapDefinition definition)
        {
            loading = true;
            rendered = manifest;
            Clear();

            var needed = new HashSet<string>();
            var folders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (ZirconMapCellManifest cell in manifest.SampleCells)
            {
                AddKey(needed, folders, cell.MiddleFile, cell.MiddleImage, definition.MapIndex);
                AddKey(needed, folders, cell.FrontFile, cell.FrontImage, definition.MapIndex);
            }

            var metadata = new Dictionary<string, Dictionary<int, SourceFrame>>(
                StringComparer.OrdinalIgnoreCase);
            foreach (string folder in folders)
            {
                string relative = "Generated/Textures/MapData/" +
                                  definition.TextureSourceRoot + folder + "/" +
                                  ManifestName(folder);
                string json = null;
                string error = null;
                yield return ZirconAssetStore.LoadText(relative,
                    value => json = value, value => error = value);
                if (string.IsNullOrEmpty(json))
                {
                    Debug.LogWarning("P2-B map metadata missing " + relative + ": " + error);
                    continue;
                }
                SourceFrameList list = JsonUtility.FromJson<SourceFrameList>("{\"Items\":" + json + "}");
                var byIndex = new Dictionary<int, SourceFrame>();
                if (list?.Items != null)
                    foreach (SourceFrame frame in list.Items)
                        byIndex[frame.Index] = frame;
                metadata[folder] = byIndex;
            }

            int loadedCount = 0;
            foreach (string key in needed)
            {
                string[] parts = key.Split(':');
                int fileId = int.Parse(parts[0]);
                int index = int.Parse(parts[1]);
                if (!ZirconMapDebugRenderer.TryGetMapLibraryFolder(fileId, out string folder))
                    continue;
                string sourceName = ZirconMapDebugRenderer.GetMapLibrarySourceName(folder);
                string file = sourceName + "_" + index.ToString("D5") + "_image.png";
                string relative = "Generated/Textures/MapData/" +
                                  definition.TextureSourceRoot + folder + "/" + file;
                byte[] bytes = null;
                yield return ZirconAssetStore.LoadBytes(relative,
                    value => bytes = value,
                    value => Debug.LogWarning("P2-B map object missing " + file + ": " + value));
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
                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0f, 1f), pixelsPerUnit);
                sprite.name = file;
                SourceFrame source = null;
                if (metadata.TryGetValue(folder, out Dictionary<int, SourceFrame> folderMetadata))
                    folderMetadata.TryGetValue(index, out source);
                sprites[key] = new ResourceSprite
                {
                    Sprite = sprite,
                    OffsetX = source?.OffSetX ?? 0,
                    OffsetY = source?.OffSetY ?? 0,
                };
                loadedCount++;
            }

            root = new GameObject("ProductionMapObjectLayers_" + definition.MapIndex).transform;
            root.SetParent(mapRenderer.transform, false);
            int middle = 0;
            int front = 0;
            foreach (ZirconMapCellManifest cell in manifest.SampleCells)
            {
                if (Create(cell, cell.MiddleFile, cell.MiddleImage, false))
                    middle++;
                if (Create(cell, cell.FrontFile, cell.FrontImage, true))
                    front++;
            }
            loading = false;
            Debug.Log("P2-B production map objects ready: map=" + definition.MapIndex +
                      " sprites=" + loadedCount + "/" + needed.Count +
                      " middle=" + middle + " front=" + front);
        }

        private static void AddKey(
            HashSet<string> keys,
            HashSet<string> folders,
            int fileId,
            int image,
            int mapIndex)
        {
            if (image <= 0 || !IsSupported(fileId, mapIndex) ||
                !ZirconMapDebugRenderer.TryGetMapLibraryFolder(fileId, out string folder))
                return;
            keys.Add(fileId + ":" + (image - 1));
            folders.Add(folder);
        }

        private bool Create(ZirconMapCellManifest cell, int fileId, int image, bool front)
        {
            if (image <= 0 ||
                !sprites.TryGetValue(fileId + ":" + (image - 1), out ResourceSprite resource))
                return false;
            var go = new GameObject((front ? "Front" : "Middle") + "_" + cell.X + "_" + cell.Y);
            go.transform.SetParent(root, false);
            go.transform.localPosition = new Vector3(
                cell.X * tileScale + resource.OffsetX / pixelsPerUnit,
                -cell.Y * tileScale - resource.OffsetY / pixelsPerUnit,
                0f);
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sharedMaterial = ZirconRuntimeSpriteMaterial.MapObjects;
            renderer.sprite = resource.Sprite;
            renderer.color = Color.white;
            renderer.sortingOrder = -cell.Y + (front ? 1 : -1);
            return true;
        }

        private static bool TryGetDefinition(int mapIndex, out MapDefinition definition)
        {
            switch (mapIndex)
            {
                case 1:
                    definition = new MapDefinition(1, "Base_");
                    return true;
                case 5:
                    definition = new MapDefinition(5, "Map5/");
                    return true;
                case 6:
                    definition = new MapDefinition(6, "Map6/");
                    return true;
                default:
                    definition = default;
                    return false;
            }
        }

        private static bool IsSupported(int fileId, int mapIndex)
        {
            if (mapIndex == 1)
                return fileId == 4 || fileId == 10;
            if (mapIndex == 5 || mapIndex == 6)
                return fileId == 4 || fileId == 5 || fileId == 10 ||
                       fileId == 19 || fileId == 20 || fileId == 23 ||
                       fileId == 25 || fileId == 40;
            return false;
        }

        private static string ManifestName(string folder)
        {
            return ZirconMapDebugRenderer.GetMapLibrarySourceName(folder) + ".manifest.json";
        }

        private void Clear()
        {
            if (root != null)
                Destroy(root.gameObject);
            root = null;
            foreach (ResourceSprite resource in sprites.Values)
            {
                if (resource?.Sprite == null)
                    continue;
                Texture texture = resource.Sprite.texture;
                Destroy(resource.Sprite);
                if (texture != null)
                    Destroy(texture);
            }
            sprites.Clear();
        }

        private void OnDestroy()
        {
            Clear();
        }
    }
}
