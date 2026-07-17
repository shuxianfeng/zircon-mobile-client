using System.Collections;
using System.IO;
using UnityEngine;
using Zircon.Mobile.Core.Assets;

namespace Zircon.Mobile.Game.World
{
    public sealed class ZirconAndroidMapLoaderBehaviour : MonoBehaviour
    {
        [SerializeField] private ZirconMapDebugRenderer mapRenderer;
        [SerializeField] private string relativeManifestPath = "Generated/Data/Maps/0.map.manifest.json";

        private IEnumerator Start()
        {
            if (mapRenderer == null)
                yield break;

            string json = null;
            string error = null;
            yield return ZirconAssetStore.LoadText(relativeManifestPath, value => json = value, value => error = value);
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogWarning("Map asset load failed: " + (error ?? relativeManifestPath), this);
                yield break;
            }

            string cacheRoot = Path.Combine(Application.persistentDataPath, "Zircon", "RuntimeMaps");
            Directory.CreateDirectory(cacheRoot);
            string cachedManifest = Path.Combine(cacheRoot, Path.GetFileName(relativeManifestPath));
            File.WriteAllText(cachedManifest, json);
            mapRenderer.RenderManifest(cachedManifest);
        }
    }
}
