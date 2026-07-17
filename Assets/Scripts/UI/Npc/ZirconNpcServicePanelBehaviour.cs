using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zircon.Mobile.Core.Protocol;
using Zircon.Mobile.Game.Items;
using Zircon.Mobile.Game.World;
using Zircon.Mobile.UI.Catalog;
using Zircon.Mobile.UI.Login;

namespace Zircon.Mobile.UI.Npc
{
    public sealed class ZirconNpcServicePanelBehaviour : MonoBehaviour
    {
        [SerializeField] private ZirconProtocolProbeBehaviour session;
        [SerializeField] private ZirconSystemCatalogBehaviour catalog;
        [SerializeField] private RectTransform inventoryContent;
        [SerializeField] private Button slotTemplate;
        [SerializeField] private TMP_Text selectedNameText;
        [SerializeField] private TMP_Text selectedDetailText;
        [SerializeField] private TMP_InputField amountInput;
        [SerializeField] private Button decreaseButton;
        [SerializeField] private Button increaseButton;
        [SerializeField] private Button sellButton;
        [SerializeField] private Button repairButton;
        [SerializeField] private Toggle specialRepairToggle;
        [SerializeField] private Toggle guildFundsToggle;
        [SerializeField] private int inventorySlots = 49;
        [SerializeField] private Color selectedColor = new Color(0.95f, 0.72f, 0.18f, 1f);

        private readonly List<SlotCell> cells = new List<SlotCell>();
        private int selectedSlot = -1;
        private float nextRefresh;

        private void Awake()
        {
            BuildGrid();
            decreaseButton?.onClick.AddListener(() => StepAmount(-1));
            increaseButton?.onClick.AddListener(() => StepAmount(1));
            sellButton?.onClick.AddListener(SellSelected);
            repairButton?.onClick.AddListener(RepairSelected);
        }

        private void Update()
        {
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + 0.15f;
            Refresh(session?.GetWorldSnapshot());
        }

        private void BuildGrid()
        {
            if (inventoryContent == null || slotTemplate == null) return;
            for (int slot = 0; slot < inventorySlots; slot++)
            {
                Button button = Instantiate(slotTemplate, inventoryContent);
                button.gameObject.SetActive(true);
                int captured = slot;
                button.onClick.AddListener(() => selectedSlot = captured);
                cells.Add(new SlotCell(slot, button, button.GetComponentInChildren<TMP_Text>(), button.colors));
            }
        }

        private void Refresh(ZirconWorldSnapshot snapshot)
        {
            foreach (SlotCell cell in cells)
            {
                ZirconItemState item = FindItem(snapshot, cell.Slot);
                ZirconSystemCatalogBehaviour.ItemEntry info = item == null ? null : catalog?.GetItem(item.InfoIndex);
                if (cell.Label != null)
                    cell.Label.text = item == null ? string.Empty : item.Count > 1 ? $"{info?.Name ?? $"#{item.InfoIndex}"}\n{item.Count}" : info?.Name ?? $"#{item.InfoIndex}";
                ColorBlock colors = cell.BaseColors;
                if (cell.Slot == selectedSlot) colors.normalColor = selectedColor;
                cell.Button.colors = colors;
            }

            ZirconItemState selected = FindItem(snapshot, selectedSlot);
            ZirconSystemCatalogBehaviour.ItemEntry selectedInfo = selected == null ? null : catalog?.GetItem(selected.InfoIndex);
            if (selected == null) selectedSlot = -1;
            if (selectedNameText != null) selectedNameText.text = selectedInfo?.Name ?? string.Empty;
            if (selectedDetailText != null)
                selectedDetailText.text = selected == null ? string.Empty : $"x{selected.Count}  {selected.CurrentDurability}/{selected.MaxDurability}\n{selectedInfo?.Description ?? string.Empty}";
            if (sellButton != null) sellButton.interactable = selected != null && selectedInfo != null && selectedInfo.CanSell;
            if (repairButton != null) repairButton.interactable = selected != null && selectedInfo != null && selectedInfo.CanRepair && selected.CurrentDurability < selected.MaxDurability;
        }

        private void StepAmount(int delta)
        {
            ZirconItemState item = FindItem(session?.GetWorldSnapshot(), selectedSlot);
            if (item == null || amountInput == null) return;
            long value = ParseAmount(item.Count);
            amountInput.text = Clamp(value + delta, 1, item.Count).ToString();
        }

        private void SellSelected()
        {
            ZirconItemState item = FindItem(session?.GetWorldSnapshot(), selectedSlot);
            if (item == null) return;
            long amount = ParseAmount(item.Count);
            session?.SendNpcSellCommandAsync(new[] { new ZirconCellLinkInfo(ZirconGridType.Inventory, selectedSlot, amount) });
        }

        private void RepairSelected()
        {
            ZirconItemState item = FindItem(session?.GetWorldSnapshot(), selectedSlot);
            if (item == null) return;
            session?.SendNpcRepairCommandAsync(
                new[] { new ZirconCellLinkInfo(ZirconGridType.Inventory, selectedSlot, item.Count) },
                specialRepairToggle != null && specialRepairToggle.isOn,
                guildFundsToggle != null && guildFundsToggle.isOn);
        }

        private long ParseAmount(long maximum)
        {
            if (amountInput == null || !long.TryParse(amountInput.text, out long value)) value = 1;
            value = Clamp(value, 1, maximum);
            if (amountInput != null) amountInput.text = value.ToString();
            return value;
        }

        private static long Clamp(long value, long minimum, long maximum) => value < minimum ? minimum : value > maximum ? maximum : value;

        private static ZirconItemState FindItem(ZirconWorldSnapshot snapshot, int slot)
        {
            if (snapshot == null || slot < 0) return null;
            foreach (ZirconItemState item in snapshot.Inventory)
                if (item.Slot == slot) return item;
            return null;
        }

        private sealed class SlotCell
        {
            public SlotCell(int slot, Button button, TMP_Text label, ColorBlock colors)
            { Slot = slot; Button = button; Label = label; BaseColors = colors; }
            public int Slot { get; }
            public Button Button { get; }
            public TMP_Text Label { get; }
            public ColorBlock BaseColors { get; }
        }
    }
}