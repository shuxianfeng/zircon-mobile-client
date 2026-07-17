using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Zircon.Mobile.Core.Protocol
{
    public readonly struct ZirconObjectMoveInfo
    {
        public ZirconObjectMoveInfo(uint objectId, byte direction, ZirconMapPoint location, int distance, TimeSpan slow)
        {
            ObjectId = objectId;
            Direction = direction;
            Location = location;
            Distance = distance;
            Slow = slow;
        }

        public uint ObjectId { get; }
        public byte Direction { get; }
        public ZirconMapPoint Location { get; }
        public int Distance { get; }
        public TimeSpan Slow { get; }
    }

    public readonly struct ZirconObjectTurnInfo
    {
        public ZirconObjectTurnInfo(uint objectId, byte direction, ZirconMapPoint location, TimeSpan slow)
        {
            ObjectId = objectId;
            Direction = direction;
            Location = location;
            Slow = slow;
        }

        public uint ObjectId { get; }
        public byte Direction { get; }
        public ZirconMapPoint Location { get; }
        public TimeSpan Slow { get; }
    }

    public readonly struct ZirconObjectAttackInfo
    {
        public ZirconObjectAttackInfo(uint objectId, byte direction, ZirconMapPoint location, int attackMagic, byte attackElement, uint targetId, TimeSpan slow)
        {
            ObjectId = objectId;
            Direction = direction;
            Location = location;
            AttackMagic = attackMagic;
            AttackElement = attackElement;
            TargetId = targetId;
            Slow = slow;
        }

        public uint ObjectId { get; }
        public byte Direction { get; }
        public ZirconMapPoint Location { get; }
        public int AttackMagic { get; }
        public byte AttackElement { get; }
        public uint TargetId { get; }
        public TimeSpan Slow { get; }
    }
    public readonly struct ZirconObjectMonsterInfo
    {
        public ZirconObjectMonsterInfo(uint objectId, int monsterIndex, int nameColourArgb, string petOwner, byte direction, ZirconMapPoint location, bool dead)
        {
            ObjectId = objectId;
            MonsterIndex = monsterIndex;
            NameColourArgb = nameColourArgb;
            PetOwner = petOwner ?? string.Empty;
            Direction = direction;
            Location = location;
            Dead = dead;
        }

        public uint ObjectId { get; }
        public int MonsterIndex { get; }
        public int NameColourArgb { get; }
        public string PetOwner { get; }
        public byte Direction { get; }
        public ZirconMapPoint Location { get; }
        public bool Dead { get; }
    }

    public readonly struct ZirconObjectNpcInfo
    {
        public ZirconObjectNpcInfo(uint objectId, int npcIndex, ZirconMapPoint location, byte direction)
        {
            ObjectId = objectId;
            NpcIndex = npcIndex;
            Location = location;
            Direction = direction;
        }

        public uint ObjectId { get; }
        public int NpcIndex { get; }
        public ZirconMapPoint Location { get; }
        public byte Direction { get; }
    }

    public readonly struct ZirconObjectSpellInfo
    {
        public ZirconObjectSpellInfo(uint objectId, byte direction, ZirconMapPoint location, int effect, int power)
        {
            ObjectId = objectId;
            Direction = direction;
            Location = location;
            Effect = effect;
            Power = power;
        }

        public uint ObjectId { get; }
        public byte Direction { get; }
        public ZirconMapPoint Location { get; }
        public int Effect { get; }
        public int Power { get; }
    }

    public readonly struct ZirconDataObjectPlayerInfo
    {
        public ZirconDataObjectPlayerInfo(uint objectId, int mapIndex, ZirconMapPoint location, string name, int health, int mana, bool dead, int maxHealth, int maxMana)
        {
            ObjectId = objectId;
            MapIndex = mapIndex;
            Location = location;
            Name = name ?? string.Empty;
            Health = health;
            Mana = mana;
            Dead = dead;
            MaxHealth = maxHealth;
            MaxMana = maxMana;
        }

        public uint ObjectId { get; }
        public int MapIndex { get; }
        public ZirconMapPoint Location { get; }
        public string Name { get; }
        public int Health { get; }
        public int Mana { get; }
        public bool Dead { get; }
        public int MaxHealth { get; }
        public int MaxMana { get; }
    }

    public readonly struct ZirconDataObjectMonsterInfo
    {
        public ZirconDataObjectMonsterInfo(uint objectId, int mapIndex, ZirconMapPoint location, int monsterIndex, string petOwner, int health, IReadOnlyDictionary<int, int> stats, bool dead)
        {
            ObjectId = objectId;
            MapIndex = mapIndex;
            Location = location;
            MonsterIndex = monsterIndex;
            PetOwner = petOwner ?? string.Empty;
            Health = health;
            Stats = stats ?? EmptyStats;
            Dead = dead;
        }

        private static readonly IReadOnlyDictionary<int, int> EmptyStats = new Dictionary<int, int>();

        public uint ObjectId { get; }
        public int MapIndex { get; }
        public ZirconMapPoint Location { get; }
        public int MonsterIndex { get; }
        public string PetOwner { get; }
        public int Health { get; }
        public IReadOnlyDictionary<int, int> Stats { get; }
        public bool Dead { get; }
    }

    public readonly struct ZirconUserItemInfo
    {
        public ZirconUserItemInfo(int index, int infoIndex, int currentDurability, int maxDurability, long count, int slot, int level = 0, int flags = 0)
        {
            Index = index;
            InfoIndex = infoIndex;
            CurrentDurability = currentDurability;
            MaxDurability = maxDurability;
            Count = count;
            Slot = slot;
            Level = level;
            Flags = flags;
        }

        public int Index { get; }
        public int InfoIndex { get; }
        public int CurrentDurability { get; }
        public int MaxDurability { get; }
        public long Count { get; }
        public int Slot { get; }
        public int Level { get; }
        public int Flags { get; }
    }

    public readonly struct ZirconObjectItemInfo
    {
        public ZirconObjectItemInfo(uint objectId, ZirconUserItemInfo? item, ZirconMapPoint location)
        {
            ObjectId = objectId;
            Item = item;
            Location = location;
        }

        public uint ObjectId { get; }
        public ZirconUserItemInfo? Item { get; }
        public ZirconMapPoint Location { get; }
    }

    public readonly struct ZirconDataObjectItemInfo
    {
        public ZirconDataObjectItemInfo(uint objectId, int mapIndex, ZirconMapPoint location, int itemInfoIndex)
        {
            ObjectId = objectId;
            MapIndex = mapIndex;
            Location = location;
            ItemInfoIndex = itemInfoIndex;
        }

        public uint ObjectId { get; }
        public int MapIndex { get; }
        public ZirconMapPoint Location { get; }
        public int ItemInfoIndex { get; }
    }

    public readonly struct ZirconObjectMagicInfo
    {
        public ZirconObjectMagicInfo(uint objectId, byte direction, ZirconMapPoint location, int magicType, IReadOnlyList<uint> targets, IReadOnlyList<ZirconMapPoint> locations, bool cast, TimeSpan slow)
        {
            ObjectId = objectId;
            Direction = direction;
            Location = location;
            MagicType = magicType;
            Targets = targets ?? Array.Empty<uint>();
            Locations = locations ?? Array.Empty<ZirconMapPoint>();
            Cast = cast;
            Slow = slow;
        }

        public uint ObjectId { get; }
        public byte Direction { get; }
        public ZirconMapPoint Location { get; }
        public int MagicType { get; }
        public IReadOnlyList<uint> Targets { get; }
        public IReadOnlyList<ZirconMapPoint> Locations { get; }
        public bool Cast { get; }
        public TimeSpan Slow { get; }
    }

    public readonly struct ZirconMagicToggleInfo
    {
        public ZirconMagicToggleInfo(int magicType, bool canUse)
        {
            MagicType = magicType;
            CanUse = canUse;
        }

        public int MagicType { get; }
        public bool CanUse { get; }
    }

    public readonly struct ZirconMagicProgressInfo
    {
        public ZirconMagicProgressInfo(int infoIndex, int level, long experience)
        {
            InfoIndex = infoIndex;
            Level = level;
            Experience = experience;
        }

        public int InfoIndex { get; }
        public int Level { get; }
        public long Experience { get; }
    }

    public readonly struct ZirconItemsGainedInfo
    {
        public ZirconItemsGainedInfo(IReadOnlyList<ZirconUserItemInfo> items)
        {
            Items = items ?? Array.Empty<ZirconUserItemInfo>();
        }

        public IReadOnlyList<ZirconUserItemInfo> Items { get; }
    }

    public readonly struct ZirconMagicCooldownInfo
    {
        public ZirconMagicCooldownInfo(int infoIndex, int delay)
        {
            InfoIndex = infoIndex;
            Delay = delay;
        }

        public int InfoIndex { get; }
        public int Delay { get; }
    }

    public readonly struct ZirconDataObjectLocationInfo
    {
        public ZirconDataObjectLocationInfo(uint objectId, int mapIndex, ZirconMapPoint location)
        {
            ObjectId = objectId;
            MapIndex = mapIndex;
            Location = location;
        }

        public uint ObjectId { get; }
        public int MapIndex { get; }
        public ZirconMapPoint Location { get; }
    }

    public readonly struct ZirconDataObjectMaxHealthManaInfo
    {
        public ZirconDataObjectMaxHealthManaInfo(uint objectId, int maxHealth, int maxMana, IReadOnlyDictionary<int, int> stats)
        {
            ObjectId = objectId;
            MaxHealth = maxHealth;
            MaxMana = maxMana;
            Stats = stats ?? EmptyStats;
        }

        private static readonly IReadOnlyDictionary<int, int> EmptyStats = new Dictionary<int, int>();

        public uint ObjectId { get; }
        public int MaxHealth { get; }
        public int MaxMana { get; }
        public IReadOnlyDictionary<int, int> Stats { get; }
    }

    public readonly struct ZirconChatMessageInfo
    {
        public ZirconChatMessageInfo(uint objectId, string text, int messageType)
        {
            ObjectId = objectId;
            Text = text ?? string.Empty;
            MessageType = messageType;
        }

        public uint ObjectId { get; }
        public string Text { get; }
        public int MessageType { get; }
    }

    public readonly struct ZirconNpcResponseInfo
    {
        public ZirconNpcResponseInfo(uint objectId, int pageIndex)
        {
            ObjectId = objectId;
            PageIndex = pageIndex;
        }

        public uint ObjectId { get; }
        public int PageIndex { get; }
    }
    public readonly struct ZirconStatsUpdateInfo
    {
        public ZirconStatsUpdateInfo(IReadOnlyDictionary<int, int> stats, IReadOnlyDictionary<int, int> hermitStats, int hermitPoints)
        {
            Stats = stats ?? EmptyStats;
            HermitStats = hermitStats ?? EmptyStats;
            HermitPoints = hermitPoints;
        }

        private static readonly IReadOnlyDictionary<int, int> EmptyStats = new Dictionary<int, int>();

        public IReadOnlyDictionary<int, int> Stats { get; }
        public IReadOnlyDictionary<int, int> HermitStats { get; }
        public int HermitPoints { get; }
    }

    public static class ZirconInGamePacketDecoder
    {
        public static bool TryDecodeObjectMove(ZirconPacketFrame frame, out ZirconObjectMoveInfo value)
        {
            if (frame.PacketId != ZirconPacketIds.Server.ObjectMove)
            {
                value = default;
                return false;
            }

            return TryRead(frame, reader => new ZirconObjectMoveInfo(reader.ReadUInt32(), reader.ReadByte(), ReadPoint(reader), reader.ReadInt32(), TimeSpan.FromTicks(reader.ReadInt64())), out value);
        }

        public static bool TryDecodeObjectTurn(ZirconPacketFrame frame, out ZirconObjectTurnInfo value)
        {
            if (frame.PacketId != ZirconPacketIds.Server.ObjectTurn)
            {
                value = default;
                return false;
            }

            return TryRead(frame, reader => new ZirconObjectTurnInfo(reader.ReadUInt32(), reader.ReadByte(), ReadPoint(reader), TimeSpan.FromTicks(reader.ReadInt64())), out value);
        }

        public static bool TryDecodeObjectAttack(ZirconPacketFrame frame, out ZirconObjectAttackInfo value)
        {
            if (frame.PacketId != ZirconPacketIds.Server.ObjectAttack)
            {
                value = default;
                return false;
            }

            return TryRead(frame, reader => new ZirconObjectAttackInfo(
                reader.ReadUInt32(),
                reader.ReadByte(),
                ReadPoint(reader),
                reader.ReadInt32(),
                reader.ReadByte(),
                reader.ReadUInt32(),
                TimeSpan.FromTicks(reader.ReadInt64())), out value);
        }

        public static bool TryDecodeCombatTime(ZirconPacketFrame frame)
        {
            return frame.PacketId == ZirconPacketIds.Server.CombatTime && frame.Payload.Length == 0;
        }
        public static bool TryDecodeObjectMonster(ZirconPacketFrame frame, out ZirconObjectMonsterInfo value)
        {
            if (frame.PacketId != ZirconPacketIds.Server.ObjectMonster)
            {
                value = default;
                return false;
            }

            return TryRead(frame, reader =>
            {
                uint objectId = reader.ReadUInt32();
                int monsterIndex = reader.ReadInt32();
                int nameColour = reader.ReadInt32();
                string petOwner = reader.ReadString();
                byte direction = reader.ReadByte();
                ZirconMapPoint location = ReadPoint(reader);
                bool dead = reader.ReadBoolean();
                return new ZirconObjectMonsterInfo(objectId, monsterIndex, nameColour, petOwner, direction, location, dead);
            }, out value);
        }

        public static bool TryDecodeObjectNpc(ZirconPacketFrame frame, out ZirconObjectNpcInfo value)
        {
            if (frame.PacketId != ZirconPacketIds.Server.ObjectNpc)
            {
                value = default;
                return false;
            }

            return TryRead(frame, reader => new ZirconObjectNpcInfo(reader.ReadUInt32(), reader.ReadInt32(), ReadPoint(reader), reader.ReadByte()), out value);
        }

        public static bool TryDecodeObjectSpell(ZirconPacketFrame frame, out ZirconObjectSpellInfo value)
        {
            if (frame.PacketId != ZirconPacketIds.Server.ObjectSpell)
            {
                value = default;
                return false;
            }

            return TryRead(frame, reader => new ZirconObjectSpellInfo(reader.ReadUInt32(), reader.ReadByte(), ReadPoint(reader), reader.ReadInt32(), reader.ReadInt32()), out value);
        }

        public static bool TryDecodeDataObjectPlayer(ZirconPacketFrame frame, out ZirconDataObjectPlayerInfo value)
        {
            if (frame.PacketId != ZirconPacketIds.Server.DataObjectPlayer)
            {
                value = default;
                return false;
            }

            return TryRead(frame, reader => new ZirconDataObjectPlayerInfo(
                reader.ReadUInt32(),
                reader.ReadInt32(),
                ReadPoint(reader),
                reader.ReadString(),
                reader.ReadInt32(),
                reader.ReadInt32(),
                reader.ReadBoolean(),
                reader.ReadInt32(),
                reader.ReadInt32()), out value);
        }

        public static bool TryDecodeDataObjectMonster(ZirconPacketFrame frame, out ZirconDataObjectMonsterInfo value)
        {
            if (frame.PacketId != ZirconPacketIds.Server.DataObjectMonster)
            {
                value = default;
                return false;
            }

            return TryRead(frame, reader =>
            {
                uint objectId = reader.ReadUInt32();
                int mapIndex = reader.ReadInt32();
                ZirconMapPoint location = ReadPoint(reader);
                int monsterIndex = reader.ReadInt32();
                string petOwner = reader.ReadString();
                int health = reader.ReadInt32();
                IReadOnlyDictionary<int, int> stats = ReadStats(reader);
                bool dead = reader.ReadBoolean();
                return new ZirconDataObjectMonsterInfo(objectId, mapIndex, location, monsterIndex, petOwner, health, stats, dead);
            }, out value);
        }

        public static bool TryDecodeObjectItem(ZirconPacketFrame frame, out ZirconObjectItemInfo value)
        {
            if (frame.PacketId != ZirconPacketIds.Server.ObjectItem)
            {
                value = default;
                return false;
            }

            return TryRead(frame, reader =>
            {
                uint objectId = reader.ReadUInt32();
                ZirconUserItemInfo? item = ReadNullableUserItem(reader);
                return new ZirconObjectItemInfo(objectId, item, ReadPoint(reader));
            }, out value);
        }

        public static bool TryDecodeDataObjectItem(ZirconPacketFrame frame, out ZirconDataObjectItemInfo value)
        {
            if (frame.PacketId != ZirconPacketIds.Server.DataObjectItem)
            {
                value = default;
                return false;
            }

            return TryRead(frame, reader => new ZirconDataObjectItemInfo(reader.ReadUInt32(), reader.ReadInt32(), ReadPoint(reader), reader.ReadInt32()), out value);
        }

        public static bool TryDecodeObjectMagic(ZirconPacketFrame frame, out ZirconObjectMagicInfo value)
        {
            if (frame.PacketId != ZirconPacketIds.Server.ObjectMagic)
            {
                value = default;
                return false;
            }

            return TryRead(frame, reader => new ZirconObjectMagicInfo(
                reader.ReadUInt32(),
                reader.ReadByte(),
                ReadPoint(reader),
                reader.ReadInt32(),
                ReadUIntList(reader),
                ReadPointList(reader),
                reader.ReadBoolean(),
                TimeSpan.FromTicks(reader.ReadInt64())), out value);
        }

        public static bool TryDecodeMagicToggle(ZirconPacketFrame frame, out ZirconMagicToggleInfo value)
        {
            if (frame.PacketId != ZirconPacketIds.Server.MagicToggle)
            {
                value = default;
                return false;
            }

            return TryRead(frame, reader => new ZirconMagicToggleInfo(reader.ReadInt32(), reader.ReadBoolean()), out value);
        }

        public static bool TryDecodeNewMagic(ZirconPacketFrame frame, out ZirconUserMagicInfo value)
        {
            if (frame.PacketId != ZirconPacketIds.Server.NewMagic)
            {
                value = default;
                return false;
            }

            return TryRead(frame, reader => ReadNullableUserMagic(reader) ?? throw new InvalidDataException("NewMagic contained no magic."), out value);
        }

        public static bool TryDecodeMagicLeveled(ZirconPacketFrame frame, out ZirconMagicProgressInfo value)
        {
            if (frame.PacketId != ZirconPacketIds.Server.MagicLeveled)
            {
                value = default;
                return false;
            }

            return TryRead(frame, reader => new ZirconMagicProgressInfo(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt64()), out value);
        }

        public static bool TryDecodeMagicCooldown(ZirconPacketFrame frame, out ZirconMagicCooldownInfo value)
        {
            if (frame.PacketId != ZirconPacketIds.Server.MagicCooldown)
            {
                value = default;
                return false;
            }

            return TryRead(frame, reader => new ZirconMagicCooldownInfo(reader.ReadInt32(), reader.ReadInt32()), out value);
        }

        public static bool TryDecodeItemsGained(ZirconPacketFrame frame, out ZirconItemsGainedInfo value)
        {
            if (frame.PacketId != ZirconPacketIds.Server.ItemsGained)
            {
                value = default;
                return false;
            }

            return TryRead(frame, reader =>
            {
                return new ZirconItemsGainedInfo(ReadUserItemList(reader));
            }, out value);
        }

        public static bool TryDecodeDataObjectLocation(ZirconPacketFrame frame, out ZirconDataObjectLocationInfo value)
        {
            if (frame.PacketId != ZirconPacketIds.Server.DataObjectLocation)
            {
                value = default;
                return false;
            }

            return TryRead(frame, reader => new ZirconDataObjectLocationInfo(reader.ReadUInt32(), reader.ReadInt32(), ReadPoint(reader)), out value);
        }

        public static bool TryDecodeDataObjectMaxHealthMana(ZirconPacketFrame frame, out ZirconDataObjectMaxHealthManaInfo value)
        {
            if (frame.PacketId != ZirconPacketIds.Server.DataObjectMaxHealthMana)
            {
                value = default;
                return false;
            }

            return TryRead(frame, reader => new ZirconDataObjectMaxHealthManaInfo(reader.ReadUInt32(), reader.ReadInt32(), reader.ReadInt32(), ReadNullableStats(reader)), out value);
        }

        public static bool TryDecodeChat(ZirconPacketFrame frame, out ZirconChatMessageInfo value)
        {
            if (frame.PacketId != ZirconPacketIds.Server.Chat)
            {
                value = default;
                return false;
            }

            return TryRead(frame, reader => new ZirconChatMessageInfo(reader.ReadUInt32(), reader.ReadString(), reader.ReadInt32()), out value);
        }

        public static bool TryDecodeNpcResponse(ZirconPacketFrame frame, out ZirconNpcResponseInfo value)
        {
            if (frame.PacketId != ZirconPacketIds.Server.NpcResponse)
            {
                value = default;
                return false;
            }

            return TryRead(frame, reader => new ZirconNpcResponseInfo(reader.ReadUInt32(), reader.ReadInt32()), out value);
        }

        public static bool TryDecodeNpcClose(ZirconPacketFrame frame)
        {
            return frame.PacketId == ZirconPacketIds.Server.NpcClose && frame.Payload.Length == 0;
        }
        public static bool TryDecodeStatsUpdate(ZirconPacketFrame frame, out ZirconStatsUpdateInfo value)
        {
            if (frame.PacketId != ZirconPacketIds.Server.StatsUpdate)
            {
                value = default;
                return false;
            }

            return TryRead(frame, reader => new ZirconStatsUpdateInfo(ReadNullableStats(reader), ReadNullableStats(reader), reader.ReadInt32()), out value);
        }

        private static bool TryRead<T>(ZirconPacketFrame frame, Func<BinaryReader, T> read, out T value)
        {
            value = default;
            try
            {
                using (var stream = new MemoryStream(frame.Payload))
                using (var reader = new BinaryReader(stream, Encoding.UTF8))
                {
                    value = read(reader);
                    return true;
                }
            }
            catch
            {
                value = default;
                return false;
            }
        }

        private static ZirconUserMagicInfo? ReadNullableUserMagic(BinaryReader reader)
        {
            if (!reader.ReadBoolean())
                return null;

            return new ZirconUserMagicInfo(
                reader.ReadInt32(),
                reader.ReadInt32(),
                reader.ReadByte(),
                reader.ReadByte(),
                reader.ReadByte(),
                reader.ReadByte(),
                reader.ReadInt32(),
                reader.ReadInt64(),
                TimeSpan.FromTicks(reader.ReadInt64()));
        }

        internal static IReadOnlyList<ZirconUserItemInfo> ReadUserItemList(BinaryReader reader)
        {
            if (!reader.ReadBoolean())
                return Array.Empty<ZirconUserItemInfo>();

            int count = reader.ReadInt32();
            var items = new List<ZirconUserItemInfo>(count);
            for (int i = 0; i < count; i++)
            {
                ZirconUserItemInfo? item = ReadNullableUserItem(reader);
                if (item.HasValue)
                    items.Add(item.Value);
            }
            return items;
        }
        internal static ZirconUserItemInfo? ReadNullableUserItem(BinaryReader reader)
        {
            if (!reader.ReadBoolean())
                return null;

            int index = reader.ReadInt32();
            int infoIndex = reader.ReadInt32();
            int currentDurability = reader.ReadInt32();
            int maxDurability = reader.ReadInt32();
            long count = reader.ReadInt64();
            int slot = reader.ReadInt32();
            int level = reader.ReadInt32();
            reader.ReadDecimal();
            reader.ReadInt32();
            reader.ReadInt64();
            reader.ReadInt64();
            ReadNullableStats(reader);
            int flags = reader.ReadInt32();
            reader.ReadInt64();
            return new ZirconUserItemInfo(index, infoIndex, currentDurability, maxDurability, count, slot, level, flags);
        }

        private static IReadOnlyList<uint> ReadUIntList(BinaryReader reader)
        {
            if (!reader.ReadBoolean())
                return Array.Empty<uint>();

            int count = reader.ReadInt32();
            var values = new List<uint>(count);
            for (int i = 0; i < count; i++)
                values.Add(reader.ReadUInt32());

            return values;
        }

        private static IReadOnlyList<ZirconMapPoint> ReadPointList(BinaryReader reader)
        {
            if (!reader.ReadBoolean())
                return Array.Empty<ZirconMapPoint>();

            int count = reader.ReadInt32();
            var values = new List<ZirconMapPoint>(count);
            for (int i = 0; i < count; i++)
                values.Add(ReadPoint(reader));

            return values;
        }

        private static ZirconMapPoint ReadPoint(BinaryReader reader)
        {
            return new ZirconMapPoint(reader.ReadInt32(), reader.ReadInt32());
        }

        private static IReadOnlyDictionary<int, int> ReadNullableStats(BinaryReader reader)
        {
            if (!reader.ReadBoolean())
                return new Dictionary<int, int>();

            if (!reader.ReadBoolean())
                return new Dictionary<int, int>();

            return ReadStats(reader);
        }

        private static IReadOnlyDictionary<int, int> ReadStats(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            var values = new Dictionary<int, int>(count);

            for (int i = 0; i < count; i++)
            {
                int stat = reader.ReadInt32();
                int value = reader.ReadInt32();
                values[stat] = value;
            }

            return values;
        }
    }
}


