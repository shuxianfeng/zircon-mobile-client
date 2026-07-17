#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Zircon.Mobile.Game.Input;
using Zircon.Mobile.Game.World;
using Zircon.Mobile.UI.Login;

namespace Zircon.Mobile.Editor
{
    public static class ZirconP0FixedJoystickFix
    {
        public static void ApplyFromCommandLine()
        {
            var scene = EditorSceneManager.OpenScene(ZirconProjectBootstrap.ScenePath, OpenSceneMode.Single);
            GameObject padObject = FindNamed("MovementPad");
            Transform knob = padObject == null ? null : padObject.transform.Find("Knob");
            ZirconProtocolProbeBehaviour session = FindSceneObject<ZirconProtocolProbeBehaviour>();
            ZirconMapDebugRenderer map = FindSceneObject<ZirconMapDebugRenderer>();
            if (padObject == null || knob == null || session == null || map == null)
                throw new InvalidOperationException("Fixed joystick dependencies are incomplete.");

            // The legacy component still owns the pickup button. Disable only
            // its movement by making its dead zone unreachable.
            ZirconMobileGameplayControlsBehaviour legacy = padObject.GetComponent<ZirconMobileGameplayControlsBehaviour>();
            if (legacy != null)
            {
                var legacyObject = new SerializedObject(legacy);
                legacyObject.FindProperty("deadZonePixels").floatValue = 100000f;
                legacyObject.ApplyModifiedPropertiesWithoutUndo();
            }

            ZirconFixedCenterJoystickBehaviour fixedJoystick = padObject.GetComponent<ZirconFixedCenterJoystickBehaviour>();
            if (fixedJoystick == null) fixedJoystick = padObject.AddComponent<ZirconFixedCenterJoystickBehaviour>();
            var serialized = new SerializedObject(fixedJoystick);
            serialized.FindProperty("session").objectReferenceValue = session;
            serialized.FindProperty("mapRenderer").objectReferenceValue = map;
            serialized.FindProperty("pad").objectReferenceValue = padObject.GetComponent<RectTransform>();
            serialized.FindProperty("knob").objectReferenceValue = knob.GetComponent<RectTransform>();
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("P0 fixed-centre joystick bound; legacy movement disabled.");
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
