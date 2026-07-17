using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zircon.Mobile.Core.Protocol;
using Zircon.Mobile.Game.Items;
using Zircon.Mobile.Game.World;
using Zircon.Mobile.UI.Catalog;
using Zircon.Mobile.UI.Login;

namespace Zircon.Mobile.UI.Storage
{
    public sealed class ZirconStoragePanelBehaviour : MonoBehaviour
    {
        [SerializeField] private ZirconProtocolProbeBehaviour session;
        [SerializeField] private ZirconSystemCatalogBehaviour catalog;
        [SerializeField] private RectTransform inventoryContent;
        [SerializeField] private RectTransform storageContent;
        [SerializeField] private Button slotTemplate;
        [SerializeField] private Button sortButton;
        [SerializeField] private TMP_Text selectedNameText;
        [SerializeField] private int inventorySlots = 49;
        [SerializeField] private int storageSlots = 100;
        [SerializeField] private Color selectedColor = new Color(0.95f, 0.72f, 0.18f, 1f);

        private readonly List<SlotCell> cells = new List<SlotCell>();
        private ZirconGridType selectedGrid;
        private int selectedSlot = -1;
        private float nextRefresh;

        private void Awake()
        {
            BuildGrid(inventoryContent, ZirconGridType.Inventory, inventorySlots);
            BuildGrid(storageContent, ZirconGridType.Storage, storageSlots);
            if (sortButton != null)
                sortButton.onClick.AddListener(SortStorage);
        }

        private void OnDestroy()
        {
            if (sortButton != null)
                sortButton.onClick.RemoveListener(SortStorage);
        }

        private void Update()
        {
            if (Time.unscaledTime < nextRefresh)
                return;
            nextRefresh = Time.unscaledTime + 0.15f;
            ZirconWorldSnapshot snapshot = session?.GetWorldSnapshot();
            if (snapshot != null && snapshot.StorageSize > storageSlots)
            {
                storageSlots = snapshot.StorageSize;
                BuildGrid(storageContent, ZirconGridType.Storage, storageSlots);
            }
            Refresh(snapshot);
        }

        private void BuildGrid(RectTransform parent, ZirconGridType grid, int count)
        {
            if (parent == null || slotTemplate == null)
                return;
            int firstSlot = 0;
            foreach (SlotCell cell in cells)
                if (cell.Grid == grid) firstSlot++;
            for (int slot = firstSlot; slot < count; slot++)
            {
                Button button = Instantiate(slotTemplate, parent);
                button.gameObject.SetActive(true);
                int captured = slot;
                button.onClick.AddListener(() => OnSlotPressed(grid, captured));
                cells.Add(new SlotCell(grid, slot, button, button.GetComponentInChildren<TMP_Text>(), button.colors));
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
                selectedSlot = -1;
            if (selectedNameText != null)
                selectedNameText.text = selected == null ? string.Empty : catalog?.GetItem(selected.InfoIndex)?.Name ?? $"#{selected.InfoIndex}";
        }

        private void OnSlotPressed(ZirconGridType grid, int slot)
        {
            ZirconWorldSnapshot snapshot = session?.GetWorldSnapshot();
            ZirconItemState selected = FindItem(snapshot, selectedGrid, selectedSlot);
            ZirconItemState pressed = FindItem(snapshot, grid, slot);
            if (selected == null)
            {
                if (pressed != null) { selectedGrid = grid; selectedSlot = slot; }
                return;
            }
            if (selectedGrid == grid && selectedSlot == slot) { selectedSlot = -1; return; }
            bool merge = pressed != null && pressed.InfoIndex == selected.InfoIndex;
            _ = session.SendItemMoveCommandAsync(selectedGrid, grid, selectedSlot, slot, merge);
            selectedSlot = -1;
        }

        private void SortStorage() => _ = session?.SendSortStorageCommandAsync();

        private static ZirconItemState FindItem(ZirconWorldSnapshot snapshot, ZirconGridType grid, int slot)
        {
            if (snapshot == null || slot < 0)
                return null;
            IReadOnlyList<ZirconItemState> source = grid == ZirconGridType.Storage ? snapshot.Storage : snapshot.Inventory;
            foreach (ZirconItemState item in source)
                if (item.Slot == slot) return item;
            return null;
        }

        private sealed class SlotCell
        {
            public SlotCell(ZirconGridType grid, int slot, Button button, TMP_Text label, ColorBlock colors)
            { Grid = grid; Slot = slot; Button = button; Label = label; BaseColors = colors; }
            public ZirconGridType Grid { get; }
            public int Slot { get; }
            public Button Button { get; }
            public TMP_Text Label { get; }
            public ColorBlock BaseColors { get; }
        }
    }
}