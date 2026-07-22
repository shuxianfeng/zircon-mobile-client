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
    public static class ZirconP2AValidation
    {
        private const string ManifestPath = "Assets/Generated/Data/Runtime/production-visuals.json";

        private sealed class AtlasRectUse
        {
            public string SetId;
            public int FrameIndex;
            public RectInt Rect;
        }

        public static void ValidateFromCommandLine()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                throw new InvalidOperationException("P2-A validation must run with Android as the active build target.");

            ZirconProductionAssetManifestBuilder.Build();
            if (!File.Exists(Path.GetFullPath(ManifestPath)))
                throw new InvalidOperationException("P2-A production visual manifest is missing: " + ManifestPath);

            ZirconRuntimeVisualManifest manifest = JsonUtility.FromJson<ZirconRuntimeVisualManifest>(File.ReadAllText(ManifestPath));
            if (manifest == null || manifest.SpriteSets == null)
                throw new InvalidOperationException("P2-A production visual manifest is invalid.");

            Dictionary<string, int[]> requiredIndexes = BuildRequiredIndexes();
            var requiredBundles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "zircon-p2-character",
                "zircon-p2-entities",
                "zircon-p2-effects",
            };
            var observedBundles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var atlasTextures = new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);
            var atlasRectangles = new Dictionary<string, List<AtlasRectUse>>(StringComparer.OrdinalIgnoreCase);
            int requiredFrameCount = 0;

            foreach (KeyValuePair<string, int[]> requirement in requiredIndexes)
            {
                ZirconRuntimeSpriteSet set = manifest.SpriteSets.FirstOrDefault(value =>
                    value != null && string.Equals(value.Id, requirement.Key, StringComparison.OrdinalIgnoreCase));
                if (set == null || set.Frames == null)
                    throw new InvalidOperationException("P2-A required set is missing: " + requirement.Key);

                int duplicate = set.Frames.GroupBy(frame => frame.Index).FirstOrDefault(group => group.Count() > 1)?.Key ?? int.MinValue;
                if (duplicate != int.MinValue)
                    throw new InvalidOperationException("P2-A duplicate frame index: " + set.Id + "/" + duplicate);

                var framesByIndex = set.Frames.ToDictionary(frame => frame.Index);
                foreach (int index in requirement.Value)
                {
                    if (!framesByIndex.TryGetValue(index, out ZirconRuntimeSpriteFrame frame) || frame == null)
                        throw new InvalidOperationException("P2-A runtime-requested frame is missing: " + set.Id + "/" + index);
                    ValidateFrame(set.Id, frame, ExpectedBundle(set.Id), atlasTextures, atlasRectangles);
                    observedBundles.Add(frame.Bundle);
                    requiredFrameCount++;
                }
            }

            foreach (string bundle in requiredBundles)
                if (!observedBundles.Contains(bundle))
                    throw new InvalidOperationException("P2-A required bundle mapping is missing: " + bundle);

            ValidateAtlasRectanglesDoNotOverlap(atlasRectangles);
            ValidateShader();

            Debug.Log("P2-A strict visual validation passed: sets=" + requiredIndexes.Count +
                      " exactFrames=" + requiredFrameCount +
                      " bundles=" + observedBundles.Count +
                      " atlases=" + atlasTextures.Count +
                      " bounds=passed overlaps=passed androidAstc=passed shader=passed");
        }

        private static Dictionary<string, int[]> BuildRequiredIndexes()
        {
            return new Dictionary<string, int[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["player.warrior.female.body"] = DirectionalIndexes(45000),
                ["player.warrior.female.overlay"] = DirectionalIndexes(45000),
                ["player.warrior.female.hair"] = DirectionalIndexes(0),
                ["player.warrior.female.weapon"] = DirectionalIndexes(35000),
                ["entity.player.sample"] = DirectionalIndexes(0, 2),
                ["entity.monster.sample"] = TwoModelIndexes(1000),
                ["entity.npc.sample"] = TwoModelIndexes(100),
                ["effect.icon.sample"] = Enumerable.Range(0, 16).Select(index => index * 2).ToArray(),
            };
        }

        private static int[] DirectionalIndexes(int baseIndex, int directionCount = 8)
        {
            var indexes = new List<int>(directionCount * 4);
            for (int direction = 0; direction < directionCount; direction++)
            for (int frame = 0; frame < 4; frame++)
                indexes.Add(baseIndex + direction * 10 + frame);
            return indexes.ToArray();
        }

        private static int[] TwoModelIndexes(int secondModelBase)
        {
            return new[] { 0, 1, 2, 3, secondModelBase, secondModelBase + 1, secondModelBase + 2, secondModelBase + 3 };
        }

        private static void ValidateFrame(
            string setId,
            ZirconRuntimeSpriteFrame frame,
            string expectedBundle,
            Dictionary<string, Texture2D> atlasTextures,
            Dictionary<string, List<AtlasRectUse>> atlasRectangles)
        {
            string label = setId + "/" + frame.Index;
            if (string.IsNullOrEmpty(frame.Path))
                throw new InvalidOperationException("P2-A source path is empty: " + label);

            string sourceAssetPath = NormalizeAssetPath(frame.Path);
            if (!File.Exists(Path.GetFullPath(sourceAssetPath)))
                throw new InvalidOperationException("P2-A source texture is missing: " + label + " path=" + sourceAssetPath);

            if (!string.Equals(frame.Bundle, expectedBundle, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("P2-A bundle mismatch: " + label + " expected=" + expectedBundle + " actual=" + frame.Bundle);
            if (string.IsNullOrEmpty(frame.AtlasAsset))
                throw new InvalidOperationException("P2-A atlas asset is empty: " + label);

            string atlasAssetPath = NormalizeAssetPath(frame.AtlasAsset);
            if (!atlasTextures.TryGetValue(atlasAssetPath, out Texture2D atlas))
            {
                atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(atlasAssetPath);
                if (atlas == null)
                    throw new InvalidOperationException("P2-A atlas cannot be loaded: " + atlasAssetPath);

                var importer = AssetImporter.GetAtPath(atlasAssetPath) as TextureImporter;
                if (importer == null)
                    throw new InvalidOperationException("P2-A atlas importer is missing: " + atlasAssetPath);
                if (!string.Equals(importer.assetBundleName, expectedBundle, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("P2-A atlas importer bundle mismatch: " + atlasAssetPath);

                TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings("Android");
                if (!android.overridden || android.format != TextureImporterFormat.ASTC_6x6)
                    throw new InvalidOperationException("P2-A Android atlas format is not ASTC 6x6: " + atlasAssetPath);
                atlasTextures.Add(atlasAssetPath, atlas);
            }

            if (frame.Width <= 0 || frame.Height <= 0 || frame.AtlasWidth <= 0 || frame.AtlasHeight <= 0)
                throw new InvalidOperationException("P2-A frame dimensions are invalid: " + label);
            if (frame.Width != frame.AtlasWidth || frame.Height != frame.AtlasHeight)
                throw new InvalidOperationException("P2-A source/atlas dimensions differ: " + label);
            if (frame.AtlasX < 0 || frame.AtlasY < 0 ||
                (long)frame.AtlasX + frame.AtlasWidth > atlas.width ||
                (long)frame.AtlasY + frame.AtlasHeight > atlas.height)
            {
                throw new InvalidOperationException("P2-A atlas rectangle is out of bounds: " + label +
                    " rect=" + frame.AtlasX + "," + frame.AtlasY + "," + frame.AtlasWidth + "," + frame.AtlasHeight +
                    " atlas=" + atlas.width + "x" + atlas.height);
            }

            if (!atlasRectangles.TryGetValue(atlasAssetPath, out List<AtlasRectUse> uses))
            {
                uses = new List<AtlasRectUse>();
                atlasRectangles.Add(atlasAssetPath, uses);
            }
            uses.Add(new AtlasRectUse
            {
                SetId = setId,
                FrameIndex = frame.Index,
                Rect = new RectInt(frame.AtlasX, frame.AtlasY, frame.AtlasWidth, frame.AtlasHeight),
            });
        }

        private static void ValidateAtlasRectanglesDoNotOverlap(Dictionary<string, List<AtlasRectUse>> atlasRectangles)
        {
            foreach (KeyValuePair<string, List<AtlasRectUse>> atlas in atlasRectangles)
            {
                for (int left = 0; left < atlas.Value.Count; left++)
                for (int right = left + 1; right < atlas.Value.Count; right++)
                {
                    AtlasRectUse first = atlas.Value[left];
                    AtlasRectUse second = atlas.Value[right];
                    if (first.Rect.Overlaps(second.Rect))
                        throw new InvalidOperationException("P2-A atlas rectangles overlap: " + atlas.Key + " " +
                            first.SetId + "/" + first.FrameIndex + " and " + second.SetId + "/" + second.FrameIndex);
                }
            }
        }

        private static void ValidateShader()
        {
            Shader shader = Shader.Find("Zircon/RuntimeSprite");
            if (shader == null || !shader.isSupported)
                throw new InvalidOperationException("P2-A Zircon/RuntimeSprite shader is unsupported for Android.");
        }

        private static string ExpectedBundle(string setId)
        {
            if (setId.StartsWith("player.warrior.", StringComparison.OrdinalIgnoreCase))
                return "zircon-p2-character";
            if (setId.StartsWith("effect.", StringComparison.OrdinalIgnoreCase))
                return "zircon-p2-effects";
            return "zircon-p2-entities";
        }

        private static string NormalizeAssetPath(string path)
        {
            string normalized = (path ?? string.Empty).Replace('\\', '/');
            if (normalized.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                return "Assets/" + normalized.Substring("Assets/".Length);
            return "Assets/" + normalized.TrimStart('/');
        }
    }
}
#endif
