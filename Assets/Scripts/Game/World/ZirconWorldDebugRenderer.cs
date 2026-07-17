using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Zircon.Mobile.Game.Entities;

namespace Zircon.Mobile.Game.World
{
    public sealed class ZirconWorldDebugRenderer : MonoBehaviour
    {
        [SerializeField] private float tileScale = 0.32f;
        [SerializeField] private float markerSize = 0.28f;
        [SerializeField] private bool useGeneratedSprites = true;
        [SerializeField] private string generatedTextureRoot = "Generated/Textures";
        [SerializeField] private float spritePixelsPerUnit = 100f;
        [SerializeField] private float spriteAnimationFps = 4f;

        private readonly Dictionary<uint, SpriteRenderer> markers = new Dictionary<uint, SpriteRenderer>();
        private readonly Dictionary<ZirconEntityKind, List<Sprite>> spritesByKind = new Dictionary<ZirconEntityKind, List<Sprite>>();
        private Sprite markerSprite;
        private bool generatedSpritesLoaded;
        private uint localPlayerObjectId;
        private bool hasLocalPlayerObjectId;
        private uint selectedObjectId;
        private bool hasSelectedObjectId;

        public float TileScale => tileScale;

        public void Render(ZirconWorldSnapshot snapshot)
        {
            if (snapshot == null)
                return;

            EnsureSprite();
            hasLocalPlayerObjectId = snapshot.LocalPlayer != null;
            localPlayerObjectId = hasLocalPlayerObjectId ? snapshot.LocalPlayer.ObjectId : 0u;

            var seen = new HashSet<uint>();
            foreach (ZirconEntityState entity in snapshot.Entities)
            {
                seen.Add(entity.ObjectId);
                SpriteRenderer renderer = GetOrCreate(entity.ObjectId);
                Sprite sprite = GetSprite(entity, snapshot);
                renderer.transform.localPosition = ToWorldPosition(entity.Location.X, entity.Location.Y);
                renderer.transform.localScale = sprite == markerSprite ? Vector3.one * markerSize : Vector3.one;
                renderer.sprite = sprite;
                renderer.color = sprite == markerSprite ? GetColour(entity, snapshot) : Color.white;
                if (hasSelectedObjectId && entity.ObjectId == selectedObjectId)
                {
                    renderer.color = new Color(1f, 0.82f, 0.2f, 1f);
                    renderer.transform.localScale *= 1.12f;
                }
                renderer.sortingOrder = -entity.Location.Y;
                renderer.name = $"ZirconEntity_{entity.Kind}_{entity.ObjectId}";
            }

            var remove = new List<uint>();
            foreach (uint objectId in markers.Keys)
            {
                if (!seen.Contains(objectId))
                    remove.Add(objectId);
            }

            foreach (uint objectId in remove)
            {
                if (markers.TryGetValue(objectId, out SpriteRenderer renderer) && renderer != null)
                    Destroy(renderer.gameObject);

                markers.Remove(objectId);
            }
        }

        public void Clear()
        {
            foreach (SpriteRenderer renderer in markers.Values)
            {
                if (renderer != null)
                    Destroy(renderer.gameObject);
            }

            markers.Clear();
            hasLocalPlayerObjectId = false;
            hasSelectedObjectId = false;
        }

        public void SetSelectedObject(uint objectId)
        {
            selectedObjectId = objectId;
            hasSelectedObjectId = true;
        }

        public void ClearSelectedObject()
        {
            hasSelectedObjectId = false;
        }

        public bool TryGetEntityWorldPosition(uint objectId, out Vector3 position)
        {
            if (markers.TryGetValue(objectId, out SpriteRenderer renderer) && renderer != null)
            {
                position = renderer.transform.position;
                return true;
            }

            position = Vector3.zero;
            return false;
        }

        public bool TryGetLocalPlayerWorldPosition(out Vector3 position)
        {
            if (hasLocalPlayerObjectId)
                return TryGetEntityWorldPosition(localPlayerObjectId, out position);

            position = Vector3.zero;
            return false;
        }

