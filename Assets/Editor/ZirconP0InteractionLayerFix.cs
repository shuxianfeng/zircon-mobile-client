#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Zircon.Mobile.Editor
{
    public static class ZirconP0InteractionLayerFix
    {
        private static readonly HashSet<string> NavigationLabels = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Inventory", "Skills", "Quests", "Map", "NPC", "Storage", "Social", "Mail", "Market"
        };

        public static void ApplyFromCommandLine()
        {
            var scene = EditorSceneManager.OpenScene(ZirconProjectBootstrap.ScenePath, OpenSceneMode.Single);
            GameObject root = FindNamed("P0PlayableUI");
            if (root == null) throw new InvalidOperationException("P0PlayableUI not found.");

            // Base feature panels live beside P0PlayableUI. Keep them later in the
            // Canvas order so their controls receive input while a panel is open.
            root.transform.SetAsFirstSibling();

            SetAnchor("Select", new Vector2(.67f, .19f));
            SetAnchor("Pickup", new Vector2(.79f, .19f));
            SetAnchor("Attack", new Vector2(.87f, .19f));
            for (int i = 0; i < 4; i++)
                SetAnchor("Skill" + (i + 1), new Vector2(.57f + i * .08f, .075f));

            foreach (Button button in FindSceneObjects<Button>())
            {
                TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
                if (label == null || !NavigationLabels.Contains(label.text.Trim()))
                    continue;

                RectTransform rect = button.GetComponent<RectTransform>();
                if (rect == null)
                    continue;

                rect.anchorMin = new Vector2(.955f, rect.anchorMin.y);
                rect.anchorMax = new Vector2(.955f, rect.anchorMax.y);
                rect.anchoredPosition = new Vector2(0f, rect.anchoredPosition.y);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("P0 interaction layer fix applied: panels above gameplay controls and right navigation inside safe bounds.");
        }

        private static void SetAnchor(string objectName, Vector2 anchor)
        {
            GameObject item = FindNamed(objectName);
            if (item == null) throw new InvalidOperationException(objectName + " not found.");
            RectTransform rect = item.GetComponent<RectTransform>();
            if (rect == null) throw new InvalidOperationException(objectName + " has no RectTransform.");
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.anchoredPosition = Vector2.zero;
        }

        private static GameObject FindNamed(string objectName)
        {
            foreach (Transform item in Resources.FindObjectsOfTypeAll<Transform>())
                if (item.gameObject.scene.IsValid() && item.name == objectName) return item.gameObject;
            return null;
        }

        private static IEnumerable<T> FindSceneObjects<T>() where T : Component
        {
            foreach (T item in Resources.FindObjectsOfTypeAll<T>())
                if (item.gameObject.scene.IsValid()) yield return item;
        }
    }
}
#endif
