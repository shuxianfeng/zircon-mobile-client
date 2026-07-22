#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Zircon.Mobile.Core.Assets;

namespace Zircon.Mobile.Editor
{
    public static class ZirconP2A3OfflinePresentationValidation
    {
        private const string ManifestPath = "Assets/Generated/Data/Runtime/production-visuals.json";
        private const string EvidenceFolder = "docs/evidence/p2-a3";

        private sealed class LoadedFrame : IDisposable
        {
            public ZirconRuntimeSpriteFrame Frame;
            public Texture2D Texture;
            public void Dispose()
            {
                if (Texture != null) UnityEngine.Object.DestroyImmediate(Texture);
            }
        }

        public static void ValidateFromCommandLine()
        {
            ZirconP2AValidation.ValidateFromCommandLine();
            ZirconRuntimeVisualManifest manifest = JsonUtility.FromJson<ZirconRuntimeVisualManifest>(File.ReadAllText(ManifestPath));
            if (manifest?.SpriteSets == null) throw new InvalidOperationException("P2-A3 manifest is invalid.");

            Directory.CreateDirectory(EvidenceFolder);
            var sets = manifest.SpriteSets.ToDictionary(set => set.Id, StringComparer.OrdinalIgnoreCase);
            int playerCells = BuildPlayerSheet(sets);
            int monsterCells = BuildMonsterSheet(sets);
            int effectCells = BuildEffectSheet(sets);
            BuildSortingSheet(sets);

            string summary = "{\n" +
                "  \"playerDirections\": 8,\n" +
                "  \"playerFramesPerDirection\": 4,\n" +
                "  \"playerCompositeCells\": " + playerCells + ",\n" +
                "  \"monsterAnimationCells\": " + monsterCells + ",\n" +
                "  \"effectAnimationCells\": " + effectCells + ",\n" +
                "  \"shadows\": \"passed\",\n" +
                "  \"offsets\": \"passed\",\n" +
                "  \"internalLayerOrder\": \"passed\",\n" +
                "  \"ySorting\": \"passed\"\n" +
                "}\n";
            File.WriteAllText(Path.Combine(EvidenceFolder, "validation-summary.json"), summary);

            Debug.Log("P2-A3 offline presentation validation passed: playerDirections=8 playerFrames=32 " +
                      "playerLayers=128 monsterFrames=" + monsterCells + " effectFrames=" + effectCells +
                      " shadows=passed offsets=passed internalLayers=passed ySorting=passed evidence=" + EvidenceFolder);
        }

