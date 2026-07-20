using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using Zircon.Mobile.Core.Assets;
using Zircon.Mobile.UI.Login;

namespace Zircon.Mobile.Game.World
{
    /// <summary>Loads the real Bitche county (map 1 / 0.map) floor tiles on Android.</summary>
    public sealed class ZirconMapOneVisualLoaderBehaviour : MonoBehaviour
    {
        [SerializeField] private ZirconProtocolProbeBehaviour session;
        [SerializeField] private ZirconMapDebugRenderer mapRenderer;
        private bool loading;
        private bool loaded;

        private void Update()
        {
            if (loading || loaded || session == null || !session.IsInGame)
                return;
            ZirconWorldSnapshot snapshot = session.GetWorldSnapshot();
            if (snapshot != null && snapshot.MapIndex == 1)
                StartCoroutine(LoadMap());
        }

        private IEnumerator LoadMap()
        {
            loading = true;
            if (mapRenderer == null)
            {
                Debug.LogError("Map 1 floor loader cannot start because ZirconMapDebugRenderer is missing.");
                loaded = true;
                loading = false;
                yield break;
            }
            string json = null;
            string error = null;
            yield return ZirconAssetStore.LoadText("Generated/Data/Maps/0.map.manifest.json",
                value => json = value, value => error = value);
            ZirconMapManifest manifest = string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<ZirconMapManifest>(json);
            if (manifest == null || manifest.SampleCells == null)
            {
                Debug.LogError("Map 1 manifest failed: " + error);
                loading = false;
                yield break;
            }

            var tiles = new HashSet<string>();
            foreach (ZirconMapCellManifest cell in manifest.SampleCells)
            {
                if (cell.BackImage <= 0) continue;
                if (cell.BackFile == 0) tiles.Add("Tilesc:" + cell.BackImage);
                else if (cell.BackFile == 1) tiles.Add("Tiles30c:" + cell.BackImage);
            }

            string visualRoot = Path.Combine(Application.persistentDataPath, "Zircon", "RuntimeVisuals");
            string textureRoot = Path.Combine(visualRoot, "Generated", "Textures", "MapData");
            int copied = 0;
            foreach (string tile in tiles)
            {
                string[] parts = tile.Split(':');
                string folder = parts[0];
                int index = int.Parse(parts[1]);
                string file = folder + "_" + index.ToString("D5") + "_image.png";
                byte[] bytes = null;
                yield return ZirconAssetStore.LoadBytes("Generated/Textures/MapData/Base_" + folder + "/" + file,
                    value => bytes = value,
                    value => Debug.LogError("Map 1 floor missing " + file + ": " + value));
                if (bytes == null || bytes.Length == 0) continue;
                string destination = Path.Combine(textureRoot, folder, file);
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.WriteAllBytes(destination, bytes);
                copied++;
            }

            string mapRoot = Path.Combine(visualRoot, "Generated", "Data", "Maps");
            Directory.CreateDirectory(mapRoot);
            string runtimeManifest = Path.Combine(mapRoot, "0.map.manifest.json");
            File.WriteAllText(runtimeManifest, json);
            SetField(mapRenderer, "generatedTextureRoot", textureRoot);
            ClearDictionary(mapRenderer, "tileSprites");
            mapRenderer.RenderManifest(runtimeManifest);
            loaded = true;
            loading = false;
            Debug.Log("Map 1 real floor ready: " + copied + "/" + tiles.Count + " tiles");
        }

        private static void SetField(object target, string name, object value)
        {
            target?.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(target, value);
        }

        private static void ClearDictionary(object target, string name)
        {
            object value = target?.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(target);
            if (value is System.Collections.IDictionary dictionary) dictionary.Clear();
        }
    }
}
