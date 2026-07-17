using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Zircon.Mobile.Core.Protocol
{
    public readonly struct ZirconUserQuestTaskInfo
    {
        public ZirconUserQuestTaskInfo(int index, int taskIndex, long amount)
        { Index = index; TaskIndex = taskIndex; Amount = amount; }
        public int Index { get; }
        public int TaskIndex { get; }
        public long Amount { get; }
    }

    public readonly struct ZirconUserQuestInfo
    {
        public ZirconUserQuestInfo(int index, int questIndex, bool track, bool completed, int selectedReward, IReadOnlyList<ZirconUserQuestTaskInfo> tasks)
        { Index = index; QuestIndex = questIndex; Track = track; Completed = completed; SelectedReward = selectedReward; Tasks = tasks ?? Array.Empty<ZirconUserQuestTaskInfo>(); }
        public int Index { get; }
        public int QuestIndex { get; }
        public bool Track { get; }
        public bool Completed { get; }
        public int SelectedReward { get; }
        public IReadOnlyList<ZirconUserQuestTaskInfo> Tasks { get; }
    }

    public static class ZirconQuestPacketDecoder
    {
        public static bool TryDecodeQuestChanged(ZirconPacketFrame frame, out ZirconUserQuestInfo value)
        {
            value = default;
            if (frame.PacketId != ZirconPacketIds.Server.QuestChanged) return false;
            try
            {
                using (var stream = new MemoryStream(frame.Payload))
                using (var reader = new BinaryReader(stream, Encoding.UTF8))
                {
                    ZirconUserQuestInfo? quest = ReadNullableQuest(reader);
                    if (!quest.HasValue || stream.Position != stream.Length) return false;
                    value = quest.Value;
                    return true;
                }
            }
            catch { return false; }
        }

        internal static IReadOnlyList<ZirconUserQuestInfo> ReadQuestList(BinaryReader reader)
        {
            if (!reader.ReadBoolean()) return Array.Empty<ZirconUserQuestInfo>();
            int count = reader.ReadInt32();
            var result = new List<ZirconUserQuestInfo>(count);
            for (int i = 0; i < count; i++)
            {
                ZirconUserQuestInfo? quest = ReadNullableQuest(reader);
                if (quest.HasValue) result.Add(quest.Value);
            }
            return result;
        }

        private static ZirconUserQuestInfo? ReadNullableQuest(BinaryReader reader)
        {
            if (!reader.ReadBoolean()) return null;
            int index = reader.ReadInt32();
            int questIndex = reader.ReadInt32();
            bool track = reader.ReadBoolean();
            bool completed = reader.ReadBoolean();
            int selectedReward = reader.ReadInt32();
            return new ZirconUserQuestInfo(index, questIndex, track, completed, selectedReward, ReadTaskList(reader));
        }

        private static IReadOnlyList<ZirconUserQuestTaskInfo> ReadTaskList(BinaryReader reader)
        {
            if (!reader.ReadBoolean()) return Array.Empty<ZirconUserQuestTaskInfo>();
            int count = reader.ReadInt32();
            var result = new List<ZirconUserQuestTaskInfo>(count);
            for (int i = 0; i < count; i++)
            {
                if (!reader.ReadBoolean()) continue;
                result.Add(new ZirconUserQuestTaskInfo(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt64()));
            }
            return result;
        }
    }
}