using System;
using System.Collections;
using System.Collections.Generic;

namespace Zircon.Mobile.Core.Assets
{
    [Serializable]
    public sealed class ZirconRuntimeVisualManifest
    {
        public string Version;
        public string GeneratedUtc;
        public ZirconRuntimeSpriteSet[] SpriteSets;
    }

    [Serializable]
    public sealed class ZirconRuntimeSpriteSet
    {
        public string Id;
        public string SourceLibrary;
        public ZirconRuntimeSpriteFrame[] Frames;
    }

    [Serializable]
    public sealed class ZirconRuntimeSpriteFrame
    {
        public int Index;
        public string Path;
        public int Width;
        public int Height;
        public int OffsetX;
        public int OffsetY;
        public int ShadowType;
        public int ShadowWidth;
        public int ShadowHeight;
        public int ShadowOffsetX;
        public int ShadowOffsetY;
        public string Bundle;
        public string AtlasAsset;
        public int AtlasX;
        public int AtlasY;
        public int AtlasWidth;
        public int AtlasHeight;
    }

    public sealed class ZirconRuntimeVisualCatalog
    {
        private const string ManifestPath = "Generated/Data/Runtime/production-visuals.json";
        private readonly Dictionary<string, Dictionary<int, ZirconRuntimeSpriteFrame>> framesBySet =
            new Dictionary<string, Dictionary<int, ZirconRuntimeSpriteFrame>>(StringComparer.OrdinalIgnoreCase);

        public string Version { get; private set; }
        public int FrameCount { get; private set; }

        public static IEnumerator Load(Action<ZirconRuntimeVisualCatalog> completed, Action<string> failed = null)
        {
            string json = null;
            string error = null;
            yield return ZirconAssetStore.LoadText(ManifestPath, value => json = value, value => error = value);
            if (string.IsNullOrEmpty(json))
            {
                failed?.Invoke(error ?? "Runtime visual manifest is empty.");
                yield break;
            }

            ZirconRuntimeVisualManifest manifest;
            try
            {
                manifest = UnityEngine.JsonUtility.FromJson<ZirconRuntimeVisualManifest>(json);
            }
            catch (Exception ex)
            {
                failed?.Invoke("Runtime visual manifest parse failed: " + ex.Message);
                yield break;
            }

            if (manifest?.SpriteSets == null)
            {
                failed?.Invoke("Runtime visual manifest has no sprite sets.");
                yield break;
            }

            var catalog = new ZirconRuntimeVisualCatalog { Version = manifest.Version ?? string.Empty };
            foreach (ZirconRuntimeSpriteSet set in manifest.SpriteSets)
            {
                if (set == null || string.IsNullOrEmpty(set.Id) || set.Frames == null)
                    continue;

                var frames = new Dictionary<int, ZirconRuntimeSpriteFrame>();
                foreach (ZirconRuntimeSpriteFrame frame in set.Frames)
                {
                    if (frame == null || string.IsNullOrEmpty(frame.Path))
                        continue;
                    frames[frame.Index] = frame;
                    catalog.FrameCount++;
                }
                catalog.framesBySet[set.Id] = frames;
            }

            completed?.Invoke(catalog);
        }

        public bool TryGetFrame(string setId, int index, out ZirconRuntimeSpriteFrame frame)
        {
            frame = null;
            return !string.IsNullOrEmpty(setId) &&
                   framesBySet.TryGetValue(setId, out Dictionary<int, ZirconRuntimeSpriteFrame> frames) &&
                   frames.TryGetValue(index, out frame);
        }
    }
}
