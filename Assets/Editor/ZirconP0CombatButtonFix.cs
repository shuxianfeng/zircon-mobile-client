#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Zircon.Mobile.Game.Input;
using Zircon.Mobile.Game.World;
using Zircon.Mobile.UI.Login;

namespace Zircon.Mobile.Editor
{
    public static class ZirconP0CombatButtonFix
    {
        public static void ApplyFromCommandLine()
        {
            var scene = EditorSceneManager.OpenScene(ZirconProjectBootstrap.ScenePath, OpenSceneMode.Single);
            GameObject root = FindNamed("P0PlayableUI");
            Button select = RequireComponent<Button>("Select");
            Button attack = RequireComponent<Button>("Attack");
            ZirconProtocolProbeBehaviour session = FindSceneObject<ZirconProtocolProbeBehaviour>();
            ZirconTargetCombatBehaviour targetCombat = FindSceneObject<ZirconTargetCombatBehaviour>();
            ZirconWorldDebugRenderer worldRenderer = FindSceneObject<ZirconWorldDebugRenderer>();
            ZirconMobileGameplayControlsBehaviour movement = FindSceneObject<ZirconMobileGameplayControlsBehaviour>();
            if (root == null || session == null || targetCombat == null || worldRenderer == null || movement == null)
                throw new InvalidOperationException("P0 combat button dependencies are incomplete.");

            // MovementPad keeps movement and pickup. The coordinator below owns
            // target/attack so each button has exactly one runtime listener.
            SerializedObject movementObject = new SerializedObject(movement);
            movementObject.FindProperty("selectButton").objectReferenceValue = null;
            movementObject.FindProperty("attackButton").objectReferenceValue = null;
            movementObject.ApplyModifiedPropertiesWithoutUndo();

            ZirconMobileCombatButtonsBehaviour coordinator = root.GetComponent<ZirconMobileCombatButtonsBehaviour>();
            if (coordinator == null) coordinator = root.AddComponent<ZirconMobileCombatButtonsBehaviour>();
            SerializedObject coordinatorObject = new SerializedObject(coordinator);
            coordinatorObject.FindProperty("session").objectReferenceValue = session;
            coordinatorObject.FindProperty("targetCombat").objectReferenceValue = targetCombat;
            coordinatorObject.FindProperty("worldRenderer").objectReferenceValue = worldRenderer;
            coordinatorObject.FindProperty("selectButton").objectReferenceValue = select;
            coordinatorObject.FindProperty("attackButton").objectReferenceValue = attack;
            coordinatorObject.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("P0 combat button fix applied: target/attack isolated from generic UI pointer releases.");
        }

        private static T RequireComponent<T>(string objectName) where T : Component
        {
            GameObject item = FindNamed(objectName);
            T component = item == null ? null : item.GetComponent<T>();
            if (component == null) throw new InvalidOperationException(objectName + " " + typeof(T).Name + " not found.");
            return component;
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
