#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Zircon.Mobile.Editor
{
    // Compatibility marker used by ZirconP0SceneBootstrap. It exists only while
    // the editor applies the generated P0 layer and is removed before saving.
    public sealed class ZirconMainHudBehaviour : MonoBehaviour { }

    public static class ZirconP0SceneBootstrapRunner
    {
        public static void ApplyFromCommandLine()
        {
            var scene = EditorSceneManager.OpenScene(ZirconProjectBootstrap.ScenePath, OpenSceneMode.Single);
            Zircon.Mobile.UI.HUD.ZirconMainHudBehaviour realHud = null;
            foreach (var item in Resources.FindObjectsOfTypeAll<Zircon.Mobile.UI.HUD.ZirconMainHudBehaviour>())
            {
                if (!item.gameObject.scene.IsValid()) continue;
                realHud = item;
                break;
            }
            if (realHud == null)
                throw new System.InvalidOperationException("Main HUD was not found in the base mobile scene.");

            ZirconMainHudBehaviour marker = realHud.gameObject.AddComponent<ZirconMainHudBehaviour>();
            EditorSceneManager.SaveScene(scene);
            ZirconP0SceneBootstrap.Apply();
            Object.DestroyImmediate(marker);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }
    }
}
#endif
