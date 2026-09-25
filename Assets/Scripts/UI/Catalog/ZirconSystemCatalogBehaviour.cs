using System;
using System.Collections.Generic;
using UnityEngine;
using Zircon.Mobile.UI.Login;

namespace Zircon.Mobile.UI.Catalog
{
    public sealed class ZirconSystemCatalogBehaviour : MonoBehaviour
    {
        [SerializeField] private TextAsset itemManifest;
        [SerializeField] private TextAsset magicManifest;
        [SerializeField] private TextAsset npcPageManifest;
        [SerializeField] private TextAsset questManifest;
        [SerializeField] private TextAsset mapManifest;
        [SerializeField] private TextAsset monsterManifest;

        private readonly Dictionary<int, ItemEntry> items = new Dictionary<int, ItemEntry>();
        private readonly Dictionary<int, MagicEntry> magics = new Dictionary<int, MagicEntry>();
        private readonly Dictionary<int, NpcEntry> npcs = new Dictionary<int, NpcEntry>();
        private readonly Dictionary<int, NpcPageEntry> npcPages = new Dictionary<int, NpcPageEntry>();
        private readonly Dictionary<int, List<NpcGoodEntry>> npcGoods = new Dictionary<int, List<NpcGoodEntry>>();
        private readonly Dictionary<int, QuestEntry> quests = new Dictionary<int, QuestEntry>();
        private readonly Dictionary<int, QuestTaskEntry> questTasks = new Dictionary<int, QuestTaskEntry>();
        private readonly Dictionary<int, MapEntry> maps = new Dictionary<int, MapEntry>();
        private readonly Dictionary<int, MonsterEntry> monsters = new Dictionary<int, MonsterEntry>();

        public ItemEntry GetItem(int index)
        {
            EnsureLoaded();
            items.TryGetValue(index, out ItemEntry value);
            return value;
        }

        public MagicEntry GetMagic(int index)
        {
            EnsureLoaded();
            magics.TryGetValue(index, out MagicEntry value);
            return value;
        }
        public QuestEntry GetQuest(int index) { EnsureLoaded(); quests.TryGetValue(index, out QuestEntry value); return value; }
        public QuestTaskEntry GetQuestTask(int index) { EnsureLoaded(); questTasks.TryGetValue(index, out QuestTaskEntry value); return value; }
        public MapEntry GetMap(int index) { EnsureLoaded(); maps.TryGetValue(index, out MapEntry value); return value; }
        public MonsterEntry GetMonster(int index) { EnsureLoaded(); monsters.TryGetValue(index, out MonsterEntry value); return value; }
        public NpcEntry GetNpc(int index) { EnsureLoaded(); npcs.TryGetValue(index, out NpcEntry value); return value; }
        public NpcPageEntry GetNpcPage(int index)
        {
            EnsureLoaded();
            npcPages.TryGetValue(index, out NpcPageEntry value);
            return value;
        }

        public IReadOnlyList<NpcGoodEntry> GetNpcGoods(int pageIndex)
        {
            EnsureLoaded();
            return npcGoods.TryGetValue(pageIndex, out List<NpcGoodEntry> value) ? value : Array.Empty<NpcGoodEntry>();
        }

        private void Awake()
        {
            EnsureLoaded();
            ZirconProtocolProbeBehaviour session = GetComponent<ZirconProtocolProbeBehaviour>();
            session?.ConfigureItemStackSizeResolver(index => GetItem(index)?.StackSize ?? 1);
            Debug.Log("Item stack catalog registered: items=" + items.Count + " session=" + (session != null));
        }

