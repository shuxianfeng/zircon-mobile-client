#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Zircon.Mobile.UI.Buffs;
using Zircon.Mobile.UI.Chat;
using Zircon.Mobile.UI.Inventory;
using Zircon.Mobile.UI.Mail;
using Zircon.Mobile.UI.Market;
using Zircon.Mobile.UI.Npc;
using Zircon.Mobile.UI.Quests;
using Zircon.Mobile.UI.Skills;
using Zircon.Mobile.UI.Social;
using Zircon.Mobile.UI.Storage;

namespace Zircon.Mobile.Editor
{
    public static class ZirconP0SceneValidator
    {
        public static void ValidateFromCommandLine()
        {
            var scene = EditorSceneManager.OpenScene(ZirconProjectBootstrap.ScenePath, OpenSceneMode.Single);
            foreach (ZirconMainHudBehaviour marker in Resources.FindObjectsOfTypeAll<ZirconMainHudBehaviour>())
                if (marker != null && marker.gameObject.scene.IsValid()) UnityEngine.Object.DestroyImmediate(marker);
            EditorSceneManager.SaveScene(scene);

            RequireNamed("P0PlayableUI"); RequireNamed("MovementPad"); RequireNamed("BuffBar"); RequireNamed("ChatPanel"); RequireNamed("NpcDialog");
            Validate<ZirconInventoryEquipmentPanelBehaviour>("session", "catalog", "inventoryContent", "equipmentContent", "slotTemplate", "useButton", "lockButton");
            Validate<ZirconSkillBookPanelBehaviour>("session", "catalog", "skillContent", "skillTemplate", "selectedNameText", "selectedDetailText");
            Validate<ZirconNpcServicePanelBehaviour>("session", "catalog", "inventoryContent", "slotTemplate", "amountInput", "sellButton", "repairButton");
            Validate<ZirconStoragePanelBehaviour>("session", "catalog", "inventoryContent", "storageContent", "slotTemplate", "sortButton");
            Validate<ZirconQuestPanelBehaviour>("session", "catalog", "questContent", "questTemplate", "titleText", "progressText", "rewardChoiceInput", "trackButton", "completeButton");
            Validate<ZirconSocialTradePanelBehaviour>("session", "allowGroupToggle", "playerNameInput", "groupInviteButton", "tradeRequestButton", "tradeGoldInput", "guildNameInput", "createGuildButton");
            Validate<ZirconMailPanelBehaviour>("session", "mailContent", "mailTemplate", "senderText", "subjectText", "messageText", "collectButton", "deleteButton", "recipientInput", "sendButton");
            Validate<ZirconMarketPanelBehaviour>("session", "catalog", "searchInput", "sortDropdown", "searchButton", "resultContent", "resultTemplate", "detailText", "buyButton", "consignButton");
            Validate<ZirconChatPanelBehaviour>("session", "messageContent", "messageTemplate", "input", "whisperTarget", "whisperTargetRoot", "sendButton");
            Validate<ZirconBuffBarBehaviour>("session", "content", "buffTemplate", "detailText");
            Validate<ZirconNpcDialogPanelBehaviour>("session", "catalog", "panelRoot", "titleText", "bodyText", "buttonContent", "buttonTemplate", "goodsContent", "goodsButtonTemplate", "closeButton");

            ZirconSkillBookPanelBehaviour skills = Find<ZirconSkillBookPanelBehaviour>();
            SerializedObject skillObject = new SerializedObject(skills);
            if (skillObject.FindProperty("hotbarAssignButtons").arraySize != 4 || skillObject.FindProperty("hotbarCasters").arraySize != 4)
                throw new InvalidOperationException("The P0 skill hotbar is not bound to four positions.");

            ZirconChatPanelBehaviour chat = Find<ZirconChatPanelBehaviour>();
            SerializedObject chatObject = new SerializedObject(chat);
            if (chatObject.FindProperty("channelButtons").arraySize != 6)
                throw new InvalidOperationException("The chat panel is not bound to all six channel buttons.");

            TMP_FontAsset mobileFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Generated/UI/ZirconMobileFont.asset");
            if (mobileFont == null || mobileFont.atlasTexture == null || mobileFont.material == null)
                throw new InvalidOperationException("The persisted TMP mobile font is incomplete.");

            int missingScripts = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
                    missingScripts += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item.gameObject);
            if (missingScripts != 0)
                throw new InvalidOperationException("Scene contains missing scripts: " + missingScripts);

            Debug.Log("P0 scene validation passed: required controls, serialized references, hotbar, font atlas, and scripts are complete.");
        }

        private static void RequireNamed(string name)
        {
            foreach (Transform item in Resources.FindObjectsOfTypeAll<Transform>())
                if (item.gameObject.scene.IsValid() && item.name == name) return;
            throw new InvalidOperationException("Required P0 scene object is missing: " + name);
        }

        private static void Validate<T>(params string[] properties) where T : Component
        {
            T component = Find<T>();
            if (component == null) throw new InvalidOperationException("Required P0 component is missing: " + typeof(T).Name);
            SerializedObject serialized = new SerializedObject(component);
            var missing = new List<string>();
            foreach (string name in properties)
            {
                SerializedProperty property = serialized.FindProperty(name);
                if (property == null || property.objectReferenceValue == null) missing.Add(name);
            }
            if (missing.Count > 0) throw new InvalidOperationException(typeof(T).Name + " has unassigned references: " + string.Join(", ", missing));
        }

        private static T Find<T>() where T : Component
        {
            foreach (T item in Resources.FindObjectsOfTypeAll<T>())
                if (item.gameObject.scene.IsValid()) return item;
            return null;
        }
    }
}
#endif
