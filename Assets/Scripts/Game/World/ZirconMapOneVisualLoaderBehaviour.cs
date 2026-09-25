using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using Zircon.Mobile.Core.Assets;
using Zircon.Mobile.UI.Login;

namespace Zircon.Mobile.Game.World
{
    /// <summary>
    /// Loads packaged production map regions by live map index.
    /// The historical class name is retained so existing scene references remain valid.
    /// </summary>
    public sealed class ZirconMapOneVisualLoaderBehaviour : MonoBehaviour
    {
        [SerializeField] private ZirconProtocolProbeBehaviour session;
        [SerializeField] private ZirconMapDebugRenderer mapRenderer;
        [SerializeField] private ZirconMapOneObjectLayersBehaviour objectLayers;
        [SerializeField] private int chunkSize = 96;
        [SerializeField] private int reloadMargin = 24;
        [SerializeField] private int predictedStepAllowance = 2;
        [SerializeField] private int objectAnchorPaddingCells = 2;
        [SerializeField] private int objectHeightPaddingCells = 24;
        private bool loading;
        private int loadedMapIndex = -1;
        private int loadedMapWidth;
        private int loadedMapHeight;
        private RectInt loadedView;
        private int stagedFloorMapIndex = -1;
        private readonly HashSet<string> stagedFloorTiles = new HashSet<string>();
        private readonly Dictionary<int, ZirconMapManifest> sourceManifests =
            new Dictionary<int, ZirconMapManifest>();

        private readonly struct MapDefinition
        {
            public MapDefinition(int mapIndex, string manifest, string textureSourceRoot)
            {
                MapIndex = mapIndex;
                Manifest = manifest;
                TextureSourceRoot = textureSourceRoot;
            }

            public int MapIndex { get; }
            public string Manifest { get; }
            public string TextureSourceRoot { get; }
        }

        private void Awake()
        {
            if (objectLayers == null)
                objectLayers = GetComponent<ZirconMapOneObjectLayersBehaviour>();
        }

        private void Update()
        {
            if (loading || session == null || !session.IsInGame)
                return;
            ZirconWorldSnapshot snapshot = session.GetWorldSnapshot();
            if (snapshot == null)
                return;
            if (TryGetDefinition(snapshot.MapIndex, out MapDefinition definition) &&
                NeedsReload(snapshot))
                StartCoroutine(LoadMap(definition, snapshot.Location.X, snapshot.Location.Y));
        }

        private bool NeedsReload(ZirconWorldSnapshot snapshot)
        {
            if (snapshot.MapIndex != loadedMapIndex)
                return true;
            if (snapshot.MapIndex != 5 && snapshot.MapIndex != 6)
                return false;
            int right = loadedView.xMax;
            int bottom = loadedView.yMax;
            int marginX = GetReloadMargin(loadedView.width, GetVisibleRadiusX());
            int marginY = GetReloadMargin(loadedView.height, GetVisibleRadiusY());
            return (loadedView.x > 0 && snapshot.Location.X < loadedView.x + marginX) ||
                   (right < loadedMapWidth && snapshot.Location.X >= right - marginX) ||
                   (loadedView.y > 0 && snapshot.Location.Y < loadedView.y + marginY) ||
                   (bottom < loadedMapHeight && snapshot.Location.Y >= bottom - marginY);
        }

        private int GetReloadMargin(int extent, int visibleRadius)
        {
            int configured = reloadMargin > 0 ? reloadMargin : 24;
            int predicted = predictedStepAllowance > 0 ? predictedStepAllowance : 2;
            int requested = Mathf.Max(configured, visibleRadius + predicted);
            return Mathf.Min(requested, Mathf.Max(1, extent / 2 - 1));
        }

        private int GetVisibleRadiusX()
        {
            return objectLayers != null ? objectLayers.EffectiveVisibleRadiusX : 30;
        }

        private int GetVisibleRadiusY()
        {
            return objectLayers != null ? objectLayers.EffectiveVisibleRadiusY : 28;
        }

