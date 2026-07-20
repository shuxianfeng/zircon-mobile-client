using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zircon.Mobile.Core.Protocol;
using Zircon.Mobile.Game.Items;
using Zircon.Mobile.Game.World;
using Zircon.Mobile.UI.Catalog;
using Zircon.Mobile.UI.Login;

namespace Zircon.Mobile.UI.Inventory
{
    public sealed class ZirconInventoryEquipmentPanelBehaviour : MonoBehaviour
    {
        [SerializeField] private ZirconProtocolProbeBehaviour session;
        [SerializeField] private ZirconSystemCatalogBehaviour catalog;
        [SerializeField] private RectTransform inventoryContent;
        [SerializeField] private RectTransform equipmentContent;
        [SerializeField] private Button slotTemplate;
        [SerializeField] private TMP_Text selectedNameText;
        [SerializeField] private TMP_Text selectedDetailText;
        [SerializeField] private Button useButton;
        [SerializeField] private Button lockButton;
        [SerializeField] private TMP_Text lockButtonText;
        [SerializeField] private int inventorySlots = 49;
        [SerializeField] private int equipmentSlots = 16;
        [SerializeField] private Color selectedColor = new Color(0.95f, 0.72f, 0.18f, 1f);

        private readonly List<SlotCell> cells = new List<SlotCell>();
        private ZirconGridType selectedGrid;
        private int selectedSlot = -1;
        private float nextRefresh;

        private void Awake()
        {
            BuildGrid(inventoryContent, ZirconGridType.Inventory, inventorySlots);
            BuildGrid(equipmentContent, ZirconGridType.Equipment, equipmentSlots);
            if (useButton != null)
                useButton.onClick.AddListener(UseSelected);
            if (lockButton != null)
                lockButton.onClick.AddListener(ToggleSelectedLock);

        }

        private void OnDestroy()
        {
            if (useButton != null)
                useButton.onClick.RemoveListener(UseSelected);
            if (lockButton != null)
                lockButton.onClick.RemoveListener(ToggleSelectedLock);
        }

        private void Update()
        {
            if (Time.unscaledTime < nextRefresh)
                return;
            nextRefresh = Time.unscaledTime + 0.15f;
            Refresh(session?.GetWorldSnapshot());
        }

        private void BuildGrid(RectTransform parent, ZirconGridType grid, int count)
        {
            if (parent == null || slotTemplate == null)
                return;

            for (int slot = 0; slot < count; slot++)
            {
                Button button = Instantiate(slotTemplate, parent);
                button.gameObject.SetActive(true);
                TMP_Text label = button.GetComponentInChildren<TMP_Text>();
                int capturedSlot = slot;
                button.onClick.AddListener(() => OnSlotPressed(grid, capturedSlot));
                cells.Add(new SlotCell(grid, slot, button, label, button.colors));
            }
        }

        private void Refresh(ZirconWorldSnapshot snapshot)
        {
            foreach (SlotCell cell in cells)
            {
                ZirconItemState item = FindItem(snapshot, cell.Grid, cell.Slot);
                ZirconSystemCatalogBehaviour.ItemEntry info = item == null ? null : catalog?.GetItem(item.InfoIndex);
                string name = info?.Name ?? (item == null ? string.Empty : $"#{item.InfoIndex}");
                if (cell.Label != null)
                    cell.Label.text = item == null ? string.Empty : item.Count > 1 ? $"{name}\n{item.Count}" : name;

                ColorBlock colors = cell.BaseColors;
                if (cell.Grid == selectedGrid && cell.Slot == selectedSlot)
                    colors.normalColor = selectedColor;
                cell.Button.colors = colors;
            }

            ZirconItemState selected = FindItem(snapshot, selectedGrid, selectedSlot);
            if (selected == null)
            {
                selectedSlot = -1;
                SetText(selectedNameText, string.Empty);
                SetText(selectedDetailText, string.Empty);
                SetInteractable(useButton, false);
                SetInteractable(lockButton, false);
                return;
            }

            ZirconSystemCatalogBehaviour.ItemEntry selectedInfo = catalog?.GetItem(selected.InfoIndex);
            SetText(selectedNameText, selectedInfo?.Name ?? $"#{selected.InfoIndex}");
            string durability = selected.MaxDurability > 0 ? $"  {selected.CurrentDurability}/{selected.MaxDurability}" : string.Empty;
            SetText(selectedDetailText, $"Lv.{selected.Level}  x{selected.Count}{durability}\n{selectedInfo?.Description ?? string.Empty}");
            SetInteractable(useButton, selected.Grid == ZirconGridType.Inventory);
            SetInteractable(lockButton, true);
            SetText(lockButtonText, selected.Locked ? "Unlock" : "Lock");
        }

        private void OnSlotPressed(ZirconGridType grid, int slot)
        {
            ZirconWorldSnapshot snapshot = session?.GetWorldSnapshot();
            ZirconItemState pressed = FindItem(snapshot, grid, slot);
            ZirconItemState selected = FindItem(snapshot, selectedGrid, selectedSlot);

            if (selected == null)
            {
                if (pressed != null)
                {
                    selectedGrid = grid;
                    selectedSlot = slot;
                }
                return;
            }

            if (selectedGrid == grid && selectedSlot == slot)
            {
                selectedSlot = -1;
                return;
            }

            bool merge = pressed != null && pressed.InfoIndex == selected.InfoIndex;
            _ = session.SendItemMoveCommandAsync(selectedGrid, grid, selectedSlot, slot, merge);
            selectedSlot = -1;
        }

        private void UseSelected()
        {
            if (selectedSlot >= 0 && selectedGrid == ZirconGridType.Inventory)
                _ = session.SendItemUseCommandAsync(selectedGrid, selectedSlot, 1);
        }

        private void ToggleSelectedLock()
        {
            ZirconItemState selected = FindItem(session?.GetWorldSnapshot(), selectedGrid, selectedSlot);
            if (selected != null)
                _ = session.SendItemLockCommandAsync(selected.Grid, selected.Slot, !selected.Locked);
        }

        private static ZirconItemState FindItem(ZirconWorldSnapshot snapshot, ZirconGridType grid, int slot)
        {
            if (snapshot == null || slot < 0)
                return null;
            IReadOnlyList<ZirconItemState> source = grid == ZirconGridType.Equipment ? snapshot.Equipment : snapshot.Inventory;
            foreach (ZirconItemState item in source)
            {
                if (item.Slot == slot)
                    return item;
            }
            return null;
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null)
                target.text = value;
        }

        private static void SetInteractable(Selectable target, bool value)
        {
            if (target != null)
                target.interactable = value;
        }

        private sealed class SlotCell
        {
            public SlotCell(ZirconGridType grid, int slot, Button button, TMP_Text label, ColorBlock baseColors)
            {
                Grid = grid;
                Slot = slot;
                Button = button;
                Label = label;
                BaseColors = baseColors;
            }

            public ZirconGridType Grid { get; }
            public int Slot { get; }
            public Button Button { get; }
            public TMP_Text Label { get; }
            public ColorBlock BaseColors { get; }
        }
    }
}
