using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Zircon.Mobile.Core.Protocol
{
    public enum ZirconGridType
    {
        None = 0,
        Inventory = 1,
        Equipment = 2,
        Belt = 3,
        Sell = 4,
        Repair = 5,
        Storage = 6,
        AutoPotion = 7,
        CompanionInventory = 17,
        CompanionEquipment = 18,
    }

    public readonly struct ZirconCellLinkInfo
    {
        public ZirconCellLinkInfo(ZirconGridType grid, int slot, long count)
        {
            Grid = grid;
            Slot = slot;
            Count = count;
        }

        public ZirconGridType Grid { get; }
        public int Slot { get; }
        public long Count { get; }
    }

    public readonly struct ZirconItemMoveInfo
    {
        public ZirconItemMoveInfo(ZirconGridType fromGrid, ZirconGridType toGrid, int fromSlot, int toSlot, bool mergeItem, bool success)
        {
            FromGrid = fromGrid;
            ToGrid = toGrid;
            FromSlot = fromSlot;
            ToSlot = toSlot;
            MergeItem = mergeItem;
            Success = success;
        }

        public ZirconGridType FromGrid { get; }
        public ZirconGridType ToGrid { get; }
        public int FromSlot { get; }
        public int ToSlot { get; }
        public bool MergeItem { get; }
        public bool Success { get; }
    }

    public readonly struct ZirconItemChangedInfo
    {
        public ZirconItemChangedInfo(ZirconCellLinkInfo link, bool success)
        {
            Link = link;
            Success = success;
        }

        public ZirconCellLinkInfo Link { get; }
        public bool Success { get; }
    }

    public readonly struct ZirconItemLockInfo
    {
        public ZirconItemLockInfo(ZirconGridType grid, int slot, bool locked)
        {
            Grid = grid;
            Slot = slot;
            Locked = locked;
        }

        public ZirconGridType Grid { get; }
        public int Slot { get; }
        public bool Locked { get; }
    }

    public readonly struct ZirconItemDurabilityInfo
    {
        public ZirconItemDurabilityInfo(ZirconGridType grid, int slot, int currentDurability)
        {
            Grid = grid;
            Slot = slot;
            CurrentDurability = currentDurability;
        }

        public ZirconGridType Grid { get; }
        public int Slot { get; }
        public int CurrentDurability { get; }
    }

    public readonly struct ZirconStorageItemsInfo
    {
        public ZirconStorageItemsInfo(IReadOnlyList<ZirconUserItemInfo> items) { Items = items ?? Array.Empty<ZirconUserItemInfo>(); }
        public IReadOnlyList<ZirconUserItemInfo> Items { get; }
    }
    public readonly struct ZirconItemsChangedInfo
    {
        public ZirconItemsChangedInfo(IReadOnlyList<ZirconCellLinkInfo> links, bool success)
        { Links = links ?? Array.Empty<ZirconCellLinkInfo>(); Success = success; }
        public IReadOnlyList<ZirconCellLinkInfo> Links { get; }
        public bool Success { get; }
    }

    public readonly struct ZirconNpcRepairInfo
    {
        public ZirconNpcRepairInfo(IReadOnlyList<ZirconCellLinkInfo> links, bool special, bool success, TimeSpan specialRepairDelay)
        { Links = links ?? Array.Empty<ZirconCellLinkInfo>(); Special = special; Success = success; SpecialRepairDelay = specialRepairDelay; }
        public IReadOnlyList<ZirconCellLinkInfo> Links { get; }
        public bool Special { get; }
        public bool Success { get; }
        public TimeSpan SpecialRepairDelay { get; }
    }
    public static class ZirconInventoryPacketDecoder
    {
        public static bool TryDecodeItemMove(ZirconPacketFrame frame, out ZirconItemMoveInfo value)
        {
            return TryRead(frame, ZirconPacketIds.Server.ItemMove, reader => new ZirconItemMoveInfo(
                (ZirconGridType)reader.ReadInt32(),
                (ZirconGridType)reader.ReadInt32(),
                reader.ReadInt32(),
                reader.ReadInt32(),
                reader.ReadBoolean(),
                reader.ReadBoolean()), out value);
        }

        public static bool TryDecodeItemChanged(ZirconPacketFrame frame, out ZirconItemChangedInfo value)
        {
            return TryRead(frame, ZirconPacketIds.Server.ItemChanged, reader =>
            {
                ZirconCellLinkInfo? link = ReadCellLink(reader);
                bool success = reader.ReadBoolean();
                return new ZirconItemChangedInfo(link ?? default, success && link.HasValue);
            }, out value);
        }

        public static bool TryDecodeItemLock(ZirconPacketFrame frame, out ZirconItemLockInfo value)
        {
            return TryRead(frame, ZirconPacketIds.Server.ItemLock, reader => new ZirconItemLockInfo(
                (ZirconGridType)reader.ReadInt32(), reader.ReadInt32(), reader.ReadBoolean()), out value);
        }

        public static bool TryDecodeItemDurability(ZirconPacketFrame frame, out ZirconItemDurabilityInfo value)
        {
            return TryRead(frame, ZirconPacketIds.Server.ItemDurability, reader => new ZirconItemDurabilityInfo(
                (ZirconGridType)reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32()), out value);
        }

        public static bool TryDecodeItemsChanged(ZirconPacketFrame frame, out ZirconItemsChangedInfo value)
        {
            return TryRead(frame, ZirconPacketIds.Server.ItemsChanged, reader =>
                new ZirconItemsChangedInfo(ReadCellLinkList(reader), reader.ReadBoolean()), out value);
        }

        public static bool TryDecodeNpcRepair(ZirconPacketFrame frame, out ZirconNpcRepairInfo value)
        {
            return TryRead(frame, ZirconPacketIds.Server.NpcRepair, reader =>
                new ZirconNpcRepairInfo(ReadCellLinkList(reader), reader.ReadBoolean(), reader.ReadBoolean(), TimeSpan.FromTicks(reader.ReadInt64())), out value);
        }
        public static bool TryDecodeStorageSize(ZirconPacketFrame frame, out int value)
        {
            return TryRead(frame, ZirconPacketIds.Server.StorageSize, reader => reader.ReadInt32(), out value);
        }

        public static bool TryDecodeSortStorageItems(ZirconPacketFrame frame, out ZirconStorageItemsInfo value)
        {
            return TryRead(frame, ZirconPacketIds.Server.SortStorageItem,
                reader => new ZirconStorageItemsInfo(ZirconInGamePacketDecoder.ReadUserItemList(reader)), out value);
        }
        internal static IReadOnlyList<ZirconCellLinkInfo> ReadCellLinkList(BinaryReader reader)
        {
            if (!reader.ReadBoolean())
                return Array.Empty<ZirconCellLinkInfo>();
            int count = reader.ReadInt32();
            var links = new List<ZirconCellLinkInfo>(count);
            for (int i = 0; i < count; i++)
            {
                ZirconCellLinkInfo? link = ReadCellLink(reader);
                if (link.HasValue)
                    links.Add(link.Value);
            }
            return links;
        }
        private static ZirconCellLinkInfo? ReadCellLink(BinaryReader reader)
        {
            if (!reader.ReadBoolean())
                return null;

            return new ZirconCellLinkInfo((ZirconGridType)reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt64());
        }

        private static bool TryRead<T>(ZirconPacketFrame frame, ushort packetId, Func<BinaryReader, T> read, out T value)
        {
            value = default;
            if (frame.PacketId != packetId)
                return false;

            try
            {
                using (var stream = new MemoryStream(frame.Payload))
                using (var reader = new BinaryReader(stream, Encoding.UTF8))
                {
                    value = read(reader);
                    return stream.Position == stream.Length;
                }
            }
            catch
            {
                value = default;
                return false;
            }
        }
    }
}