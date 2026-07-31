using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zircon.Mobile.Core.Assets;
using Zircon.Mobile.UI.Login;

namespace Zircon.Mobile.Game.World
{
    /// <summary>
    /// Restores production middle/front map layers using the original map-cell anchors.
    /// The historical class name is retained so existing scene references remain valid.
    /// </summary>
    public sealed class ZirconMapOneObjectLayersBehaviour : MonoBehaviour
    {
        [SerializeField] private ZirconProtocolProbeBehaviour session;
        [SerializeField] private ZirconMapDebugRenderer mapRenderer;
        [SerializeField] private float tileScale = 0.32f;
        [SerializeField] private float tileHeightRatio = 2f / 3f;
        [SerializeField] private float pixelsPerUnit = 150f;
        [SerializeField] private int visibleRadiusX = 30;
        [SerializeField] private int visibleRadiusY = 28;

        private readonly Dictionary<string, ResourceSprite> sprites =
            new Dictionary<string, ResourceSprite>();
        private ZirconMapManifest rendered;
        private Transform root;
        private bool loading;
        private bool hasVisibleCellBounds;
        private RectInt visibleCellBounds;
        private Vector2Int visibleCenter = new Vector2Int(int.MinValue, int.MinValue);
        private readonly List<MapObjectVisual> visuals = new List<MapObjectVisual>();
        private readonly Dictionary<int, List<MapObjectVisual>> visualsByColumn =
            new Dictionary<int, List<MapObjectVisual>>();
        private Texture2D objectAtlas;

        private sealed class ResourceSprite
        {
            public Sprite Sprite;
            public Texture2D SourceTexture;
            public string Name;
        }

        private sealed class MapObjectVisual
        {
            public SpriteRenderer Renderer;
            public int X;
            public int Y;
            public int HeightCells;
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
            if (mapRenderer == null)
                return;
            ZirconWorldSnapshot snapshot = session?.GetWorldSnapshot();
            if (snapshot != null)
                UpdateVisibleRegion(snapshot.Location.X, snapshot.Location.Y);
            if (loading || mapRenderer.Manifest == null || mapRenderer.Manifest == rendered)
                return;
            if (snapshot != null && TryGetDefinition(snapshot.MapIndex, out MapDefinition definition))
                StartCoroutine(LoadAndRender(mapRenderer.Manifest, definition));
        }

        private void UpdateVisibleRegion(int centerX, int centerY)
        {
            var center = new Vector2Int(centerX, centerY);
            if (hasVisibleCellBounds && center == visibleCenter)
                return;

            bool hadVisibleCellBounds = hasVisibleCellBounds;
            RectInt previousBounds = visibleCellBounds;
            visibleCenter = center;
            // Older scenes do not serialize newly added fields. Preserve the tuned
            // mobile defaults instead of collapsing the view to a one-cell radius.
            int radiusX = visibleRadiusX > 1 ? visibleRadiusX : 30;
            int radiusY = visibleRadiusY > 1 ? visibleRadiusY : 28;
            visibleCellBounds = new RectInt(
                centerX - radiusX,
                centerY - radiusY,
                radiusX * 2 + 1,
                radiusY * 2 + 1);
            hasVisibleCellBounds = true;
            mapRenderer.SetVisibleCellBounds(visibleCellBounds);

            if (!hadVisibleCellBounds)
            {
                foreach (MapObjectVisual visual in visuals)
                    SetObjectVisibility(visual,
                        IsInsideVisibleBounds(visual.X, visual.Y, visual.HeightCells, visibleCellBounds));
                return;
            }

            // Limit visibility work to columns touched by the old/new camera areas.
            // This turns the former whole-map spike every four cells into small edge
            // updates on each grid transition, keeping camera motion even.
            int xMin = Mathf.Min(previousBounds.xMin, visibleCellBounds.xMin) - 1;
            int xMax = Mathf.Max(previousBounds.xMax, visibleCellBounds.xMax) + 1;
            for (int x = xMin; x < xMax; x++)
            {
                if (!visualsByColumn.TryGetValue(x, out List<MapObjectVisual> column))
                    continue;
                foreach (MapObjectVisual visual in column)
                {
                    bool wasVisible = IsInsideVisibleBounds(
                        visual.X, visual.Y, visual.HeightCells, previousBounds);
                    bool isVisible = IsInsideVisibleBounds(
                        visual.X, visual.Y, visual.HeightCells, visibleCellBounds);
                    if (wasVisible != isVisible)
                        SetObjectVisibility(visual, isVisible);
                }
            }
        }

        private static void SetObjectVisibility(MapObjectVisual visual, bool visible)
        {
            if (visual?.Renderer != null && visual.Renderer.enabled != visible)
                visual.Renderer.enabled = visible;
        }

        private IEnumerator LoadAndRender(ZirconMapManifest manifest, MapDefinition definition)
        {
            loading = true;
            rendered = manifest;
            Clear();

            var needed = new HashSet<string>();
            foreach (ZirconMapCellManifest cell in manifest.SampleCells)
            {
                AddKey(needed, cell.MiddleFile, cell.MiddleImage, definition.MapIndex);
                AddKey(needed, cell.FrontFile, cell.FrontImage, definition.MapIndex);
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
                sprites[key] = new ResourceSprite
                {
                    SourceTexture = texture,
                    Name = file,
                };
                loadedCount++;
            }

            BuildObjectAtlas();

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
            int fileId,
            int image,
            int mapIndex)
        {
            if (image <= 0 || !IsSupported(fileId, mapIndex) ||
                !ZirconMapDebugRenderer.TryGetMapLibraryFolder(fileId, out string folder))
                return;
            keys.Add(fileId + ":" + (image - 1));
        }