        private void EnsureLoaded()
        {
            if (items.Count == 0 && itemManifest != null)
            {
                ItemManifest manifest = JsonUtility.FromJson<ItemManifest>(itemManifest.text);
                if (manifest?.Items != null)
                {
                    foreach (ItemEntry item in manifest.Items)
                        items[item.Index] = item;
                }
            }

            if (magics.Count == 0 && magicManifest != null)
            {
                MagicManifest manifest = JsonUtility.FromJson<MagicManifest>(magicManifest.text);
                if (manifest?.Magics != null)
                {
                    foreach (MagicEntry magic in manifest.Magics)
                        magics[magic.Index] = magic;
                }
            }
            if (quests.Count == 0 && questManifest != null)
            {
                QuestManifest manifest = JsonUtility.FromJson<QuestManifest>(questManifest.text);
                if (manifest?.Quests != null) foreach (QuestEntry quest in manifest.Quests) quests[quest.Index] = quest;
                if (manifest?.Tasks != null) foreach (QuestTaskEntry task in manifest.Tasks) questTasks[task.Index] = task;
            }
            if (maps.Count == 0 && mapManifest != null)
            {
                MapManifest manifest = JsonUtility.FromJson<MapManifest>(mapManifest.text);
                if (manifest?.Maps != null) foreach (MapEntry map in manifest.Maps) maps[map.Index] = map;
            }
            if (monsters.Count == 0 && monsterManifest != null)
            {
                MonsterManifest manifest = JsonUtility.FromJson<MonsterManifest>(monsterManifest.text);
                if (manifest?.Monsters != null)
                    foreach (MonsterEntry monster in manifest.Monsters)
                        monsters[monster.Index] = monster;
            }
            if (npcPages.Count != 0 || npcPageManifest == null)
                return;

            NpcPageManifest npcManifest = JsonUtility.FromJson<NpcPageManifest>(npcPageManifest.text);
            if (npcManifest?.Npcs != null)
            {
                foreach (NpcEntry npc in npcManifest.Npcs)
                    npcs[npc.Index] = npc;
            }
            if (npcManifest?.Pages != null)
            {
                foreach (NpcPageEntry page in npcManifest.Pages)
                    npcPages[page.Index] = page;
            }

            if (npcManifest?.Goods == null)
                return;

            foreach (NpcGoodEntry good in npcManifest.Goods)
            {
                if (!npcGoods.TryGetValue(good.PageIndex, out List<NpcGoodEntry> pageGoods))
                {
                    pageGoods = new List<NpcGoodEntry>();
                    npcGoods.Add(good.PageIndex, pageGoods);
                }
                pageGoods.Add(good);
            }
        }

        [Serializable]
        private sealed class ItemManifest { public ItemEntry[] Items; }

        [Serializable]
        private sealed class MagicManifest { public MagicEntry[] Magics; }

        [Serializable]
        
        private sealed class QuestManifest { public QuestEntry[] Quests; public QuestTaskEntry[] Tasks; }
        [Serializable]
        private sealed class MapManifest { public MapEntry[] Maps; }
        [Serializable]
        private sealed class MonsterManifest { public MonsterEntry[] Monsters; }
        [Serializable]
        private sealed class NpcPageManifest { public NpcEntry[] Npcs; public NpcPageEntry[] Pages; public NpcGoodEntry[] Goods; }

        [Serializable]
        public sealed class ItemEntry
        {
            public int Index;
            public string Name;
            public int ItemType;
            public int Image;
            public int Durability;
            public int Price;
            public int Weight;
            public int StackSize;
            public bool CanRepair;
            public bool CanSell;
            public bool CanStore;
            public bool CanTrade;
            public bool CanDrop;
            public string Description;
            public int Rarity;
        }

        [Serializable]
        public sealed class MagicEntry
        {
            public int Index;
            public string Name;
            public int MagicType;
            public int CharacterClass;
            public int School;
            public int Mode;
            public int Icon;
            public int BaseCost;
            public int LevelCost;
            public int Delay;
            public string Description;
        }

        [Serializable]
        public sealed class NpcPageEntry
        {
            public int Index;
            public string Description;
            public int DialogType;
            public string Say;
            public int SuccessPage;
            public string Arguments;
        }

        [Serializable]
        public sealed class NpcEntry
        {
            public int Index;
            public string Name;
            public int Region;
            public int Image;
            public int EntryPage;
        }

        [Serializable]
        
        public sealed class QuestEntry
        {
            public int Index; public string Name; public string AcceptText; public string ProgressText; public string CompletedText; public string ArchiveText; public int StartNpcIndex; public int FinishNpcIndex;
        }
        [Serializable]
        public sealed class QuestTaskEntry
        {
            public int Index; public int QuestIndex; public int TaskType; public int ItemInfoIndex; public string MobDescription; public int Amount;
        }
        [Serializable]
        public sealed class MapEntry
        {
            public int Index; public string FileName; public string Description; public int MiniMap;
        }
        [Serializable]
        public sealed class MonsterEntry
        {
            public int Index;
            public string Name;
            public int Image;
            public int BodyShape;
            public string LibraryFile;
        }
        public sealed class NpcGoodEntry
        {
            public int Index;
            public int PageIndex;
            public int ItemInfoIndex;
            public float Rate;
        }
    }
}
