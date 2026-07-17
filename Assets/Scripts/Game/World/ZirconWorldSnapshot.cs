using System.Collections.Generic;
using Zircon.Mobile.Core.Protocol;
using Zircon.Mobile.Game.Buffs;
using Zircon.Mobile.Game.Commerce;
using Zircon.Mobile.Game.Entities;
using Zircon.Mobile.Game.Items;
using Zircon.Mobile.Game.Quests;
using Zircon.Mobile.Game.Skills;
using Zircon.Mobile.Game.Social;

namespace Zircon.Mobile.Game.World
{
    public sealed class ZirconWorldSnapshot
    {
        public ZirconWorldSnapshot(
            ZirconEntityState localPlayer,
            IReadOnlyList<ZirconEntityState> entities,
            IReadOnlyList<ZirconChatLogEntry> chatMessages,
            IReadOnlyDictionary<int, int> stats,
            IReadOnlyList<ZirconSkillState> skills,
            IReadOnlyList<ZirconItemState> inventory,
            IReadOnlyList<ZirconItemState> equipment,
            IReadOnlyList<ZirconItemState> storage,
            int storageSize,
            IReadOnlyList<ZirconBuffState> buffs,
            IReadOnlyList<ZirconQuestState> quests,
            ZirconSocialState social,
            ZirconCommerceState commerce,
            int mapIndex,
            ZirconMapPoint location,
            bool hasLocalPlayer,
            int bagWeight,
            int wearWeight,
            int handWeight,
            long gold,
            long gameGold,
            long huntGold,
            long autoTime,
            int skillLevelLimit,
            bool npcDialogOpen,
            uint npcObjectId,
            int npcPageIndex)
        {
            LocalPlayer = localPlayer;
            Entities = entities;
            ChatMessages = chatMessages;
            Stats = stats;
            Skills = skills;
            Inventory = inventory;
            Equipment = equipment;
            Storage = storage;
            StorageSize = storageSize;
            Buffs = buffs;
            Quests = quests;
            Social = social;
            Commerce = commerce;
            MapIndex = mapIndex;
            Location = location;
            HasLocalPlayer = hasLocalPlayer;
            BagWeight = bagWeight;
            WearWeight = wearWeight;
            HandWeight = handWeight;
            Gold = gold;
            GameGold = gameGold;
            HuntGold = huntGold;
            AutoTime = autoTime;
            SkillLevelLimit = skillLevelLimit;
            NpcDialogOpen = npcDialogOpen;
            NpcObjectId = npcObjectId;
            NpcPageIndex = npcPageIndex;
        }

        public ZirconEntityState LocalPlayer { get; }
        public IReadOnlyList<ZirconEntityState> Entities { get; }
        public IReadOnlyList<ZirconChatLogEntry> ChatMessages { get; }
        public IReadOnlyDictionary<int, int> Stats { get; }
        public IReadOnlyList<ZirconSkillState> Skills { get; }
        public IReadOnlyList<ZirconItemState> Inventory { get; }
        public IReadOnlyList<ZirconItemState> Equipment { get; }
        public IReadOnlyList<ZirconItemState> Storage { get; }
        public int StorageSize { get; }
        public IReadOnlyList<ZirconBuffState> Buffs { get; }
        public IReadOnlyList<ZirconQuestState> Quests { get; }
        public ZirconSocialState Social { get; }
        public ZirconCommerceState Commerce { get; }
        public int MapIndex { get; }
        public ZirconMapPoint Location { get; }
        public bool HasLocalPlayer { get; }
        public int BagWeight { get; }
        public int WearWeight { get; }
        public int HandWeight { get; }
        public long Gold { get; }
        public long GameGold { get; }
        public long HuntGold { get; }
        public long AutoTime { get; }
        public int SkillLevelLimit { get; }
        public bool NpcDialogOpen { get; }
        public uint NpcObjectId { get; }
        public int NpcPageIndex { get; }
    }
}