        private bool Create(ZirconMapCellManifest cell, int fileId, int image, bool front)
        {
            if (image <= 0 ||
                !sprites.TryGetValue(fileId + ":" + (image - 1), out ResourceSprite resource))
                return false;
            var go = new GameObject((front ? "Front" : "Middle") + "_" + cell.X + "_" + cell.Y);
            go.transform.SetParent(root, false);
            // Crystal/Zircon map objects ignore the ZL frame offsets. 48-pixel-wide
            // strips of different heights are all left aligned and share the bottom
            // edge of their owning 48x32 map cell.
            go.transform.localPosition = new Vector3(
                cell.X * tileScale,
                -(cell.Y + 1) * tileScale * tileHeightRatio,
                0f);
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sharedMaterial = ZirconRuntimeSpriteMaterial.MapObjects;
            renderer.sprite = resource.Sprite;
            renderer.color = Color.white;
            int rowBase = cell.Y * 4;
            renderer.sortingOrder = rowBase + (front ? 1 : 0);
            int heightCells = Mathf.Max(1,
                Mathf.CeilToInt(resource.Sprite.rect.height / 32f));
            renderer.enabled = IsInsideVisibleBounds(cell.X, cell.Y, heightCells);
            var visual = new MapObjectVisual
            {
                Renderer = renderer,
                X = cell.X,
                Y = cell.Y,
                HeightCells = heightCells,
            };
            visuals.Add(visual);
            if (!visualsByColumn.TryGetValue(cell.X, out List<MapObjectVisual> column))
            {
                column = new List<MapObjectVisual>();
                visualsByColumn[cell.X] = column;
            }
            column.Add(visual);
            return true;
        }

        private bool IsInsideVisibleBounds(int x, int y, int heightCells)
        {
            return !hasVisibleCellBounds ||
                   IsInsideVisibleBounds(x, y, heightCells, visibleCellBounds);
        }

        private static bool IsInsideVisibleBounds(int x, int y, int heightCells, RectInt bounds)
        {
            return x >= bounds.xMin - 1 && x < bounds.xMax + 1 &&
                   y >= bounds.yMin - 1 &&
                   y < bounds.yMax + Mathf.Max(1, heightCells);
        }

        private void BuildObjectAtlas()
        {
            var resources = new List<ResourceSprite>(sprites.Count);
            var textures = new List<Texture2D>(sprites.Count);
            foreach (ResourceSprite resource in sprites.Values)
            {
                if (resource?.SourceTexture == null)
                    continue;
                resources.Add(resource);
                textures.Add(resource.SourceTexture);
            }

            if (textures.Count == 0)
                return;
            if (!TryPack(textures, 2048, out Texture2D atlas, out Rect[] rects) &&
                !TryPack(textures, 4096, out atlas, out rects))
            {
                foreach (ResourceSprite resource in resources)
                {
                    Texture2D texture = resource.SourceTexture;
                    resource.Sprite = CreateObjectSprite(texture,
                        new Rect(0f, 0f, texture.width, texture.height), resource.Name);
                }
                return;
            }

            objectAtlas = atlas;
            objectAtlas.name = "ZirconMapObjectAtlas";
            objectAtlas.filterMode = FilterMode.Point;
            objectAtlas.wrapMode = TextureWrapMode.Clamp;
            for (int i = 0; i < resources.Count; i++)
            {
                Texture2D source = resources[i].SourceTexture;
                Rect packed = rects[i];
                Rect pixels = new Rect(
                    Mathf.Round(packed.x * objectAtlas.width),
                    Mathf.Round(packed.y * objectAtlas.height),
                    source.width,
                    source.height);
                resources[i].Sprite = CreateObjectSprite(objectAtlas, pixels, resources[i].Name);
                Destroy(source);
                resources[i].SourceTexture = null;
            }
            objectAtlas.Apply(false, true);
        }

        private bool TryPack(
            List<Texture2D> textures,
            int maximumSize,
            out Texture2D atlas,
            out Rect[] rects)
        {
            atlas = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                rects = atlas.PackTextures(textures.ToArray(), 4, maximumSize, false);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Map object atlas packing failed at " + maximumSize +
                                 "px; falling back safely. " + exception.Message, this);
                Destroy(atlas);
                atlas = null;
                rects = null;
                return false;
            }
            if (rects == null || rects.Length != textures.Count)
            {
                Destroy(atlas);
                atlas = null;
                return false;
            }
            for (int i = 0; i < rects.Length; i++)
            {
                int width = Mathf.RoundToInt(rects[i].width * atlas.width);
                int height = Mathf.RoundToInt(rects[i].height * atlas.height);
                if (width == textures[i].width && height == textures[i].height)
                    continue;
                Destroy(atlas);
                atlas = null;
                rects = null;
                return false;
            }
            return true;
        }

        private Sprite CreateObjectSprite(Texture2D texture, Rect rect, string name)
        {
            Sprite sprite = Sprite.Create(texture, rect, new Vector2(0f, 0f), pixelsPerUnit);
            sprite.name = name;
            return sprite;
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

        private void Clear()
        {
            if (root != null)
                Destroy(root.gameObject);
            root = null;
            visuals.Clear();
            visualsByColumn.Clear();
            foreach (ResourceSprite resource in sprites.Values)
            {
                if (resource?.Sprite != null)
                    Destroy(resource.Sprite);
                if (resource?.SourceTexture != null)
                    Destroy(resource.SourceTexture);
            }
            sprites.Clear();
            if (objectAtlas != null)
                Destroy(objectAtlas);
            objectAtlas = null;
        }

        private void OnDestroy()
        {
            Clear();
        }
    }
}
