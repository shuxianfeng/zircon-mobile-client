#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Zircon.Mobile.Game.World;
using Zircon.Mobile.UI.Login;

namespace Zircon.Mobile.Editor
{
    public static class ZirconP0PlayerAppearanceCaptureFix
    {
        public static void ApplyFromCommandLine()
        {
            var scene = EditorSceneManager.OpenScene(ZirconProjectBootstrap.ScenePath, OpenSceneMode.Single);
            ZirconProtocolProbeBehaviour session = FindSceneObject<ZirconProtocolProbeBehaviour>();
            if (session == null)
                throw new InvalidOperationException("Protocol session was not found.");

            ZirconPlayerAppearanceCaptureBehaviour behaviour = session.GetComponent<ZirconPlayerAppearanceCaptureBehaviour>();
            if (behaviour == null)
                behaviour = session.gameObject.AddComponent<ZirconPlayerAppearanceCaptureBehaviour>();
            var serialized = new SerializedObject(behaviour);
            serialized.FindProperty("session").objectReferenceValue = session;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("P0 player appearance capture bound.");
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
