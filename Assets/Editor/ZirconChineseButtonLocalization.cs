#if UNITY_EDITOR
using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Zircon.Mobile.UI.Layout;

namespace Zircon.Mobile.Editor
{
    public static class ZirconChineseButtonLocalization
    {
        public static void ApplyFromCommandLine()
        {
            var scene = EditorSceneManager.OpenScene(ZirconProjectBootstrap.ScenePath, OpenSceneMode.Single);
            Canvas canvas = FindSceneObject<Canvas>();
            if (canvas == null) throw new InvalidOperationException("Mobile canvas not found.");

            ZirconChineseButtonLabelsBehaviour runtime = canvas.GetComponent<ZirconChineseButtonLabelsBehaviour>();
            if (runtime == null) runtime = canvas.gameObject.AddComponent<ZirconChineseButtonLabelsBehaviour>();

            int translated = 0;
            foreach (Button button in Resources.FindObjectsOfTypeAll<Button>())
            {
                if (!button.gameObject.scene.IsValid())
                    continue;
                TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
                if (label == null)
                    continue;
                string value = ZirconChineseButtonLabelsBehaviour.Translate(label.text);
                if (string.Equals(value, label.text, StringComparison.Ordinal))
                    continue;
                label.text = value;
                translated++;
                EditorUtility.SetDirty(label);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Chinese button localization applied. Labels translated=" + translated + ".");
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
