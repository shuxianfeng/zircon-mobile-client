using System;
using System.Collections.Generic;
using Zircon.Mobile.Core.Protocol;

namespace Zircon.Mobile.Game.Entities
{
    public sealed class ZirconEntityState
    {
        public uint ObjectId { get; set; }
        public ZirconEntityKind Kind { get; set; }
        public string Name { get; set; } = string.Empty;
        public int ModelIndex { get; set; }
        public int MapIndex { get; set; }
        public ZirconMapPoint Location { get; set; }
        public byte Direction { get; set; }
        public ZirconMirAction Action { get; set; }
        public int ActionMagic { get; set; }
        public uint ActionTargetId { get; set; }
        public long ActionSequence { get; set; }
        public int Level { get; set; }
        public int Health { get; set; }
        public int MaxHealth { get; set; }
        public int Mana { get; set; }
        public int MaxMana { get; set; }
        public bool Dead { get; set; }
        public string PetOwner { get; set; } = string.Empty;
        public Dictionary<int, int> Stats { get; } = new Dictionary<int, int>();
        public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;

        public ZirconEntityState Clone()
        {
            var clone = new ZirconEntityState
            {
                ObjectId = ObjectId,
                Kind = Kind,
                Name = Name,
                ModelIndex = ModelIndex,
                MapIndex = MapIndex,
                Location = Location,
                Direction = Direction,
                Action = Action,
                ActionMagic = ActionMagic,
                ActionTargetId = ActionTargetId,
                ActionSequence = ActionSequence,
                Level = Level,
                Health = Health,
                MaxHealth = MaxHealth,
                Mana = Mana,
                MaxMana = MaxMana,
                Dead = Dead,
                PetOwner = PetOwner,
                LastUpdatedUtc = LastUpdatedUtc,
            };

            foreach (KeyValuePair<int, int> pair in Stats)
                clone.Stats[pair.Key] = pair.Value;

            return clone;
        }
    }
}
