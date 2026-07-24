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
        [SerializeField] private Vector2 tileSize = new Vector2(0.3f, 0.3f);
        [SerializeField] private float tilePixelsPerUnit = 150f;
        [SerializeField] private bool renderOnStart = true;
        [SerializeField] private int maxRenderedCells = 4096;
        [SerializeField] private bool showFallbackCells;
        [SerializeField] private float backgroundTileVerticalScale = 1.5f;

        private readonly List<SpriteRenderer> cells = new List<SpriteRenderer>();
        private readonly Dictionary<int, ZirconMapCellManifest> cellsByLocation = new Dictionary<int, ZirconMapCellManifest>();
        private readonly Dictionary<string, Sprite> tileSprites = new Dictionary<string, Sprite>();
        private Sprite cellSprite;

        public ZirconMapManifest Manifest { get; private set; }

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
            for (int i = 0; i < count; i++)
            {
                ZirconMapCellManifest cell = Manifest.SampleCells[i];
                cellsByLocation[GetKey(cell.X, cell.Y)] = cell;
                CreateCell(cell);
            }
        }

        public void Clear()
        {
            foreach (SpriteRenderer renderer in cells)
            {
                if (renderer != null)
                    Destroy(renderer.gameObject);
            }

            cells.Clear();
            cellsByLocation.Clear();
        }

        public bool TryGetCell(int x, int y, out ZirconMapCellManifest cell)
        {
            return cellsByLocation.TryGetValue(GetKey(x, y), out cell);
        }

        public bool IsBlocking(int x, int y)
        {
            return TryGetCell(x, y, out ZirconMapCellManifest cell) && cell.Blocking;
        }

        private void CreateCell(ZirconMapCellManifest cell)
        {
            Sprite tileSprite = GetBackTileSprite(cell);
            if (tileSprite == null && !showFallbackCells)
                return;

            var go = new GameObject($"ZirconMapCell_{cell.X}_{cell.Y}");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(cell.X * tileScale, -cell.Y * tileScale, 0.1f);

            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = tileSprite != null ? tileSprite : cellSprite;
            renderer.color = tileSprite != null ? Color.white : GetCellColour(cell);
            go.transform.localScale = tileSprite != null
                ? new Vector3(1f, backgroundTileVerticalScale, 1f)
                : new Vector3(tileSize.x, tileSize.y, 1f);
            renderer.sortingOrder = -10000 - cell.Y;
            cells.Add(renderer);
        }

        public void ClearTileCache()
        {
            foreach (Sprite sprite in tileSprites.Values)
            {
                if (sprite == null)
                    continue;
                Texture2D texture = sprite.texture;
                Destroy(sprite);
                if (texture != null)
                    Destroy(texture);
            }
            tileSprites.Clear();
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
            if (!File.Exists(file))
                return null;

            byte[] bytes = File.ReadAllBytes(file);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(bytes))
            {
                Destroy(texture);
                return null;
            }

            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.name = Path.GetFileNameWithoutExtension(file);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0f, 1f), tilePixelsPerUnit);
            sprite.name = texture.name;
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

        private static int GetKey(int x, int y)
        {
            unchecked
            {
                return (x * 397) ^ y;
            }
        }
    }
}
