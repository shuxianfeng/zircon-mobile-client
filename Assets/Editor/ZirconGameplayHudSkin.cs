#if UNITY_EDITOR
using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Zircon.Mobile.Game.Input;

namespace Zircon.Mobile.Editor
{
    /// <summary>
    /// One-time scene styling pass for the playable landscape HUD. It changes
    /// presentation only; the existing buttons and their listeners are kept.
    /// </summary>
    public static class ZirconGameplayHudSkin
    {
        private const string PlatePath = "Assets/Art/UI/action-plate.png";
        private const string JoystickPath = "Assets/Art/UI/joystick-base.png";
        private const string SwordsPath = "Assets/Art/UI/crossed-swords.png";
        private const float JoystickDiameter = 357f;
        private const float AttackDiameter = 222f;

        private static readonly Color Ivory = new Color(0.96f, 0.91f, 0.79f, 1f);
        private static readonly Color MutedGold = new Color(0.78f, 0.66f, 0.42f, 1f);

        public static void ApplyFromCommandLine()
        {
            // The scene still uses the old touch-relative joystick. Bind the
            // existing fixed-centre controller before giving it a visible base.
            ZirconP0FixedJoystickFix.ApplyFromCommandLine();
            ConfigureSprite(PlatePath);
            ConfigureSprite(JoystickPath);
            ConfigureSprite(SwordsPath);
            Sprite plate = RequireSprite(PlatePath);
            Sprite joystick = RequireSprite(JoystickPath);
            Sprite swords = RequireSprite(SwordsPath);

            var scene = EditorSceneManager.OpenScene(ZirconProjectBootstrap.ScenePath, OpenSceneMode.Single);
            Transform hud = FindSceneTransform("HUD");
            Transform controls = hud.Find("P0PlayableUI");
            if (controls == null) throw new InvalidOperationException("P0PlayableUI is missing from HUD.");

            StyleStatus(RequireChild(hud, "Status"));
            StyleNavigation(hud, plate);
            StyleJoystick(RequireChild(controls, "MovementPad"), joystick, plate);
            StyleCombat(controls, plate, swords);
            StyleBuffs(controls, plate);
            StyleSecondaryPanels(hud, controls);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Gameplay HUD skin applied: round joystick and combat buttons, compact navigation, status trim.");
        }

        private static void ConfigureSprite(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("UI texture not found: " + path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 512;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        private static Sprite RequireSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) throw new InvalidOperationException("UI sprite could not be loaded: " + path);
            return sprite;
        }

        private static Transform FindSceneTransform(string name)
        {
            foreach (Transform transform in Resources.FindObjectsOfTypeAll<Transform>())
                if (transform.gameObject.scene.IsValid() && transform.name == name)
                    return transform;
            throw new InvalidOperationException("Scene object not found: " + name);
        }

