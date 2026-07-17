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
    public sealed class ZirconAndroidVisualAssetLoaderBehaviour : MonoBehaviour
    {
        [SerializeField] private ZirconProtocolProbeBehaviour session;
        [SerializeField] private ZirconMapDebugRenderer mapRenderer;
        [SerializeField] private ZirconWorldDebugRenderer worldRenderer;

        private bool spritesLoading;
        private bool spritesReady;
        private bool mapLoading;
        private int loadedMapIndex = -1;

        private static readonly string[] EntityFolders = { "M-Hum", "Mon-1", "NPC" };
        private static readonly string[][] EntityFiles =
        {
            new[] { "M-Hum_00000_image.png", "M-Hum_00001_image.png", "M-Hum_00002_image.png", "M-Hum_00003_image.png", "M-Hum_00010_image.png", "M-Hum_00011_image.png", "M-Hum_00012_image.png", "M-Hum_00013_image.png" },
            new[] { "Mon-1_00000_image.png", "Mon-1_00001_image.png", "Mon-1_00002_image.png", "Mon-1_00003_image.png", "Mon-1_01000_image.png", "Mon-1_01001_image.png", "Mon-1_01002_image.png", "Mon-1_01003_image.png" },
            new[] { "NPC_00000_image.png", "NPC_00001_image.png", "NPC_00002_image.png", "NPC_00003_image.png", "NPC_00100_image.png", "NPC_00101_image.png", "NPC_00102_image.png", "NPC_00103_image.png" }
        };

        private void Start()
        {
            if (!spritesLoading && !spritesReady)
                StartCoroutine(PrepareEntitySprites());
        }

        private void Update()
        {
            if (session == null || !session.IsInGame || mapLoading)
                return;

            ZirconWorldSnapshot snapshot = session.GetWorldSnapshot();
            if (snapshot == null || snapshot.MapIndex == loadedMapIndex)
                return;

            if (snapshot.MapIndex == 294)
                StartCoroutine(PrepareD2401Map());
            else
                loadedMapIndex = snapshot.MapIndex;
        }

        private IEnumerator PrepareEntitySprites()
        {
            spritesLoading = true;
            string textureRoot = Path.Combine(Application.persistentDataPath, "Zircon", "RuntimeVisuals", "Generated", "Textures");
            int copied = 0;
            for (int folderIndex = 0; folderIndex < EntityFolders.Length; folderIndex++)
            {
                string folder = EntityFolders[folderIndex];
                foreach (string file in EntityFiles[folderIndex])
                {
                    string relative = "Generated/Textures/" + folder + "/" + file;
                    string destination = Path.Combine(textureRoot, folder, file);
                    yield return CopyAsset(relative, destination, () => copied++);
                }
            }

            SetPrivateField(worldRenderer, "generatedTextureRoot", textureRoot);
            SetPrivateField(worldRenderer, "generatedSpritesLoaded", false);
            ClearPrivateDictionary(worldRenderer, "spritesByKind");
            spritesReady = copied == 24;
            spritesLoading = false;
            Debug.Log("Android visual assets: entity frames=" + copied + "/24 root=" + textureRoot);
        }

        private IEnumerator PrepareD2401Map()
        {
            mapLoading = true;
            string visualRoot = Path.Combine(Application.persistentDataPath, "Zircon", "RuntimeVisuals");
            string mapTextureRoot = Path.Combine(visualRoot, "Generated", "Textures", "MapData");
            int copied = 0;

            for (int index = 20; index <= 24; index++)
            {
                string name = "Tiles5c_" + index.ToString("D5") + "_image.png";
                yield return CopyAsset("Generated/Textures/MapData/Tiles5c/" + name,
                    Path.Combine(mapTextureRoot, "Tiles5c", name), () => copied++);
            }

            for (int index = 5; index <= 9; index++)
            {
                string name = "Tiles5c_" + index.ToString("D5") + "_image.png";
                yield return CopyAsset("Generated/Textures/MapData/Wood_Tiles5c/" + name,
                    Path.Combine(mapTextureRoot, "Tiles5c", name), () => copied++);
            }

            string manifest = null;
            string loadError = null;
            yield return ZirconAssetStore.LoadText("Generated/Data/Maps/D2401.map.manifest.json",
                value => manifest = value, value => loadError = value);

            if (string.IsNullOrEmpty(manifest))
            {
                Debug.LogError("Android visual assets: D2401 manifest failed: " + loadError);
                mapLoading = false;
                yield break;
            }

            // Wood/Tiles5c is staged in a separate source folder but cached under
            // Tiles5c with non-overlapping indices, allowing the prototype map
            // renderer to draw both library variants without a fallback block.
            manifest = manifest.Replace("\"BackFile\": 17", "\"BackFile\": 2");
            string runtimeMapRoot = Path.Combine(visualRoot, "Generated", "Data", "Maps");
            Directory.CreateDirectory(runtimeMapRoot);
            string runtimeManifest = Path.Combine(runtimeMapRoot, "D2401.map.manifest.json");
            File.WriteAllText(runtimeManifest, manifest);

            SetPrivateField(mapRenderer, "generatedTextureRoot", mapTextureRoot);
            ClearPrivateDictionary(mapRenderer, "tileSprites");
            mapRenderer.RenderManifest(runtimeManifest);
            loadedMapIndex = 294;
            mapLoading = false;
            Debug.Log("Android visual assets: D2401 map tiles=" + copied + "/10 manifest=" + runtimeManifest);
        }

        private static IEnumerator CopyAsset(string relative, string destination, Action copied)
        {
            byte[] bytes = null;
            string error = null;
            yield return ZirconAssetStore.LoadBytes(relative, value => bytes = value, value => error = value);
            if (bytes == null || bytes.Length == 0)
            {
                Debug.LogError("Android visual asset missing: " + relative + " error=" + error);
                yield break;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            File.WriteAllBytes(destination, bytes);
            copied?.Invoke();
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            if (target == null)
                return;
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field?.SetValue(target, value);
        }

        private static void ClearPrivateDictionary(object target, string fieldName)
        {
            if (target == null)
                return;
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field?.GetValue(target) is IDictionary dictionary)
                dictionary.Clear();
        }
    }
}
