#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Zircon.Mobile.Game.World;
using Zircon.Mobile.UI.Login;

namespace Zircon.Mobile.Editor
{
    public static class ZirconP0AndroidVisualFix
    {
        public static void ApplyFromCommandLine()
        {
            var scene = EditorSceneManager.OpenScene(ZirconProjectBootstrap.ScenePath, OpenSceneMode.Single);
            ZirconProtocolProbeBehaviour session = FindSceneObject<ZirconProtocolProbeBehaviour>();
            ZirconMapDebugRenderer map = FindSceneObject<ZirconMapDebugRenderer>();
            ZirconWorldDebugRenderer world = FindSceneObject<ZirconWorldDebugRenderer>();
            if (session == null || map == null || world == null)
                throw new InvalidOperationException("Android visual dependencies are incomplete.");

            ZirconAndroidMapLoaderBehaviour oldLoader = map.GetComponent<ZirconAndroidMapLoaderBehaviour>();
            if (oldLoader != null)
                oldLoader.enabled = false;

            ZirconAndroidVisualAssetLoaderBehaviour loader = map.GetComponent<ZirconAndroidVisualAssetLoaderBehaviour>();
            if (loader == null)
                loader = map.gameObject.AddComponent<ZirconAndroidVisualAssetLoaderBehaviour>();

            SerializedObject serialized = new SerializedObject(loader);
            serialized.FindProperty("session").objectReferenceValue = session;
            serialized.FindProperty("mapRenderer").objectReferenceValue = map;
            serialized.FindProperty("worldRenderer").objectReferenceValue = world;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("P0 Android visual fix applied: live D2401 viewport and entity StreamingAssets loader bound.");
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
