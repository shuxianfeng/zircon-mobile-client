#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Zircon.Mobile.Editor
{
    public static class ZirconEditorMarkerCleaner
    {
        public static void CleanFromCommandLine()
        {
            var scene = EditorSceneManager.OpenScene(ZirconProjectBootstrap.ScenePath, OpenSceneMode.Single);
            GameObject hud = null;
            foreach (Transform item in Resources.FindObjectsOfTypeAll<Transform>())
                if (item.gameObject.scene.IsValid() && item.name == "HUD") { hud = item.gameObject; break; }
            if (hud == null) throw new InvalidOperationException("HUD not found.");

            SerializedObject gameObject = new SerializedObject(hud);
            SerializedProperty components = gameObject.FindProperty("m_Component");
            int removed = 0;
            for (int index = components.arraySize - 1; index >= 0; index--)
            {
                UnityEngine.Object component = components.GetArrayElementAtIndex(index).FindPropertyRelative("component").objectReferenceValue;
                if (!(component is MonoBehaviour behaviour)) continue;
                SerializedObject componentObject = new SerializedObject(behaviour);
                MonoScript script = componentObject.FindProperty("m_Script")?.objectReferenceValue as MonoScript;
                Type type = script?.GetClass();
                bool editorOnly = type != null && type.Assembly.GetName().Name.EndsWith("-Editor", StringComparison.OrdinalIgnoreCase);
                bool marker = script != null && script.name == "ZirconP0SceneBootstrapRunner";
                if (!editorOnly && !marker) continue;
                UnityEngine.Object.DestroyImmediate(behaviour, true);
                removed++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Editor marker cleanup removed HUD components=" + removed);
        }
    }
}
#endif
