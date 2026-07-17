#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using Zircon.Mobile.Game.World;
using Zircon.Mobile.UI.Login;

namespace Zircon.Mobile.Editor
{
    public static class ZirconP0ComposedPlayerFix
    {
        public static void ApplyFromCommandLine()
        {
            var scene = EditorSceneManager.OpenScene(ZirconProjectBootstrap.ScenePath, OpenSceneMode.Single);
            ZirconProtocolProbeBehaviour session = FindSceneObject<ZirconProtocolProbeBehaviour>();
            ZirconWorldDebugRenderer world = FindSceneObject<ZirconWorldDebugRenderer>();
            if (session == null || world == null)
                throw new InvalidOperationException("Composed player dependencies are incomplete.");

            ZirconComposedLocalPlayerBehaviour behaviour = world.GetComponent<ZirconComposedLocalPlayerBehaviour>();
            if (behaviour == null)
                behaviour = world.gameObject.AddComponent<ZirconComposedLocalPlayerBehaviour>();
            var serialized = new SerializedObject(behaviour);
            serialized.FindProperty("session").objectReferenceValue = session;
            serialized.FindProperty("worldRenderer").objectReferenceValue = world;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("P0 exact composed local player renderer bound.");
        }

        private static T FindSceneObject<T>() where T : Component
        {
            foreach (T item in Resources.FindObjectsOfTypeAll<T>())
                if (item.gameObject.scene.IsValid()) return item;
            return null;
        }
    }

    // Full ZL manifests are useful conversion evidence but are not runtime
    // assets. Remove only their temporary staged copies before Android packing.
    public sealed class ZirconPlayerAppearanceBuildPruner : IPreprocessBuildWithReport
    {
        public int callbackOrder => -1000;
        public void OnPreprocessBuild(BuildReport report)
        {
            string root = Path.GetFullPath("Assets/StreamingAssets/Zircon/Generated/Textures/PlayerAppearance");
            if (!Directory.Exists(root)) return;
            foreach (string file in Directory.GetFiles(root, "*.manifest.json", SearchOption.AllDirectories))
                File.Delete(file);
            AssetDatabase.Refresh();
        }
    }
}
#endif
