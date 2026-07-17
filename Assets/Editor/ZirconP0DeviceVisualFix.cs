#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Zircon.Mobile.UI.Layout;
using Zircon.Mobile.UI.Login;

namespace Zircon.Mobile.Editor
{
    public static class ZirconP0DeviceVisualFix
    {
        public static void ApplyFromCommandLine()
        {
            var scene = EditorSceneManager.OpenScene(ZirconProjectBootstrap.ScenePath, OpenSceneMode.Single);

            GameObject backgroundObject = FindNamed("DeviceBackground");
            if (backgroundObject == null) throw new InvalidOperationException("DeviceBackground not found.");
            Image background = backgroundObject.GetComponent<Image>();
            if (background == null) throw new InvalidOperationException("DeviceBackground image not found.");
            ZirconProtocolProbeBehaviour session = FindSceneObject<ZirconProtocolProbeBehaviour>();
            if (session == null) throw new InvalidOperationException("Protocol session not found.");

            ZirconAdaptiveDeviceBackgroundBehaviour controller = backgroundObject.GetComponent<ZirconAdaptiveDeviceBackgroundBehaviour>();
            if (controller == null) controller = backgroundObject.AddComponent<ZirconAdaptiveDeviceBackgroundBehaviour>();
            SerializedObject serializedController = new SerializedObject(controller);
            serializedController.FindProperty("session").objectReferenceValue = session;
            serializedController.FindProperty("background").objectReferenceValue = background;
            serializedController.ApplyModifiedPropertiesWithoutUndo();

            SetAnchor("Select", new Vector2(.70f, .19f));
            SetAnchor("Pickup", new Vector2(.79f, .19f));
            SetAnchor("Attack", new Vector2(.89f, .19f));
            for (int i = 0; i < 4; i++)
                SetAnchor("Skill" + (i + 1), new Vector2(.58f + i * .08f, .075f));

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("P0 device visual fix applied: adaptive shell background and non-overlapping combat controls.");
        }

        private static void SetAnchor(string objectName, Vector2 anchor)
        {
            GameObject item = FindNamed(objectName);
            if (item == null) throw new InvalidOperationException(objectName + " not found.");
            RectTransform rect = item.GetComponent<RectTransform>();
            if (rect == null) throw new InvalidOperationException(objectName + " has no RectTransform.");
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.anchoredPosition = Vector2.zero;
        }

        private static GameObject FindNamed(string objectName)
        {
            foreach (Transform item in Resources.FindObjectsOfTypeAll<Transform>())
                if (item.gameObject.scene.IsValid() && item.name == objectName) return item.gameObject;
            return null;
        }

        private static T FindSceneObject<T>() where T : Component
        {
            foreach (T item in Resources.FindObjectsOfTypeAll<T>())
                if (item.gameObject.scene.IsValid()) return item;
            return null;
        }
    }
}
#endif
