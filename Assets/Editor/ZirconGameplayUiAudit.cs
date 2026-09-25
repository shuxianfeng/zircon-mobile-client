#if UNITY_EDITOR
using System;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Zircon.Mobile.Editor
{
    public static class ZirconGameplayUiAudit
    {
        public static void DumpFromCommandLine()
        {
            EditorSceneManager.OpenScene(ZirconProjectBootstrap.ScenePath, OpenSceneMode.Single);
            foreach (string rootName in new[] { "HUD", "P0PlayableUI" })
            {
                GameObject root = null;
                foreach (Transform transform in Resources.FindObjectsOfTypeAll<Transform>())
                {
                    if (transform.gameObject.scene.IsValid() && transform.name == rootName)
                    {
                        root = transform.gameObject;
                        break;
                    }
                }
                if (root == null) throw new InvalidOperationException(rootName + " not found.");
                Dump(root.transform, 0);
            }
        }

        private static void Dump(Transform transform, int depth)
        {
            RectTransform rect = transform as RectTransform;
            Image image = transform.GetComponent<Image>();
            Button button = transform.GetComponent<Button>();
            TMP_Text text = transform.GetComponent<TMP_Text>();
            HorizontalLayoutGroup horizontal = transform.GetComponent<HorizontalLayoutGroup>();
            string layout = rect == null ? string.Empty :
                $" anchor={rect.anchorMin}/{rect.anchorMax} pivot={rect.pivot}" +
                $" pos={rect.anchoredPosition} size={rect.sizeDelta}";
            string graphic = image == null ? string.Empty :
                $" image={(image.sprite == null ? "none" : image.sprite.name)} color={image.color}";
            string label = text == null ? string.Empty : $" text={text.text}";
            string group = horizontal == null ? string.Empty :
                $" HORIZONTAL spacing={horizontal.spacing} expand={horizontal.childForceExpandWidth}" +
                $" control={horizontal.childControlWidth} alignment={horizontal.childAlignment}";
            Debug.Log("UIAUDIT " + new string(' ', depth * 2) + transform.name +
                      layout + graphic + (button == null ? string.Empty : " BUTTON") + group + label);
            foreach (Transform child in transform) Dump(child, depth + 1);
        }
    }
}
#endif
