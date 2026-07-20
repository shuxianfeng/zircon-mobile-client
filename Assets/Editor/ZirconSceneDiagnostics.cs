#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Zircon.Mobile.Editor
{
    public static class ZirconSceneDiagnostics
    {
        public static void ValidateFromCommandLine()
        {
            string scenePath = EditorBuildSettings.scenes.Length > 0
                ? EditorBuildSettings.scenes[0].path
                : "Assets/Scenes/ZirconMobile.unity";
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            GameObject runtime = GameObject.Find("Runtime");
            if (runtime == null)
                throw new InvalidOperationException("Runtime GameObject is missing from " + scenePath);

            Component[] components = runtime.GetComponents<Component>();
            int missing = 0;
            for (int index = 0; index < components.Length; index++)
            {
                Component component = components[index];
                if (component == null)
                {
                    missing++;
                    Debug.LogError("Runtime component missing at index=" + index);
                }
                else
                {
                    Debug.Log("Runtime component index=" + index + " type=" + component.GetType().FullName);
                }
            }

            if (missing > 0)
                throw new InvalidOperationException("Runtime has " + missing + " missing script component(s).");
        }
    }
}
#endif
