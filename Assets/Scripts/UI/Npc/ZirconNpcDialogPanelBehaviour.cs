using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zircon.Mobile.Game.World;
using Zircon.Mobile.UI.Catalog;
using Zircon.Mobile.UI.Login;

namespace Zircon.Mobile.UI.Npc
{
    public sealed class ZirconNpcDialogPanelBehaviour : MonoBehaviour
    {
        private static readonly Regex ButtonPattern = new Regex(@"\[(?<Text>.*?):(?<ID>.+?)\]", RegexOptions.Compiled);

        [SerializeField] private ZirconProtocolProbeBehaviour session;
        [SerializeField] private ZirconSystemCatalogBehaviour catalog;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private RectTransform buttonContent;
        [SerializeField] private Button buttonTemplate;
        [SerializeField] private RectTransform goodsContent;
        [SerializeField] private Button goodsButtonTemplate;
        [SerializeField] private Button closeButton;

        private readonly List<Button> spawnedButtons = new List<Button>();
        private int displayedPage = -1;
        private bool closing;

        private void Awake()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);
            SetActive(panelRoot, false);
        }

        private void OnDestroy()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(Close);
            ClearButtons();
        }

        private void Update()
        {
            ZirconWorldSnapshot snapshot = session?.GetWorldSnapshot();
            bool open = snapshot != null && snapshot.NpcDialogOpen;
            if (!open)
            {
                closing = false;
                displayedPage = -1;
                SetActive(panelRoot, false);
                return;
            }

            if (closing)
            {
                SetActive(panelRoot, false);
                return;
            }

            SetActive(panelRoot, true);
            if (snapshot.NpcPageIndex != displayedPage)
                ShowPage(snapshot.NpcPageIndex);
        }

        private void ShowPage(int pageIndex)
        {
            displayedPage = pageIndex;
            ClearButtons();
            ZirconSystemCatalogBehaviour.NpcPageEntry page = catalog?.GetNpcPage(pageIndex);
            string raw = page?.Say ?? string.Empty;
            SetText(titleText, page?.Description ?? $"NPC {pageIndex}");
            SetText(bodyText, ButtonPattern.Replace(raw, "${Text}"));

            foreach (Match match in ButtonPattern.Matches(raw))
            {
                if (!int.TryParse(match.Groups["ID"].Value, out int buttonId))
                    continue;
                SpawnButton(buttonContent, buttonTemplate, match.Groups["Text"].Value, () => _ = session.SendNpcButtonCommandAsync(buttonId));
            }

            IReadOnlyList<ZirconSystemCatalogBehaviour.NpcGoodEntry> goods = catalog?.GetNpcGoods(pageIndex) ?? Array.Empty<ZirconSystemCatalogBehaviour.NpcGoodEntry>();
            foreach (ZirconSystemCatalogBehaviour.NpcGoodEntry good in goods)
            {
                ZirconSystemCatalogBehaviour.ItemEntry item = catalog?.GetItem(good.ItemInfoIndex);
                long price = item == null ? 0 : (long)System.Math.Floor(item.Price * good.Rate);
                string label = item == null ? $"#{good.ItemInfoIndex}" : $"{item.Name}  {price:N0}";
                int itemInfoIndex = good.ItemInfoIndex;
                SpawnButton(goodsContent, goodsButtonTemplate, label, () => _ = session.SendNpcBuyCommandAsync(itemInfoIndex, 1));
            }
        }

        private void SpawnButton(RectTransform parent, Button template, string labelText, UnityEngine.Events.UnityAction action)
        {
            if (parent == null || template == null)
                return;
            Button button = Instantiate(template, parent);
            button.gameObject.SetActive(true);
            TMP_Text label = button.GetComponentInChildren<TMP_Text>();
            if (label != null)
                label.text = labelText;
            button.onClick.AddListener(action);
            spawnedButtons.Add(button);
        }

        private void Close()
        {
            if (session == null)
                return;
            closing = true;
            SetActive(panelRoot, false);
            _ = session.SendNpcCloseCommandAsync();
        }

        private void ClearButtons()
        {
            foreach (Button button in spawnedButtons)
            {
                if (button != null)
                    Destroy(button.gameObject);
            }
            spawnedButtons.Clear();
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null)
                target.text = value ?? string.Empty;
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
                target.SetActive(active);
        }
    }
}