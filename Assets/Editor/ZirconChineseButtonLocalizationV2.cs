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
    public static class ZirconChineseButtonLocalizationV2
    {
        public static void ApplyFromCommandLine()
        {
            var scene = EditorSceneManager.OpenScene(ZirconProjectBootstrap.ScenePath, OpenSceneMode.Single);
            Canvas canvas = FindSceneObject<Canvas>();
            if (canvas == null) throw new InvalidOperationException("Mobile canvas not found.");

            foreach (MonoBehaviour component in canvas.GetComponents<MonoBehaviour>())
            {
                if (component != null && component.GetType().FullName == "Zircon.Mobile.UI.Layout.ZirconChineseButtonLabelsBehaviour")
                    UnityEngine.Object.DestroyImmediate(component);
            }

            ZirconChineseButtonsRuntimeBehaviour runtime = canvas.GetComponent<ZirconChineseButtonsRuntimeBehaviour>();
            if (runtime == null) runtime = canvas.gameObject.AddComponent<ZirconChineseButtonsRuntimeBehaviour>();

            int translated = 0;
            int remainingEnglish = 0;
            foreach (Button button in Resources.FindObjectsOfTypeAll<Button>())
            {
                if (!button.gameObject.scene.IsValid())
                    continue;
                TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
                if (label == null)
                    continue;
                string value = ZirconChineseButtonsRuntimeBehaviour.Translate(label.text);
                if (!string.Equals(value, label.text, StringComparison.Ordinal))
                {
                    label.text = value;
                    translated++;
                    EditorUtility.SetDirty(label);
                }
                if (ContainsAsciiLetter(label.text) && IsFixedControlName(button.gameObject.name))
                    remainingEnglish++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Chinese button localization V2 applied. Labels translated=" + translated + ", fixed English remaining=" + remainingEnglish + ".");
        }

        private static bool ContainsAsciiLetter(string value)
        {
            foreach (char ch in value ?? string.Empty)
                if ((ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z')) return true;
            return false;
        }

        private static bool IsFixedControlName(string value)
        {
            return value != "Character" && value != "Template" && value != "BuffTemplate";
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