        private IEnumerator LoadMap(MapDefinition definition, int centerX, int centerY)
        {
            loading = true;
            float startedAt = Time.realtimeSinceStartup;
            if (mapRenderer == null)
            {
                Debug.LogError("Production map loader cannot start because ZirconMapDebugRenderer is missing.");
                loading = false;
                yield break;
            }

            ZirconMapManifest sourceManifest = null;
            string error = null;
            if (!sourceManifests.TryGetValue(definition.MapIndex, out sourceManifest))
            {
                string sourceJson = null;
                string manifestRelative = "Generated/Data/Maps/" + definition.Manifest;
                yield return ZirconAssetStore.LoadText(manifestRelative,
                    value => sourceJson = value, value => error = value);
                sourceManifest = string.IsNullOrEmpty(sourceJson)
                    ? null
                    : JsonUtility.FromJson<ZirconMapManifest>(sourceJson);
                if (sourceManifest != null)
                    sourceManifests[definition.MapIndex] = sourceManifest;
            }
            if (sourceManifest == null || sourceManifest.SampleCells == null)
            {
                Debug.LogError("Production map manifest failed map=" + definition.MapIndex + ": " + error);
                loading = false;
                yield break;
            }
            ZirconMapManifest manifest = definition.MapIndex == 5 || definition.MapIndex == 6
                ? CreateChunk(sourceManifest, centerX, centerY)
                : sourceManifest;
            string json = JsonUtility.ToJson(manifest);

            var tiles = new HashSet<string>();
            foreach (ZirconMapCellManifest cell in manifest.SampleCells)
            {
                if (cell.BackImage <= 0 ||
                    !ZirconMapDebugRenderer.TryGetMapLibraryFolder(cell.BackFile, out string folder))
                    continue;
                tiles.Add(folder + ":" + cell.BackImage);
            }

            string visualRoot = Path.Combine(Application.persistentDataPath, "Zircon", "RuntimeVisuals");
            string textureRoot = Path.Combine(visualRoot, "Generated", "Textures", "MapData");
            if (stagedFloorMapIndex != definition.MapIndex)
            {
                stagedFloorMapIndex = definition.MapIndex;
                stagedFloorTiles.Clear();
            }
            int copied = 0;
            int reused = 0;
            foreach (string tile in tiles)
            {
                string[] parts = tile.Split(':');
                string folder = parts[0];
                int index = int.Parse(parts[1]);
                string sourceName = ZirconMapDebugRenderer.GetMapLibrarySourceName(folder);
                string sourceFile = sourceName + "_" + index.ToString("D5") + "_image.png";
                string runtimeFile = folder + "_" + index.ToString("D5") + "_image.png";
                string source = "Generated/Textures/MapData/" +
                                definition.TextureSourceRoot + folder + "/" + sourceFile;
                string destination = Path.Combine(textureRoot, folder, runtimeFile);
                if (stagedFloorTiles.Contains(destination) && File.Exists(destination))
                {
                    reused++;
                    continue;
                }
                byte[] bytes = null;
                yield return ZirconAssetStore.LoadBytes(source,
                    value => bytes = value,
                    value => Debug.LogWarning("Production map tile missing map=" +
                                              definition.MapIndex + " file=" + sourceFile + ": " + value));
                if (bytes == null || bytes.Length == 0)
                    continue;
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.WriteAllBytes(destination, bytes);
                stagedFloorTiles.Add(destination);
                copied++;
            }

            string mapRoot = Path.Combine(visualRoot, "Generated", "Data", "Maps");
            Directory.CreateDirectory(mapRoot);
            string runtimeManifest = Path.Combine(mapRoot, definition.Manifest);
            File.WriteAllText(runtimeManifest, json);
            SetField(mapRenderer, "generatedTextureRoot", textureRoot);
            mapRenderer.ClearTileCache();
            // On first entry there is no old map to preserve, so spread texture
            // decoding and cell creation across frames. Chunk changes still use
            // the atomic renderer path to avoid exposing a partly rebuilt map.
            if (mapRenderer.Manifest == null)
                yield return mapRenderer.RenderManifestIncremental(runtimeManifest);
            else
                mapRenderer.RenderManifest(runtimeManifest);
            if (!mapRenderer.IsVisualBuildComplete)
            {
                Debug.LogError("Production map visuals failed map=" + definition.MapIndex);
                loading = false;
                yield break;
            }
            loadedMapIndex = definition.MapIndex;
            loadedMapWidth = sourceManifest.Width;
            loadedMapHeight = sourceManifest.Height;
            loadedView = new RectInt(manifest.ViewX, manifest.ViewY, manifest.ViewWidth, manifest.ViewHeight);
            loading = false;
            Debug.Log("P2-B production map ready: map=" + definition.MapIndex +
                      " source=" + manifest.Source +
                      " view=" + manifest.ViewX + "," + manifest.ViewY + "," +
                      manifest.ViewWidth + "," + manifest.ViewHeight +
                      " objectAnchors=" + (manifest.ObjectCells?.Count ?? manifest.SampleCells.Count) +
                      " floorTiles=" + copied + " loaded " + reused + " reused / " + tiles.Count +
                      " elapsed=" + (Time.realtimeSinceStartup - startedAt).ToString("F2") + "s");
        }