        private SpriteRenderer GetOrCreate(uint objectId)
        {
            if (markers.TryGetValue(objectId, out SpriteRenderer renderer) && renderer != null)
                return renderer;

            var marker = new GameObject($"ZirconEntity_{objectId}");
            marker.transform.SetParent(transform, false);
            renderer = marker.AddComponent<SpriteRenderer>();
            renderer.sprite = markerSprite;
            markers[objectId] = renderer;
            return renderer;
        }

        private void EnsureSprite()
        {
            if (markerSprite != null)
                return;

            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply(false, true);
            markerSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }

        private Sprite GetSprite(ZirconEntityState entity, ZirconWorldSnapshot snapshot)
        {
            LoadGeneratedSprites();

            ZirconEntityKind kind = entity.Kind;
            if (snapshot.LocalPlayer != null && entity.ObjectId == snapshot.LocalPlayer.ObjectId)
                kind = ZirconEntityKind.Player;

            if (!spritesByKind.TryGetValue(kind, out List<Sprite> sprites) || sprites.Count == 0)
                return markerSprite;

            int frame = Mathf.FloorToInt(Time.time * spriteAnimationFps);
            int objectOffset = (int)(entity.ObjectId % 2147483647u);
            int index = Mathf.Abs(frame + objectOffset) % sprites.Count;
            return sprites[index];
        }

        private void LoadGeneratedSprites()
        {
            EnsureSprite();

            if (generatedSpritesLoaded)
                return;

            generatedSpritesLoaded = true;
            if (!useGeneratedSprites)
                return;

            string normalizedRoot = generatedTextureRoot.Replace('/', Path.DirectorySeparatorChar);
            string root = Path.Combine(Application.dataPath, normalizedRoot);
            LoadSpriteSequence(root, "M-Hum", ZirconEntityKind.Player);
            LoadSpriteSequence(root, "Mon-1", ZirconEntityKind.Monster);
            LoadSpriteSequence(root, "NPC", ZirconEntityKind.Npc);
            LoadSpriteSequence(root, "MIcon", ZirconEntityKind.Spell);
            LoadSpriteSequence(root, "Items", ZirconEntityKind.Item);
        }

        private void LoadSpriteSequence(string root, string folder, ZirconEntityKind kind)
        {
            string directory = Path.Combine(root, folder);
            if (!Directory.Exists(directory))
                return;

            string[] files = Directory.GetFiles(directory, "*_image.png");
            System.Array.Sort(files, System.StringComparer.OrdinalIgnoreCase);

            var sprites = new List<Sprite>();
            foreach (string file in files)
            {
                byte[] bytes = File.ReadAllBytes(file);
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!texture.LoadImage(bytes))
                {
                    Destroy(texture);
                    continue;
                }

                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;
                string fileName = Path.GetFileNameWithoutExtension(file);
                texture.name = fileName;
                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0f), spritePixelsPerUnit);
                sprite.name = fileName;
                sprites.Add(sprite);
            }

            if (sprites.Count > 0)
                spritesByKind[kind] = sprites;
        }

        private Vector3 ToWorldPosition(int x, int y)
        {
            return new Vector3(x * tileScale, -y * tileScale, 0f);
        }

        private static Color GetColour(ZirconEntityState entity, ZirconWorldSnapshot snapshot)
        {
            if (snapshot.LocalPlayer != null && entity.ObjectId == snapshot.LocalPlayer.ObjectId)
                return new Color(0.1f, 0.9f, 0.35f, 1f);

            switch (entity.Kind)
            {
                case ZirconEntityKind.Player:
                    return new Color(0.2f, 0.75f, 1f, 1f);
                case ZirconEntityKind.Monster:
                    return entity.Dead ? new Color(0.45f, 0.15f, 0.15f, 0.75f) : new Color(1f, 0.25f, 0.2f, 1f);
                case ZirconEntityKind.Npc:
                    return new Color(1f, 0.85f, 0.15f, 1f);
                case ZirconEntityKind.Spell:
                    return new Color(0.9f, 0.35f, 1f, 1f);
                case ZirconEntityKind.Item:
                    return new Color(0.25f, 0.95f, 0.8f, 1f);
                default:
                    return new Color(0.75f, 0.75f, 0.75f, 1f);
            }
        }
    }
}

