using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Zircon.Mobile.Core.Protocol
{
    public readonly struct ZirconMailInfo
    {
        public ZirconMailInfo(int index, bool opened, bool hasItem, DateTime date, string sender, string subject, string message, int gold, IReadOnlyList<ZirconUserItemInfo> items)
        { Index = index; Opened = opened; HasItem = hasItem; Date = date; Sender = sender ?? string.Empty; Subject = subject ?? string.Empty; Message = message ?? string.Empty; Gold = gold; Items = items ?? Array.Empty<ZirconUserItemInfo>(); }
        public int Index { get; } public bool Opened { get; } public bool HasItem { get; } public DateTime Date { get; } public string Sender { get; } public string Subject { get; } public string Message { get; } public int Gold { get; } public IReadOnlyList<ZirconUserItemInfo> Items { get; }
    }

    public readonly struct ZirconMarketListingInfo
    {
        public ZirconMarketListingInfo(int index, ZirconUserItemInfo? item, int price, string seller, string message, bool isOwner)
        { Index = index; Item = item; Price = price; Seller = seller ?? string.Empty; Message = message ?? string.Empty; IsOwner = isOwner; }
        public int Index { get; } public ZirconUserItemInfo? Item { get; } public int Price { get; } public string Seller { get; } public string Message { get; } public bool IsOwner { get; }
    }

    public enum ZirconCommerceEventKind { None, MailList, MailNew, MailDelete, MailItemDelete, MailSent, MarketConsignments, MarketSearch, MarketSearchCount, MarketSearchIndex, MarketBuy, MarketConsignChanged, MarketHistory }
    public readonly struct ZirconCommerceEvent
    {
        public ZirconCommerceEvent(ZirconCommerceEventKind kind, IReadOnlyList<ZirconMailInfo> mail = null, ZirconMailInfo? mailItem = null, IReadOnlyList<ZirconMarketListingInfo> listings = null, ZirconMarketListingInfo? listing = null, int index = 0, int slot = 0, int count = 0, long amount = 0, bool success = false, long lastPrice = 0, long averagePrice = 0)
        { Kind = kind; Mail = mail ?? Array.Empty<ZirconMailInfo>(); MailItem = mailItem; Listings = listings ?? Array.Empty<ZirconMarketListingInfo>(); Listing = listing; Index = index; Slot = slot; Count = count; Amount = amount; Success = success; LastPrice = lastPrice; AveragePrice = averagePrice; }
        public ZirconCommerceEventKind Kind { get; } public IReadOnlyList<ZirconMailInfo> Mail { get; } public ZirconMailInfo? MailItem { get; } public IReadOnlyList<ZirconMarketListingInfo> Listings { get; } public ZirconMarketListingInfo? Listing { get; } public int Index { get; } public int Slot { get; } public int Count { get; } public long Amount { get; } public bool Success { get; } public long LastPrice { get; } public long AveragePrice { get; }
    }

    public static class ZirconCommercePacketDecoder
    {
        public static bool TryDecode(ZirconPacketFrame frame, out ZirconCommerceEvent value)
        {
            switch (frame.PacketId)
            {
                case ZirconPacketIds.Server.MailList: return TryRead(frame, r => new ZirconCommerceEvent(ZirconCommerceEventKind.MailList, mail: ReadMailList(r)), out value);
                case ZirconPacketIds.Server.MailNew: return TryRead(frame, r => new ZirconCommerceEvent(ZirconCommerceEventKind.MailNew, mailItem: ReadNullableMail(r)), out value);
                case ZirconPacketIds.Server.MailDelete: return TryRead(frame, r => new ZirconCommerceEvent(ZirconCommerceEventKind.MailDelete, index: r.ReadInt32()), out value);
                case ZirconPacketIds.Server.MailItemDelete: return TryRead(frame, r => new ZirconCommerceEvent(ZirconCommerceEventKind.MailItemDelete, index: r.ReadInt32(), slot: r.ReadInt32()), out value);
                case ZirconPacketIds.Server.MailSend: value = new ZirconCommerceEvent(ZirconCommerceEventKind.MailSent); return frame.Payload.Length == 0;
                case ZirconPacketIds.Server.MarketConsign: return TryRead(frame, r => new ZirconCommerceEvent(ZirconCommerceEventKind.MarketConsignments, listings: ReadMarketList(r)), out value);
                case ZirconPacketIds.Server.MarketSearch: return TryRead(frame, r => new ZirconCommerceEvent(ZirconCommerceEventKind.MarketSearch, count: r.ReadInt32(), listings: ReadMarketList(r)), out value);
                case ZirconPacketIds.Server.MarketSearchCount: return TryRead(frame, r => new ZirconCommerceEvent(ZirconCommerceEventKind.MarketSearchCount, count: r.ReadInt32()), out value);
                case ZirconPacketIds.Server.MarketSearchIndex: return TryRead(frame, r => new ZirconCommerceEvent(ZirconCommerceEventKind.MarketSearchIndex, index: r.ReadInt32(), listing: ReadNullableMarket(r)), out value);
                case ZirconPacketIds.Server.MarketBuy: return TryRead(frame, r => new ZirconCommerceEvent(ZirconCommerceEventKind.MarketBuy, index: r.ReadInt32(), amount: r.ReadInt64(), success: r.ReadBoolean()), out value);
                case ZirconPacketIds.Server.MarketConsignChanged: return TryRead(frame, r => new ZirconCommerceEvent(ZirconCommerceEventKind.MarketConsignChanged, index: r.ReadInt32(), amount: r.ReadInt64()), out value);
                case ZirconPacketIds.Server.MarketHistory: return TryRead(frame, r => ReadHistory(r), out value);
                default: value = default; return false;
            }
        }

        private static ZirconCommerceEvent ReadHistory(BinaryReader reader)
        {
            int index = reader.ReadInt32(); reader.ReadInt64(); long last = reader.ReadInt64(); long average = reader.ReadInt64(); reader.ReadInt32();
            return new ZirconCommerceEvent(ZirconCommerceEventKind.MarketHistory, index: index, lastPrice: last, averagePrice: average);
        }

        private static IReadOnlyList<ZirconMailInfo> ReadMailList(BinaryReader reader)
        {
            if (!reader.ReadBoolean()) return Array.Empty<ZirconMailInfo>();
            int count = reader.ReadInt32(); var list = new List<ZirconMailInfo>(count);
            for (int i = 0; i < count; i++) { ZirconMailInfo? item = ReadNullableMail(reader); if (item.HasValue) list.Add(item.Value); }
            return list;
        }

        private static ZirconMailInfo? ReadNullableMail(BinaryReader reader)
        {
            if (!reader.ReadBoolean()) return null;
            return new ZirconMailInfo(reader.ReadInt32(), reader.ReadBoolean(), reader.ReadBoolean(), DateTime.FromBinary(reader.ReadInt64()), reader.ReadString(), reader.ReadString(), reader.ReadString(), reader.ReadInt32(), ZirconInGamePacketDecoder.ReadUserItemList(reader));
        }

        private static IReadOnlyList<ZirconMarketListingInfo> ReadMarketList(BinaryReader reader)
        {
            if (!reader.ReadBoolean()) return Array.Empty<ZirconMarketListingInfo>();
            int count = reader.ReadInt32(); var list = new List<ZirconMarketListingInfo>(count);
            for (int i = 0; i < count; i++) { ZirconMarketListingInfo? item = ReadNullableMarket(reader); if (item.HasValue) list.Add(item.Value); }
            return list;
        }

        private static ZirconMarketListingInfo? ReadNullableMarket(BinaryReader reader)
        {
            if (!reader.ReadBoolean()) return null;
            int index = reader.ReadInt32(); ZirconUserItemInfo? item = ZirconInGamePacketDecoder.ReadNullableUserItem(reader);
            return new ZirconMarketListingInfo(index, item, reader.ReadInt32(), reader.ReadString(), reader.ReadString(), reader.ReadBoolean());
        }

        private static bool TryRead(ZirconPacketFrame frame, Func<BinaryReader, ZirconCommerceEvent> read, out ZirconCommerceEvent value)
        {
            value = default;
            try { using var stream = new MemoryStream(frame.Payload); using var reader = new BinaryReader(stream, Encoding.UTF8); value = read(reader); return stream.Position == stream.Length; }
            catch { value = default; return false; }
        }
    }
}