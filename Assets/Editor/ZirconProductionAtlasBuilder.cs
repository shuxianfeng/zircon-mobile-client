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
    /// <summary>Packs production frames into independently loadable Android atlas chunks.</summary>
    public static class ZirconProductionAtlasBuilder
    {
        private const int AtlasSize = 2048;
        private const int Padding = 2;
        private const string AtlasFolder = "Assets/Generated/Atlases";

        private sealed class SourceImage
        {
            public ZirconRuntimeSpriteFrame Frame;
            public Texture2D Texture;
        }

        private sealed class Sheet
        {
            public readonly List<SourceImage> Images = new List<SourceImage>();
            public int CursorX = Padding;
            public int CursorY = Padding;
            public int RowHeight;
            public int UsedWidth;
            public int UsedHeight;
        }

        public static int Build(IList<ZirconRuntimeSpriteSet> sets)
        {
            Directory.CreateDirectory(AtlasFolder);
            foreach (string stale in Directory.GetFiles(AtlasFolder, "p2-*.png", SearchOption.TopDirectoryOnly))
                AssetDatabase.DeleteAsset(stale.Replace('\\', '/'));

            int atlasCount = 0;
            foreach (IGrouping<string, ZirconRuntimeSpriteSet> chunk in sets.GroupBy(ChunkForSet))
            {
                var images = new List<SourceImage>();
                foreach (ZirconRuntimeSpriteSet set in chunk)
                foreach (ZirconRuntimeSpriteFrame frame in set.Frames)
                {
                    string sourcePath = Path.GetFullPath(Path.Combine("Assets", frame.Path));
                    if (!File.Exists(sourcePath)) continue;
                    var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (!texture.LoadImage(File.ReadAllBytes(sourcePath)))
                    {
                        UnityEngine.Object.DestroyImmediate(texture);
                        continue;
                    }
                    images.Add(new SourceImage { Frame = frame, Texture = texture });
                }

                images.Sort((left, right) => right.Texture.height.CompareTo(left.Texture.height));
                List<Sheet> sheets = Pack(images);
                for (int i = 0; i < sheets.Count; i++)
                {
                    WriteSheet(chunk.Key, i, sheets[i]);
                    atlasCount++;
                }
                foreach (SourceImage image in images)
                    UnityEngine.Object.DestroyImmediate(image.Texture);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("P2 production atlases ready: atlases=" + atlasCount + " compression=ASTC_6x6 chunks=character/entities/effects");
            return atlasCount;
        }

        private static List<Sheet> Pack(List<SourceImage> images)
        {
            var sheets = new List<Sheet>();
            foreach (SourceImage image in images)
            {
                if (image.Texture.width + Padding * 2 > AtlasSize || image.Texture.height + Padding * 2 > AtlasSize)
                    throw new InvalidOperationException("P2 sprite exceeds atlas size: " + image.Frame.Path);

                Sheet target = null;
                foreach (Sheet sheet in sheets)
                    if (TryPlace(sheet, image, false)) { target = sheet; break; }
                if (target != null)
                {
                    TryPlace(target, image, true);
                    continue;
                }
                target = new Sheet();
                sheets.Add(target);
                if (!TryPlace(target, image, true))
                    throw new InvalidOperationException("P2 sprite could not be packed: " + image.Frame.Path);
            }
            return sheets;
        }

        private static bool TryPlace(Sheet sheet, SourceImage image, bool commit)
        {
            int x = sheet.CursorX;
            int y = sheet.CursorY;
            int rowHeight = sheet.RowHeight;
            int paddedWidth = image.Texture.width + Padding * 2;
            int paddedHeight = image.Texture.height + Padding * 2;
            if (x + paddedWidth > AtlasSize)
            {
                x = Padding;
                y += rowHeight;
                rowHeight = 0;
            }
            if (y + paddedHeight > AtlasSize) return false;
            if (!commit) return true;

            image.Frame.AtlasX = x + Padding;
            image.Frame.AtlasY = y + Padding;
            image.Frame.AtlasWidth = image.Texture.width;
            image.Frame.AtlasHeight = image.Texture.height;
            sheet.Images.Add(image);
            sheet.CursorX = x + paddedWidth;
            sheet.CursorY = y;
            sheet.RowHeight = Math.Max(rowHeight, paddedHeight);
            sheet.UsedWidth = Math.Max(sheet.UsedWidth, sheet.CursorX + Padding);
            sheet.UsedHeight = Math.Max(sheet.UsedHeight, y + sheet.RowHeight + Padding);
            return true;
        }

        private static void WriteSheet(string chunk, int index, Sheet sheet)
        {
            int width = Mathf.Clamp(Mathf.NextPowerOfTwo(sheet.UsedWidth), 64, AtlasSize);
            int height = Mathf.Clamp(Mathf.NextPowerOfTwo(sheet.UsedHeight), 64, AtlasSize);
            var atlas = new Texture2D(width, height, TextureFormat.RGBA32, false);
            atlas.SetPixels32(new Color32[width * height]);
            foreach (SourceImage image in sheet.Images)
                atlas.SetPixels32(image.Frame.AtlasX, image.Frame.AtlasY, image.Texture.width, image.Texture.height,
                    image.Texture.GetPixels32());
            atlas.Apply(false, false);

            string shortChunk = chunk.Substring("zircon-p2-".Length);
            string assetPath = AtlasFolder + "/p2-" + shortChunk + "-" + index + ".png";
            File.WriteAllBytes(assetPath, atlas.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(atlas);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.maxTextureSize = AtlasSize;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                name = "Android",
                overridden = true,
                maxTextureSize = AtlasSize,
                format = TextureImporterFormat.ASTC_6x6,
                textureCompression = TextureImporterCompression.CompressedHQ,
                compressionQuality = 100,
            });
            importer.assetBundleName = chunk;
            importer.SaveAndReimport();

            string runtimeAssetName = assetPath.ToLowerInvariant();
            foreach (SourceImage image in sheet.Images)
            {
                image.Frame.Bundle = chunk;
                image.Frame.AtlasAsset = runtimeAssetName;
            }
        }

        private static string ChunkForSet(ZirconRuntimeSpriteSet set)
        {
            if (set.Id.StartsWith("player.", StringComparison.OrdinalIgnoreCase)) return "zircon-p2-character";
            if (set.Id.StartsWith("effect.", StringComparison.OrdinalIgnoreCase)) return "zircon-p2-effects";
            return "zircon-p2-entities";
        }
    }
}
#endif
