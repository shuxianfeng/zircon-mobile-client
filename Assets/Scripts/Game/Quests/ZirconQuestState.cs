using System.Collections.Generic;
using Zircon.Mobile.Core.Protocol;

namespace Zircon.Mobile.Game.Quests
{
    public sealed class ZirconQuestState
    {
        public int Index { get; set; }
        public int QuestIndex { get; set; }
        public bool Track { get; set; }
        public bool Completed { get; set; }
        public int SelectedReward { get; set; }
        public List<ZirconQuestTaskState> Tasks { get; } = new List<ZirconQuestTaskState>();

        public ZirconQuestState Clone()
        {
            var value = new ZirconQuestState { Index = Index, QuestIndex = QuestIndex, Track = Track, Completed = Completed, SelectedReward = SelectedReward };
            foreach (ZirconQuestTaskState task in Tasks) value.Tasks.Add(task.Clone());
            return value;
        }

        public static ZirconQuestState FromPacket(ZirconUserQuestInfo info)
        {
            var value = new ZirconQuestState { Index = info.Index, QuestIndex = info.QuestIndex, Track = info.Track, Completed = info.Completed, SelectedReward = info.SelectedReward };
            foreach (ZirconUserQuestTaskInfo task in info.Tasks)
                value.Tasks.Add(new ZirconQuestTaskState { Index = task.Index, TaskIndex = task.TaskIndex, Amount = task.Amount });
            return value;
        }
    }

    public sealed class ZirconQuestTaskState
    {
        public int Index { get; set; }
        public int TaskIndex { get; set; }
        public long Amount { get; set; }
        public ZirconQuestTaskState Clone() => (ZirconQuestTaskState)MemberwiseClone();
    }
}