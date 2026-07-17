using System.Collections.Generic;
using Zircon.Mobile.Core.Protocol;

namespace Zircon.Mobile.Game.Commerce
{
    public sealed class ZirconCommerceState
    {
        public List<ZirconMailInfo> Mail { get; } = new List<ZirconMailInfo>();
        public List<ZirconMarketListingInfo> MarketResults { get; } = new List<ZirconMarketListingInfo>();
        public int MarketResultCount { get; set; }
        public long LastMarketPrice { get; set; }
        public long AverageMarketPrice { get; set; }

        public ZirconCommerceState Clone()
        {
            var value = new ZirconCommerceState { MarketResultCount = MarketResultCount, LastMarketPrice = LastMarketPrice, AverageMarketPrice = AverageMarketPrice };
            value.Mail.AddRange(Mail); value.MarketResults.AddRange(MarketResults); return value;
        }
    }
}