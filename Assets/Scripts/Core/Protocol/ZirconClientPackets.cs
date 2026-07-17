using System;
using System.Collections.Generic;

namespace Zircon.Mobile.Core.Protocol
{
    public static class ZirconClientPackets
    {
        public static byte[] Connected()
        {
            return ZirconBinary.EncodePacket(ZirconPacketIds.General.Connected);
        }

        public static byte[] Ping()
        {
            return ZirconBinary.EncodePacket(ZirconPacketIds.General.Ping);
        }

        public static byte[] Version(byte[] clientHash)
        {
            return ZirconBinary.EncodePacket(
                ZirconPacketIds.General.Version,
                writer => ZirconBinary.WriteByteArray(writer, clientHash));
        }

        public static byte[] SelectLanguage(string language)
        {
            return ZirconBinary.EncodePacket(
                ZirconPacketIds.Client.SelectLanguage,
                writer => writer.Write(language ?? "Chinese"));
        }

        public static byte[] Login(string emailAddress, string passwordHash, string checksum)
        {
            return ZirconBinary.EncodePacket(
                ZirconPacketIds.Client.Login,
                writer =>
                {
                    writer.Write(emailAddress ?? string.Empty);
                    writer.Write(passwordHash ?? string.Empty);
                    writer.Write(checksum ?? string.Empty);
                });
        }

        public static byte[] LoginSimple(string emailAddress, string passwordHash, string checksum)
        {
            return ZirconBinary.EncodePacket(
                ZirconPacketIds.Client.LoginSimple,
                writer =>
                {
                    writer.Write(emailAddress ?? string.Empty);
                    writer.Write(passwordHash ?? string.Empty);
                    writer.Write(checksum ?? string.Empty);
                });
        }

        public static byte[] StartGame(int characterIndex)
        {
            return ZirconBinary.EncodePacket(
                ZirconPacketIds.Client.StartGame,
                writer => writer.Write(characterIndex));
        }
        public static byte[] Turn(byte direction)
        {
            return ZirconBinary.EncodePacket(
                ZirconPacketIds.Client.Turn,
                writer => writer.Write(direction));
        }

        public static byte[] Move(byte direction, int distance)
        {
            return ZirconBinary.EncodePacket(
                ZirconPacketIds.Client.Move,
                writer =>
                {
                    writer.Write(direction);
                    writer.Write(distance);
                });
        }

        public static byte[] Attack(byte direction, ZirconMirAction action = ZirconMirAction.Attack, int attackMagic = 0)
        {
            return ZirconBinary.EncodePacket(
                ZirconPacketIds.Client.Attack,
                writer =>
                {
                    writer.Write(direction);
                    writer.Write((byte)action);
                    writer.Write(attackMagic);
                });
        }

        public static byte[] Magic(byte direction, int magicType, uint target, ZirconMapPoint location)
        {
            return ZirconBinary.EncodePacket(
                ZirconPacketIds.Client.Magic,
                writer =>
                {
                    writer.Write(direction);
                    writer.Write((byte)ZirconMirAction.Spell);
                    writer.Write(magicType);
                    writer.Write(target);
                    writer.Write(location.X);
                    writer.Write(location.Y);
                });
        }

        public static byte[] Chat(string text)
        {
            return ZirconBinary.EncodePacket(
                ZirconPacketIds.Client.Chat,
                writer => writer.Write(text ?? string.Empty));
        }

        public static byte[] NpcCall(uint objectId)
        {
            return ZirconBinary.EncodePacket(
                ZirconPacketIds.Client.NpcCall,
                writer => writer.Write(objectId));
        }
        public static byte[] PickUp(byte pickType)
        {
            return ZirconBinary.EncodePacket(
                ZirconPacketIds.Client.PickUp,
                writer => writer.Write(pickType));
        }

        public static byte[] ItemDrop(int slot, long count)
        {
            return ZirconBinary.EncodePacket(
                ZirconPacketIds.Client.ItemDrop,
                writer =>
                {
                    writer.Write(true); // CellLinkInfo is a non-null class.
                    writer.Write(1); // GridType.Inventory
                    writer.Write(slot);
                    writer.Write(count);
                });
        }
        public static byte[] ItemMove(ZirconGridType fromGrid, ZirconGridType toGrid, int fromSlot, int toSlot, bool mergeItem)
        {
            return ZirconBinary.EncodePacket(
                ZirconPacketIds.Client.ItemMove,
                writer =>
                {
                    writer.Write((int)fromGrid);
                    writer.Write((int)toGrid);
                    writer.Write(fromSlot);
                    writer.Write(toSlot);
                    writer.Write(mergeItem);
                });
        }

