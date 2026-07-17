#if UNITY_EDITOR
using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Zircon.Mobile.Game.World;
using Zircon.Mobile.UI.Catalog;
using Zircon.Mobile.UI.Login;

namespace Zircon.Mobile.Editor
{
    public static class ZirconP0ReadableWorldFix
    {
        public static void ApplyFromCommandLine()
        {
            var scene = EditorSceneManager.OpenScene(ZirconProjectBootstrap.ScenePath, OpenSceneMode.Single);
            Canvas canvas = FindSceneObject<Canvas>();
            ZirconProtocolProbeBehaviour session = FindSceneObject<ZirconProtocolProbeBehaviour>();
            ZirconSystemCatalogBehaviour catalog = FindSceneObject<ZirconSystemCatalogBehaviour>();
            ZirconMapDebugRenderer map = FindSceneObject<ZirconMapDebugRenderer>();
            ZirconWorldDebugRenderer world = FindSceneObject<ZirconWorldDebugRenderer>();
            if (canvas == null || session == null || catalog == null || map == null || world == null)
                throw new InvalidOperationException("Readable world dependencies are incomplete.");

            GameObject root = FindNamed("WorldStatusOverlay");
            if (root == null)
            {
                root = new GameObject("WorldStatusOverlay", typeof(RectTransform), typeof(Image));
                root.transform.SetParent(canvas.transform, false);
            }

            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(900f, 44f);
            rect.anchoredPosition = new Vector2(0f, -8f);
            Image background = root.GetComponent<Image>();
            background.color = new Color(0.02f, 0.025f, 0.035f, 0.82f);
            background.raycastTarget = false;

            TMP_Text text = root.GetComponentInChildren<TMP_Text>(true);
            if (text == null)
            {
                GameObject label = new GameObject("Status", typeof(RectTransform), typeof(TextMeshProUGUI));
                label.transform.SetParent(root.transform, false);
                text = label.GetComponent<TextMeshProUGUI>();
            }

            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12f, 2f);
            textRect.offsetMax = new Vector2(-12f, -2f);
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 22f;
            text.color = Color.white;
            text.raycastTarget = false;
            foreach (TMP_Text existing in FindSceneObjects<TMP_Text>())
                if (existing != text && existing.font != null) { text.font = existing.font; break; }

            SerializedObject worldObject = new SerializedObject(world);
            worldObject.FindProperty("spriteAnimationFps").floatValue = 0f;
            worldObject.ApplyModifiedPropertiesWithoutUndo();

            ZirconReadableWorldVisualsBehaviour behaviour = root.GetComponent<ZirconReadableWorldVisualsBehaviour>();
            if (behaviour == null)
                behaviour = root.AddComponent<ZirconReadableWorldVisualsBehaviour>();
            SerializedObject serialized = new SerializedObject(behaviour);
            serialized.FindProperty("session").objectReferenceValue = session;
            serialized.FindProperty("catalog").objectReferenceValue = catalog;
            serialized.FindProperty("mapRenderer").objectReferenceValue = map;
            serialized.FindProperty("worldRenderer").objectReferenceValue = world;
            serialized.FindProperty("statusText").objectReferenceValue = text;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("P0 readable world fix applied: debug blocks removed, stable entity frames, status overlay added.");
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

        private static System.Collections.Generic.IEnumerable<T> FindSceneObjects<T>() where T : Component
        {
            foreach (T item in Resources.FindObjectsOfTypeAll<T>())
                if (item.gameObject.scene.IsValid())
                    yield return item;
        }
    }
}
#endif
