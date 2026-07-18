using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zircon.Mobile.Core.Protocol;
using Zircon.Mobile.Game.Items;
using Zircon.Mobile.Game.World;
using Zircon.Mobile.UI.Login;

namespace Zircon.Mobile.UI.Social
{
    public sealed class ZirconSocialTradePanelBehaviour : MonoBehaviour
    {
        [SerializeField] private ZirconProtocolProbeBehaviour session;
        [SerializeField] private Toggle allowGroupToggle;
        [SerializeField] private TMP_InputField playerNameInput;
        [SerializeField] private Button groupInviteButton;
        [SerializeField] private Button acceptGroupButton;
        [SerializeField] private Button declineGroupButton;
        [SerializeField] private TMP_Text groupMembersText;
        [SerializeField] private TMP_Text invitationText;
        [SerializeField] private Button tradeRequestButton;
        [SerializeField] private Button acceptTradeButton;
        [SerializeField] private Button declineTradeButton;
        [SerializeField] private TMP_InputField tradeGoldInput;
        [SerializeField] private TMP_InputField tradeSlotInput;
        [SerializeField] private TMP_InputField tradeCountInput;
        [SerializeField] private Button addTradeGoldButton;
        [SerializeField] private Button addTradeItemButton;
        [SerializeField] private Button confirmTradeButton;
        [SerializeField] private Button closeTradeButton;
        [SerializeField] private TMP_Text tradeStatusText;
        [SerializeField] private TMP_InputField guildNameInput;
        [SerializeField] private TMP_InputField guildNoticeInput;
        [SerializeField] private Button createGuildButton;
        [SerializeField] private Button inviteGuildMemberButton;
        [SerializeField] private Button updateGuildNoticeButton;
        [SerializeField] private Button acceptGuildButton;
        [SerializeField] private Button declineGuildButton;

        private float nextRefresh;

        private void Awake()
        {
            allowGroupToggle?.onValueChanged.AddListener(SetAllowGroup);
            groupInviteButton?.onClick.AddListener(() => Send(ZirconClientPackets.GroupInvite(NameValue()), "group invite"));
            acceptGroupButton?.onClick.AddListener(() => Send(ZirconClientPackets.GroupResponse(true), "group accept"));
            declineGroupButton?.onClick.AddListener(() => Send(ZirconClientPackets.GroupResponse(false), "group decline"));
            tradeRequestButton?.onClick.AddListener(() => Send(ZirconClientPackets.TradeRequest(), "trade request"));
            acceptTradeButton?.onClick.AddListener(() => Send(ZirconClientPackets.TradeResponse(true), "trade accept"));
            declineTradeButton?.onClick.AddListener(() => Send(ZirconClientPackets.TradeResponse(false), "trade decline"));
            addTradeGoldButton?.onClick.AddListener(AddTradeGold);
            addTradeItemButton?.onClick.AddListener(AddTradeItem);
            confirmTradeButton?.onClick.AddListener(() => Send(ZirconClientPackets.TradeConfirm(), "trade confirm"));
            closeTradeButton?.onClick.AddListener(() => Send(ZirconClientPackets.TradeClose(), "trade close"));
            createGuildButton?.onClick.AddListener(() => Send(ZirconClientPackets.GuildCreate(guildNameInput?.text, true, 0, 0), "guild create"));
            inviteGuildMemberButton?.onClick.AddListener(() => Send(ZirconClientPackets.GuildInviteMember(NameValue()), "guild invite"));
            updateGuildNoticeButton?.onClick.AddListener(() => Send(ZirconClientPackets.GuildEditNotice(guildNoticeInput?.text), "guild notice"));
            acceptGuildButton?.onClick.AddListener(() => Send(ZirconClientPackets.GuildResponse(true), "guild accept"));
            declineGuildButton?.onClick.AddListener(() => Send(ZirconClientPackets.GuildResponse(false), "guild decline"));
        }

        private void Update()
        {
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + 0.2f;
            Refresh(session?.GetWorldSnapshot());
        }

