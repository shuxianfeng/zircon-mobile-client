using System;
using System.Collections;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Networking;

namespace Zircon.Mobile.Core.Assets
{
    public static class ZirconAssetStore
    {
        private const string RootName = "Zircon";

        public static IEnumerator LoadBytes(string relativePath, Action<byte[]> completed, Action<string> failed = null)
        {
            string normalized = Normalize(relativePath);
            string cached = Path.Combine(Application.persistentDataPath, RootName, normalized);
            if (File.Exists(cached))
            {
                completed?.Invoke(File.ReadAllBytes(cached));
                yield break;
            }

#if UNITY_EDITOR
            string editorFile = Path.Combine(Application.dataPath, normalized);
            if (File.Exists(editorFile))
            {
                completed?.Invoke(File.ReadAllBytes(editorFile));
                yield break;
            }
#endif

            string source = CombineUri(Application.streamingAssetsPath, RootName + "/" + normalized);
            using (UnityWebRequest request = UnityWebRequest.Get(source))
            {
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                {
                    failed?.Invoke(request.error);
                    yield break;
                }
                completed?.Invoke(request.downloadHandler.data);
            }
        }

        public static IEnumerator LoadText(string relativePath, Action<string> completed, Action<string> failed = null)
        {
            string text = null;
            yield return LoadBytes(relativePath, bytes => text = System.Text.Encoding.UTF8.GetString(bytes), failed);
            if (text != null) completed?.Invoke(text);
        }

        public static bool VerifySha256(byte[] bytes, string expectedHex)
        {
            if (bytes == null || string.IsNullOrWhiteSpace(expectedHex)) return false;
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(bytes);
                return string.Equals(BitConverter.ToString(hash).Replace("-", string.Empty), expectedHex, StringComparison.OrdinalIgnoreCase);
            }
        }

        private static string Normalize(string value) => (value ?? string.Empty).Replace('\\', '/').TrimStart('/');
        private static string CombineUri(string root, string path) => root.TrimEnd('/', '\\') + "/" + path.Replace('\\', '/').TrimStart('/');
    }

    [Serializable]
    public sealed class ZirconAssetCatalog
    {
        public string Version;
        public string GeneratedUtc;
        public ZirconAssetCatalogEntry[] Files;
    }

    [Serializable]
    public sealed class ZirconAssetCatalogEntry
    {
        public string Path;
        public long Size;
        public string Sha256;
    }
}