        private static Transform RequireChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child == null) throw new InvalidOperationException(parent.name + "/" + name + " is missing.");
            return child;
        }

        private static void Record(UnityEngine.Object item)
        {
            if (PrefabUtility.IsPartOfPrefabInstance(item))
                PrefabUtility.RecordPrefabInstancePropertyModifications(item);
            EditorUtility.SetDirty(item);
        }

        private static void PlaceCircle(Transform transform, float x, float y, float diameter)
        {
            RectTransform rect = transform as RectTransform;
            if (rect == null) throw new InvalidOperationException(transform.name + " has no RectTransform.");
            rect.anchorMin = new Vector2(x, y);
            rect.anchorMax = new Vector2(x, y);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(diameter, diameter);
            Record(rect);
        }

        private static void StylePlateButton(
            Transform transform, Sprite plate, string label, int fontSize, bool compact = false)
        {
            Button button = transform.GetComponent<Button>();
            Image image = transform.GetComponent<Image>();
            TMP_Text text = transform.GetComponentInChildren<TMP_Text>(true);
            if (button == null || image == null || text == null)
                throw new InvalidOperationException("HUD button is incomplete: " + transform.name);
            image.sprite = plate;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
            image.raycastTarget = true;
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.93f, 0.72f, 1f);
            colors.pressedColor = new Color(0.65f, 0.55f, 0.38f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.72f, 0.72f, 0.72f, 0.82f);
            colors.fadeDuration = 0.09f;
            button.colors = colors;
            text.text = label;
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Bold;
            text.color = Ivory;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.enableWordWrapping = compact;
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10f, 10f);
            textRect.offsetMax = new Vector2(-10f, -10f);
            Record(image);
            Record(button);
            Record(text);
            Record(textRect);
        }

        private static void StyleStatus(Transform status)
        {
            RectTransform rect = status as RectTransform;
            Image image = status.GetComponent<Image>();
            if (rect == null || image == null) throw new InvalidOperationException("Status is incomplete.");
            rect.sizeDelta = new Vector2(0f, 94f);
            rect.anchoredPosition = new Vector2(0f, -47f);
            image.color = new Color(0.035f, 0.043f, 0.052f, 0.86f);
            Record(rect);
            Record(image);

            TMP_Text name = RequireChild(status, "Name").GetComponent<TMP_Text>();
            RectTransform nameRect = name.rectTransform;
            nameRect.anchorMin = nameRect.anchorMax = new Vector2(0f, 0.5f);
            nameRect.pivot = new Vector2(0f, 0.5f);
            nameRect.anchoredPosition = new Vector2(24f, 0f);
            nameRect.sizeDelta = new Vector2(300f, 44f);
            name.alignment = TextAlignmentOptions.Left;
            name.fontSize = 27f;
            name.color = Ivory;
            Record(nameRect);
            Record(name);
            foreach (string value in new[] { "HPText", "MPText", "Gold", "Location" })
            {
                TMP_Text label = RequireChild(status, value).GetComponent<TMP_Text>();
                label.color = value == "Gold" ? MutedGold : Ivory;
                label.fontSize = value == "HPText" || value == "MPText" ? 18f : 20f;
                Record(label);
            }

            Transform existing = status.Find("GoldTrim");
            GameObject trim = existing == null
                ? new GameObject("GoldTrim", typeof(RectTransform), typeof(Image))
                : existing.gameObject;
            trim.transform.SetParent(status, false);
            RectTransform trimRect = trim.GetComponent<RectTransform>();
            trimRect.anchorMin = new Vector2(0f, 0f);
            trimRect.anchorMax = new Vector2(1f, 0f);
            trimRect.pivot = new Vector2(0.5f, 0f);
            trimRect.anchoredPosition = Vector2.zero;
            trimRect.sizeDelta = new Vector2(0f, 2f);
            Image trimImage = trim.GetComponent<Image>();
            trimImage.color = new Color(0.61f, 0.45f, 0.22f, 0.85f);
            trimImage.raycastTarget = false;
            Record(trimRect);
            Record(trimImage);
        }

        private static void StyleNavigation(Transform hud, Sprite plate)
        {
            string[] names = { "Inventory", "Skills", "Quests", "Map", "NPC", "Storage", "Social", "Mail", "Market" };
            string[] labels = { "背包", "技能", "任务", "地图", "NPC", "仓库", "社交", "邮件", "市场" };
            float[] x = { 0.68f, 0.75f, 0.82f, 0.89f, 0.96f, 0.72f, 0.79f, 0.86f, 0.93f };
            float[] y = { 0.83f, 0.83f, 0.83f, 0.83f, 0.83f, 0.73f, 0.73f, 0.73f, 0.73f };
            for (int i = 0; i < names.Length; i++)
            {
                Transform button = RequireChild(hud, names[i]);
                PlaceCircle(button, x[i], y[i], 86f);
                StylePlateButton(button, plate, labels[i], labels[i].Length > 2 ? 18 : 21);
            }
        }

        private static void StyleJoystick(Transform pad, Sprite baseSprite, Sprite plate)
        {
            PlaceCircle(pad, 0.12f, 0.19f, JoystickDiameter);
            Image image = pad.GetComponent<Image>();
            if (image == null) throw new InvalidOperationException("MovementPad Image is missing.");
            image.sprite = baseSprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = new Color(1f, 1f, 1f, 0.76f);
            image.raycastTarget = true;
            Record(image);

            Transform knob = RequireChild(pad, "Knob");
            RectTransform knobRect = knob as RectTransform;
            knobRect.sizeDelta = new Vector2(138f, 138f);
            Image knobImage = knob.GetComponent<Image>();
            knobImage.sprite = plate;
            knobImage.type = Image.Type.Simple;
            knobImage.preserveAspect = true;
            knobImage.color = new Color(1f, 1f, 1f, 0.95f);
            knobImage.raycastTarget = false;
            Record(knobRect);
            Record(knobImage);

            ZirconFixedCenterJoystickBehaviour controller = pad.GetComponent<ZirconFixedCenterJoystickBehaviour>();
            if (controller == null) throw new InvalidOperationException("Fixed joystick behaviour is missing.");
            SerializedObject serialized = new SerializedObject(controller);
            serialized.FindProperty("deadZonePixels").floatValue = 24f;
            serialized.FindProperty("knobRadiusPixels").floatValue = 96f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Record(controller);
        }

        private static void StyleCombat(Transform controls, Sprite plate, Sprite swords)
        {
            Transform attack = RequireChild(controls, "Attack");
            PlaceCircle(attack, 0.90f, 0.19f, AttackDiameter);
            StylePlateButton(attack, plate, "攻击", 31);
            TMP_Text attackText = attack.GetComponentInChildren<TMP_Text>(true);
            RectTransform attackTextRect = attackText.rectTransform;
            attackTextRect.anchorMin = new Vector2(0.16f, 0.10f);
            attackTextRect.anchorMax = new Vector2(0.84f, 0.39f);
            attackTextRect.offsetMin = Vector2.zero;
            attackTextRect.offsetMax = Vector2.zero;
            Record(attackTextRect);
            Transform icon = attack.Find("CrossedSwords");
            GameObject iconObject = icon == null
                ? new GameObject("CrossedSwords", typeof(RectTransform), typeof(Image))
                : icon.gameObject;
            iconObject.transform.SetParent(attack, false);
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = new Vector2(0f, 25.5f);
            iconRect.sizeDelta = new Vector2(114f, 114f);
            Image iconImage = iconObject.GetComponent<Image>();
            iconImage.sprite = swords;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            Record(iconRect);
            Record(iconImage);

            string[] names = { "Select", "Pickup", "Skill1", "Skill2", "Skill3", "Skill4", "ChatOpen" };
            string[] labels = { "锁定", "拾取", "技能1", "技能2", "技能3", "技能4", "聊天" };
            float[] x = { 0.73f, 0.81f, 0.58f, 0.66f, 0.74f, 0.82f, 0.30f };
            float[] y = { 0.23f, 0.29f, 0.08f, 0.08f, 0.08f, 0.08f, 0.08f };
            float[] sizes = { 86f, 82f, 82f, 82f, 82f, 82f, 76f };
            for (int i = 0; i < names.Length; i++)
            {
                Transform button = RequireChild(controls, names[i]);
                PlaceCircle(button, x[i], y[i], sizes[i]);
                StylePlateButton(button, plate, labels[i], names[i].StartsWith("Skill", StringComparison.Ordinal) ? 17 : 20);
            }
        }

        private static void StyleBuffs(Transform controls, Sprite plate)
        {
            Transform bar = RequireChild(controls, "BuffBar");
            Transform content = RequireChild(bar, "Content");
            HorizontalLayoutGroup layout = content.GetComponent<HorizontalLayoutGroup>();
            if (layout == null) throw new InvalidOperationException("BuffBar Content layout is missing.");
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            Record(layout);

            Transform template = RequireChild(content, "BuffTemplate");
            RectTransform templateRect = template as RectTransform;
            templateRect.sizeDelta = new Vector2(82f, 82f);
            Record(templateRect);
            StylePlateButton(template, plate, "增益", 15, true);

            TMP_Text details = RequireChild(bar, "Details").GetComponent<TMP_Text>();
            details.color = Ivory;
            details.fontSize = 17f;
            Record(details);
        }

        private static void StyleSecondaryPanels(Transform hud, Transform controls)
        {
            foreach (Transform child in hud)
            {
                if (!child.name.EndsWith("Panel", StringComparison.Ordinal)) continue;
                Image image = child.GetComponent<Image>();
                if (image != null)
                {
                    image.color = new Color(0.055f, 0.064f, 0.076f, 0.97f);
                    Record(image);
                }
            }
            foreach (string name in new[] { "ChatPanel", "NpcDialog" })
            {
                Image image = RequireChild(controls, name).GetComponent<Image>();
                if (image != null)
                {
                    image.color = new Color(0.055f, 0.064f, 0.076f, 0.97f);
                    Record(image);
                }
            }
        }
    }
}
#endif
