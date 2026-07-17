using System;
using Zircon.Mobile.Core.Protocol;

namespace Zircon.Mobile.Game.Skills
{
    public sealed class ZirconSkillState
    {
        public int Index { get; set; }
        public int InfoIndex { get; set; }
        public byte Set1Key { get; set; }
        public byte Set2Key { get; set; }
        public byte Set3Key { get; set; }
        public byte Set4Key { get; set; }
        public int Level { get; set; }
        public long Experience { get; set; }
        public DateTime CooldownUntilUtc { get; set; }

        public TimeSpan RemainingCooldown
        {
            get
            {
                TimeSpan remaining = CooldownUntilUtc - DateTime.UtcNow;
                return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
            }
        }

        public ZirconSkillState Clone()
        {
            return (ZirconSkillState)MemberwiseClone();
        }

        public static ZirconSkillState FromPacket(ZirconUserMagicInfo magic)
        {
            return new ZirconSkillState
            {
                Index = magic.Index,
                InfoIndex = magic.InfoIndex,
                Set1Key = magic.Set1Key,
                Set2Key = magic.Set2Key,
                Set3Key = magic.Set3Key,
                Set4Key = magic.Set4Key,
                Level = magic.Level,
                Experience = magic.Experience,
                CooldownUntilUtc = DateTime.UtcNow + magic.Cooldown,
            };
        }
    }
}