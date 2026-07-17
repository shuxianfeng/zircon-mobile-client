using System;
using System.IO;
using System.Text;

namespace Zircon.Mobile.Core.Protocol
{
    public readonly struct ZirconObjectRemoveInfo
    {
        public ZirconObjectRemoveInfo(uint objectId)
        {
            ObjectId = objectId;
        }

        public uint ObjectId { get; }
    }

    public readonly struct ZirconWeightUpdateInfo
    {
        public ZirconWeightUpdateInfo(int bagWeight, int wearWeight, int handWeight)
        {
            BagWeight = bagWeight;
            WearWeight = wearWeight;
            HandWeight = handWeight;
        }

        public int BagWeight { get; }
        public int WearWeight { get; }
        public int HandWeight { get; }
    }

    public readonly struct ZirconCurrencyUpdateInfo
    {
        public ZirconCurrencyUpdateInfo(long value)
        {
            Value = value;
        }

        public long Value { get; }
    }

    public readonly struct ZirconAutoTimeChangedInfo
    {
        public ZirconAutoTimeChangedInfo(long autoTime)
        {
            AutoTime = autoTime;
        }

        public long AutoTime { get; }
    }

    public readonly struct ZirconSkillConfigInfo
    {
        public ZirconSkillConfigInfo(int skillLevelLimit)
        {
            SkillLevelLimit = skillLevelLimit;
        }

        public int SkillLevelLimit { get; }
    }

    public static class ZirconStatusPacketDecoder
    {
        public static bool TryDecodeObjectRemove(ZirconPacketFrame frame, out ZirconObjectRemoveInfo value)
        {
            value = default;
            if (frame.PacketId != ZirconPacketIds.Server.ObjectRemove) return false;
            return TryRead(frame, reader => new ZirconObjectRemoveInfo(reader.ReadUInt32()), out value);
        }

        public static bool TryDecodeWeightUpdate(ZirconPacketFrame frame, out ZirconWeightUpdateInfo value)
        {
            value = default;
            if (frame.PacketId != ZirconPacketIds.Server.WeightUpdate) return false;
            return TryRead(frame, reader => new ZirconWeightUpdateInfo(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32()), out value);
        }

        public static bool TryDecodeGoldChanged(ZirconPacketFrame frame, out ZirconCurrencyUpdateInfo value)
        {
            value = default;
            if (frame.PacketId != ZirconPacketIds.Server.GoldChanged) return false;
            return TryRead(frame, reader => new ZirconCurrencyUpdateInfo(reader.ReadInt64()), out value);
        }

        public static bool TryDecodeGameGoldChanged(ZirconPacketFrame frame, out ZirconCurrencyUpdateInfo value)
        {
            value = default;
            if (frame.PacketId != ZirconPacketIds.Server.GameGoldChanged) return false;
            return TryRead(frame, reader => new ZirconCurrencyUpdateInfo(reader.ReadInt32()), out value);
        }

        public static bool TryDecodeHuntGoldChanged(ZirconPacketFrame frame, out ZirconCurrencyUpdateInfo value)
        {
            value = default;
            if (frame.PacketId != ZirconPacketIds.Server.HuntGoldChanged) return false;
            return TryRead(frame, reader => new ZirconCurrencyUpdateInfo(reader.ReadInt32()), out value);
        }

        public static bool TryDecodeAutoTimeChanged(ZirconPacketFrame frame, out ZirconAutoTimeChangedInfo value)
        {
            value = default;
            if (frame.PacketId != ZirconPacketIds.Server.AutoTimeChanged) return false;
            return TryRead(frame, reader => new ZirconAutoTimeChangedInfo(reader.ReadInt64()), out value);
        }

        public static bool TryDecodeSkillConfig(ZirconPacketFrame frame, out ZirconSkillConfigInfo value)
        {
            value = default;
            if (frame.PacketId != ZirconPacketIds.Server.SkillConfig) return false;
            return TryRead(frame, reader => new ZirconSkillConfigInfo(reader.ReadInt32()), out value);
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
    }
}


