using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace Zircon.Mobile.Core.Assets
{
    public sealed class ZirconAssetCatalogBehaviour : MonoBehaviour
    {
        [SerializeField] private string remoteBaseUrl;
        [SerializeField] private bool checkRemoteOnStart;
        [SerializeField] private int retryCount = 2;

        public ZirconAssetCatalog Catalog { get; private set; }
        public bool IsUpdating { get; private set; }
        public float Progress { get; private set; }
        public string LastError { get; private set; } = string.Empty;

        private IEnumerator Start()
        {
            yield return ZirconAssetStore.LoadText("catalog.json", text => Catalog = JsonUtility.FromJson<ZirconAssetCatalog>(text), error => LastError = error);
            if (checkRemoteOnStart && !string.IsNullOrWhiteSpace(remoteBaseUrl))
                yield return UpdateFromRemote();
        }

        public IEnumerator UpdateFromRemote()
        {
            if (IsUpdating || string.IsNullOrWhiteSpace(remoteBaseUrl)) yield break;
            IsUpdating = true; Progress = 0f; LastError = string.Empty;
            ZirconAssetCatalog remote = null;
            yield return DownloadText(RemoteUrl("catalog.json"), text => remote = JsonUtility.FromJson<ZirconAssetCatalog>(text));
            if (remote == null || remote.Files == null) { IsUpdating = false; yield break; }

            int total = remote.Files.Length;
            for (int i = 0; i < total; i++)
            {
                ZirconAssetCatalogEntry entry = remote.Files[i];
                string target = Path.Combine(Application.persistentDataPath, "Zircon", entry.Path.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(target) || !VerifyFile(target, entry))
                {
                    byte[] bytes = null;
                    for (int attempt = 0; attempt <= retryCount && bytes == null; attempt++)
                        yield return DownloadBytes(RemoteUrl(entry.Path), value => { if (ZirconAssetStore.VerifySha256(value, entry.Sha256)) bytes = value; });
                    if (bytes == null) { LastError = "Asset verification failed: " + entry.Path; IsUpdating = false; yield break; }
                    Directory.CreateDirectory(Path.GetDirectoryName(target));
                    string temporary = target + ".download";
                    File.WriteAllBytes(temporary, bytes);
                    if (File.Exists(target)) File.Delete(target);
                    File.Move(temporary, target);
                }
                Progress = total == 0 ? 1f : (i + 1f) / total;
            }

            Catalog = remote;
            IsUpdating = false;
        }

        public void ClearDownloadedCache()
        {
            if (IsUpdating) return;
            string root = Path.Combine(Application.persistentDataPath, "Zircon");
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }

        private bool VerifyFile(string path, ZirconAssetCatalogEntry entry)
        {
            var info = new FileInfo(path);
            return info.Length == entry.Size && ZirconAssetStore.VerifySha256(File.ReadAllBytes(path), entry.Sha256);
        }

        private IEnumerator DownloadText(string url, Action<string> completed)
        {
            yield return DownloadBytes(url, bytes => completed?.Invoke(System.Text.Encoding.UTF8.GetString(bytes)));
        }

        private IEnumerator DownloadBytes(string url, Action<byte[]> completed)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success) { LastError = request.error; yield break; }
                completed?.Invoke(request.downloadHandler.data);
            }
        }

        private string RemoteUrl(string path) => remoteBaseUrl.TrimEnd('/') + "/" + path.Replace('\\', '/').TrimStart('/');
    }
}