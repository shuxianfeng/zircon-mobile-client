#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using Zircon.Mobile.Game.Input;
using Zircon.Mobile.Game.World;
using Zircon.Mobile.UI.Login;

namespace Zircon.Mobile.Editor
{
    public static class ZirconP0MapOneCharacterFix
    {
        public static void ApplyFromCommandLine()
        {
            var scene = EditorSceneManager.OpenScene(ZirconProjectBootstrap.ScenePath, OpenSceneMode.Single);
            ZirconProtocolProbeBehaviour session = Find<ZirconProtocolProbeBehaviour>();
            ZirconMapDebugRenderer map = Find<ZirconMapDebugRenderer>();
            ZirconWorldDebugRenderer world = Find<ZirconWorldDebugRenderer>();
            ZirconFixedCenterJoystickBehaviour joystick = Find<ZirconFixedCenterJoystickBehaviour>();
            if (session == null || map == null || world == null || joystick == null)
                throw new InvalidOperationException("Map 1 / character / joystick dependencies are incomplete.");

            Set(joystick, "repeatSeconds", 0.65f);

            ZirconMapOneVisualLoaderBehaviour floor = map.GetComponent<ZirconMapOneVisualLoaderBehaviour>();
            if (floor == null) floor = map.gameObject.AddComponent<ZirconMapOneVisualLoaderBehaviour>();
            Set(floor, "session", session); Set(floor, "mapRenderer", map);

            ZirconMapOneObjectLayersBehaviour objects = map.GetComponent<ZirconMapOneObjectLayersBehaviour>();
            if (objects == null) objects = map.gameObject.AddComponent<ZirconMapOneObjectLayersBehaviour>();
            Set(objects, "session", session); Set(objects, "mapRenderer", map);

            ZirconComposedLocalPlayerBehaviour old = world.GetComponent<ZirconComposedLocalPlayerBehaviour>();
            if (old != null) old.enabled = false;
            ZirconFemaleWarriorPlayerBehaviour player = world.GetComponent<ZirconFemaleWarriorPlayerBehaviour>();
            if (player == null) player = world.gameObject.AddComponent<ZirconFemaleWarriorPlayerBehaviour>();
            Set(player, "session", session); Set(player, "worldRenderer", world);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("P0 map 1, female warrior and 0.65-second joystick throttle bound.");
        }

        private static void Set(UnityEngine.Object target, string name, object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(name);
            if (property == null) throw new MissingFieldException(target.GetType().Name, name);
            if (value is UnityEngine.Object unityObject) property.objectReferenceValue = unityObject;
            else if (value is float number) property.floatValue = number;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static T Find<T>() where T : Component
        {
            foreach (T item in Resources.FindObjectsOfTypeAll<T>())
                if (item.gameObject.scene.IsValid()) return item;
            return null;
        }
    }

    public sealed class ZirconMapTextureManifestBuildPruner : IPreprocessBuildWithReport
    {
        public int callbackOrder => -900;
        public void OnPreprocessBuild(BuildReport report)
        {
            string root = Path.GetFullPath("Assets/StreamingAssets/Zircon/Generated/Textures/MapData");
            if (!Directory.Exists(root)) return;
            foreach (string file in Directory.GetFiles(root, "*.manifest.json", SearchOption.AllDirectories))
                File.Delete(file);
            AssetDatabase.Refresh();
        }
    }
}
#endif
