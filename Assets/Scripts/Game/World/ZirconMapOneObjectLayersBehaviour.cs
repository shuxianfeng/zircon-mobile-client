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
        [SerializeField] private ZirconProductionLocalPlayerBehaviour localPlayerVisuals;
        [SerializeField] private float tileScale = 0.32f;
        [SerializeField] private float tileHeightRatio = 2f / 3f;
        [SerializeField] private float pixelsPerUnit = 150f;
        [SerializeField] private int visibleRadiusX = 30;
        [SerializeField] private int visibleRadiusY = 28;

        private ZirconMapManifest rendered;
        private ZirconMapManifest requestedManifest;
        private int requestedMapIndex = -1;
        private int observedMapIndex = -1;
        private int requestVersion;
        private float nextLoadAttemptAt;
        private Transform root;
        private bool loading;
        private bool hasVisibleCellBounds;
        private RectInt visibleCellBounds;
        private Vector2Int visibleCenter = new Vector2Int(int.MinValue, int.MinValue);
        private readonly List<MapObjectVisual> visuals = new List<MapObjectVisual>();
        private readonly Dictionary<int, List<MapObjectVisual>> visualsByColumn =
            new Dictionary<int, List<MapObjectVisual>>();
        private ResourceCache activeCache;
        private ResourceCache pendingCache;

        public int EffectiveVisibleRadiusX => visibleRadiusX > 1 ? visibleRadiusX : 30;
        public int EffectiveVisibleRadiusY => visibleRadiusY > 1 ? visibleRadiusY : 28;

        private sealed class ResourceSprite
        {
            public Sprite Sprite;
            public Texture2D SourceTexture;
            public string Name;
        }

        private sealed class ResourceCache
        {
            public ResourceCache(int mapIndex)
            {
                MapIndex = mapIndex;
            }

            public int MapIndex { get; }
            public readonly Dictionary<string, ResourceSprite> Sprites =
                new Dictionary<string, ResourceSprite>();
            public readonly List<Texture2D> Atlases = new List<Texture2D>();
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
            public MapDefinition(int mapIndex, string expectedSource, string textureSourceRoot)
            {
                MapIndex = mapIndex;
                ExpectedSource = expectedSource;
                TextureSourceRoot = textureSourceRoot;
            }

            public int MapIndex { get; }
            public string ExpectedSource { get; }
            public string TextureSourceRoot { get; }
        }

        private void Awake()
        {
            if (localPlayerVisuals == null)
                localPlayerVisuals = GetComponent<ZirconProductionLocalPlayerBehaviour>();
        }

        private void Update()
        {
            if (mapRenderer == null)
                return;

            ZirconWorldSnapshot snapshot = session?.GetWorldSnapshot();
            if (snapshot == null)
                return;

            UpdateVisibleRegion(snapshot.Location.X, snapshot.Location.Y);
            if (snapshot.MapIndex != observedMapIndex)
            {
                observedMapIndex = snapshot.MapIndex;
                requestedManifest = null;
                requestedMapIndex = -1;
                requestVersion++;
                if (root != null)
                    root.gameObject.SetActive(activeCache != null &&
                                              activeCache.MapIndex == observedMapIndex);
                if (pendingCache != null &&
                    !ReferenceEquals(pendingCache, activeCache) &&
                    pendingCache.MapIndex != observedMapIndex)
                {
                    DisposeCache(pendingCache);
                    pendingCache = null;
                }
            }

            ZirconMapManifest manifest = mapRenderer.Manifest;
            if (manifest == null || !mapRenderer.IsVisualBuildComplete ||
                !TryGetDefinition(snapshot.MapIndex, out MapDefinition definition) ||
                !ManifestMatchesDefinition(manifest, definition))
                return;

            if (!ReferenceEquals(requestedManifest, manifest) ||
                requestedMapIndex != definition.MapIndex)
            {
                requestedManifest = manifest;
                requestedMapIndex = definition.MapIndex;
                requestVersion++;
            }

            if (loading || ReferenceEquals(manifest, rendered) ||
                Time.unscaledTime < nextLoadAttemptAt)
                return;

            // Houses and trees are the heaviest first-entry resource batch.
            // Give the local player's preview frame exclusive priority so the
            // controlled character appears before decorative map objects.
            if (localPlayerVisuals != null && localPlayerVisuals.IsAppearancePending)
                return;

            StartCoroutine(LoadAndRender(manifest, definition, requestVersion));
        }

        private void UpdateVisibleRegion(int centerX, int centerY)
        {
            var center = new Vector2Int(centerX, centerY);
            if (hasVisibleCellBounds && center == visibleCenter)
                return;

            bool hadVisibleCellBounds = hasVisibleCellBounds;
            RectInt previousBounds = visibleCellBounds;
            visibleCenter = center;
            int radiusX = EffectiveVisibleRadiusX;
            int radiusY = EffectiveVisibleRadiusY;
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

            // All production object strips are one map column wide. Restrict
            // incremental visibility work to columns touched by either bounds.
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

        private IEnumerator LoadAndRender(
            ZirconMapManifest manifest,
            MapDefinition definition,
            int version)
        {
            loading = true;
            float startedAt = Time.realtimeSinceStartup;
            IReadOnlyList<ZirconMapCellManifest> objectCells = GetObjectCells(manifest);
            if (objectCells == null)
            {
                FailLoad("object cell data is missing", false);
                yield break;
            }

            ResourceCache cache = GetResourceCache(definition.MapIndex);
            var needed = new HashSet<string>();
            foreach (ZirconMapCellManifest cell in objectCells)
            {
                AddKey(needed, cell.MiddleFile, cell.MiddleImage, definition.MapIndex);
                AddKey(needed, cell.FrontFile, cell.FrontImage, definition.MapIndex);
            }

            int newlyLoaded = 0;
            int failed = 0;
            foreach (string key in needed)
            {
                if (cache.Sprites.TryGetValue(key, out ResourceSprite cached) &&
                    (cached?.Sprite != null || cached?.SourceTexture != null))
                    continue;

                string[] parts = key.Split(':');
                int fileId = int.Parse(parts[0]);
                int index = int.Parse(parts[1]);
                if (!ZirconMapDebugRenderer.TryGetMapLibraryFolder(fileId, out string folder))
                {
                    failed++;
                    continue;
                }

                string sourceName = ZirconMapDebugRenderer.GetMapLibrarySourceName(folder);
                string file = sourceName + "_" + index.ToString("D5") + "_image.png";
                string relative = "Generated/Textures/MapData/" +
                                  definition.TextureSourceRoot + folder + "/" + file;
                byte[] bytes = null;
                string loadError = null;
                yield return ZirconAssetStore.LoadBytes(relative,
                    value => bytes = value,
                    value => loadError = value);

                if (!IsRequestCurrent(manifest, definition, version))
                {
                    FailLoad("superseded by a newer map chunk", true);
                    yield break;
                }

                if (bytes == null || bytes.Length == 0)
                {
                    failed++;
                    Debug.LogWarning("P2-B map object missing " + file + ": " + loadError, this);
                    continue;
                }

                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!texture.LoadImage(bytes))
                {
                    Destroy(texture);
                    failed++;
                    Debug.LogWarning("P2-B map object image is invalid " + file, this);
                    continue;
                }

                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;
                cache.Sprites[key] = new ResourceSprite
                {
                    SourceTexture = texture,
                    Name = file,
                };
                newlyLoaded++;
            }

            if (!IsRequestCurrent(manifest, definition, version))
            {
                FailLoad("superseded by a newer map chunk", true);
                yield break;
            }

            yield return BuildObjectAtlasesIncremental(cache, manifest, definition, version);
            if (!IsRequestCurrent(manifest, definition, version))
            {
                FailLoad("superseded while building object atlases", true);
                yield break;
            }
            int resolved = 0;
            foreach (string key in needed)
            {
                if (cache.Sprites.TryGetValue(key, out ResourceSprite resource) &&
                    resource?.Sprite != null)
                    resolved++;
            }
            // A handful of source maps intentionally reference empty legacy
            // frames. Keep tolerating individual misses, but never replace a
            // valid old layer when the whole resource batch failed.
            int attemptedLoads = newlyLoaded + failed;
            bool batchFailure = failed > 16 && failed * 2 > attemptedLoads;
            if (needed.Count > 0 && (resolved == 0 || batchFailure))
            {
                FailLoad("object resource batch failed (resolved=" + resolved +
                         " attempted=" + attemptedLoads + " failures=" + failed + ")", false);
                yield break;
            }

            if (!IsRequestCurrent(manifest, definition, version))
            {
                FailLoad("superseded by a newer map chunk", true);
                yield break;
            }

            Transform stagedRoot = new GameObject(
                "ProductionMapObjectLayers_" + definition.MapIndex + "_Pending").transform;
            stagedRoot.SetParent(mapRenderer.transform, false);
            stagedRoot.gameObject.SetActive(false);
            var stagedVisuals = new List<MapObjectVisual>();
            var stagedColumns = new Dictionary<int, List<MapObjectVisual>>();
            int middle = 0;
            int front = 0;
            int processedCells = 0;
            foreach (ZirconMapCellManifest cell in objectCells)
            {
                if (Create(cell, cell.MiddleFile, cell.MiddleImage, false,
                        cache, stagedRoot, stagedVisuals, stagedColumns))
                    middle++;
                if (Create(cell, cell.FrontFile, cell.FrontImage, true,
                        cache, stagedRoot, stagedVisuals, stagedColumns))
                    front++;
                if (++processedCells % 64 == 0)
                {
                    yield return null;
                    if (!IsRequestCurrent(manifest, definition, version))
                    {
                        Destroy(stagedRoot.gameObject);
                        FailLoad("superseded while staging map objects", true);
                        yield break;
                    }
                }
            }

            if (!IsRequestCurrent(manifest, definition, version))
            {
                Destroy(stagedRoot.gameObject);
                FailLoad("superseded while constructing the object root", true);
                yield break;
            }

            CommitGeneration(manifest, definition, cache, stagedRoot, stagedVisuals, stagedColumns);
            Debug.Log("P2-B production map objects ready: map=" + definition.MapIndex +
                      " cachedSprites=" + cache.Sprites.Count +
                      " newlyLoaded=" + newlyLoaded +
                      " resolved=" + resolved + "/" + needed.Count +
                      " missing=" + failed +
                      " middle=" + middle + " front=" + front +
                      " elapsed=" + (Time.realtimeSinceStartup - startedAt).ToString("F2") + "s");
        }

        private static IReadOnlyList<ZirconMapCellManifest> GetObjectCells(
            ZirconMapManifest manifest)
        {
            return manifest?.ObjectCells ?? manifest?.SampleCells;
        }

        private bool IsRequestCurrent(
            ZirconMapManifest manifest,
            MapDefinition definition,
            int version)
        {
            return version == requestVersion &&
                   requestedMapIndex == definition.MapIndex &&
                   observedMapIndex == definition.MapIndex &&
                   ReferenceEquals(requestedManifest, manifest) &&
                   ReferenceEquals(mapRenderer?.Manifest, manifest);
        }

        private void FailLoad(string reason, bool superseded)
        {
            loading = false;
            if (superseded)
            {
                nextLoadAttemptAt = 0f;
                Debug.Log("P2-B map object load discarded: " + reason, this);
            }
            else
            {
                nextLoadAttemptAt = Time.unscaledTime + 1f;
                Debug.LogError("P2-B map object load failed; retaining the previous layer: " + reason, this);
            }
        }

        private ResourceCache GetResourceCache(int mapIndex)
        {
            if (activeCache != null && activeCache.MapIndex == mapIndex)
                return activeCache;
            if (pendingCache != null && pendingCache.MapIndex == mapIndex)
                return pendingCache;

            if (pendingCache != null && !ReferenceEquals(pendingCache, activeCache))
                DisposeCache(pendingCache);
            pendingCache = new ResourceCache(mapIndex);
            return pendingCache;
        }

        private void CommitGeneration(
            ZirconMapManifest manifest,
            MapDefinition definition,
            ResourceCache cache,
            Transform stagedRoot,
            List<MapObjectVisual> stagedVisuals,
            Dictionary<int, List<MapObjectVisual>> stagedColumns)
        {
            Transform oldRoot = root;
            ResourceCache oldCache = activeCache;
            if (oldRoot != null)
                oldRoot.gameObject.SetActive(false);

            root = stagedRoot;
            root.name = "ProductionMapObjectLayers_" + definition.MapIndex;
            activeCache = cache;
            if (ReferenceEquals(pendingCache, cache))
                pendingCache = null;

            visuals.Clear();
            visuals.AddRange(stagedVisuals);
            visualsByColumn.Clear();
            foreach (KeyValuePair<int, List<MapObjectVisual>> pair in stagedColumns)
                visualsByColumn[pair.Key] = pair.Value;

            rendered = manifest;
            root.gameObject.SetActive(true);
            loading = false;
            nextLoadAttemptAt = 0f;

            if (oldRoot != null)
                Destroy(oldRoot.gameObject);
            if (oldCache != null && !ReferenceEquals(oldCache, activeCache))
                DisposeCache(oldCache);
            if (pendingCache != null && !ReferenceEquals(pendingCache, activeCache))
            {
                DisposeCache(pendingCache);
                pendingCache = null;
            }
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

        private bool Create(
            ZirconMapCellManifest cell,
            int fileId,
            int image,
            bool front,
            ResourceCache cache,
            Transform targetRoot,
            List<MapObjectVisual> targetVisuals,
            Dictionary<int, List<MapObjectVisual>> targetColumns)
        {
            if (image <= 0 ||
                !cache.Sprites.TryGetValue(fileId + ":" + (image - 1),
                    out ResourceSprite resource) ||
                resource?.Sprite == null)
                return false;

            var go = new GameObject((front ? "Front" : "Middle") + "_" + cell.X + "_" + cell.Y);
            go.transform.SetParent(targetRoot, false);
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
            targetVisuals.Add(visual);
            if (!targetColumns.TryGetValue(cell.X, out List<MapObjectVisual> column))
            {
                column = new List<MapObjectVisual>();
                targetColumns[cell.X] = column;
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

        private IEnumerator BuildObjectAtlasesIncremental(ResourceCache cache,
            ZirconMapManifest manifest, MapDefinition definition, int version)
        {
            var pending = new List<ResourceSprite>();
            foreach (ResourceSprite resource in cache.Sprites.Values)
            {
                if (resource?.Sprite != null || resource?.SourceTexture == null)
                    continue;
                pending.Add(resource);
            }

            const int texturesPerAtlas = 96;
            for (int start = 0; start < pending.Count; start += texturesPerAtlas)
            {
                if (!IsRequestCurrent(manifest, definition, version))
                    yield break;

                int count = Mathf.Min(texturesPerAtlas, pending.Count - start);
                var textures = new List<Texture2D>(count);
                for (int i = 0; i < count; i++)
                    textures.Add(pending[start + i].SourceTexture);

                if (!TryPack(textures, 2048, out Texture2D atlas, out Rect[] rects) &&
                    !TryPack(textures, 4096, out atlas, out rects))
                {
                    for (int i = 0; i < count; i++)
                    {
                        ResourceSprite resource = pending[start + i];
                        Texture2D texture = resource.SourceTexture;
                        resource.Sprite = CreateObjectSprite(texture,
                            new Rect(0f, 0f, texture.width, texture.height), resource.Name);
                    }
                }
                else
                {
                    atlas.name = "ZirconMapObjectAtlas_" + cache.MapIndex + "_" + cache.Atlases.Count;
                    atlas.filterMode = FilterMode.Point;
                    atlas.wrapMode = TextureWrapMode.Clamp;
                    cache.Atlases.Add(atlas);
                    for (int i = 0; i < count; i++)
                    {
                        ResourceSprite resource = pending[start + i];
                        Texture2D source = resource.SourceTexture;
                        Rect packed = rects[i];
                        Rect pixels = new Rect(
                            Mathf.Round(packed.x * atlas.width),
                            Mathf.Round(packed.y * atlas.height),
                            source.width,
                            source.height);
                        resource.Sprite = CreateObjectSprite(atlas, pixels, resource.Name);
                        Destroy(source);
                        resource.SourceTexture = null;
                    }
                    atlas.Apply(false, true);
                }
                yield return null;
            }
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
                rects = null;
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

        private static bool ManifestMatchesDefinition(
            ZirconMapManifest manifest,
            MapDefinition definition)
        {
            return manifest != null &&
                   string.Equals(manifest.Source, definition.ExpectedSource,
                       StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryGetDefinition(int mapIndex, out MapDefinition definition)
        {
            switch (mapIndex)
            {
                case 1:
                    definition = new MapDefinition(1, "0.map", "Base_");
                    return true;
                case 5:
                    definition = new MapDefinition(5, "1.map", "Map5/");
                    return true;
                case 6:
                    definition = new MapDefinition(6, "2.map", "Map6/");
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

        private void DisposeCache(ResourceCache cache)
        {
            if (cache == null)
                return;
            foreach (ResourceSprite resource in cache.Sprites.Values)
            {
                if (resource?.Sprite != null)
                    Destroy(resource.Sprite);
                if (resource?.SourceTexture != null)
                    Destroy(resource.SourceTexture);
            }
            cache.Sprites.Clear();
            foreach (Texture2D atlas in cache.Atlases)
                if (atlas != null)
                    Destroy(atlas);
            cache.Atlases.Clear();
        }

        private void Clear()
        {
            requestVersion++;
            if (root != null)
                Destroy(root.gameObject);
            root = null;
            visuals.Clear();
            visualsByColumn.Clear();

            ResourceCache oldActive = activeCache;
            activeCache = null;
            if (oldActive != null)
                DisposeCache(oldActive);
            if (pendingCache != null && !ReferenceEquals(pendingCache, oldActive))
                DisposeCache(pendingCache);
            pendingCache = null;
            rendered = null;
            requestedManifest = null;
            requestedMapIndex = -1;
            loading = false;
            nextLoadAttemptAt = 0f;
        }

        private void OnDestroy()
        {
            Clear();
        }
    }
}
