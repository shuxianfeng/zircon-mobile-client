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
            if (session == null || map == null || world == null)
                throw new InvalidOperationException(
                    $"Map / character dependencies are incomplete. session={session != null} map={map != null} world={world != null}.");

            if (joystick != null) Set(joystick, "repeatSeconds", 0.65f);

            ZirconMapOneVisualLoaderBehaviour floor = map.GetComponent<ZirconMapOneVisualLoaderBehaviour>();
            if (floor == null) floor = map.gameObject.AddComponent<ZirconMapOneVisualLoaderBehaviour>();
            Set(floor, "session", session); Set(floor, "mapRenderer", map);

            ZirconMapOneObjectLayersBehaviour objects = map.GetComponent<ZirconMapOneObjectLayersBehaviour>();
            if (objects == null) objects = map.gameObject.AddComponent<ZirconMapOneObjectLayersBehaviour>();
            Set(objects, "session", session); Set(objects, "mapRenderer", map);

            ZirconComposedLocalPlayerBehaviour old = world.GetComponent<ZirconComposedLocalPlayerBehaviour>();
            if (old != null) old.enabled = false;
            int removedMissingScripts = RemoveMissingComponents(world.gameObject);
            ZirconProductionLocalPlayerBehaviour player = world.GetComponent<ZirconProductionLocalPlayerBehaviour>();
            if (player == null) player = world.gameObject.AddComponent<ZirconProductionLocalPlayerBehaviour>();
            BindScriptAsset(player, "Assets/Scripts/Game/World/ZirconProductionLocalPlayerBehaviour.cs");
            Set(player, "session", session); Set(player, "worldRenderer", world);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"P0 map visuals and production local player bound. joystick={joystick != null} removedMissingScripts={removedMissingScripts}.");
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

        private static int RemoveMissingComponents(GameObject target)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty components = serialized.FindProperty("m_Component");
            int removed = 0;
            for (int index = components.arraySize - 1; index >= 0; index--)
            {
                SerializedProperty reference = components.GetArrayElementAtIndex(index).FindPropertyRelative("component");
                if (reference.objectReferenceValue != null) continue;
                components.DeleteArrayElementAtIndex(index);
                removed++;
            }
            if (removed > 0) serialized.ApplyModifiedPropertiesWithoutUndo();
            return removed;
        }

        private static void BindScriptAsset(MonoBehaviour target, string assetPath)
        {
            MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(assetPath);
            if (script == null || script.GetClass() != target.GetType())
                throw new InvalidOperationException($"Unable to bind MonoScript asset {assetPath} to {target.GetType().FullName}.");
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty("m_Script");
            property.objectReferenceValue = script;
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
            {
                if (file.IndexOf(Path.DirectorySeparatorChar + "Map6" + Path.DirectorySeparatorChar,
                        StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;
                File.Delete(file);
            }
            AssetDatabase.Refresh();
        }
    }
}
#endif
