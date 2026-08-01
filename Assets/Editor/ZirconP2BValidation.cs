#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Zircon.Mobile.Core.Assets;
using Zircon.Mobile.Game.World;

namespace Zircon.Mobile.Editor
{
    public static class ZirconP2BValidation
    {
        private const string VisualManifestPath =
            "Assets/Generated/Data/Runtime/production-visuals.json";
        private static readonly HashSet<string> KnownEmptySourceEntries =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Cliffsc/Cliffsc_01500_image.png",
                "Cliffsc/Cliffsc_01578_image.png",
                "Forest_SmObjectsc/SmObjectsc_00396_image.png",
                "Forest_SmObjectsc/SmObjectsc_00397_image.png",
                "Forest_SmObjectsc/SmObjectsc_00401_image.png",
                "Forest_SmObjectsc/SmObjectsc_00402_image.png",
                "Forest_SmObjectsc/SmObjectsc_00451_image.png",
                "Forest_SmObjectsc/SmObjectsc_00452_image.png",
                "Forest_SmObjectsc/SmObjectsc_00453_image.png",
                "Forest_SmObjectsc/SmObjectsc_00456_image.png",
                "Forest_SmObjectsc/SmObjectsc_00457_image.png",
                "Forest_SmObjectsc/SmObjectsc_00460_image.png",
                "Forest_SmObjectsc/SmObjectsc_00461_image.png",
                "Forest_SmObjectsc/SmObjectsc_00465_image.png",
                "Forest_SmObjectsc/SmObjectsc_00466_image.png",
                "SmObjectsc/SmObjectsc_03174_image.png",
                "Wood_SmObjectsc/SmObjectsc_00854_image.png",
                "Wood_SmObjectsc/SmObjectsc_00855_image.png",
            };
        public static void ValidateFromCommandLine()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                throw new InvalidOperationException("P2-B validation requires the Android build target.");

