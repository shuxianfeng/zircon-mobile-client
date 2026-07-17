using System;
using System.Collections.Generic;
using Zircon.Mobile.Core.Protocol;

namespace Zircon.Mobile.Game.Buffs
{
    public sealed class ZirconBuffState
    {
        public int Index { get; set; }
        public int Type { get; set; }
        public TimeSpan RemainingTime { get; set; }
        public TimeSpan TickFrequency { get; set; }
        public IReadOnlyDictionary<int, int> Stats { get; set; } = new Dictionary<int, int>();
        public bool Paused { get; set; }
        public int ItemIndex { get; set; }
        public DateTime UpdatedUtc { get; set; }

        public TimeSpan GetRemaining(DateTime utcNow)
        {
            if (Paused || RemainingTime <= TimeSpan.Zero)
                return RemainingTime;
            TimeSpan value = RemainingTime - (utcNow - UpdatedUtc);
            return value > TimeSpan.Zero ? value : TimeSpan.Zero;
        }

        public ZirconBuffState Clone(DateTime utcNow)
        {
            return new ZirconBuffState
            {
                Index = Index,
                Type = Type,
                RemainingTime = GetRemaining(utcNow),
                TickFrequency = TickFrequency,
                Stats = new Dictionary<int, int>(Stats),
                Paused = Paused,
                ItemIndex = ItemIndex,
                UpdatedUtc = utcNow,
            };
        }

        public static ZirconBuffState FromPacket(ZirconBuffInfo buff, DateTime utcNow)
        {
            return new ZirconBuffState
            {
                Index = buff.Index,
                Type = buff.Type,
                RemainingTime = buff.RemainingTime,
                TickFrequency = buff.TickFrequency,
                Stats = new Dictionary<int, int>(buff.Stats),
                Paused = buff.Paused,
                ItemIndex = buff.ItemIndex,
                UpdatedUtc = utcNow,
            };
        }
    }
}