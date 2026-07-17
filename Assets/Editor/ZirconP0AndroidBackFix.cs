#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Zircon.Mobile.UI.Layout;

namespace Zircon.Mobile.Editor
{
    public static class ZirconP0AndroidBackFix
    {
        public static void ApplyFromCommandLine()
        {
            var scene = EditorSceneManager.OpenScene(ZirconProjectBootstrap.ScenePath, OpenSceneMode.Single);
            GameObject root = FindNamed("P0PlayableUI");
            ZirconPanelNavigationBehaviour navigation = FindSceneObject<ZirconPanelNavigationBehaviour>();
            if (root == null || navigation == null)
                throw new InvalidOperationException("P0 panel navigation dependencies are incomplete.");

            SerializedObject navigationObject = new SerializedObject(navigation);
            SerializedProperty sourcePanels = navigationObject.FindProperty("panels");
            if (sourcePanels == null || !sourcePanels.isArray || sourcePanels.arraySize == 0)
                throw new InvalidOperationException("P0 panel array is empty.");

            ZirconAndroidBackPanelBehaviour handler = root.GetComponent<ZirconAndroidBackPanelBehaviour>();
            if (handler == null)
                handler = root.AddComponent<ZirconAndroidBackPanelBehaviour>();

            SerializedObject handlerObject = new SerializedObject(handler);
            SerializedProperty targetPanels = handlerObject.FindProperty("panels");
            targetPanels.arraySize = sourcePanels.arraySize;
            for (int i = 0; i < sourcePanels.arraySize; i++)
                targetPanels.GetArrayElementAtIndex(i).objectReferenceValue =
                    sourcePanels.GetArrayElementAtIndex(i).objectReferenceValue;
            handlerObject.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("P0 Android Back fix applied to " + sourcePanels.arraySize + " panels.");
        }

        private static GameObject FindNamed(string objectName)
        {
            foreach (Transform item in Resources.FindObjectsOfTypeAll<Transform>())
                if (item.gameObject.scene.IsValid() && item.name == objectName)
                    return item.gameObject;
            return null;
        }

        private static T FindSceneObject<T>() where T : Component
        {
            foreach (T item in Resources.FindObjectsOfTypeAll<T>())
                if (item.gameObject.scene.IsValid())
                    return item;
            return null;
        }
    }
}
#endif