            ValidateCurrentMalePlayer();
            ValidateMap("Assets/Generated/Data/Maps/1.map.manifest.json", "Map5", 350, 350);
            ValidateMap("Assets/Generated/Data/Maps/2.map.manifest.json", "Map6", 300, 300);
        }

        private static void ValidateCurrentMalePlayer()
        {
            ZirconRuntimeVisualManifest manifest =
                JsonUtility.FromJson<ZirconRuntimeVisualManifest>(
                    File.ReadAllText(Path.GetFullPath(VisualManifestPath)));
            if (manifest?.SpriteSets == null)
                throw new InvalidOperationException("P2-B production visual manifest is invalid.");

            int[] drawFrames = ProductionDrawFrames();
            ValidateSet(manifest, "player.standard.male.body",
                drawFrames.Select(value => 10000 + value));
            ValidateSet(manifest, "player.standard.male.overlay",
                drawFrames.Select(value => 10000 + value));
            ValidateSet(manifest, "player.standard.male.hair", drawFrames);
            ValidateSet(manifest, "player.standard.male.weapon11",
                drawFrames.Select(value => 5000 + value));
            ValidateSet(manifest, "player.standard.male.body",
                drawFrames.Select(value => 30000 + value));
            ValidateSet(manifest, "player.standard.male.overlay",
                drawFrames.Where(value => value < 1920).Select(value => 30000 + value));
            ValidateSet(manifest, "player.standard.male.weapon2",
                drawFrames.Select(value => 20000 + value));

            Debug.Log("P2-B male player validation passed: staged appearances=" +
                      "armour=2/weapon=101,armour=6/weapon=14 drawFrames=" +
                      drawFrames.Length + " layers=4");
        }

        private static void ValidateSet(
            ZirconRuntimeVisualManifest manifest,
            string setId,
            IEnumerable<int> requiredIndexes)
        {
            ZirconRuntimeSpriteSet set = manifest.SpriteSets.FirstOrDefault(value =>
                value != null && string.Equals(value.Id, setId, StringComparison.OrdinalIgnoreCase));
            if (set?.Frames == null)
                throw new InvalidOperationException("P2-B sprite set is missing: " + setId);
            Dictionary<int, ZirconRuntimeSpriteFrame> frames =
                set.Frames.ToDictionary(value => value.Index);
            foreach (int index in requiredIndexes)
            {
                if (!frames.TryGetValue(index, out ZirconRuntimeSpriteFrame frame))
                    throw new InvalidOperationException("P2-B frame is missing: " + setId + "/" + index);
                if (!string.Equals(frame.Bundle, "zircon-p2-character",
                        StringComparison.OrdinalIgnoreCase) ||
                    string.IsNullOrEmpty(frame.AtlasAsset) ||
                    frame.AtlasWidth <= 0 || frame.AtlasHeight <= 0)
                    throw new InvalidOperationException("P2-B atlas mapping is invalid: " + setId + "/" + index);
            }
        }

        private static void ValidateMap(
            string manifestPath,
            string textureFolder,
            int width,
            int height)
        {
            ZirconMapManifest map = JsonUtility.FromJson<ZirconMapManifest>(
                File.ReadAllText(Path.GetFullPath(manifestPath)));
            if (map?.SampleCells == null || map.Width != width || map.Height != height ||
                map.ViewX != 0 || map.ViewY != 0 ||
                map.ViewWidth != width || map.ViewHeight != height ||
                map.SampleCells.Count != width * height)
                throw new InvalidOperationException("P2-B full " + textureFolder + " manifest is invalid.");

            var floor = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var objects = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (ZirconMapCellManifest cell in map.SampleCells)
            {
                AddMapResource(floor, cell.BackFile, cell.BackImage, false);
                AddMapResource(objects, cell.MiddleFile, cell.MiddleImage, true);
                AddMapResource(objects, cell.FrontFile, cell.FrontImage, true);
            }

            foreach (string relative in floor.Concat(objects))
            {
                string path = Path.GetFullPath("Assets/Generated/Textures/MapData/" +
                                               textureFolder + "/" + relative);
                if (!File.Exists(path) && !IsKnownEmptySourceEntry(relative))
                    throw new InvalidOperationException("P2-B " + textureFolder +
                                                        " resource is missing: " + relative);
            }

            Debug.Log("P2-B " + textureFolder + " validation passed: source=" + map.Source + " view=" +
                      map.ViewX + "," + map.ViewY + "," + map.ViewWidth + "," + map.ViewHeight +
                      " cells=" + map.SampleCells.Count +
                      " floorSprites=" + floor.Count +
                      " objectSprites=" + objects.Count);
        }

        private static bool IsKnownEmptySourceEntry(string relative)
        {
            return KnownEmptySourceEntries.Contains(relative);
        }

        private static void AddMapResource(
            HashSet<string> resources,
            int fileId,
            int image,
            bool storedAsOneBased)
        {
            if (image <= 0 ||
                !ZirconMapDebugRenderer.TryGetMapLibraryFolder(fileId, out string folder))
                return;
            if (storedAsOneBased && fileId != 4 && fileId != 5 && fileId != 10 &&
                fileId != 19 && fileId != 20 && fileId != 23 &&
                fileId != 25 && fileId != 40)
                return;
            int sourceIndex = storedAsOneBased ? image - 1 : image;
            string sourceName = ZirconMapDebugRenderer.GetMapLibrarySourceName(folder);
            resources.Add(folder + "/" + sourceName + "_" + sourceIndex.ToString("D5") + "_image.png");
        }

        private static int[] ProductionDrawFrames()
        {
            var values = new List<int>();
            Add(values, 0, 4);
            Add(values, 80, 6);
            Add(values, 160, 6);
            Add(values, 480, 2);
            Add(values, 560, 5);
            Add(values, 640, 5);
            Add(values, 720, 6);
            Add(values, 1840, 3);
            Add(values, 1920, 10);
            return values.ToArray();
        }

        private static void Add(List<int> values, int start, int count)
        {
            for (int direction = 0; direction < 8; direction++)
            for (int frame = 0; frame < count; frame++)
                values.Add(start + direction * 10 + frame);
        }
    }
}
#endif
