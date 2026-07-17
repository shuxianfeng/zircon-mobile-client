using Zircon.Mobile.Core.Protocol;

namespace Zircon.Mobile.Game.Items
{
    public sealed class ZirconItemState
    {
        public int Index { get; set; }
        public int InfoIndex { get; set; }
        public ZirconGridType Grid { get; set; }
        public int Slot { get; set; }
        public long Count { get; set; }
        public int CurrentDurability { get; set; }
        public int MaxDurability { get; set; }
        public int Level { get; set; }
        public int Flags { get; set; }
        public bool Locked { get; set; }

        public ZirconItemState Clone()
        {
            return (ZirconItemState)MemberwiseClone();
        }

        public static ZirconItemState FromPacket(ZirconUserItemInfo item)
        {
            bool equipment = item.Slot >= 1000;
            return new ZirconItemState
            {
                Index = item.Index,
                InfoIndex = item.InfoIndex,
                Grid = equipment ? ZirconGridType.Equipment : ZirconGridType.Inventory,
                Slot = equipment ? item.Slot - 1000 : item.Slot,
                Count = item.Count,
                CurrentDurability = item.CurrentDurability,
                MaxDurability = item.MaxDurability,
                Level = item.Level,
                Flags = item.Flags,
                Locked = (item.Flags & 1) != 0,
            };
        }
    }
}