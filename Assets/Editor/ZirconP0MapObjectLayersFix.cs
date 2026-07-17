#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Zircon.Mobile.Game.World;
using Zircon.Mobile.UI.Login;

namespace Zircon.Mobile.Editor
{
    public static class ZirconP0MapObjectLayersFix
    {
        public static void ApplyFromCommandLine()
        {
            var scene = EditorSceneManager.OpenScene(ZirconProjectBootstrap.ScenePath, OpenSceneMode.Single);
            ZirconProtocolProbeBehaviour session = FindSceneObject<ZirconProtocolProbeBehaviour>();
            ZirconMapDebugRenderer map = FindSceneObject<ZirconMapDebugRenderer>();
            if (session == null || map == null)
                throw new InvalidOperationException("Map object layer dependencies are incomplete.");

            ZirconAndroidMapObjectLayersBehaviour behaviour = map.GetComponent<ZirconAndroidMapObjectLayersBehaviour>();
            if (behaviour == null)
                behaviour = map.gameObject.AddComponent<ZirconAndroidMapObjectLayersBehaviour>();
            SerializedObject serialized = new SerializedObject(behaviour);
            serialized.FindProperty("session").objectReferenceValue = session;
            serialized.FindProperty("mapRenderer").objectReferenceValue = map;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("P0 D2401 middle/front object layer renderer bound.");
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
