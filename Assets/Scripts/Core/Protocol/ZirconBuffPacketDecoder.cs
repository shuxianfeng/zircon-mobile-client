using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Zircon.Mobile.Core.Protocol
{
    public readonly struct ZirconBuffInfo
    {
        public ZirconBuffInfo(int index, int type, TimeSpan remainingTime, TimeSpan tickFrequency, IReadOnlyDictionary<int, int> stats, bool paused, int itemIndex)
        {
            Index = index;
            Type = type;
            RemainingTime = remainingTime;
            TickFrequency = tickFrequency;
            Stats = stats ?? new Dictionary<int, int>();
            Paused = paused;
            ItemIndex = itemIndex;
        }

        public int Index { get; }
        public int Type { get; }
        public TimeSpan RemainingTime { get; }
        public TimeSpan TickFrequency { get; }
        public IReadOnlyDictionary<int, int> Stats { get; }
        public bool Paused { get; }
        public int ItemIndex { get; }
    }

    public readonly struct ZirconBuffStatsInfo
    {
        public ZirconBuffStatsInfo(int index, IReadOnlyDictionary<int, int> stats) { Index = index; Stats = stats; }
        public int Index { get; }
        public IReadOnlyDictionary<int, int> Stats { get; }
    }

    public readonly struct ZirconBuffTimeInfo
    {
        public ZirconBuffTimeInfo(int index, TimeSpan remainingTime) { Index = index; RemainingTime = remainingTime; }
        public int Index { get; }
        public TimeSpan RemainingTime { get; }
    }

    public readonly struct ZirconBuffPausedInfo
    {
        public ZirconBuffPausedInfo(int index, bool paused) { Index = index; Paused = paused; }
        public int Index { get; }
        public bool Paused { get; }
    }

    public static class ZirconBuffPacketDecoder
    {
        public static bool TryDecodeBuffAdd(ZirconPacketFrame frame, out ZirconBuffInfo value)
        {
            return TryRead(frame, ZirconPacketIds.Server.BuffAdd, reader => ReadNullableBuff(reader) ?? throw new InvalidDataException("BuffAdd contained no buff."), out value);
        }

        public static bool TryDecodeBuffRemove(ZirconPacketFrame frame, out int index)
        {
            return TryRead(frame, ZirconPacketIds.Server.BuffRemove, reader => reader.ReadInt32(), out index);
        }

        public static bool TryDecodeBuffChanged(ZirconPacketFrame frame, out ZirconBuffStatsInfo value)
        {
            return TryRead(frame, ZirconPacketIds.Server.BuffChanged, reader => new ZirconBuffStatsInfo(reader.ReadInt32(), ReadStats(reader)), out value);
        }

        public static bool TryDecodeBuffTime(ZirconPacketFrame frame, out ZirconBuffTimeInfo value)
        {
            return TryRead(frame, ZirconPacketIds.Server.BuffTime, reader => new ZirconBuffTimeInfo(reader.ReadInt32(), TimeSpan.FromTicks(reader.ReadInt64())), out value);
        }

        public static bool TryDecodeBuffPaused(ZirconPacketFrame frame, out ZirconBuffPausedInfo value)
        {
            return TryRead(frame, ZirconPacketIds.Server.BuffPaused, reader => new ZirconBuffPausedInfo(reader.ReadInt32(), reader.ReadBoolean()), out value);
        }

        internal static IReadOnlyList<ZirconBuffInfo> ReadBuffList(BinaryReader reader)
        {
            if (!reader.ReadBoolean())
                return Array.Empty<ZirconBuffInfo>();

            int count = ReadCount(reader);
            var result = new List<ZirconBuffInfo>(count);
            for (int i = 0; i < count; i++)
            {
                ZirconBuffInfo? buff = ReadNullableBuff(reader);
                if (buff.HasValue)
                    result.Add(buff.Value);
            }
            return result;
        }

        private static ZirconBuffInfo? ReadNullableBuff(BinaryReader reader)
        {
            if (!reader.ReadBoolean())
                return null;

            return new ZirconBuffInfo(
                reader.ReadInt32(),
                reader.ReadInt32(),
                TimeSpan.FromTicks(reader.ReadInt64()),
                TimeSpan.FromTicks(reader.ReadInt64()),
                ReadStats(reader),
                reader.ReadBoolean(),
                reader.ReadInt32());
        }

        private static IReadOnlyDictionary<int, int> ReadStats(BinaryReader reader)
        {
            if (!reader.ReadBoolean() || !reader.ReadBoolean())
                return new Dictionary<int, int>();

            int count = ReadCount(reader);
            var result = new Dictionary<int, int>(count);
            for (int i = 0; i < count; i++)
                result[reader.ReadInt32()] = reader.ReadInt32();
            return result;
        }

        private static int ReadCount(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            if (count < 0 || count > 100000)
                throw new InvalidDataException($"Invalid collection count: {count}.");
            return count;
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