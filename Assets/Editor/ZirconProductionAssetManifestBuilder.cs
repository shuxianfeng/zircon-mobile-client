#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Zircon.Mobile.Core.Assets;

namespace Zircon.Mobile.Editor
{
    public static class ZirconProductionAssetManifestBuilder
    {
        private const string OutputPath = "Assets/Generated/Data/Runtime/production-visuals.json";

        private sealed class SourceSpec
        {
            public string Id;
            public string Folder;
            public string Manifest;
            public string Suffix;
        }

        [Serializable]
        private sealed class SourceFrameList
        {
            public SourceFrame[] Items;
        }

        [Serializable]
        private sealed class SourceFrame
        {
            public int Index;
            public int Width;
            public int Height;
            public int OffSetX;
            public int OffSetY;
            public int ShadowType;
            public int ShadowWidth;
            public int ShadowHeight;
            public int ShadowOffSetX;
            public int ShadowOffSetY;
        }

        private static readonly SourceSpec[] Sources =
        {
            Spec("player.warrior.female.body", "PlayerAppearance/WM-Hum", "WM-Hum.manifest.json", "_image.png"),
            Spec("player.warrior.female.overlay", "PlayerAppearance/WM-HumOverlay", "WM-Hum.manifest.json", "_overlay.png"),
            Spec("player.warrior.female.hair", "PlayerAppearance/WM-Hair", "WM-Hair.manifest.json", "_image.png"),
            Spec("player.warrior.female.weapon", "PlayerAppearance/WM-Weapon1", "WM-Weapon1.manifest.json", "_image.png"),
            Spec("player.standard.male.body", "PlayerAppearance/M-Hum", "M-Hum.manifest.json", "_image.png"),
            Spec("player.standard.male.overlay", "PlayerAppearance/M-HumOverlay", "M-Hum.manifest.json", "_overlay.png"),
            Spec("player.standard.male.hair", "PlayerAppearance/M-Hair", "M-Hair.manifest.json", "_image.png"),
            Spec("player.standard.male.weapon2", "PlayerAppearance/M-Weapon2", "M-Weapon2.manifest.json", "_image.png"),
            Spec("player.standard.male.weapon11", "PlayerAppearance/M-Weapon11", "M-Weapon11.manifest.json", "_image.png"),
            Spec("player.standard.male.weapon15", "PlayerAppearance/M-Weapon15", "M-Weapon15.manifest.json", "_image.png"),
            Spec("entity.player.sample", "M-Hum", "M-Hum.manifest.json", "_image.png"),
            Spec("entity.monster.sample", "Mon-1", "Mon-1.manifest.json", "_image.png"),
            Spec("entity.monster.mon3", "Mon-3", "Mon-3.manifest.json", "_image.png"),
            Spec("entity.monster.mon13", "Mon-13", "Mon-13.manifest.json", "_image.png"),
            Spec("entity.monster.mon34", "Mon-34", "Mon-34.manifest.json", "_image.png"),
            Spec("entity.npc.sample", "NPC", "NPC.manifest.json", "_image.png"),
            Spec("effect.icon.sample", "MIcon", "MIcon.manifest.json", "_image.png"),
        };

        [MenuItem("Zircon/P2/Build Production Visual Manifest")]
        public static void Build()
        {
            var sets = new List<ZirconRuntimeSpriteSet>();
            foreach (SourceSpec source in Sources)
            {
                ZirconRuntimeSpriteSet set = BuildSet(source);
                if (set != null && set.Frames.Length > 0)
                    sets.Add(set);
            }

            var manifest = new ZirconRuntimeVisualManifest
            {
                Version = "1.0.0",
                GeneratedUtc = GetNewestSourceUtc().ToString("O", CultureInfo.InvariantCulture),
                SpriteSets = sets.ToArray(),
            };

            ZirconProductionAtlasBuilder.Build(sets);

            string fullOutput = Path.GetFullPath(OutputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullOutput));
            File.WriteAllText(fullOutput, JsonUtility.ToJson(manifest, true));
            AssetDatabase.ImportAsset(OutputPath, ImportAssetOptions.ForceUpdate);
            int frames = sets.Sum(value => value.Frames.Length);
            Debug.Log("P2 production visual manifest: sets=" + sets.Count + " frames=" + frames + " path=" + OutputPath);
        }

        public static void BuildFromCommandLine()
        {
            Build();
        }

        private static ZirconRuntimeSpriteSet BuildSet(SourceSpec source)
        {
            string folder = Path.GetFullPath(Path.Combine("Assets/Generated/Textures", source.Folder));
            string manifestPath = Path.Combine(folder, source.Manifest);
            if (!Directory.Exists(folder) || !File.Exists(manifestPath))
            {
                Debug.LogWarning("P2 visual source missing: " + source.Folder);
                return null;
            }

            string wrapped = "{\"Items\":" + File.ReadAllText(manifestPath) + "}";
            SourceFrameList sourceFrames = JsonUtility.FromJson<SourceFrameList>(wrapped);
            var metadata = new Dictionary<int, SourceFrame>();
            if (sourceFrames?.Items != null)
                foreach (SourceFrame frame in sourceFrames.Items)
                    metadata[frame.Index] = frame;

            string[] files = Directory.GetFiles(folder, "*" + source.Suffix, SearchOption.TopDirectoryOnly);
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);
            var frames = new List<ZirconRuntimeSpriteFrame>();
            foreach (string file in files)
            {
                if (!TryReadIndex(file, source.Suffix, out int index) || !metadata.TryGetValue(index, out SourceFrame frame))
                    continue;

                frames.Add(new ZirconRuntimeSpriteFrame
                {
                    Index = index,
                    Path = "Generated/Textures/" + source.Folder.Replace('\\', '/') + "/" + Path.GetFileName(file),
                    Width = frame.Width,
                    Height = frame.Height,
                    OffsetX = frame.OffSetX,
                    OffsetY = frame.OffSetY,
                    ShadowType = frame.ShadowType,
                    ShadowWidth = frame.ShadowWidth,
                    ShadowHeight = frame.ShadowHeight,
                    ShadowOffsetX = frame.ShadowOffSetX,
                    ShadowOffsetY = frame.ShadowOffSetY,
                });
            }

            return new ZirconRuntimeSpriteSet
            {
                Id = source.Id,
                SourceLibrary = source.Folder,
                Frames = frames.ToArray(),
            };
        }

        private static bool TryReadIndex(string path, string suffix, out int index)
        {
            index = 0;
            string name = Path.GetFileName(path);
            int suffixStart = name.Length - suffix.Length;
            int underscore = suffixStart > 0 ? name.LastIndexOf('_', suffixStart - 1) : -1;
            return underscore >= 0 && int.TryParse(name.Substring(underscore + 1, suffixStart - underscore - 1),
                NumberStyles.None, CultureInfo.InvariantCulture, out index);
        }

        private static SourceSpec Spec(string id, string folder, string manifest, string suffix)
        {
            return new SourceSpec { Id = id, Folder = folder, Manifest = manifest, Suffix = suffix };
        }

        private static DateTime GetNewestSourceUtc()
        {
            DateTime newest = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            foreach (SourceSpec source in Sources)
            {
                string folder = Path.GetFullPath(Path.Combine("Assets/Generated/Textures", source.Folder));
                if (!Directory.Exists(folder)) continue;
                foreach (string file in Directory.GetFiles(folder, "*" + source.Suffix, SearchOption.TopDirectoryOnly))
                {
                    DateTime written = File.GetLastWriteTimeUtc(file);
                    if (written > newest) newest = written;
                }
            }
            return newest;
        }
    }
}
#endif
