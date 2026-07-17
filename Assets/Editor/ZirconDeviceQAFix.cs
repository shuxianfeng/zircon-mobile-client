#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Zircon.Mobile.Game.World;

namespace Zircon.Mobile.Editor
{
    public static class ZirconDeviceQAFix
    {
        public static void ApplyFromCommandLine()
        {
            var scene = EditorSceneManager.OpenScene(ZirconProjectBootstrap.ScenePath, OpenSceneMode.Single);
            GameObject hud = FindNamed("HUD");
            if (hud == null) throw new InvalidOperationException("HUD not found.");

            int removed = 0;
            foreach (MonoBehaviour component in hud.GetComponents<MonoBehaviour>())
            {
                if (component == null) continue;
                string assembly = component.GetType().Assembly.GetName().Name;
                if (!assembly.EndsWith("-Editor", StringComparison.OrdinalIgnoreCase)) continue;
                UnityEngine.Object.DestroyImmediate(component);
                removed++;
            }

            Canvas canvas = FindSceneObject<Canvas>();
            if (canvas == null) throw new InvalidOperationException("Mobile canvas not found.");
            Transform oldBackground = canvas.transform.Find("DeviceBackground");
            if (oldBackground != null) UnityEngine.Object.DestroyImmediate(oldBackground.gameObject);
            GameObject background = new GameObject("DeviceBackground", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(canvas.transform, false);
            background.transform.SetSiblingIndex(0);
            RectTransform rect = (RectTransform)background.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            Image image = background.GetComponent<Image>();
            image.color = new Color32(13, 18, 24, 255); image.raycastTarget = false;

            ZirconMapDebugRenderer map = FindSceneObject<ZirconMapDebugRenderer>();
            if (map == null) throw new InvalidOperationException("Map renderer not found.");
            SerializedObject mapObject = new SerializedObject(map);
            mapObject.FindProperty("renderOnStart").boolValue = false;
            mapObject.ApplyModifiedPropertiesWithoutUndo();
            ZirconAndroidMapLoaderBehaviour loader = map.GetComponent<ZirconAndroidMapLoaderBehaviour>();
            if (loader == null) loader = map.gameObject.AddComponent<ZirconAndroidMapLoaderBehaviour>();
            SerializedObject loaderObject = new SerializedObject(loader);
            loaderObject.FindProperty("mapRenderer").objectReferenceValue = map;
            loaderObject.ApplyModifiedPropertiesWithoutUndo();

            foreach (MonoBehaviour component in hud.GetComponents<MonoBehaviour>())
                if (component != null && component.GetType().Assembly.GetName().Name.EndsWith("-Editor", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Editor-only HUD component remains: " + component.GetType().FullName);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Device QA fix applied. Removed editor-only HUD components=" + removed + ", added full-canvas background and StreamingAssets map loader.");
        }

        private static GameObject FindNamed(string name)
        {
            foreach (Transform item in Resources.FindObjectsOfTypeAll<Transform>())
                if (item.gameObject.scene.IsValid() && item.name == name) return item.gameObject;
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