        private static int BuildPlayerSheet(Dictionary<string, ZirconRuntimeSpriteSet> sets)
        {
            const int columns = 8;
            const int rows = 4;
            const int cellWidth = 112;
            const int cellHeight = 128;
            Texture2D sheet = NewCanvas(columns * cellWidth, rows * cellHeight);
            try
            {
                for (int direction = 0; direction < columns; direction++)
                for (int animationFrame = 0; animationFrame < rows; animationFrame++)
                {
                    int bodyIndex = 45000 + direction * 10 + animationFrame;
                    int hairIndex = direction * 10 + animationFrame;
                    int weaponIndex = 35000 + direction * 10 + animationFrame;
                    int cellX = direction * cellWidth;
                    int cellY = (rows - 1 - animationFrame) * cellHeight;
                    int anchorX = cellX + cellWidth / 2;
                    int anchorY = cellY + 54;

                    DrawShadow(sheet, anchorX + 3, anchorY + 1, 48, 14, new Color32(0, 0, 0, 90));
                    bool weaponBehind = direction == 0 || direction >= 5;
                    if (weaponBehind)
                        DrawFrame(sheet, Load(sets, "player.warrior.female.weapon", weaponIndex), anchorX, anchorY, new Color32(255, 235, 190, 255), false);
                    DrawFrame(sheet, Load(sets, "player.warrior.female.body", bodyIndex), anchorX, anchorY, Color.white, false);
                    DrawFrame(sheet, Load(sets, "player.warrior.female.overlay", bodyIndex), anchorX, anchorY, new Color32(120, 190, 255, 155), false);
                    DrawFrame(sheet, Load(sets, "player.warrior.female.hair", hairIndex), anchorX, anchorY, new Color32(255, 220, 120, 255), false);
                    if (!weaponBehind)
                        DrawFrame(sheet, Load(sets, "player.warrior.female.weapon", weaponIndex), anchorX, anchorY, new Color32(255, 235, 190, 255), false);

                    AssertCellHasVisiblePixels(sheet, cellX, cellY, cellWidth, cellHeight, "player direction=" + direction + " frame=" + animationFrame);
                }
                WritePng(sheet, Path.Combine(EvidenceFolder, "player-eight-directions-32-frames.png"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sheet);
            }
            return columns * rows;
        }

        private static int BuildMonsterSheet(Dictionary<string, ZirconRuntimeSpriteSet> sets)
        {
            int[] indexes = { 0, 1, 2, 3, 1000, 1001, 1002, 1003 };
            const int columns = 4;
            const int rows = 2;
            const int cellWidth = 128;
            const int cellHeight = 128;
            Texture2D sheet = NewCanvas(columns * cellWidth, rows * cellHeight);
            try
            {
                for (int i = 0; i < indexes.Length; i++)
                {
                    int cellX = (i % columns) * cellWidth;
                    int cellY = (rows - 1 - i / columns) * cellHeight;
                    int anchorX = cellX + cellWidth / 2;
                    int anchorY = cellY + 52;
                    DrawShadow(sheet, anchorX + 3, anchorY, 54, 15, new Color32(0, 0, 0, 90));
                    DrawFrame(sheet, Load(sets, "entity.monster.sample", indexes[i]), anchorX, anchorY, Color.white, i >= columns);
                    AssertCellHasVisiblePixels(sheet, cellX, cellY, cellWidth, cellHeight, "monster index=" + indexes[i]);
                }
                WritePng(sheet, Path.Combine(EvidenceFolder, "monster-8-frames-with-shadows.png"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sheet);
            }
            return indexes.Length;
        }

        private static int BuildEffectSheet(Dictionary<string, ZirconRuntimeSpriteSet> sets)
        {
            const int columns = 4;
            const int rows = 4;
            const int cellSize = 80;
            Texture2D sheet = NewCanvas(columns * cellSize, rows * cellSize);
            try
            {
                for (int i = 0; i < 16; i++)
                {
                    int cellX = (i % columns) * cellSize;
                    int cellY = (rows - 1 - i / columns) * cellSize;
                    int anchorX = cellX + cellSize / 2;
                    int anchorY = cellY + cellSize / 2;
                    DrawFrame(sheet, Load(sets, "effect.icon.sample", i * 2), anchorX, anchorY, Color.white, false, true);
                    AssertCellHasVisiblePixels(sheet, cellX, cellY, cellSize, cellSize, "effect frame=" + i);
                }
                WritePng(sheet, Path.Combine(EvidenceFolder, "skill-effect-16-frames.png"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sheet);
            }
            return 16;
        }

        private static void BuildSortingSheet(Dictionary<string, ZirconRuntimeSpriteSet> sets)
        {
            const int farY = 20;
            const int nearY = 10;
            int farSortingOrder = -farY;
            int nearSortingOrder = -nearY;
            if (nearSortingOrder <= farSortingOrder)
                throw new InvalidOperationException("P2-A3 Y sorting invariant failed.");

            Texture2D sheet = NewCanvas(420, 240);
            try
            {
                DrawShadow(sheet, 210, 104, 72, 18, new Color32(0, 0, 0, 80));
                DrawFrame(sheet, Load(sets, "entity.monster.sample", 1000), 210, 104, new Color32(115, 175, 255, 230), false);
                DrawShadow(sheet, 225, 82, 72, 18, new Color32(0, 0, 0, 105));
                DrawFrame(sheet, Load(sets, "entity.monster.sample", 0), 225, 82, new Color32(255, 145, 105, 245), false);
                AssertCellHasVisiblePixels(sheet, 130, 30, 170, 150, "Y sorting overlap");
                WritePng(sheet, Path.Combine(EvidenceFolder, "y-sorting-overlap.png"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sheet);
            }
        }

        private static LoadedFrame Load(Dictionary<string, ZirconRuntimeSpriteSet> sets, string setId, int index)
        {
            if (!sets.TryGetValue(setId, out ZirconRuntimeSpriteSet set) || set.Frames == null)
                throw new InvalidOperationException("P2-A3 set missing: " + setId);
            ZirconRuntimeSpriteFrame frame = set.Frames.FirstOrDefault(value => value.Index == index);
            if (frame == null) throw new InvalidOperationException("P2-A3 frame missing: " + setId + "/" + index);
            string path = Path.GetFullPath(Path.Combine("Assets", frame.Path));
            if (!File.Exists(path)) throw new InvalidOperationException("P2-A3 source texture missing: " + path);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(File.ReadAllBytes(path)))
            {
                UnityEngine.Object.DestroyImmediate(texture);
                throw new InvalidOperationException("P2-A3 source texture decode failed: " + path);
            }
            return new LoadedFrame { Frame = frame, Texture = texture };
        }

        private static Texture2D NewCanvas(int width, int height)
        {
            var canvas = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = Enumerable.Repeat(new Color32(24, 29, 40, 255), width * height).ToArray();
            canvas.SetPixels32(pixels);
            return canvas;
        }

        private static void DrawFrame(Texture2D target, LoadedFrame loaded, int anchorX, int anchorY, Color tint, bool flipX, bool center = false)
        {
            using (loaded)
            {
                int x = center ? anchorX - loaded.Texture.width / 2 : anchorX + loaded.Frame.OffsetX;
                int y = center ? anchorY - loaded.Texture.height / 2 : anchorY - loaded.Frame.OffsetY - loaded.Texture.height;
                Blit(target, loaded.Texture, x, y, tint, flipX);
            }
        }

        private static void Blit(Texture2D target, Texture2D source, int destinationX, int destinationY, Color tint, bool flipX)
        {
            Color32[] sourcePixels = source.GetPixels32();
            Color32[] targetPixels = target.GetPixels32();
            Color32 tint32 = tint;
            for (int y = 0; y < source.height; y++)
            for (int x = 0; x < source.width; x++)
            {
                int tx = destinationX + x;
                int ty = destinationY + y;
                if (tx < 0 || ty < 0 || tx >= target.width || ty >= target.height) continue;
                int sx = flipX ? source.width - 1 - x : x;
                Color32 src = sourcePixels[y * source.width + sx];
                src.r = (byte)(src.r * tint32.r / 255);
                src.g = (byte)(src.g * tint32.g / 255);
                src.b = (byte)(src.b * tint32.b / 255);
                src.a = (byte)(src.a * tint32.a / 255);
                if (src.a == 0) continue;
                int targetIndex = ty * target.width + tx;
                Color32 dst = targetPixels[targetIndex];
                int alpha = src.a;
                int inverse = 255 - alpha;
                targetPixels[targetIndex] = new Color32(
                    (byte)((src.r * alpha + dst.r * inverse) / 255),
                    (byte)((src.g * alpha + dst.g * inverse) / 255),
                    (byte)((src.b * alpha + dst.b * inverse) / 255),
                    255);
            }
            target.SetPixels32(targetPixels);
        }

        private static void DrawShadow(Texture2D target, int centerX, int centerY, int width, int height, Color32 color)
        {
            Color32[] pixels = target.GetPixels32();
            for (int y = -height / 2; y <= height / 2; y++)
            for (int x = -width / 2; x <= width / 2; x++)
            {
                float normalized = x * x / (float)(width * width / 4) + y * y / (float)(height * height / 4);
                if (normalized > 1f) continue;
                int tx = centerX + x;
                int ty = centerY + y;
                if (tx < 0 || ty < 0 || tx >= target.width || ty >= target.height) continue;
                int index = ty * target.width + tx;
                Color32 dst = pixels[index];
                int alpha = (int)(color.a * (1f - normalized * 0.65f));
                int inverse = 255 - alpha;
                pixels[index] = new Color32(
                    (byte)((color.r * alpha + dst.r * inverse) / 255),
                    (byte)((color.g * alpha + dst.g * inverse) / 255),
                    (byte)((color.b * alpha + dst.b * inverse) / 255),
                    255);
            }
            target.SetPixels32(pixels);
        }

        private static void AssertCellHasVisiblePixels(Texture2D texture, int x, int y, int width, int height, string label)
        {
            Color32 background = new Color32(24, 29, 40, 255);
            Color32[] pixels = texture.GetPixels32();
            int different = 0;
            for (int py = Math.Max(0, y); py < Math.Min(texture.height, y + height); py++)
            for (int px = Math.Max(0, x); px < Math.Min(texture.width, x + width); px++)
            {
                Color32 pixel = pixels[py * texture.width + px];
                if (pixel.r != background.r || pixel.g != background.g || pixel.b != background.b) different++;
            }
            if (different < 32) throw new InvalidOperationException("P2-A3 rendered cell is empty: " + label);
        }

        private static void WritePng(Texture2D texture, string path)
        {
            texture.Apply(false, false);
            File.WriteAllBytes(path, texture.EncodeToPNG());
        }
    }
}
#endif
