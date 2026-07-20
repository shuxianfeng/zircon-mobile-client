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

        public static void ValidateFromCommandLine()
        {
            ZirconProductionAssetManifestBuilder.Build();
            ZirconRuntimeVisualManifest manifest = JsonUtility.FromJson<ZirconRuntimeVisualManifest>(File.ReadAllText(ManifestPath));
            if (manifest == null || manifest.SpriteSets == null)
                throw new InvalidOperationException("P2-A production visual manifest is invalid.");

            var minimums = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["player.warrior.female.body"] = 32,
                ["player.warrior.female.overlay"] = 32,
                ["player.warrior.female.hair"] = 32,
                ["player.warrior.female.weapon"] = 32,
                ["entity.player.sample"] = 8,
                ["entity.monster.sample"] = 8,
                ["entity.npc.sample"] = 8,
                ["effect.icon.sample"] = 16,
            };

            int frames = 0;
            var bundles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, int> requirement in minimums)
            {
                ZirconRuntimeSpriteSet set = manifest.SpriteSets.FirstOrDefault(value =>
                    string.Equals(value.Id, requirement.Key, StringComparison.OrdinalIgnoreCase));
                if (set?.Frames == null || set.Frames.Length < requirement.Value)
                    throw new InvalidOperationException("P2-A set incomplete: " + requirement.Key);

                foreach (ZirconRuntimeSpriteFrame frame in set.Frames)
                {
                    frames++;
                    if (string.IsNullOrEmpty(frame.Bundle) || string.IsNullOrEmpty(frame.AtlasAsset))
                        throw new InvalidOperationException("P2-A frame has no bundle: " + set.Id + "/" + frame.Index);
                    if (frame.AtlasWidth <= 0 || frame.AtlasHeight <= 0)
                        throw new InvalidOperationException("P2-A frame has invalid atlas rect: " + set.Id + "/" + frame.Index);
                    bundles.Add(frame.Bundle);
                }
            }

            string[] expectedAtlases =
            {
                "Assets/Generated/Atlases/p2-character-0.png",
                "Assets/Generated/Atlases/p2-entities-0.png",
                "Assets/Generated/Atlases/p2-effects-0.png",
            };
            foreach (string atlasPath in expectedAtlases)
            {
                Texture2D atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(atlasPath);
                if (atlas == null || atlas.width <= 0 || atlas.height <= 0)
                    throw new InvalidOperationException("P2-A atlas missing: " + atlasPath);
            }

            string[] requiredBundles = { "zircon-p2-character", "zircon-p2-entities", "zircon-p2-effects" };
            foreach (string bundle in requiredBundles)
                if (!bundles.Contains(bundle)) throw new InvalidOperationException("P2-A bundle mapping missing: " + bundle);

            Debug.Log("P2-A visual validation passed: sets=" + manifest.SpriteSets.Length +
                      " requiredFrames=" + frames + " bundles=" + bundles.Count + " atlases=" + expectedAtlases.Length);
        }
    }
}
#endif
