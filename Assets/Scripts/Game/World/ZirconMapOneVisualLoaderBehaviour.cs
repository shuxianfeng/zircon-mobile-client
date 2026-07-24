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
        [SerializeField] private int chunkSize = 96;
        [SerializeField] private int reloadMargin = 24;
        private bool loading;
        private int loadedMapIndex = -1;
        private int loadedMapWidth;
        private int loadedMapHeight;
        private RectInt loadedView;
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
            return (loadedView.x > 0 && snapshot.Location.X < loadedView.x + reloadMargin) ||
                   (right < loadedMapWidth && snapshot.Location.X >= right - reloadMargin) ||
                   (loadedView.y > 0 && snapshot.Location.Y < loadedView.y + reloadMargin) ||
                   (bottom < loadedMapHeight && snapshot.Location.Y >= bottom - reloadMargin);
        }

        private IEnumerator LoadMap(MapDefinition definition, int centerX, int centerY)
        {
            loading = true;
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
            int copied = 0;
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
                byte[] bytes = null;
                yield return ZirconAssetStore.LoadBytes(source,
                    value => bytes = value,
                    value => Debug.LogWarning("Production map tile missing map=" +
                                              definition.MapIndex + " file=" + sourceFile + ": " + value));
                if (bytes == null || bytes.Length == 0)
                    continue;
                string destination = Path.Combine(textureRoot, folder, runtimeFile);
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.WriteAllBytes(destination, bytes);
                copied++;
            }

            string mapRoot = Path.Combine(visualRoot, "Generated", "Data", "Maps");
            Directory.CreateDirectory(mapRoot);
            string runtimeManifest = Path.Combine(mapRoot, definition.Manifest);
            File.WriteAllText(runtimeManifest, json);
            SetField(mapRenderer, "generatedTextureRoot", textureRoot);
            mapRenderer.ClearTileCache();
            mapRenderer.RenderManifest(runtimeManifest);
            loadedMapIndex = definition.MapIndex;
            loadedMapWidth = sourceManifest.Width;
            loadedMapHeight = sourceManifest.Height;
            loadedView = new RectInt(manifest.ViewX, manifest.ViewY, manifest.ViewWidth, manifest.ViewHeight);
            loading = false;
            Debug.Log("P2-B production map ready: map=" + definition.MapIndex +
                      " source=" + manifest.Source +
                      " view=" + manifest.ViewX + "," + manifest.ViewY + "," +
                      manifest.ViewWidth + "," + manifest.ViewHeight +
                      " floorTiles=" + copied + "/" + tiles.Count);
        }

        private ZirconMapManifest CreateChunk(ZirconMapManifest source, int centerX, int centerY)
        {
            int width = Mathf.Min(chunkSize, source.Width);
            int height = Mathf.Min(chunkSize, source.Height);
            int x = Mathf.Clamp(centerX - width / 2, 0, source.Width - width);
            int y = Mathf.Clamp(centerY - height / 2, 0, source.Height - height);
            var cells = new List<ZirconMapCellManifest>(width * height);
            int right = x + width;
            int bottom = y + height;
            foreach (ZirconMapCellManifest cell in source.SampleCells)
                if (cell.X >= x && cell.X < right && cell.Y >= y && cell.Y < bottom)
                    cells.Add(cell);
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
