#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Zircon.Mobile.Editor
{
    public static class ZirconHudComponentListCleaner
    {
        public static void CleanFromCommandLine()
        {
            var scene = EditorSceneManager.OpenScene(ZirconProjectBootstrap.ScenePath, OpenSceneMode.Single);
            GameObject hud = null;
            foreach (Transform item in Resources.FindObjectsOfTypeAll<Transform>())
                if (item.gameObject.scene.IsValid() && item.name == "HUD") { hud = item.gameObject; break; }
            if (hud == null) throw new System.InvalidOperationException("HUD not found.");

            SerializedObject serialized = new SerializedObject(hud);
            SerializedProperty components = serialized.FindProperty("m_Component");
            if (components.arraySize != 4)
                throw new System.InvalidOperationException("Unexpected HUD component count: " + components.arraySize);
            components.DeleteArrayElementAtIndex(3);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(hud);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Removed the editor-only fourth HUD component reference.");
        }
    }
}
#endif