        private void Refresh(ZirconWorldSnapshot snapshot)
        {
            if (snapshot?.Social == null) return;
            string playerName = NameValue();
            if (allowGroupToggle != null) allowGroupToggle.SetIsOnWithoutNotify(snapshot.Social.AllowGroup);
            if (groupMembersText != null)
            {
                var text = new System.Text.StringBuilder();
                foreach (KeyValuePair<uint, string> member in snapshot.Social.GroupMembers) text.AppendLine(member.Value);
                groupMembersText.text = text.ToString().TrimEnd();
            }
            if (invitationText != null)
                invitationText.text = !string.IsNullOrEmpty(snapshot.Social.PendingGuildInvite) ? $"{snapshot.Social.PendingGuildName}: {snapshot.Social.PendingGuildInvite}" : snapshot.Social.PendingGroupInvite;
            if (tradeStatusText != null)
                tradeStatusText.text = snapshot.Social.TradeOpen ? $"{snapshot.Social.TradePartner}\nGold {snapshot.Social.OfferedGold} / {snapshot.Social.PartnerGold}\nItems {snapshot.Social.PartnerItems.Count}" : string.Empty;
            SetInteractable(groupInviteButton, playerName.Length > 0);
            bool hasGroupInvite = !string.IsNullOrEmpty(snapshot.Social.PendingGroupInvite);
            SetInteractable(acceptGroupButton, hasGroupInvite); SetInteractable(declineGroupButton, hasGroupInvite);
            bool hasTradeRequest = !snapshot.Social.TradeOpen && !string.IsNullOrEmpty(snapshot.Social.TradePartner);
            SetInteractable(acceptTradeButton, hasTradeRequest); SetInteractable(declineTradeButton, hasTradeRequest);
            SetInteractable(addTradeGoldButton, snapshot.Social.TradeOpen); SetInteractable(addTradeItemButton, snapshot.Social.TradeOpen);
            SetInteractable(confirmTradeButton, snapshot.Social.TradeOpen); SetInteractable(closeTradeButton, snapshot.Social.TradeOpen);
            SetInteractable(createGuildButton, !string.IsNullOrWhiteSpace(guildNameInput?.text));
            SetInteractable(inviteGuildMemberButton, playerName.Length > 0);
            SetInteractable(updateGuildNoticeButton, !string.IsNullOrWhiteSpace(guildNoticeInput?.text));
            bool hasGuildInvite = !string.IsNullOrEmpty(snapshot.Social.PendingGuildInvite);
            SetInteractable(acceptGuildButton, hasGuildInvite); SetInteractable(declineGuildButton, hasGuildInvite);
        }

        private void SetAllowGroup(bool allow) => Send(ZirconClientPackets.GroupSwitch(allow), "group switch");
        private string NameValue() => playerNameInput?.text?.Trim() ?? string.Empty;

        private void AddTradeGold()
        {
            long gold = tradeGoldInput != null && long.TryParse(tradeGoldInput.text, out long value) ? System.Math.Max(0, value) : 0;
            Send(ZirconClientPackets.TradeAddGold(gold), "trade gold");
        }

        private void AddTradeItem()
        {
            int slot = tradeSlotInput != null && int.TryParse(tradeSlotInput.text, out int parsedSlot) ? parsedSlot : -1;
            long count = tradeCountInput != null && long.TryParse(tradeCountInput.text, out long parsedCount) ? parsedCount : 1;
            ZirconItemState item = FindInventoryItem(session?.GetWorldSnapshot(), slot);
            if (item == null) return;
            count = System.Math.Max(1, System.Math.Min(item.Count, count));
            Send(ZirconClientPackets.TradeAddItem(new ZirconCellLinkInfo(ZirconGridType.Inventory, slot, count)), "trade item");
        }

        private void Send(byte[] packet, string label) => _ = session?.SendGamePacketCommandAsync(packet, label);
        private static void SetInteractable(Selectable target, bool value) { if (target != null) target.interactable = value; }

        private static ZirconItemState FindInventoryItem(ZirconWorldSnapshot snapshot, int slot)
        {
            if (snapshot == null) return null;
            foreach (ZirconItemState item in snapshot.Inventory) if (item.Slot == slot) return item;
            return null;
        }
    }
}
