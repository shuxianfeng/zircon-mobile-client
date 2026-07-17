using System.Collections.Generic;
using Zircon.Mobile.Core.Protocol;

namespace Zircon.Mobile.Game.Social
{
    public sealed class ZirconSocialState
    {
        public bool AllowGroup { get; set; }
        public string PendingGroupInvite { get; set; } = string.Empty;
        public string PendingGuildInvite { get; set; } = string.Empty;
        public string PendingGuildName { get; set; } = string.Empty;
        public Dictionary<uint, string> GroupMembers { get; } = new Dictionary<uint, string>();
        public bool TradeOpen { get; set; }
        public bool TradeLocked { get; set; }
        public string TradePartner { get; set; } = string.Empty;
        public long OfferedGold { get; set; }
        public long PartnerGold { get; set; }
        public List<ZirconUserItemInfo> PartnerItems { get; } = new List<ZirconUserItemInfo>();

        public ZirconSocialState Clone()
        {
            var value = new ZirconSocialState { AllowGroup = AllowGroup, PendingGroupInvite = PendingGroupInvite, PendingGuildInvite = PendingGuildInvite, PendingGuildName = PendingGuildName, TradeOpen = TradeOpen, TradeLocked = TradeLocked, TradePartner = TradePartner, OfferedGold = OfferedGold, PartnerGold = PartnerGold };
            foreach (KeyValuePair<uint, string> member in GroupMembers) value.GroupMembers.Add(member.Key, member.Value);
            value.PartnerItems.AddRange(PartnerItems);
            return value;
        }
    }
}