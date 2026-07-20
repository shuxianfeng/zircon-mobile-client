using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Zircon.Mobile.Core.Assets
{
    /// <summary>Loads Android atlas bundles one chunk at a time and keeps each loaded chunk shared.</summary>
    public static class ZirconRuntimeAtlasStore
    {
        private const string BundleRoot = "Bundles/Android/";
        private static readonly Dictionary<string, AssetBundle> Bundles =
            new Dictionary<string, AssetBundle>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, Texture2D> Atlases =
            new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);

        public static IEnumerator LoadSprite(ZirconRuntimeSpriteFrame frame, float pixelsPerUnit,
            Action<Sprite> completed, Action<string> failed = null)
        {
            if (frame == null || string.IsNullOrEmpty(frame.Bundle) || string.IsNullOrEmpty(frame.AtlasAsset) ||
                frame.AtlasWidth <= 0 || frame.AtlasHeight <= 0)
            {
                failed?.Invoke("Frame has no atlas location.");
                yield break;
            }

            string atlasKey = frame.Bundle + ":" + frame.AtlasAsset;
            if (!Atlases.TryGetValue(atlasKey, out Texture2D atlas) || atlas == null)
            {
                if (!Bundles.TryGetValue(frame.Bundle, out AssetBundle bundle) || bundle == null)
                {
                    byte[] bytes = null;
                    string loadError = null;
                    yield return ZirconAssetStore.LoadBytes(BundleRoot + frame.Bundle,
                        value => bytes = value, value => loadError = value);
                    if (bytes == null || bytes.Length == 0)
                    {
                        failed?.Invoke("Chunk " + frame.Bundle + " unavailable: " + loadError);
                        yield break;
                    }

                    AssetBundleCreateRequest create = AssetBundle.LoadFromMemoryAsync(bytes);
                    yield return create;
                    bundle = create.assetBundle;
                    if (bundle == null)
                    {
                        failed?.Invoke("Chunk " + frame.Bundle + " could not be opened.");
                        yield break;
                    }
                    Bundles[frame.Bundle] = bundle;
                    Debug.Log("P2 resource chunk loaded: " + frame.Bundle + " bytes=" + bytes.Length);
                }

                AssetBundleRequest request = bundle.LoadAssetAsync<Texture2D>(frame.AtlasAsset);
                yield return request;
                atlas = request.asset as Texture2D;
                if (atlas == null)
                {
                    failed?.Invoke("Atlas " + frame.AtlasAsset + " missing from " + frame.Bundle);
                    yield break;
                }
                atlas.filterMode = FilterMode.Point;
                atlas.wrapMode = TextureWrapMode.Clamp;
                Atlases[atlasKey] = atlas;
            }

            Rect rect = new Rect(frame.AtlasX, frame.AtlasY, frame.AtlasWidth, frame.AtlasHeight);
            if (rect.xMin < 0 || rect.yMin < 0 || rect.xMax > atlas.width || rect.yMax > atlas.height)
            {
                failed?.Invoke("Atlas rect is outside " + frame.AtlasAsset);
                yield break;
            }
            Sprite sprite = Sprite.Create(atlas, rect, new Vector2(0f, 1f), pixelsPerUnit);
            sprite.name = frame.AtlasAsset + "_" + frame.Index;
            completed?.Invoke(sprite);
        }
    }
}
