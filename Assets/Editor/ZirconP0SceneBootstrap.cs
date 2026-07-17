#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Zircon.Mobile.Game.Input;
using Zircon.Mobile.Game.Skills;
using Zircon.Mobile.Game.World;
using Zircon.Mobile.UI.Buffs;
using Zircon.Mobile.UI.Catalog;
using Zircon.Mobile.UI.Chat;
using Zircon.Mobile.UI.Inventory;
using Zircon.Mobile.UI.Layout;
using Zircon.Mobile.UI.Login;
using Zircon.Mobile.UI.Npc;
using Zircon.Mobile.UI.Skills;
using Zircon.Mobile.UI.Storage;

namespace Zircon.Mobile.Editor
{
    public static class ZirconP0SceneBootstrap
    {
        private static readonly Color Ink = new Color32(13, 18, 24, 255);
        private static readonly Color Panel = new Color32(28, 35, 43, 248);
        private static readonly Color PanelSoft = new Color32(43, 52, 62, 235);
        private static readonly Color Accent = new Color32(211, 163, 60, 255);
        private static readonly Color Muted = new Color32(155, 170, 181, 255);
        private static TMP_FontAsset font;
        private static GameObject buttonPrefab;

        [MenuItem("Zircon/Apply P0 Playable UI")]
        public static void Apply()
        {
            var scene = EditorSceneManager.OpenScene(ZirconProjectBootstrap.ScenePath, OpenSceneMode.Single);
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Generated/UI/ZirconMobileFont.asset");
            buttonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ZirconButton.prefab");
            if (font == null || buttonPrefab == null)
                throw new InvalidOperationException("Run Zircon/Setup Mobile Project before applying P0 UI.");

            ZirconProtocolProbeBehaviour session = FindSceneObject<ZirconProtocolProbeBehaviour>();
            ZirconSystemCatalogBehaviour catalog = FindSceneObject<ZirconSystemCatalogBehaviour>();
            ZirconWorldDebugRenderer world = FindSceneObject<ZirconWorldDebugRenderer>();
            ZirconMapDebugRenderer map = FindSceneObject<ZirconMapDebugRenderer>();
            ZirconMainHudBehaviour hudBehaviour = FindSceneObject<ZirconMainHudBehaviour>();
            Camera camera = FindSceneObject<Camera>();
            if (session == null || catalog == null || world == null || map == null || hudBehaviour == null || camera == null)
                throw new InvalidOperationException("Base mobile scene is incomplete.");

            GameObject hud = hudBehaviour.gameObject;
            Transform old = hud.transform.Find("P0PlayableUI");
            if (old != null)
                UnityEngine.Object.DestroyImmediate(old.gameObject);

            GameObject root = Rect("P0PlayableUI", hud.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            CreateGameplayControls(root.transform, session, world, map, camera, out ZirconMobileSkillButtonBehaviour[] hotbar);
            CreateBuffBar(root.transform, session);

            var extraButtons = new List<Button>();
            var extraPanels = new List<GameObject>();
            var extraCloses = new List<Button>();
            BuildInventory(FindChild(hud.transform, "InventoryPanel"), session, catalog);
            BuildSkills(FindChild(hud.transform, "SkillsPanel"), session, catalog, hotbar);
            BuildNpcServices(FindChild(hud.transform, "NPCPanel"), session, catalog);
            BuildStorage(FindChild(hud.transform, "StoragePanel"), session, catalog);
            BuildChat(root.transform, session, extraButtons, extraPanels, extraCloses);
            BuildNpcDialog(root.transform, session, catalog);
            AppendNavigation(hud.GetComponent<ZirconPanelNavigationBehaviour>(), extraButtons, extraPanels, extraCloses);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("P0 playable mobile UI applied: movement, combat, pickup, hotbar, Buff, chat, inventory, skills, NPC, and storage.");
        }

        public static void ApplyFromCommandLine() => Apply();

        private static void CreateGameplayControls(Transform parent, ZirconProtocolProbeBehaviour session, ZirconWorldDebugRenderer world, ZirconMapDebugRenderer map, Camera camera, out ZirconMobileSkillButtonBehaviour[] hotbar)
        {
            GameObject pad = Rect("MovementPad", parent, new Vector2(0.02f, 0.04f), new Vector2(0.22f, 0.39f), Vector2.zero, Vector2.zero);
            Image padImage = pad.AddComponent<Image>();
            padImage.color = new Color(1f, 1f, 1f, 0.09f);
            GameObject knobGo = Rect("Knob", pad.transform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            RectTransform knob = (RectTransform)knobGo.transform;
            knob.sizeDelta = new Vector2(92, 92);
            knobGo.AddComponent<Image>().color = new Color(Accent.r, Accent.g, Accent.b, .65f);

            Button select = Button("Select", parent, "TARGET", new Vector2(.72f, .19f), new Vector2(150, 58));
            Button attack = Button("Attack", parent, "ATTACK", new Vector2(.88f, .19f), new Vector2(190, 86));
            Button pickup = Button("Pickup", parent, "PICK", new Vector2(.80f, .09f), new Vector2(140, 56));

            ZirconTargetCombatBehaviour combat = parent.gameObject.AddComponent<ZirconTargetCombatBehaviour>();
            Set(combat, "protocolProbe", session); Set(combat, "worldRenderer", world); Set(combat, "worldCamera", camera);

            ZirconMobileGameplayControlsBehaviour controls = pad.AddComponent<ZirconMobileGameplayControlsBehaviour>();
            Set(controls, "session", session); Set(controls, "mapRenderer", map); Set(controls, "combat", combat);
            Set(controls, "selectButton", select); Set(controls, "attackButton", attack); Set(controls, "pickupButton", pickup); Set(controls, "knob", knob);

            hotbar = new ZirconMobileSkillButtonBehaviour[4];
            for (int i = 0; i < hotbar.Length; i++)
            {
                Button cast = Button("Skill" + (i + 1), parent, "S" + (i + 1), new Vector2(.58f + i * .075f, .08f), new Vector2(112, 64));
                ZirconMobileSkillButtonBehaviour caster = cast.gameObject.AddComponent<ZirconMobileSkillButtonBehaviour>();
                Set(caster, "protocolProbe", session); Set(caster, "targetCombat", combat); Set(caster, "button", cast);
                hotbar[i] = caster;
            }
        }

        private static void CreateBuffBar(Transform parent, ZirconProtocolProbeBehaviour session)
        {
            GameObject bar = Rect("BuffBar", parent, new Vector2(.02f, .79f), new Vector2(.62f, .88f), Vector2.zero, Vector2.zero);
            RectTransform content = (RectTransform)Rect("Content", bar.transform, Vector2.zero, new Vector2(.72f, 1), Vector2.zero, Vector2.zero).transform;
            var layout = content.gameObject.AddComponent<HorizontalLayoutGroup>(); layout.spacing = 6; layout.childControlHeight = true; layout.childControlWidth = false;
            Button template = Button("BuffTemplate", content, "BUFF", new Vector2(.5f, .5f), new Vector2(108, 62)); template.gameObject.SetActive(false);
            TMP_Text details = Text("Details", bar.transform, string.Empty, 15, Muted, new Vector2(.73f, 0), Vector2.one, Vector2.zero);
            details.alignment = TextAlignmentOptions.MidlineLeft;
            ZirconBuffBarBehaviour controller = bar.AddComponent<ZirconBuffBarBehaviour>();
            Set(controller, "session", session); Set(controller, "content", content); Set(controller, "buffTemplate", template); Set(controller, "detailText", details);
        }

        private static void BuildInventory(GameObject panel, ZirconProtocolProbeBehaviour session, ZirconSystemCatalogBehaviour catalog)
        {
            if (panel == null) return;
            Transform contentRoot = ResetContent(panel.transform);
            RectTransform inventory = ScrollGrid("Inventory", contentRoot, new Vector2(.03f, .12f), new Vector2(.64f, .82f), new Vector2(102, 72), 7);
            RectTransform equipment = ScrollGrid("Equipment", contentRoot, new Vector2(.67f, .42f), new Vector2(.97f, .82f), new Vector2(126, 72), 2);
            Button template = SlotTemplate(contentRoot);
            TMP_Text name = Text("SelectedName", contentRoot, "Select an item", 21, Accent, new Vector2(.67f, .32f), new Vector2(.97f, .40f), Vector2.zero);
            TMP_Text detail = Text("SelectedDetail", contentRoot, string.Empty, 15, Muted, new Vector2(.67f, .16f), new Vector2(.97f, .32f), Vector2.zero); detail.enableWordWrapping = true;
            Button use = Button("Use", contentRoot, "USE", new Vector2(.74f, .10f), new Vector2(150, 52));
            Button lockButton = Button("Lock", contentRoot, "LOCK", new Vector2(.90f, .10f), new Vector2(150, 52));
            ZirconInventoryEquipmentPanelBehaviour controller = panel.GetComponent<ZirconInventoryEquipmentPanelBehaviour>();
            Set(controller, "session", session); Set(controller, "catalog", catalog); Set(controller, "inventoryContent", inventory); Set(controller, "equipmentContent", equipment);
            Set(controller, "slotTemplate", template); Set(controller, "selectedNameText", name); Set(controller, "selectedDetailText", detail); Set(controller, "useButton", use);
            Set(controller, "lockButton", lockButton); Set(controller, "lockButtonText", lockButton.GetComponentInChildren<TMP_Text>());
        }

        private static void BuildSkills(GameObject panel, ZirconProtocolProbeBehaviour session, ZirconSystemCatalogBehaviour catalog, ZirconMobileSkillButtonBehaviour[] hotbar)
        {
            if (panel == null) return;
            Transform contentRoot = ResetContent(panel.transform);
            RectTransform list = ScrollGrid("SkillList", contentRoot, new Vector2(.03f, .15f), new Vector2(.57f, .82f), new Vector2(190, 72), 3);
            Button template = SlotTemplate(contentRoot);
            TMP_Text name = Text("SelectedName", contentRoot, "Select a skill", 22, Accent, new Vector2(.61f, .67f), new Vector2(.96f, .80f), Vector2.zero);
            TMP_Text detail = Text("SelectedDetail", contentRoot, string.Empty, 16, Muted, new Vector2(.61f, .30f), new Vector2(.96f, .67f), Vector2.zero); detail.enableWordWrapping = true;
            var assign = new Button[4];
            for (int i = 0; i < assign.Length; i++)
                assign[i] = Button("Assign" + (i + 1), contentRoot, "SET " + (i + 1), new Vector2(.64f + i * .09f, .20f), new Vector2(120, 52));
            ZirconSkillBookPanelBehaviour controller = panel.GetComponent<ZirconSkillBookPanelBehaviour>();
            Set(controller, "session", session); Set(controller, "catalog", catalog); Set(controller, "skillContent", list); Set(controller, "skillTemplate", template);
            SetArray(controller, "hotbarAssignButtons", assign); SetArray(controller, "hotbarCasters", hotbar); Set(controller, "selectedNameText", name); Set(controller, "selectedDetailText", detail);
        }

        private static void BuildNpcServices(GameObject panel, ZirconProtocolProbeBehaviour session, ZirconSystemCatalogBehaviour catalog)
        {
            if (panel == null) return;
            Transform contentRoot = ResetContent(panel.transform);
            RectTransform inventory = ScrollGrid("Inventory", contentRoot, new Vector2(.03f, .16f), new Vector2(.64f, .82f), new Vector2(102, 72), 7);
            Button template = SlotTemplate(contentRoot);
            TMP_Text name = Text("SelectedName", contentRoot, "Select an item", 21, Accent, new Vector2(.68f, .68f), new Vector2(.96f, .80f), Vector2.zero);
            TMP_Text detail = Text("SelectedDetail", contentRoot, string.Empty, 15, Muted, new Vector2(.68f, .46f), new Vector2(.96f, .68f), Vector2.zero); detail.enableWordWrapping = true;
            TMP_InputField amount = Input("Amount", contentRoot, "1", new Vector2(.82f, .39f), new Vector2(190, 52)); amount.contentType = TMP_InputField.ContentType.IntegerNumber;
            Button minus = Button("Decrease", contentRoot, "-", new Vector2(.69f, .39f), new Vector2(64, 52));
            Button plus = Button("Increase", contentRoot, "+", new Vector2(.95f, .39f), new Vector2(64, 52));
            Button sell = Button("Sell", contentRoot, "SELL", new Vector2(.73f, .25f), new Vector2(150, 56));
            Button repair = Button("Repair", contentRoot, "REPAIR", new Vector2(.91f, .25f), new Vector2(150, 56));
            Toggle special = Toggle("SpecialRepair", contentRoot, "Special", new Vector2(.73f, .14f));
            Toggle guild = Toggle("GuildFunds", contentRoot, "Guild funds", new Vector2(.91f, .14f));
            ZirconNpcServicePanelBehaviour controller = panel.GetComponent<ZirconNpcServicePanelBehaviour>();
            Set(controller, "session", session); Set(controller, "catalog", catalog); Set(controller, "inventoryContent", inventory); Set(controller, "slotTemplate", template);
            Set(controller, "selectedNameText", name); Set(controller, "selectedDetailText", detail); Set(controller, "amountInput", amount); Set(controller, "decreaseButton", minus);
            Set(controller, "increaseButton", plus); Set(controller, "sellButton", sell); Set(controller, "repairButton", repair); Set(controller, "specialRepairToggle", special); Set(controller, "guildFundsToggle", guild);
        }

        private static void BuildStorage(GameObject panel, ZirconProtocolProbeBehaviour session, ZirconSystemCatalogBehaviour catalog)
        {
            if (panel == null) return;
            Transform contentRoot = ResetContent(panel.transform);
            RectTransform inventory = ScrollGrid("Inventory", contentRoot, new Vector2(.03f, .17f), new Vector2(.48f, .82f), new Vector2(102, 72), 5);
            RectTransform storage = ScrollGrid("Storage", contentRoot, new Vector2(.52f, .17f), new Vector2(.97f, .82f), new Vector2(102, 72), 5);
            Text("InventoryLabel", contentRoot, "INVENTORY", 17, Accent, new Vector2(.03f, .83f), new Vector2(.48f, .89f), Vector2.zero);
            Text("StorageLabel", contentRoot, "STORAGE", 17, Accent, new Vector2(.52f, .83f), new Vector2(.97f, .89f), Vector2.zero);
            Button template = SlotTemplate(contentRoot);
            TMP_Text selected = Text("Selected", contentRoot, string.Empty, 18, Muted, new Vector2(.20f, .08f), new Vector2(.62f, .15f), Vector2.zero);
            Button sort = Button("Sort", contentRoot, "SORT STORAGE", new Vector2(.81f, .11f), new Vector2(230, 54));
            ZirconStoragePanelBehaviour controller = panel.GetComponent<ZirconStoragePanelBehaviour>();
            Set(controller, "session", session); Set(controller, "catalog", catalog); Set(controller, "inventoryContent", inventory); Set(controller, "storageContent", storage);
            Set(controller, "slotTemplate", template); Set(controller, "sortButton", sort); Set(controller, "selectedNameText", selected);
        }

        private static void BuildChat(Transform parent, ZirconProtocolProbeBehaviour session, List<Button> buttons, List<GameObject> panels, List<Button> closes)
        {
            Button open = Button("ChatOpen", parent, "CHAT", new Vector2(.30f, .06f), new Vector2(126, 52));
            GameObject panel = Rect("ChatPanel", parent, new Vector2(.17f, .11f), new Vector2(.76f, .68f), Vector2.zero, Vector2.zero); panel.AddComponent<Image>().color = Panel;
            Text("Title", panel.transform, "CHAT", 26, Accent, new Vector2(.04f, .88f), new Vector2(.30f, .98f), Vector2.zero);
            Button close = Button("Close", panel.transform, "CLOSE", new Vector2(.90f, .93f), new Vector2(130, 48));
            RectTransform messages = ScrollGrid("Messages", panel.transform, new Vector2(.04f, .24f), new Vector2(.96f, .86f), new Vector2(800, 48), 1);
            Button template = SlotTemplate(panel.transform);
            TMP_InputField input = Input("Message", panel.transform, "Message", new Vector2(.44f, .12f), new Vector2(720, 58));
            Button send = Button("Send", panel.transform, "SEND", new Vector2(.88f, .12f), new Vector2(150, 58));
            ZirconChatPanelBehaviour controller = panel.AddComponent<ZirconChatPanelBehaviour>();
            Set(controller, "session", session); Set(controller, "messageContent", messages); Set(controller, "messageTemplate", template); Set(controller, "input", input); Set(controller, "sendButton", send);
            panel.SetActive(false); buttons.Add(open); panels.Add(panel); closes.Add(close);
        }

        private static void BuildNpcDialog(Transform parent, ZirconProtocolProbeBehaviour session, ZirconSystemCatalogBehaviour catalog)
        {
            GameObject panel = Rect("NpcDialog", parent, new Vector2(.20f, .16f), new Vector2(.80f, .82f), Vector2.zero, Vector2.zero); panel.AddComponent<Image>().color = Panel;
            TMP_Text title = Text("Title", panel.transform, "NPC", 28, Accent, new Vector2(.04f, .86f), new Vector2(.72f, .98f), Vector2.zero);
            TMP_Text body = Text("Body", panel.transform, string.Empty, 18, Color.white, new Vector2(.05f, .46f), new Vector2(.95f, .84f), Vector2.zero); body.enableWordWrapping = true; body.alignment = TextAlignmentOptions.TopLeft;
            RectTransform actions = ScrollGrid("Actions", panel.transform, new Vector2(.05f, .08f), new Vector2(.48f, .44f), new Vector2(330, 52), 1);
            RectTransform goods = ScrollGrid("Goods", panel.transform, new Vector2(.52f, .08f), new Vector2(.95f, .44f), new Vector2(330, 52), 1);
            Button actionTemplate = SlotTemplate(panel.transform); Button goodsTemplate = SlotTemplate(panel.transform);
            Button close = Button("Close", panel.transform, "CLOSE", new Vector2(.88f, .92f), new Vector2(140, 52));
            ZirconNpcDialogPanelBehaviour controller = parent.gameObject.AddComponent<ZirconNpcDialogPanelBehaviour>();
            Set(controller, "session", session); Set(controller, "catalog", catalog); Set(controller, "panelRoot", panel); Set(controller, "titleText", title); Set(controller, "bodyText", body);
            Set(controller, "buttonContent", actions); Set(controller, "buttonTemplate", actionTemplate); Set(controller, "goodsContent", goods); Set(controller, "goodsButtonTemplate", goodsTemplate); Set(controller, "closeButton", close);
        }

        private static Transform ResetContent(Transform panel)
        {
            Transform existing = panel.Find("P0Content");
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);
            return Rect("P0Content", panel, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).transform;
        }

        private static RectTransform ScrollGrid(string name, Transform parent, Vector2 min, Vector2 max, Vector2 cellSize, int columns)
        {
            GameObject viewport = Rect(name, parent, min, max, Vector2.zero, Vector2.zero);
            viewport.AddComponent<Image>().color = new Color(0, 0, 0, .22f);
            viewport.AddComponent<Mask>().showMaskGraphic = true;
            GameObject contentGo = Rect("Content", viewport.transform, new Vector2(0, 1), Vector2.one, Vector2.zero, Vector2.zero);
            RectTransform content = (RectTransform)contentGo.transform; content.pivot = new Vector2(.5f, 1);
            var grid = contentGo.AddComponent<GridLayoutGroup>(); grid.cellSize = cellSize; grid.spacing = new Vector2(6, 6); grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = Mathf.Max(1, columns); grid.childAlignment = TextAnchor.UpperLeft;
            var fitter = contentGo.AddComponent<ContentSizeFitter>(); fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = viewport.AddComponent<ScrollRect>(); scroll.content = content; scroll.viewport = (RectTransform)viewport.transform; scroll.horizontal = false; scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Clamped;
            return content;
        }

        private static Button SlotTemplate(Transform parent)
        {
            Button result = Button("Template", parent, "", new Vector2(-1, -1), new Vector2(100, 64));
            result.gameObject.SetActive(false);
            return result;
        }

        private static TMP_InputField Input(string name, Transform parent, string placeholder, Vector2 anchor, Vector2 size)
        {
            GameObject go = Rect(name, parent, anchor, anchor, Vector2.zero, Vector2.zero); ((RectTransform)go.transform).sizeDelta = size; go.AddComponent<Image>().color = PanelSoft;
            TMP_InputField input = go.AddComponent<TMP_InputField>();
            TMP_Text hint = Text("Placeholder", go.transform, placeholder, 18, Muted, Vector2.zero, Vector2.one, new Vector2(-24, -12));
            TMP_Text value = Text("Text", go.transform, string.Empty, 18, Color.white, Vector2.zero, Vector2.one, new Vector2(-24, -12));
            hint.alignment = value.alignment = TextAlignmentOptions.MidlineLeft; input.textViewport = (RectTransform)go.transform; input.textComponent = (TextMeshProUGUI)value; input.placeholder = hint;
            return input;
        }

        private static Toggle Toggle(string name, Transform parent, string label, Vector2 anchor)
        {
            GameObject go = Rect(name, parent, anchor, anchor, Vector2.zero, Vector2.zero); ((RectTransform)go.transform).sizeDelta = new Vector2(190, 46);
            GameObject box = Rect("Box", go.transform, new Vector2(0, .5f), new Vector2(0, .5f), Vector2.zero, Vector2.zero); ((RectTransform)box.transform).sizeDelta = new Vector2(36, 36); Image bg = box.AddComponent<Image>(); bg.color = PanelSoft;
            GameObject mark = Rect("Checkmark", box.transform, new Vector2(.18f, .18f), new Vector2(.82f, .82f), Vector2.zero, Vector2.zero); Image check = mark.AddComponent<Image>(); check.color = Accent;
            TMP_Text text = Text("Label", go.transform, label, 16, Color.white, new Vector2(.25f, 0), Vector2.one, Vector2.zero); text.alignment = TextAlignmentOptions.MidlineLeft;
            Toggle toggle = go.AddComponent<Toggle>(); toggle.targetGraphic = bg; toggle.graphic = check; return toggle;
        }

        private static Button Button(string name, Transform parent, string label, Vector2 anchor, Vector2 size)
        {
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(buttonPrefab, parent); go.name = name;
            RectTransform rect = (RectTransform)go.transform; rect.anchorMin = anchor; rect.anchorMax = anchor; rect.anchoredPosition = Vector2.zero; rect.sizeDelta = size;
            TMP_Text text = go.GetComponentInChildren<TMP_Text>(); if (text != null) { text.text = label; text.font = font; }
            return go.GetComponent<Button>();
        }

        private static TMP_Text Text(string name, Transform parent, string value, float size, Color color, Vector2 min, Vector2 max, Vector2 offsets)
        {
            GameObject go = Rect(name, parent, min, max, offsets, -offsets); TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>(); text.text = value; text.font = font; text.fontSize = size; text.color = color; text.alignment = TextAlignmentOptions.Center; text.enableWordWrapping = false; return text;
        }

        private static GameObject Rect(string name, Transform parent, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false); RectTransform rect = (RectTransform)go.transform; rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = offsetMin; rect.offsetMax = offsetMax; return go;
        }

