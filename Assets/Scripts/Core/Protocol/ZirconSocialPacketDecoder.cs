using System;
using System.IO;
using System.Text;

namespace Zircon.Mobile.Core.Protocol
{
    public readonly struct ZirconGroupMemberInfo { public ZirconGroupMemberInfo(uint objectId, string name) { ObjectId = objectId; Name = name ?? string.Empty; } public uint ObjectId { get; } public string Name { get; } }
    public enum ZirconSocialEventKind { None, GroupSwitch, GroupMember, GroupRemove, GroupInvite, GuildInvite, TradeRequest, TradeOpen, TradeClose, TradeAddItem, TradeAddGold, TradeItemAdded, TradeGoldAdded, TradeUnlock }
    public readonly struct ZirconSocialEvent
    {
        public ZirconSocialEvent(ZirconSocialEventKind kind, string name = null, string guildName = null, uint objectId = 0, bool allow = false, long gold = 0, ZirconCellLinkInfo link = default, bool success = false, ZirconUserItemInfo? item = null)
        { Kind = kind; Name = name ?? string.Empty; GuildName = guildName ?? string.Empty; ObjectId = objectId; Allow = allow; Gold = gold; Link = link; Success = success; Item = item; }
        public ZirconSocialEventKind Kind { get; } public string Name { get; } public string GuildName { get; } public uint ObjectId { get; } public bool Allow { get; } public long Gold { get; } public ZirconCellLinkInfo Link { get; } public bool Success { get; } public ZirconUserItemInfo? Item { get; }
    }

    public static class ZirconSocialPacketDecoder
    {
        public static bool TryDecode(ZirconPacketFrame frame, out ZirconSocialEvent value)
        {
            switch (frame.PacketId)
            {
                case ZirconPacketIds.Server.GroupSwitch: return TryRead(frame, r => new ZirconSocialEvent(ZirconSocialEventKind.GroupSwitch, allow: r.ReadBoolean()), out value);
                case ZirconPacketIds.Server.GroupMember: return TryRead(frame, r => new ZirconSocialEvent(ZirconSocialEventKind.GroupMember, r.ReadString(), objectId: r.ReadUInt32()), out value, true);
                case ZirconPacketIds.Server.GroupRemove: return TryRead(frame, r => new ZirconSocialEvent(ZirconSocialEventKind.GroupRemove, objectId: r.ReadUInt32()), out value);
                case ZirconPacketIds.Server.GroupInvite: return TryRead(frame, r => new ZirconSocialEvent(ZirconSocialEventKind.GroupInvite, r.ReadString()), out value);
                case ZirconPacketIds.Server.GuildInvite: return TryRead(frame, r => new ZirconSocialEvent(ZirconSocialEventKind.GuildInvite, r.ReadString(), r.ReadString()), out value);
                case ZirconPacketIds.Server.TradeRequest: return TryRead(frame, r => new ZirconSocialEvent(ZirconSocialEventKind.TradeRequest, r.ReadString()), out value);
                case ZirconPacketIds.Server.TradeOpen: return TryRead(frame, r => new ZirconSocialEvent(ZirconSocialEventKind.TradeOpen, r.ReadString()), out value);
                case ZirconPacketIds.Server.TradeClose: value = new ZirconSocialEvent(ZirconSocialEventKind.TradeClose); return frame.Payload.Length == 0;
                case ZirconPacketIds.Server.TradeAddItem: return TryRead(frame, r => new ZirconSocialEvent(ZirconSocialEventKind.TradeAddItem, link: ReadRequiredLink(r), success: r.ReadBoolean()), out value);
                case ZirconPacketIds.Server.TradeAddGold: return TryRead(frame, r => new ZirconSocialEvent(ZirconSocialEventKind.TradeAddGold, gold: r.ReadInt64()), out value);
                case ZirconPacketIds.Server.TradeItemAdded: return TryRead(frame, r => new ZirconSocialEvent(ZirconSocialEventKind.TradeItemAdded, item: ZirconInGamePacketDecoder.ReadNullableUserItem(r)), out value);
                case ZirconPacketIds.Server.TradeGoldAdded: return TryRead(frame, r => new ZirconSocialEvent(ZirconSocialEventKind.TradeGoldAdded, gold: r.ReadInt64()), out value);
                case ZirconPacketIds.Server.TradeUnlock: value = new ZirconSocialEvent(ZirconSocialEventKind.TradeUnlock); return frame.Payload.Length == 0;
                default: value = default; return false;
            }
        }

        private static ZirconCellLinkInfo ReadRequiredLink(BinaryReader reader)
        {
            if (!reader.ReadBoolean()) return default;
            return new ZirconCellLinkInfo((ZirconGridType)reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt64());
        }

        private static bool TryRead(ZirconPacketFrame frame, Func<BinaryReader, ZirconSocialEvent> read, out ZirconSocialEvent value, bool groupMemberOrder = false)
        {
            value = default;
            try { using var stream = new MemoryStream(frame.Payload); using var reader = new BinaryReader(stream, Encoding.UTF8); if (groupMemberOrder) { uint id = reader.ReadUInt32(); string name = reader.ReadString(); value = new ZirconSocialEvent(ZirconSocialEventKind.GroupMember, name, objectId: id); } else value = read(reader); return stream.Position == stream.Length; }
            catch { value = default; return false; }
        }
    }
}