        public static byte[] ItemUse(ZirconGridType grid, int slot, long count = 1)
        {
            return ZirconBinary.EncodePacket(
                ZirconPacketIds.Client.ItemUse,
                writer => WriteCellLink(writer, grid, slot, count));
        }

        public static byte[] ItemLock(ZirconGridType grid, int slot, bool locked)
        {
            return ZirconBinary.EncodePacket(
                ZirconPacketIds.Client.ItemLock,
                writer =>
                {
                    writer.Write((int)grid);
                    writer.Write(slot);
                    writer.Write(locked);
                });
        }

        public static byte[] NpcButton(int buttonId)
        {
            return ZirconBinary.EncodePacket(ZirconPacketIds.Client.NpcButton, writer => writer.Write(buttonId));
        }

        public static byte[] NpcBuy(int itemInfoIndex, long amount, bool guildFunds = false)
        {
            return ZirconBinary.EncodePacket(
                ZirconPacketIds.Client.NpcBuy,
                writer =>
                {
                    writer.Write(itemInfoIndex);
                    writer.Write(amount);
                    writer.Write(guildFunds);
                });
        }

        public static byte[] GroupSwitch(bool allow) => ZirconBinary.EncodePacket(ZirconPacketIds.Client.GroupSwitch, writer => writer.Write(allow));
        public static byte[] GroupInvite(string name) => ZirconBinary.EncodePacket(ZirconPacketIds.Client.GroupInvite, writer => writer.Write(name ?? string.Empty));
        public static byte[] GroupRemove(string name) => ZirconBinary.EncodePacket(ZirconPacketIds.Client.GroupRemove, writer => writer.Write(name ?? string.Empty));
        public static byte[] GroupResponse(bool accept) => ZirconBinary.EncodePacket(ZirconPacketIds.Client.GroupResponse, writer => writer.Write(accept));

        public static byte[] TradeRequest() => ZirconBinary.EncodePacket(ZirconPacketIds.Client.TradeRequest);
        public static byte[] TradeResponse(bool accept) => ZirconBinary.EncodePacket(ZirconPacketIds.Client.TradeResponse, writer => writer.Write(accept));
        public static byte[] TradeClose() => ZirconBinary.EncodePacket(ZirconPacketIds.Client.TradeClose);
        public static byte[] TradeAddGold(long gold) => ZirconBinary.EncodePacket(ZirconPacketIds.Client.TradeAddGold, writer => writer.Write(gold));
        public static byte[] TradeAddItem(ZirconCellLinkInfo link) => ZirconBinary.EncodePacket(ZirconPacketIds.Client.TradeAddItem, writer => WriteCellLink(writer, link.Grid, link.Slot, link.Count));
        public static byte[] TradeConfirm() => ZirconBinary.EncodePacket(ZirconPacketIds.Client.TradeConfirm);

        public static byte[] MailOpened(int index) => ZirconBinary.EncodePacket(ZirconPacketIds.Client.MailOpened, writer => writer.Write(index));
        public static byte[] MailGetItem(int index, int slot) => ZirconBinary.EncodePacket(ZirconPacketIds.Client.MailGetItem, writer => { writer.Write(index); writer.Write(slot); });
        public static byte[] MailDelete(int index) => ZirconBinary.EncodePacket(ZirconPacketIds.Client.MailDelete, writer => writer.Write(index));
        public static byte[] MailSend(IReadOnlyList<ZirconCellLinkInfo> links, string recipient, string subject, string message, long gold)
        {
            return ZirconBinary.EncodePacket(ZirconPacketIds.Client.MailSend, writer =>
            {
                WriteCellLinkList(writer, links);
                writer.Write(recipient ?? string.Empty);
                writer.Write(subject ?? string.Empty);
                writer.Write(message ?? string.Empty);
                writer.Write(gold);
            });
        }

        public static byte[] MarketHistory(int index, int display, int partIndex) => ZirconBinary.EncodePacket(ZirconPacketIds.Client.MarketHistory, writer => { writer.Write(index); writer.Write(display); writer.Write(partIndex); });
        public static byte[] MarketSearch(string name, bool itemTypeFilter, int itemType, int sort) => ZirconBinary.EncodePacket(ZirconPacketIds.Client.MarketSearch, writer => { writer.Write(name ?? string.Empty); writer.Write(itemTypeFilter); writer.Write(itemType); writer.Write(sort); });
        public static byte[] MarketSearchIndex(int index) => ZirconBinary.EncodePacket(ZirconPacketIds.Client.MarketSearchIndex, writer => writer.Write(index));
        public static byte[] MarketBuy(long index, long count, bool guildFunds) => ZirconBinary.EncodePacket(ZirconPacketIds.Client.MarketBuy, writer => { writer.Write(index); writer.Write(count); writer.Write(guildFunds); });
        public static byte[] MarketCancel(int index, long count) => ZirconBinary.EncodePacket(ZirconPacketIds.Client.MarketCancel, writer => { writer.Write(index); writer.Write(count); });
        public static byte[] MarketConsign(ZirconCellLinkInfo link, int price, string message, bool guildFunds) => ZirconBinary.EncodePacket(ZirconPacketIds.Client.MarketConsign, writer => { WriteCellLink(writer, link.Grid, link.Slot, link.Count); writer.Write(price); writer.Write(message ?? string.Empty); writer.Write(guildFunds); });

