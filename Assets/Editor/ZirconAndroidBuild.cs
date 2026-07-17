#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Zircon.Mobile.Editor
{
    public static class ZirconAndroidBuild
    {
        private const string PackageName = "com.zircon.mobile";

        [MenuItem("Zircon/Build Android")]
        public static void BuildAndroid()
        {
                        if (EnabledScenes().Length == 0)
                ZirconProjectBootstrap.SetupProject();
ConfigurePlayer();
            StageGeneratedAssets();
            try
            {
                string[] scenes = EnabledScenes();
                if (scenes.Length == 0) throw new InvalidOperationException("No enabled scenes in Build Settings.");
                Directory.CreateDirectory("Builds/Android");
                BuildReport report = BuildPipeline.BuildPlayer(scenes, "Builds/Android/ZirconMobile.apk", BuildTarget.Android, BuildOptions.None);
                if (report.summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException("Android build failed: " + report.summary.result);
            }
            finally
            {
                ClearStagedGeneratedAssets();
            }
        }

        public static void BuildFromCommandLine() => BuildAndroid();

        private static void ConfigurePlayer()
        {
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, PackageName);
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.Android.bundleVersionCode = 1;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.Android, ManagedStrippingLevel.Medium);
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
        }

        private static string[] EnabledScenes()
        {
            var scenes = new List<string>();
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
                if (scene.enabled) scenes.Add(scene.path);
            return scenes.ToArray();
        }

        private static void StageGeneratedAssets()
        {
            string source = Path.GetFullPath("Assets/Generated");
            string target = Path.GetFullPath("Assets/StreamingAssets/Zircon/Generated");
            ClearStagedGeneratedAssets();
            CopyDirectory(source, target);
            AssetDatabase.Refresh();
        }

        private static void ClearStagedGeneratedAssets()
        {
            string target = Path.GetFullPath("Assets/StreamingAssets/Zircon/Generated");
            if (Directory.Exists(target)) Directory.Delete(target, true);
            string meta = target + ".meta";
            if (File.Exists(meta)) File.Delete(meta);
            AssetDatabase.Refresh();
        }

        private static void CopyDirectory(string source, string target)
        {
            Directory.CreateDirectory(target);
            foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                if (file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)) continue;
                string destination = Path.Combine(target, file.Substring(source.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.Copy(file, destination, true);
            }
        }
    }
}
#endif