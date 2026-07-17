#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Zircon.Mobile.Editor
{
    public static class ZirconHudRawReferenceCleaner
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
            if (components.arraySize != 4) throw new System.InvalidOperationException("Unexpected HUD component count: " + components.arraySize);
            SerializedProperty reference = components.GetArrayElementAtIndex(3).FindPropertyRelative("component");
            UnityEngine.Object raw = reference.objectReferenceValue;
            if (!object.ReferenceEquals(raw, null)) UnityEngine.Object.DestroyImmediate(raw, true);
            reference.objectReferenceValue = null;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            components.DeleteArrayElementAtIndex(3);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(hud);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Raw fourth HUD component reference removed. Runtime type=" + (object.ReferenceEquals(raw, null) ? "null" : raw.GetType().FullName));
        }
    }
}
#endif