        public static byte[] GuildCreate(string name, bool useGold, int members, int storage) => ZirconBinary.EncodePacket(ZirconPacketIds.Client.GuildCreate, writer => { writer.Write(name ?? string.Empty); writer.Write(useGold); writer.Write(members); writer.Write(storage); });
        public static byte[] GuildEditNotice(string notice) => ZirconBinary.EncodePacket(ZirconPacketIds.Client.GuildEditNotice, writer => writer.Write(notice ?? string.Empty));
        public static byte[] GuildInviteMember(string name) => ZirconBinary.EncodePacket(ZirconPacketIds.Client.GuildInviteMember, writer => writer.Write(name ?? string.Empty));
        public static byte[] GuildKickMember(int index) => ZirconBinary.EncodePacket(ZirconPacketIds.Client.GuildKickMember, writer => writer.Write(index));
        public static byte[] GuildResponse(bool accept) => ZirconBinary.EncodePacket(ZirconPacketIds.Client.GuildResponse, writer => writer.Write(accept));
        public static byte[] QuestAccept(int index) => ZirconBinary.EncodePacket(ZirconPacketIds.Client.QuestAccept, writer => writer.Write(index));

        public static byte[] QuestComplete(int index, int choiceIndex)
        {
            return ZirconBinary.EncodePacket(ZirconPacketIds.Client.QuestComplete, writer => { writer.Write(index); writer.Write(choiceIndex); });
        }

        public static byte[] QuestTrack(int index, bool track)
        {
            return ZirconBinary.EncodePacket(ZirconPacketIds.Client.QuestTrack, writer => { writer.Write(index); writer.Write(track); });
        }
        public static byte[] MagicKey(int magicType, byte set1Key, byte set2Key, byte set3Key, byte set4Key)
        {
            return ZirconBinary.EncodePacket(
                ZirconPacketIds.Client.MagicKey,
                writer =>
                {
                    writer.Write(magicType);
                    writer.Write(set1Key);
                    writer.Write(set2Key);
                    writer.Write(set3Key);
                    writer.Write(set4Key);
                });
        }

        public static byte[] SortStorageItem()
        {
            return ZirconBinary.EncodePacket(ZirconPacketIds.Client.SortStorageItem);
        }
        public static byte[] NpcSell(IReadOnlyList<ZirconCellLinkInfo> links)
        {
            return ZirconBinary.EncodePacket(ZirconPacketIds.Client.NpcSell, writer => WriteCellLinkList(writer, links));
        }

        public static byte[] NpcRepair(IReadOnlyList<ZirconCellLinkInfo> links, bool special, bool guildFunds)
        {
            return ZirconBinary.EncodePacket(
                ZirconPacketIds.Client.NpcRepair,
                writer =>
                {
                    WriteCellLinkList(writer, links);
                    writer.Write(special);
                    writer.Write(guildFunds);
                });
        }
        public static byte[] NpcClose()
        {
            return ZirconBinary.EncodePacket(ZirconPacketIds.Client.NpcClose);
        }

        private static void WriteCellLinkList(System.IO.BinaryWriter writer, IReadOnlyList<ZirconCellLinkInfo> links)
        {
            if (links == null)
            {
                writer.Write(false);
                return;
            }

            writer.Write(true);
            writer.Write(links.Count);
            foreach (ZirconCellLinkInfo link in links)
                WriteCellLink(writer, link.Grid, link.Slot, link.Count);
        }
        private static void WriteCellLink(System.IO.BinaryWriter writer, ZirconGridType grid, int slot, long count)
        {
            writer.Write(true);
            writer.Write((int)grid);
            writer.Write(slot);
            writer.Write(count);
        }
        public static string CreateChecksum(int length = 20)
        {
            const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            var chars = new char[length];

            for (int i = 0; i < chars.Length; i++)
                chars[i] = alphabet[random.Next(alphabet.Length)];

            return new string(chars);
        }
    }
}



