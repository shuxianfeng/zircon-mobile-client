using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Zircon.Mobile.Core.Protocol
{
    public readonly struct ZirconCharacterSelectInfo
    {
        public ZirconCharacterSelectInfo(int index, string name, int level, byte gender, byte characterClass, int location, DateTime lastLogin)
        {
            Index = index;
            Name = name ?? string.Empty;
            Level = level;
            Gender = gender;
            CharacterClass = characterClass;
            Location = location;
            LastLogin = lastLogin;
        }

        public int Index { get; }
        public string Name { get; }
        public int Level { get; }
        public byte Gender { get; }
        public byte CharacterClass { get; }
        public int Location { get; }
        public DateTime LastLogin { get; }
    }

    public readonly struct ZirconMapPoint
    {
        public ZirconMapPoint(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }
        public int Y { get; }
    }

    public readonly struct ZirconUserMagicInfo
    {
        public ZirconUserMagicInfo(int index, int infoIndex, byte set1Key, byte set2Key, byte set3Key, byte set4Key, int level, long experience, TimeSpan cooldown)
        {
            Index = index;
            InfoIndex = infoIndex;
            Set1Key = set1Key;
            Set2Key = set2Key;
            Set3Key = set3Key;
            Set4Key = set4Key;
            Level = level;
            Experience = experience;
            Cooldown = cooldown;
        }

        public int Index { get; }
        public int InfoIndex { get; }
        public byte Set1Key { get; }
        public byte Set2Key { get; }
        public byte Set3Key { get; }
        public byte Set4Key { get; }
        public int Level { get; }
        public long Experience { get; }
        public TimeSpan Cooldown { get; }
    }

    public readonly struct ZirconStartInformation
    {
        public ZirconStartInformation(int index, uint objectId, string name, byte characterClass, byte gender, ZirconMapPoint location, byte direction, int mapIndex, int level, int currentHp, int currentMp, IReadOnlyList<ZirconUserItemInfo> items, IReadOnlyList<ZirconUserMagicInfo> magics, IReadOnlyList<ZirconBuffInfo> buffs, IReadOnlyList<ZirconUserQuestInfo> quests)
        {
            Index = index;
            ObjectId = objectId;
            Name = name ?? string.Empty;
            CharacterClass = characterClass;
            Gender = gender;
            Location = location;
            Direction = direction;
            MapIndex = mapIndex;
            Level = level;
            CurrentHp = currentHp;
            CurrentMp = currentMp;
            Items = items ?? Array.Empty<ZirconUserItemInfo>();
            Magics = magics ?? Array.Empty<ZirconUserMagicInfo>();
            Buffs = buffs ?? Array.Empty<ZirconBuffInfo>();
            Quests = quests ?? Array.Empty<ZirconUserQuestInfo>();
        }

        public int Index { get; }
        public uint ObjectId { get; }
        public string Name { get; }
        public byte CharacterClass { get; }
        public byte Gender { get; }
        public ZirconMapPoint Location { get; }
        public byte Direction { get; }
        public int MapIndex { get; }
        public int Level { get; }
        public int CurrentHp { get; }
        public int CurrentMp { get; }
        public IReadOnlyList<ZirconUserItemInfo> Items { get; }
        public IReadOnlyList<ZirconUserMagicInfo> Magics { get; }
        public IReadOnlyList<ZirconBuffInfo> Buffs { get; }
        public IReadOnlyList<ZirconUserQuestInfo> Quests { get; }
    }

    public readonly struct ZirconDecodedLogin
    {
        public ZirconDecodedLogin(ZirconLoginResult result, string message, TimeSpan duration, IReadOnlyList<ZirconCharacterSelectInfo> characters, IReadOnlyList<ZirconUserItemInfo> storageItems = null)
        {
            Result = result;
            Message = message ?? string.Empty;
            Duration = duration;
            Characters = characters ?? Array.Empty<ZirconCharacterSelectInfo>();
            StorageItems = storageItems ?? Array.Empty<ZirconUserItemInfo>();
        }

        public ZirconLoginResult Result { get; }
        public string Message { get; }
        public TimeSpan Duration { get; }
        public IReadOnlyList<ZirconCharacterSelectInfo> Characters { get; }
        public IReadOnlyList<ZirconUserItemInfo> StorageItems { get; }
    }

    public readonly struct ZirconDecodedStartGame
    {
        public ZirconDecodedStartGame(ZirconStartGameResult result, string message, TimeSpan duration, ZirconStartInformation? startInformation)
        {
            Result = result;
            Message = message ?? string.Empty;
            Duration = duration;
            StartInformation = startInformation;
        }

        public ZirconStartGameResult Result { get; }
        public string Message { get; }
        public TimeSpan Duration { get; }
        public ZirconStartInformation? StartInformation { get; }
    }

    public static class ZirconServerPacketDecoder
    {
        public static bool TryDecodeLogin(ZirconPacketFrame frame, out ZirconDecodedLogin login)
        {
            login = default;
            if (frame.PacketId != ZirconPacketIds.Server.Login && frame.PacketId != ZirconPacketIds.Server.LoginSimple)
                return false;

            try
            {
                using (var stream = new MemoryStream(frame.Payload))
                using (var reader = new BinaryReader(stream, Encoding.UTF8))
                {
                    var result = (ZirconLoginResult)reader.ReadByte();
                    string message = reader.ReadString();
                    TimeSpan duration = TimeSpan.FromTicks(reader.ReadInt64());
                    IReadOnlyList<ZirconCharacterSelectInfo> characters = ReadSelectInfoList(reader);
                    IReadOnlyList<ZirconUserItemInfo> storageItems = frame.PacketId == ZirconPacketIds.Server.Login
                        ? ReadClientUserItemList(reader)
                        : Array.Empty<ZirconUserItemInfo>();
                    login = new ZirconDecodedLogin(result, message, duration, characters, storageItems);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public static bool TryDecodeStartGame(ZirconPacketFrame frame, out ZirconDecodedStartGame startGame)
        {
            startGame = default;
            if (frame.PacketId != ZirconPacketIds.Server.StartGame)
                return false;

            try
            {
                using (var stream = new MemoryStream(frame.Payload))
                using (var reader = new BinaryReader(stream, Encoding.UTF8))
                {
                    var result = (ZirconStartGameResult)reader.ReadByte();
                    string message = reader.ReadString();
                    TimeSpan duration = TimeSpan.FromTicks(reader.ReadInt64());
                    ZirconStartInformation? startInformation = ReadStartInformation(reader);
                    startGame = new ZirconDecodedStartGame(result, message, duration, startInformation);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        private static IReadOnlyList<ZirconCharacterSelectInfo> ReadSelectInfoList(BinaryReader reader)
        {
            if (reader.BaseStream.Position >= reader.BaseStream.Length || !reader.ReadBoolean())
                return Array.Empty<ZirconCharacterSelectInfo>();

            int count = reader.ReadInt32();
            var characters = new List<ZirconCharacterSelectInfo>(count);

            for (int i = 0; i < count; i++)
            {
                if (!reader.ReadBoolean())
                    continue;

                int index = reader.ReadInt32();
                string name = reader.ReadString();
                int level = reader.ReadInt32();
                byte gender = reader.ReadByte();
                byte characterClass = reader.ReadByte();
                int location = reader.ReadInt32();
                DateTime lastLogin = DateTime.FromBinary(reader.ReadInt64());
                characters.Add(new ZirconCharacterSelectInfo(index, name, level, gender, characterClass, location, lastLogin));
            }

            return characters;
        }

        private static ZirconStartInformation? ReadStartInformation(BinaryReader reader)
        {
            if (reader.BaseStream.Position >= reader.BaseStream.Length || !reader.ReadBoolean())
                return null;

            int index = reader.ReadInt32();
            uint objectId = reader.ReadUInt32();
            string name = reader.ReadString();
            reader.ReadInt32();
            reader.ReadString();
            reader.ReadString();
            byte characterClass = reader.ReadByte();
            byte gender = reader.ReadByte();
            var location = new ZirconMapPoint(reader.ReadInt32(), reader.ReadInt32());
            byte direction = reader.ReadByte();
            int mapIndex = reader.ReadInt32();
            reader.ReadInt64();
            reader.ReadInt32();
            int level = reader.ReadInt32();
            reader.ReadInt32();
            reader.ReadInt32();
            reader.ReadInt32();
            reader.ReadInt32();
            reader.ReadInt32();
            reader.ReadInt32();
            reader.ReadInt32();
            reader.ReadDecimal();
            int currentHp = reader.ReadInt32();
            int currentMp = reader.ReadInt32();

            reader.ReadByte(); // AttackMode
            reader.ReadByte(); // PetMode
            reader.ReadInt32(); // HermitPoints
            reader.ReadSingle(); // DayTime
            reader.ReadBoolean(); // AllowGroup

            IReadOnlyList<ZirconUserItemInfo> items = ReadClientUserItemList(reader);
            SkipBeltLinkList(reader);
            SkipAutoPotionLinkList(reader);
            IReadOnlyList<ZirconUserMagicInfo> magics = ReadUserMagicList(reader);
            IReadOnlyList<ZirconBuffInfo> buffs = ZirconBuffPacketDecoder.ReadBuffList(reader);
            IReadOnlyList<ZirconUserQuestInfo> quests = Array.Empty<ZirconUserQuestInfo>();
            if (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                reader.ReadInt32(); // PoisonType
                reader.ReadBoolean(); // InSafeZone
                reader.ReadBoolean(); // Observable
                reader.ReadBoolean(); // Dead
                reader.ReadByte(); // Horse
                reader.ReadInt32(); // HelmetShape
                reader.ReadInt32(); // HorseShape
                quests = ZirconQuestPacketDecoder.ReadQuestList(reader);
            }

            return new ZirconStartInformation(index, objectId, name, characterClass, gender, location, direction, mapIndex, level, currentHp, currentMp, items, magics, buffs, quests);
        }

        private static IReadOnlyList<ZirconUserMagicInfo> ReadUserMagicList(BinaryReader reader)
        {
            if (!reader.ReadBoolean())
                return Array.Empty<ZirconUserMagicInfo>();

            int count = ReadCollectionCount(reader, "magic");
            var result = new List<ZirconUserMagicInfo>(count);
            for (int i = 0; i < count; i++)
            {
                if (!reader.ReadBoolean())
                    continue;

                result.Add(new ZirconUserMagicInfo(
                    reader.ReadInt32(),
                    reader.ReadInt32(),
                    reader.ReadByte(),
                    reader.ReadByte(),
                    reader.ReadByte(),
                    reader.ReadByte(),
                    reader.ReadInt32(),
                    reader.ReadInt64(),
                    TimeSpan.FromTicks(reader.ReadInt64())));
            }

            return result;
        }

        private static IReadOnlyList<ZirconUserItemInfo> ReadClientUserItemList(BinaryReader reader)
        {
            if (!reader.ReadBoolean())
                return Array.Empty<ZirconUserItemInfo>();

            int count = ReadCollectionCount(reader, "item");
            var result = new List<ZirconUserItemInfo>(count);
            for (int i = 0; i < count; i++)
            {
                if (!reader.ReadBoolean())
                    continue;

                int index = reader.ReadInt32();
                int infoIndex = reader.ReadInt32();
                int currentDurability = reader.ReadInt32();
                int maxDurability = reader.ReadInt32();
                long itemCount = reader.ReadInt64();
                int slot = reader.ReadInt32();
                int level = reader.ReadInt32();
                reader.ReadDecimal(); // Experience
                reader.ReadInt32(); // Colour
                reader.ReadInt64(); // SpecialRepairCoolDown
                reader.ReadInt64(); // ResetCoolDown
                SkipStats(reader);
                int flags = reader.ReadInt32();
                reader.ReadInt64(); // ExpireTime
                result.Add(new ZirconUserItemInfo(index, infoIndex, currentDurability, maxDurability, itemCount, slot, level, flags));
            }

            return result;
        }

        private static void SkipBeltLinkList(BinaryReader reader)
        {
            if (!reader.ReadBoolean())
                return;

            int count = ReadCollectionCount(reader, "belt link");
            for (int i = 0; i < count; i++)
            {
                if (!reader.ReadBoolean())
                    continue;

                reader.ReadInt32();
                reader.ReadInt32();
                reader.ReadInt32();
            }
        }

        private static void SkipAutoPotionLinkList(BinaryReader reader)
        {
            if (!reader.ReadBoolean())
                return;

            int count = ReadCollectionCount(reader, "auto potion link");
            for (int i = 0; i < count; i++)
            {
                if (!reader.ReadBoolean())
                    continue;

                reader.ReadInt32();
                reader.ReadInt32();
                reader.ReadInt32();
                reader.ReadInt32();
                reader.ReadBoolean();
            }
        }

        private static void SkipStats(BinaryReader reader)
        {
            if (!reader.ReadBoolean())
                return;

            if (!reader.ReadBoolean())
                return;

            int count = ReadCollectionCount(reader, "stat");
            for (int i = 0; i < count; i++)
            {
                reader.ReadInt32();
                reader.ReadInt32();
            }
        }

        private static int ReadCollectionCount(BinaryReader reader, string name)
        {
            int count = reader.ReadInt32();
            if (count < 0 || count > 100000)
                throw new InvalidDataException($"Invalid {name} count: {count}.");

            return count;
        }
    }
}
