using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Zircon.Mobile.Game.World
{
    public sealed class ZirconMapDebugRenderer : MonoBehaviour
    {
        [SerializeField] private string generatedMapRoot = "Generated/Data/Maps";
        [SerializeField] private string generatedTextureRoot = "Generated/Textures/MapData";
        [SerializeField] private string manifestFileName = "0.map.manifest.json";
        [SerializeField] private float tileScale = 0.32f;
        [SerializeField] private float tileHeightRatio = 2f / 3f;
        [SerializeField] private Vector2 tileSize = new Vector2(0.3f, 0.3f);
        [SerializeField] private float tilePixelsPerUnit = 150f;
        [SerializeField] private bool renderOnStart = true;
        [SerializeField] private int maxRenderedCells = 4096;
        [SerializeField] private bool showFallbackCells;
        [SerializeField] private float backgroundTileVerticalScale = 1f;

        private readonly List<MapCellVisual> cells = new List<MapCellVisual>();
        private readonly Dictionary<long, ZirconMapCellManifest> cellsByLocation = new Dictionary<long, ZirconMapCellManifest>();
        private readonly Dictionary<long, MapCellVisual> cellVisualsByLocation = new Dictionary<long, MapCellVisual>();
        private readonly Dictionary<string, Sprite> tileSprites = new Dictionary<string, Sprite>();
        private readonly HashSet<Texture2D> ownedTileTextures = new HashSet<Texture2D>();
        private Sprite cellSprite;
        private bool hasVisibleCellBounds;
        private RectInt visibleCellBounds;

        private sealed class MapCellVisual
        {
            public SpriteRenderer Renderer;
            public int X;
            public int Y;
        }

        public ZirconMapManifest Manifest { get; private set; }
        public bool IsVisualBuildComplete { get; private set; }

        private void Start()
        {
            if (renderOnStart)
                RenderConfiguredMap();
        }

        public void RenderConfiguredMap()
        {
            string path = Path.Combine(Application.dataPath, generatedMapRoot.Replace('/', Path.DirectorySeparatorChar), manifestFileName);
            RenderManifest(path);
        }

        public void RenderManifest(string manifestPath)
        {
            IsVisualBuildComplete = false;
            Clear();
            EnsureSprite();

            if (!File.Exists(manifestPath))
            {
                Debug.LogWarning($"Map manifest not found: {manifestPath}", this);
                return;
            }

            string json = File.ReadAllText(manifestPath);
            Manifest = JsonUtility.FromJson<ZirconMapManifest>(json);
            if (Manifest == null || Manifest.SampleCells == null)
                return;

            int count = maxRenderedCells > 0
                ? Mathf.Min(maxRenderedCells, Manifest.SampleCells.Count)
                : Manifest.SampleCells.Count;
            if (tileSprites.Count == 0)
                BuildTileAtlas(Manifest.SampleCells, count);
            for (int i = 0; i < count; i++)
            {
                ZirconMapCellManifest cell = Manifest.SampleCells[i];
                cellsByLocation[GetKey(cell.X, cell.Y)] = cell;
                CreateCell(cell);
            }
            IsVisualBuildComplete = true;
        }

        public IEnumerator RenderManifestIncremental(string manifestPath)
        {
            IsVisualBuildComplete = false;
            Clear();
            EnsureSprite();
            if (!File.Exists(manifestPath))
            {
                Debug.LogWarning($"Map manifest not found: {manifestPath}", this);
                yield break;
            }

            Manifest = JsonUtility.FromJson<ZirconMapManifest>(File.ReadAllText(manifestPath));
            if (Manifest?.SampleCells == null)
                yield break;

            int count = maxRenderedCells > 0
                ? Mathf.Min(maxRenderedCells, Manifest.SampleCells.Count)
                : Manifest.SampleCells.Count;
            // Collision must be complete before any visual batching yields.
            for (int i = 0; i < count; i++)
            {
                ZirconMapCellManifest cell = Manifest.SampleCells[i];
                cellsByLocation[GetKey(cell.X, cell.Y)] = cell;
            }

            if (tileSprites.Count == 0)
                yield return BuildTileAtlasIncremental(Manifest.SampleCells, count, 12);

            const int cellsPerFrame = 96;
            for (int i = 0; i < count; i++)
            {
                CreateCell(Manifest.SampleCells[i]);
                if ((i + 1) % cellsPerFrame == 0)
                    yield return null;
            }
            IsVisualBuildComplete = true;
        }

        public void Clear()
        {
            IsVisualBuildComplete = false;
            foreach (MapCellVisual visual in cells)
            {
                if (visual?.Renderer != null)
                    Destroy(visual.Renderer.gameObject);
            }

            cells.Clear();
            cellsByLocation.Clear();
            cellVisualsByLocation.Clear();
        }

        public bool TryGetCell(int x, int y, out ZirconMapCellManifest cell)
        {
            return cellsByLocation.TryGetValue(GetKey(x, y), out cell);
        }

        public bool IsBlocking(int x, int y)
        {
            if (Manifest == null)
                return false;
            if (x < 0 || y < 0 || x >= Manifest.Width || y >= Manifest.Height)
                return true;
            // Production maps 5/6 are streamed as moving chunks. A cell that is
            // temporarily outside the loaded chunk must not be treated as open,
            // otherwise local prediction runs into it and the server snaps the
            // player back after rejecting the move.
            return !TryGetCell(x, y, out ZirconMapCellManifest cell) || cell.Blocking;
        }

        public void SetVisibleCellBounds(RectInt bounds)
        {
            bool hadVisibleCellBounds = hasVisibleCellBounds;
            RectInt previousBounds = visibleCellBounds;
            hasVisibleCellBounds = true;
            visibleCellBounds = bounds;

            if (!hadVisibleCellBounds)
            {
                foreach (MapCellVisual visual in cells)
                    SetCellVisibility(visual, IsInsideVisibleBounds(visual.X, visual.Y));
                return;
            }

            if (HaveSameBounds(previousBounds, bounds))
                return;

            // Only touch cells in the old/new edge strips. Scanning every renderer
            // each time the player crosses a grid cell creates a visible CPU spike.
            SetCellsInDifference(previousBounds, bounds, false);
            SetCellsInDifference(bounds, previousBounds, true);
        }

        private void SetCellsInDifference(RectInt candidate, RectInt overlap, bool visible)
        {
            for (int x = candidate.xMin; x < candidate.xMax; x++)
            {
                if (x < overlap.xMin || x >= overlap.xMax)
                {
                    SetCellRange(x, candidate.yMin, candidate.yMax, visible);
                    continue;
                }

                SetCellRange(x, candidate.yMin, Mathf.Min(candidate.yMax, overlap.yMin), visible);
                SetCellRange(x, Mathf.Max(candidate.yMin, overlap.yMax), candidate.yMax, visible);
            }
        }

        private void SetCellRange(int x, int yMin, int yMax, bool visible)
        {
            for (int y = yMin; y < yMax; y++)
            {
                if (cellVisualsByLocation.TryGetValue(GetKey(x, y), out MapCellVisual visual))
                    SetCellVisibility(visual, visible);
            }
        }

        private static void SetCellVisibility(MapCellVisual visual, bool visible)
        {
            if (visual?.Renderer != null && visual.Renderer.enabled != visible)
                visual.Renderer.enabled = visible;
        }

        private static bool HaveSameBounds(RectInt left, RectInt right)
        {
            return left.xMin == right.xMin && left.xMax == right.xMax &&
                   left.yMin == right.yMin && left.yMax == right.yMax;
        }

        private void CreateCell(ZirconMapCellManifest cell)
        {
            Sprite tileSprite = GetBackTileSprite(cell);
            if (tileSprite == null && !showFallbackCells)
                return;

            var go = new GameObject($"ZirconMapCell_{cell.X}_{cell.Y}");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(
                cell.X * tileScale,
                -cell.Y * tileScale * tileHeightRatio,
                0.1f);

            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = tileSprite != null ? tileSprite : cellSprite;
            renderer.color = tileSprite != null ? Color.white : GetCellColour(cell);
            go.transform.localScale = tileSprite != null
                ? new Vector3(1f, backgroundTileVerticalScale, 1f)
                : new Vector3(tileSize.x, tileSize.y, 1f);
            renderer.sortingOrder = -10000 - cell.Y;
            renderer.enabled = IsInsideVisibleBounds(cell.X, cell.Y);
            var visual = new MapCellVisual { Renderer = renderer, X = cell.X, Y = cell.Y };
            cells.Add(visual);
            cellVisualsByLocation[GetKey(cell.X, cell.Y)] = visual;
        }

        private bool IsInsideVisibleBounds(int x, int y)
        {
            return !hasVisibleCellBounds ||
                   (x >= visibleCellBounds.xMin && x < visibleCellBounds.xMax &&
                    y >= visibleCellBounds.yMin && y < visibleCellBounds.yMax);
        }

        public void ClearTileCache()
        {
            foreach (Sprite sprite in tileSprites.Values)
            {
                if (sprite == null)
                    continue;
                Destroy(sprite);
            }
            tileSprites.Clear();
            foreach (Texture2D texture in ownedTileTextures)
                if (texture != null)
                    Destroy(texture);
            ownedTileTextures.Clear();
        }

        private void BuildTileAtlas(IReadOnlyList<ZirconMapCellManifest> mapCells, int count)
        {
            IEnumerator routine = BuildTileAtlasIncremental(mapCells, count, int.MaxValue);
            while (routine.MoveNext()) { }
        }

        private IEnumerator BuildTileAtlasIncremental(
            IReadOnlyList<ZirconMapCellManifest> mapCells, int count, int tilesPerFrame)
        {
            var keys = new List<string>();
            var textures = new List<Texture2D>();
            var seen = new HashSet<string>();
            int loadedSinceYield = 0;
            string root = Path.Combine(Application.dataPath,
                generatedTextureRoot.Replace('/', Path.DirectorySeparatorChar));
            for (int i = 0; i < count; i++)
            {
                ZirconMapCellManifest cell = mapCells[i];
                if (!TryGetMapLibraryFolder(cell.BackFile, out string folder) || cell.BackImage <= 0)
                    continue;
                string key = $"{folder}:{cell.BackImage}";
                if (!seen.Add(key))
                    continue;
                string file = Path.Combine(root, folder,
                    $"{folder}_{cell.BackImage:D5}_image.png");
                Texture2D texture = LoadTexture(file);
                if (texture == null)
                    continue;
                keys.Add(key);
                textures.Add(texture);
                if (++loadedSinceYield >= tilesPerFrame)
                {
                    loadedSinceYield = 0;
                    yield return null;
                }
            }

            if (textures.Count == 0)
                yield break;
            if (!TryPack(textures, 2048, out Texture2D atlas, out Rect[] rects) &&
                !TryPack(textures, 4096, out atlas, out rects))
            {
                for (int i = 0; i < textures.Count; i++)
                {
                    Texture2D texture = textures[i];
                    ownedTileTextures.Add(texture);
                    tileSprites[keys[i]] = CreateTileSprite(texture,
                        new Rect(0f, 0f, texture.width, texture.height), keys[i]);
                }
                yield break;
            }

            atlas.name = "ZirconMapFloorAtlas";
            atlas.filterMode = FilterMode.Point;
            atlas.wrapMode = TextureWrapMode.Clamp;
            ownedTileTextures.Add(atlas);
            for (int i = 0; i < textures.Count; i++)
            {
                Rect packed = rects[i];
                Rect pixels = new Rect(
                    Mathf.Round(packed.x * atlas.width),
                    Mathf.Round(packed.y * atlas.height),
                    textures[i].width,
                    textures[i].height);
                tileSprites[keys[i]] = CreateTileSprite(atlas, pixels, keys[i]);
                Destroy(textures[i]);
            }
            atlas.Apply(false, true);
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
            catch (System.Exception exception)
            {
                Debug.LogWarning("Map floor atlas packing failed at " + maximumSize +
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

        private Texture2D LoadTexture(string file)
        {
            if (!File.Exists(file))
                return null;
            byte[] bytes = File.ReadAllBytes(file);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (texture.LoadImage(bytes))
            {
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.name = Path.GetFileNameWithoutExtension(file);
                return texture;
            }
            Destroy(texture);
            return null;
        }

        private Sprite CreateTileSprite(Texture2D texture, Rect rect, string name)
        {
            Sprite sprite = Sprite.Create(texture, rect, new Vector2(0f, 1f), tilePixelsPerUnit);
            sprite.name = name;
            return sprite;
        }

        private Sprite GetBackTileSprite(ZirconMapCellManifest cell)
        {
            if (!TryGetMapLibraryFolder(cell.BackFile, out string folder) || cell.BackImage <= 0)
                return null;

            string key = $"{folder}:{cell.BackImage}";
            if (tileSprites.TryGetValue(key, out Sprite cached))
                return cached;

            string root = Path.Combine(Application.dataPath, generatedTextureRoot.Replace('/', Path.DirectorySeparatorChar));
            string file = Path.Combine(root, folder, $"{folder}_{cell.BackImage:D5}_image.png");
            Texture2D texture = LoadTexture(file);
            if (texture == null)
                return null;
            ownedTileTextures.Add(texture);
            Sprite sprite = CreateTileSprite(texture,
                new Rect(0, 0, texture.width, texture.height), texture.name);
            tileSprites[key] = sprite;
            return sprite;
        }

        private void EnsureSprite()
        {
            if (cellSprite != null)
                return;

            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply(false, true);
            cellSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }

        public static bool TryGetMapLibraryFolder(int mapFile, out string folder)
        {
            switch (mapFile)
            {
                case 0:
                    folder = "Tilesc";
                    return true;
                case 1:
                    folder = "Tiles30c";
                    return true;
                case 2:
                    folder = "Tiles5c";
                    return true;
                case 3:
                    folder = "SmTilesc";
                    return true;
                case 4:
                    folder = "Housesc";
                    return true;
                case 5:
                    folder = "Cliffsc";
                    return true;
                case 6:
                    folder = "Dungeonsc";
                    return true;
                case 7:
                    folder = "Innersc";
                    return true;
                case 8:
                    folder = "Furnituresc";
                    return true;
                case 9:
                    folder = "Wallsc";
                    return true;
                case 10:
                    folder = "SmObjectsc";
                    return true;
                case 11:
                    folder = "Animationsc";
                    return true;
                case 12:
                    folder = "Object1c";
                    return true;
                case 13:
                    folder = "Object2c";
                    return true;
                case 15:
                    folder = "Wood_Tilesc";
                    return true;
                case 16:
                    folder = "Wood_Tiles30c";
                    return true;
                case 17:
                    folder = "Wood_Tiles5c";
                    return true;
                case 18:
                    folder = "Wood_SmTilesc";
                    return true;
                case 19:
                    folder = "Wood_Housesc";
                    return true;
                case 20:
                    folder = "Wood_Cliffsc";
                    return true;
                case 21:
                    folder = "Wood_Dungeonsc";
                    return true;
                case 22:
                    folder = "Wood_Innersc";
                    return true;
                case 23:
                    folder = "Wood_Furnituresc";
                    return true;
                case 24:
                    folder = "Wood_Wallsc";
                    return true;
                case 25:
                    folder = "Wood_SmObjectsc";
                    return true;
                case 26:
                    folder = "Wood_Animationsc";
                    return true;
                case 40:
                    folder = "Forest_SmObjectsc";
                    return true;
                default:
                    folder = null;
                    return false;
            }
        }

        public static string GetMapLibrarySourceName(string folder)
        {
            if (string.IsNullOrEmpty(folder))
                return folder;
            int separator = folder.IndexOf('_');
            return separator >= 0 ? folder.Substring(separator + 1) : folder;
        }

        private static Color GetCellColour(ZirconMapCellManifest cell)
        {
            return cell.Blocking ? new Color(1f, 0.2f, 0.15f, 0.45f) : new Color(0.15f, 0.75f, 1f, 0.35f);
        }

        private static long GetKey(int x, int y)
        {
            unchecked
            {
                return ((long)x << 32) ^ (uint)y;
            }
        }
    }
}