        private static GameObject FindChild(Transform root, string name)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true)) if (child.name == name) return child.gameObject;
            return null;
        }

        private static T FindSceneObject<T>() where T : Component
        {
            foreach (T item in Resources.FindObjectsOfTypeAll<T>()) if (item.gameObject.scene.IsValid()) return item;
            return null;
        }

        private static void AppendNavigation(ZirconPanelNavigationBehaviour navigation, List<Button> extraButtons, List<GameObject> extraPanels, List<Button> extraCloses)
        {
            if (navigation == null || extraButtons.Count == 0) return;
            SerializedObject so = new SerializedObject(navigation);
            Append(so.FindProperty("openButtons"), extraButtons.ToArray()); Append(so.FindProperty("panels"), extraPanels.ToArray()); Append(so.FindProperty("closeButtons"), extraCloses.ToArray()); so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Append(SerializedProperty property, UnityEngine.Object[] values)
        {
            int start = property.arraySize; property.arraySize += values.Length;
            for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(start + i).objectReferenceValue = values[i];
        }

        private static void Set(UnityEngine.Object target, string property, UnityEngine.Object value)
        {
            if (target == null) return; SerializedObject so = new SerializedObject(target); SerializedProperty p = so.FindProperty(property); if (p != null) { p.objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        }

        private static void SetArray<T>(UnityEngine.Object target, string property, T[] values) where T : UnityEngine.Object
        {
            SerializedObject so = new SerializedObject(target); SerializedProperty p = so.FindProperty(property); p.arraySize = values.Length; for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i]; so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