        private ZirconMapManifest CreateChunk(ZirconMapManifest source, int centerX, int centerY)
        {
            int effectiveChunkSize = chunkSize > 8 ? chunkSize : 96;
            int width = Mathf.Min(effectiveChunkSize, source.Width);
            int height = Mathf.Min(effectiveChunkSize, source.Height);
            int x = Mathf.Clamp(centerX - width / 2, 0, source.Width - width);
            int y = Mathf.Clamp(centerY - height / 2, 0, source.Height - height);
            var cells = new List<ZirconMapCellManifest>(width * height);
            var objectCells = new List<ZirconMapCellManifest>();
            int right = x + width;
            int bottom = y + height;
            int predicted = predictedStepAllowance > 0 ? predictedStepAllowance : 2;
            int anchorPadding = Mathf.Max(predicted,
                objectAnchorPaddingCells > 0 ? objectAnchorPaddingCells : 2);
            int heightPadding = Mathf.Max(23,
                objectHeightPaddingCells > 0 ? objectHeightPaddingCells : 24);
            int objectLeft = Mathf.Max(0, x - anchorPadding);
            int objectRight = Mathf.Min(source.Width, right + anchorPadding);
            int objectTop = Mathf.Max(0, y - anchorPadding);
            // Map object sprites use a bottom-left pivot. Existing production
            // assets reach 736px (23 map rows), so anchors below the logical
            // floor chunk must be retained for their upper pixels to survive.
            int objectBottom = Mathf.Min(source.Height, bottom + heightPadding);
            foreach (ZirconMapCellManifest cell in source.SampleCells)
            {
                if (cell.X >= x && cell.X < right && cell.Y >= y && cell.Y < bottom)
                    cells.Add(cell);
                if (cell.X >= objectLeft && cell.X < objectRight &&
                    cell.Y >= objectTop && cell.Y < objectBottom &&
                    (cell.MiddleImage > 0 || cell.FrontImage > 0))
                    objectCells.Add(cell);
            }
            return new ZirconMapManifest
            {
                Source = source.Source,
                Width = source.Width,
                Height = source.Height,
                BlockingCells = source.BlockingCells,
                NonEmptyCells = source.NonEmptyCells,
                ViewX = x,
                ViewY = y,
                ViewWidth = width,
                ViewHeight = height,
                SampleCells = cells,
                ObjectCells = objectCells,
            };
        }

        private static bool TryGetDefinition(int mapIndex, out MapDefinition definition)
        {
            switch (mapIndex)
            {
                case 1:
                    definition = new MapDefinition(1, "0.map.manifest.json", "Base_");
                    return true;
                case 5:
                    definition = new MapDefinition(5, "1.map.manifest.json", "Map5/");
                    return true;
                case 6:
                    definition = new MapDefinition(6, "2.map.manifest.json", "Map6/");
                    return true;
                default:
                    definition = default;
                    return false;
            }
        }

        private static void SetField(object target, string name, object value)
        {
            target?.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(target, value);
        }

    }
}
