using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zircon.Mobile.Core.Protocol;
using Zircon.Mobile.UI.Catalog;
using Zircon.Mobile.UI.Login;

namespace Zircon.Mobile.UI.Market
{
    public sealed class ZirconMarketPanelBehaviour : MonoBehaviour
    {
        [SerializeField] private ZirconProtocolProbeBehaviour session;
        [SerializeField] private ZirconSystemCatalogBehaviour catalog;
        [SerializeField] private TMP_InputField searchInput;
        [SerializeField] private TMP_Dropdown sortDropdown;
        [SerializeField] private Button searchButton;
        [SerializeField] private RectTransform resultContent;
        [SerializeField] private Button resultTemplate;
        [SerializeField] private TMP_Text detailText;
        [SerializeField] private TMP_InputField buyCountInput;
        [SerializeField] private Button buyButton;
        [SerializeField] private Button cancelConsignButton;
        [SerializeField] private TMP_InputField consignSlotInput;
        [SerializeField] private TMP_InputField consignCountInput;
        [SerializeField] private TMP_InputField consignPriceInput;
        [SerializeField] private TMP_InputField consignMessageInput;
        [SerializeField] private Button consignButton;

        private readonly List<ResultCell> cells = new List<ResultCell>();
        private int selectedIndex = -1;
        private float nextRefresh;

        private void Awake()
        {
            searchButton?.onClick.AddListener(Search);
            buyButton?.onClick.AddListener(BuySelected);
            cancelConsignButton?.onClick.AddListener(CancelSelected);
            consignButton?.onClick.AddListener(Consign);
        }

        private void Update()
        {
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + 0.2f;
            Refresh(session?.GetWorldSnapshot());
        }

        private void Refresh(Zircon.Mobile.Game.World.ZirconWorldSnapshot snapshot)
        {
            IReadOnlyList<ZirconMarketListingInfo> listings = snapshot?.Commerce?.MarketResults;
            EnsureRows(listings?.Count ?? 0);
            for (int i = 0; i < cells.Count; i++)
            {
                bool active = listings != null && i < listings.Count; cells[i].Button.gameObject.SetActive(active); if (!active) continue;
                ZirconMarketListingInfo listing = listings[i]; cells[i].Index = listing.Index;
                ZirconSystemCatalogBehaviour.ItemEntry info = listing.Item.HasValue ? catalog?.GetItem(listing.Item.Value.InfoIndex) : null;
                if (cells[i].Label != null) cells[i].Label.text = $"{info?.Name ?? "Item"}  x{listing.Item?.Count ?? 0}\n{listing.Price}  {listing.Seller}";
            }
            ZirconMarketListingInfo? selected = FindListing(listings, selectedIndex);
            if (detailText != null) detailText.text = selected.HasValue ? $"{selected.Value.Seller}\n{selected.Value.Message}\nPrice {selected.Value.Price}" : string.Empty;
            if (buyButton != null) buyButton.interactable = selected.HasValue && !selected.Value.IsOwner;
            if (cancelConsignButton != null) cancelConsignButton.interactable = selected.HasValue && selected.Value.IsOwner;
        }

        private void EnsureRows(int count)
        {
            if (resultContent == null || resultTemplate == null) return;
            while (cells.Count < count) { Button button = Instantiate(resultTemplate, resultContent); var cell = new ResultCell(button, button.GetComponentInChildren<TMP_Text>()); button.onClick.AddListener(() => selectedIndex = cell.Index); cells.Add(cell); }
        }

        private void Search() => Send(ZirconClientPackets.MarketSearch(searchInput?.text, false, 0, sortDropdown?.value ?? 0), "market search");
        private void BuySelected() { long count = ParseLong(buyCountInput, 1); if (selectedIndex >= 0) Send(ZirconClientPackets.MarketBuy(selectedIndex, count, false), "market buy"); }
        private void CancelSelected() { long count = ParseLong(buyCountInput, 1); if (selectedIndex >= 0) Send(ZirconClientPackets.MarketCancel(selectedIndex, count), "market cancel"); }
        private void Consign()
        {
            if (consignSlotInput == null || !int.TryParse(consignSlotInput.text, out int slot)) return;
            long count = ParseLong(consignCountInput, 1); int price = (int)System.Math.Min(int.MaxValue, ParseLong(consignPriceInput, 1));
            Send(ZirconClientPackets.MarketConsign(new ZirconCellLinkInfo(ZirconGridType.Inventory, slot, count), price, consignMessageInput?.text, false), "market consign");
        }

        private void Send(byte[] packet, string label) => _ = session?.SendGamePacketCommandAsync(packet, label);
        private static long ParseLong(TMP_InputField input, long fallback) => input != null && long.TryParse(input.text, out long value) ? System.Math.Max(1, value) : fallback;
        private static ZirconMarketListingInfo? FindListing(IReadOnlyList<ZirconMarketListingInfo> list, int index) { if (list != null) foreach (ZirconMarketListingInfo item in list) if (item.Index == index) return item; return null; }
        private sealed class ResultCell { public ResultCell(Button button, TMP_Text label) { Button = button; Label = label; } public int Index { get; set; } public Button Button { get; } public TMP_Text Label { get; } }
    }
}