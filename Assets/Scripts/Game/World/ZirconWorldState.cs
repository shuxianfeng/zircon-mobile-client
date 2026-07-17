using System;
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
    public sealed class ZirconWorldState
    {
        private const int MaxChatMessages = 80;
        private readonly object syncRoot = new object();
        private readonly Dictionary<uint, ZirconEntityState> entities = new Dictionary<uint, ZirconEntityState>();
        private readonly List<ZirconChatLogEntry> chatMessages = new List<ZirconChatLogEntry>();
        private readonly Dictionary<int, int> stats = new Dictionary<int, int>();
        private readonly Dictionary<int, ZirconSkillState> skills = new Dictionary<int, ZirconSkillState>();
        private readonly Dictionary<int, ZirconItemState> items = new Dictionary<int, ZirconItemState>();
        private readonly Dictionary<int, ZirconBuffState> buffs = new Dictionary<int, ZirconBuffState>();
        private readonly Dictionary<int, ZirconQuestState> quests = new Dictionary<int, ZirconQuestState>();
        private readonly ZirconSocialState social = new ZirconSocialState();
        private readonly ZirconCommerceState commerce = new ZirconCommerceState();
        private ZirconEntityState localPlayer;
        private int mapIndex;
        private ZirconMapPoint location;
        private int bagWeight;
        private int wearWeight;
        private int handWeight;
        private long gold;
        private long gameGold;
        private long huntGold;
        private long autoTime;
        private int skillLevelLimit;
        private int storageSize = 100;
        private bool npcDialogOpen;
        private uint npcObjectId;
        private int npcPageIndex;

        public void Reset()
        {
            lock (syncRoot)
            {
                entities.Clear();
                chatMessages.Clear();
                stats.Clear();
                skills.Clear();
                items.Clear();
                buffs.Clear();
                quests.Clear();
                social.GroupMembers.Clear();
                social.PartnerItems.Clear();
                social.PendingGroupInvite = string.Empty;
                social.PendingGuildInvite = string.Empty;
                social.TradeOpen = false;
                commerce.Mail.Clear();
                commerce.MarketResults.Clear();
                localPlayer = null;
                mapIndex = 0;
                location = default;
                bagWeight = 0;
                wearWeight = 0;
                handWeight = 0;
                gold = 0;
                gameGold = 0;
                huntGold = 0;
                autoTime = 0;
                skillLevelLimit = 0;
                storageSize = 100;
                npcDialogOpen = false;
                npcObjectId = 0;
                npcPageIndex = 0;
            }
        }

        public bool ApplyPacket(ZirconPacketFrame frame, out string summary)
        {
            if (ZirconServerPacketDecoder.TryDecodeLogin(frame, out ZirconDecodedLogin login))
                return ApplyLogin(login, out summary);

            if (ZirconServerPacketDecoder.TryDecodeStartGame(frame, out ZirconDecodedStartGame startGame))
                return ApplyStartGame(startGame, out summary);

            if (ZirconInGamePacketDecoder.TryDecodeDataObjectPlayer(frame, out ZirconDataObjectPlayerInfo player))
                return ApplyDataObjectPlayer(player, out summary);

            if (ZirconInGamePacketDecoder.TryDecodeDataObjectMonster(frame, out ZirconDataObjectMonsterInfo dataMonster))
                return ApplyDataObjectMonster(dataMonster, out summary);

            if (ZirconInGamePacketDecoder.TryDecodeDataObjectItem(frame, out ZirconDataObjectItemInfo dataItem))
                return ApplyDataObjectItem(dataItem, out summary);

            if (ZirconInGamePacketDecoder.TryDecodeObjectItem(frame, out ZirconObjectItemInfo item))
                return ApplyObjectItem(item, out summary);

            if (ZirconInGamePacketDecoder.TryDecodeObjectMonster(frame, out ZirconObjectMonsterInfo monster))
                return ApplyObjectMonster(monster, out summary);

            if (ZirconInGamePacketDecoder.TryDecodeObjectNpc(frame, out ZirconObjectNpcInfo npc))
                return ApplyObjectNpc(npc, out summary);

            if (ZirconInGamePacketDecoder.TryDecodeObjectSpell(frame, out ZirconObjectSpellInfo spell))
                return ApplyObjectSpell(spell, out summary);

            if (ZirconInGamePacketDecoder.TryDecodeObjectAttack(frame, out ZirconObjectAttackInfo attack))
                return ApplyObjectAttack(attack, out summary);

            if (ZirconInGamePacketDecoder.TryDecodeObjectMagic(frame, out ZirconObjectMagicInfo objectMagic))
                return ApplyObjectMagic(objectMagic, out summary);

            if (ZirconInGamePacketDecoder.TryDecodeNewMagic(frame, out ZirconUserMagicInfo newMagic))
                return ApplyNewMagic(newMagic, out summary);

            if (ZirconInGamePacketDecoder.TryDecodeMagicLeveled(frame, out ZirconMagicProgressInfo magicProgress))
                return ApplyMagicLeveled(magicProgress, out summary);

            if (ZirconInGamePacketDecoder.TryDecodeMagicCooldown(frame, out ZirconMagicCooldownInfo magicCooldown))
                return ApplyMagicCooldown(magicCooldown, out summary);

            if (ZirconInGamePacketDecoder.TryDecodeItemsGained(frame, out ZirconItemsGainedInfo gained))
                return ApplyItemsGained(gained, out summary);

            if (ZirconInventoryPacketDecoder.TryDecodeItemMove(frame, out ZirconItemMoveInfo itemMove))
                return ApplyItemMove(itemMove, out summary);

            if (ZirconInventoryPacketDecoder.TryDecodeItemChanged(frame, out ZirconItemChangedInfo itemChanged))
                return ApplyItemChanged(itemChanged, out summary);

            if (ZirconInventoryPacketDecoder.TryDecodeItemLock(frame, out ZirconItemLockInfo itemLock))
                return ApplyItemLock(itemLock, out summary);

            if (ZirconInventoryPacketDecoder.TryDecodeItemsChanged(frame, out ZirconItemsChangedInfo itemsChanged))
                return ApplyItemsChanged(itemsChanged, out summary);

            if (ZirconInventoryPacketDecoder.TryDecodeNpcRepair(frame, out ZirconNpcRepairInfo repair))
                return ApplyNpcRepair(repair, out summary);
            if (ZirconInventoryPacketDecoder.TryDecodeItemDurability(frame, out ZirconItemDurabilityInfo durability))
                return ApplyItemDurability(durability, out summary);

            if (ZirconInventoryPacketDecoder.TryDecodeSortStorageItems(frame, out ZirconStorageItemsInfo storageItems))
                return ApplyStorageItems(storageItems, out summary);

            if (ZirconInventoryPacketDecoder.TryDecodeStorageSize(frame, out int newStorageSize))
                return ApplyStorageSize(newStorageSize, out summary);

            if (ZirconBuffPacketDecoder.TryDecodeBuffAdd(frame, out ZirconBuffInfo buffAdd))
                return ApplyBuffAdd(buffAdd, out summary);

            if (ZirconBuffPacketDecoder.TryDecodeBuffRemove(frame, out int buffRemove))
                return ApplyBuffRemove(buffRemove, out summary);

            if (ZirconBuffPacketDecoder.TryDecodeBuffChanged(frame, out ZirconBuffStatsInfo buffChanged))
                return ApplyBuffChanged(buffChanged, out summary);

            if (ZirconBuffPacketDecoder.TryDecodeBuffTime(frame, out ZirconBuffTimeInfo buffTime))
                return ApplyBuffTime(buffTime, out summary);

            if (ZirconBuffPacketDecoder.TryDecodeBuffPaused(frame, out ZirconBuffPausedInfo buffPaused))
                return ApplyBuffPaused(buffPaused, out summary);
            if (ZirconQuestPacketDecoder.TryDecodeQuestChanged(frame, out ZirconUserQuestInfo questChanged))
                return ApplyQuestChanged(questChanged, out summary);
            if (ZirconInGamePacketDecoder.TryDecodeObjectMove(frame, out ZirconObjectMoveInfo move))
                return ApplyObjectMove(move, out summary);

            if (ZirconInGamePacketDecoder.TryDecodeObjectTurn(frame, out ZirconObjectTurnInfo turn))
                return ApplyObjectTurn(turn, out summary);

            if (ZirconInGamePacketDecoder.TryDecodeDataObjectLocation(frame, out ZirconDataObjectLocationInfo objectLocation))
                return ApplyDataObjectLocation(objectLocation, out summary);

            if (ZirconInGamePacketDecoder.TryDecodeDataObjectMaxHealthMana(frame, out ZirconDataObjectMaxHealthManaInfo max))
                return ApplyDataObjectMaxHealthMana(max, out summary);

            if (ZirconInGamePacketDecoder.TryDecodeNpcResponse(frame, out ZirconNpcResponseInfo npcResponse))
                return ApplyNpcResponse(npcResponse, out summary);

            if (ZirconInGamePacketDecoder.TryDecodeNpcClose(frame))
                return ApplyNpcClose(out summary);

            if (ZirconInGamePacketDecoder.TryDecodeStatsUpdate(frame, out ZirconStatsUpdateInfo statsUpdate))
                return ApplyStatsUpdate(statsUpdate, out summary);

            if (ZirconInGamePacketDecoder.TryDecodeChat(frame, out ZirconChatMessageInfo chat))
                return ApplyChat(chat, out summary);
            if (ZirconStatusPacketDecoder.TryDecodeObjectRemove(frame, out ZirconObjectRemoveInfo remove))
                return ApplyObjectRemove(remove, out summary);

            if (ZirconStatusPacketDecoder.TryDecodeWeightUpdate(frame, out ZirconWeightUpdateInfo weight))
                return ApplyWeightUpdate(weight, out summary);

            if (ZirconStatusPacketDecoder.TryDecodeGoldChanged(frame, out ZirconCurrencyUpdateInfo goldChanged))
                return ApplyGoldChanged(goldChanged, out summary);

            if (ZirconStatusPacketDecoder.TryDecodeGameGoldChanged(frame, out ZirconCurrencyUpdateInfo gameGoldChanged))
                return ApplyGameGoldChanged(gameGoldChanged, out summary);

            if (ZirconStatusPacketDecoder.TryDecodeHuntGoldChanged(frame, out ZirconCurrencyUpdateInfo huntGoldChanged))
                return ApplyHuntGoldChanged(huntGoldChanged, out summary);

            if (ZirconStatusPacketDecoder.TryDecodeAutoTimeChanged(frame, out ZirconAutoTimeChangedInfo autoTimeChanged))
                return ApplyAutoTimeChanged(autoTimeChanged, out summary);

            if (ZirconStatusPacketDecoder.TryDecodeSkillConfig(frame, out ZirconSkillConfigInfo skillConfig))
                return ApplySkillConfig(skillConfig, out summary);

            if (ZirconSocialPacketDecoder.TryDecode(frame, out ZirconSocialEvent socialEvent))
                return ApplySocialEvent(socialEvent, out summary);

            if (ZirconCommercePacketDecoder.TryDecode(frame, out ZirconCommerceEvent commerceEvent))
                return ApplyCommerceEvent(commerceEvent, out summary);
            summary = string.Empty;
            return false;
        }

        public bool SetSkillKeys(int infoIndex, byte set1Key, byte set2Key, byte set3Key, byte set4Key)
        {
            lock (syncRoot)
            {
                if (!skills.TryGetValue(infoIndex, out ZirconSkillState skill))
                    return false;

                skill.Set1Key = set1Key;
                skill.Set2Key = set2Key;
                skill.Set3Key = set3Key;
                skill.Set4Key = set4Key;
                return true;
            }
        }
        public ZirconWorldSnapshot GetSnapshot()
        {
            lock (syncRoot)
            {
                var entityCopies = new List<ZirconEntityState>(entities.Count);
                foreach (ZirconEntityState entity in entities.Values)
                    entityCopies.Add(entity.Clone());

                var chatCopies = new List<ZirconChatLogEntry>(chatMessages);
                var statCopies = new Dictionary<int, int>(stats);
                var skillCopies = new List<ZirconSkillState>(skills.Count);
                foreach (ZirconSkillState skill in skills.Values)
                    skillCopies.Add(skill.Clone());

                var inventoryCopies = new List<ZirconItemState>();
                var equipmentCopies = new List<ZirconItemState>();
                var storageCopies = new List<ZirconItemState>();
                foreach (ZirconItemState item in items.Values)
                {
                    if (item.Grid == ZirconGridType.Equipment)
                        equipmentCopies.Add(item.Clone());
                    else if (item.Grid == ZirconGridType.Inventory)
                        inventoryCopies.Add(item.Clone());
                    else if (item.Grid == ZirconGridType.Storage)
                        storageCopies.Add(item.Clone());
                }
                inventoryCopies.Sort((left, right) => left.Slot.CompareTo(right.Slot));
                equipmentCopies.Sort((left, right) => left.Slot.CompareTo(right.Slot));
                storageCopies.Sort((left, right) => left.Slot.CompareTo(right.Slot));

                DateTime snapshotUtc = DateTime.UtcNow;
                var buffCopies = new List<ZirconBuffState>(buffs.Count);
                foreach (ZirconBuffState buff in buffs.Values)
                    buffCopies.Add(buff.Clone(snapshotUtc));

                var questCopies = new List<ZirconQuestState>(quests.Count);
                foreach (ZirconQuestState quest in quests.Values)
                    questCopies.Add(quest.Clone());
                return new ZirconWorldSnapshot(
                    localPlayer?.Clone(),
                    entityCopies,
                    chatCopies,
                    statCopies,
                    skillCopies,
                    inventoryCopies,
                    equipmentCopies,
                    storageCopies,
                    storageSize,
                    buffCopies,
                    questCopies,
                    social.Clone(),
                    commerce.Clone(),
                    mapIndex,
                    location,
                    localPlayer != null,
                    bagWeight,
                    wearWeight,
                    handWeight,
                    gold,
                    gameGold,
                    huntGold,
                    autoTime,
                    skillLevelLimit,
                    npcDialogOpen,
                    npcObjectId,
                    npcPageIndex);
            }
        }

        private bool ApplyLogin(ZirconDecodedLogin login, out string summary)
        {
            lock (syncRoot)
            {
                RemoveGridItems(ZirconGridType.Storage);
                foreach (ZirconUserItemInfo packetItem in login.StorageItems)
                {
                    ZirconItemState item = ZirconItemState.FromPacket(packetItem);
                    item.Grid = ZirconGridType.Storage;
                    items[ItemKey(item.Grid, item.Slot)] = item;
                }
            }

            summary = $"world login storage={login.StorageItems.Count}";
            return true;
        }
        private bool ApplyStartGame(ZirconDecodedStartGame startGame, out string summary)
        {
            if (startGame.Result != ZirconStartGameResult.Success || !startGame.StartInformation.HasValue)
            {
                summary = $"world start rejected result={startGame.Result}";
                return false;
            }

            ZirconStartInformation info = startGame.StartInformation.Value;

            lock (syncRoot)
            {
                localPlayer = GetOrCreate(info.ObjectId, ZirconEntityKind.Player);
                localPlayer.Name = info.Name;
                localPlayer.ModelIndex = info.Index;
                localPlayer.MapIndex = info.MapIndex;
                localPlayer.Location = info.Location;
                localPlayer.Direction = info.Direction;
                localPlayer.Level = info.Level;
                localPlayer.Health = info.CurrentHp;
                localPlayer.Mana = info.CurrentMp;
                localPlayer.LastUpdatedUtc = DateTime.UtcNow;
                mapIndex = info.MapIndex;
                location = info.Location;
                skills.Clear();
                foreach (ZirconUserMagicInfo magic in info.Magics)
                    skills[magic.InfoIndex] = ZirconSkillState.FromPacket(magic);
                RemoveGridItems(ZirconGridType.Inventory, ZirconGridType.Equipment);
                foreach (ZirconUserItemInfo item in info.Items)
                {
                    ZirconItemState state = ZirconItemState.FromPacket(item);
                    items[ItemKey(state.Grid, state.Slot)] = state;
                }

                buffs.Clear();
                quests.Clear();
                social.GroupMembers.Clear();
                social.PartnerItems.Clear();
                social.PendingGroupInvite = string.Empty;
                social.PendingGuildInvite = string.Empty;
                social.TradeOpen = false;
                commerce.Mail.Clear();
                commerce.MarketResults.Clear();
                foreach (ZirconUserQuestInfo quest in info.Quests)
                    quests[quest.Index] = ZirconQuestState.FromPacket(quest);
                DateTime now = DateTime.UtcNow;
                foreach (ZirconBuffInfo buff in info.Buffs)
                    buffs[buff.Index] = ZirconBuffState.FromPacket(buff, now);
            }

            summary = $"world local player id={info.ObjectId} map={info.MapIndex} xy={info.Location.X},{info.Location.Y} items={info.Items.Count} skills={info.Magics.Count} buffs={info.Buffs.Count}";
            return true;
        }

        private bool ApplyDataObjectPlayer(ZirconDataObjectPlayerInfo player, out string summary)
        {
            lock (syncRoot)
            {
                ZirconEntityState entity = GetOrCreate(player.ObjectId, ZirconEntityKind.Player);
                entity.Name = player.Name;
                entity.MapIndex = player.MapIndex;
                entity.Location = player.Location;
                entity.Health = player.Health;
                entity.Mana = player.Mana;
                entity.MaxHealth = player.MaxHealth;
                entity.MaxMana = player.MaxMana;
                entity.Dead = player.Dead;
                entity.LastUpdatedUtc = DateTime.UtcNow;
                UpdateLocalIfMatching(entity);
            }

            summary = $"world player id={player.ObjectId} name={player.Name} xy={player.Location.X},{player.Location.Y}";
            return true;
        }

        private bool ApplyDataObjectMonster(ZirconDataObjectMonsterInfo monster, out string summary)
        {
            lock (syncRoot)
            {
                ZirconEntityState entity = GetOrCreate(monster.ObjectId, ZirconEntityKind.Monster);
                entity.MapIndex = monster.MapIndex;
                entity.Location = monster.Location;
                entity.ModelIndex = monster.MonsterIndex;
                entity.PetOwner = monster.PetOwner;
                entity.Health = monster.Health;
                entity.Dead = monster.Dead;
                ReplaceStats(entity.Stats, monster.Stats);
                entity.LastUpdatedUtc = DateTime.UtcNow;
            }

            summary = $"world monster data id={monster.ObjectId} monster={monster.MonsterIndex} xy={monster.Location.X},{monster.Location.Y}";
            return true;
        }

        private bool ApplyDataObjectItem(ZirconDataObjectItemInfo item, out string summary)
        {
            lock (syncRoot)
            {
                ZirconEntityState entity = GetOrCreate(item.ObjectId, ZirconEntityKind.Item);
                entity.MapIndex = item.MapIndex;
                entity.Location = item.Location;
                entity.ModelIndex = item.ItemInfoIndex;
                entity.LastUpdatedUtc = DateTime.UtcNow;
            }

            summary = $"world item data id={item.ObjectId} itemInfo={item.ItemInfoIndex} xy={item.Location.X},{item.Location.Y}";
            return true;
        }

        private bool ApplyObjectItem(ZirconObjectItemInfo item, out string summary)
        {
            lock (syncRoot)
            {
                ZirconEntityState entity = GetOrCreate(item.ObjectId, ZirconEntityKind.Item);
                entity.Location = item.Location;
                entity.ModelIndex = item.Item?.InfoIndex ?? 0;
                entity.Health = item.Item?.Index ?? 0;
                entity.LastUpdatedUtc = DateTime.UtcNow;
            }

            summary = $"world item id={item.ObjectId} itemInfo={item.Item?.InfoIndex ?? 0} xy={item.Location.X},{item.Location.Y}";
            return true;
        }

        private bool ApplyObjectMonster(ZirconObjectMonsterInfo monster, out string summary)
        {
            lock (syncRoot)
            {
                ZirconEntityState entity = GetOrCreate(monster.ObjectId, ZirconEntityKind.Monster);
                entity.ModelIndex = monster.MonsterIndex;
                entity.Location = monster.Location;
                entity.Direction = monster.Direction;
                entity.PetOwner = monster.PetOwner;
                entity.Dead = monster.Dead;
                entity.LastUpdatedUtc = DateTime.UtcNow;
            }

            summary = $"world monster id={monster.ObjectId} monster={monster.MonsterIndex} xy={monster.Location.X},{monster.Location.Y}";
            return true;
        }

        private bool ApplyObjectNpc(ZirconObjectNpcInfo npc, out string summary)
        {
            lock (syncRoot)
            {
                ZirconEntityState entity = GetOrCreate(npc.ObjectId, ZirconEntityKind.Npc);
                entity.ModelIndex = npc.NpcIndex;
                entity.Location = npc.Location;
                entity.Direction = npc.Direction;
                entity.LastUpdatedUtc = DateTime.UtcNow;
            }

            summary = $"world npc id={npc.ObjectId} npc={npc.NpcIndex} xy={npc.Location.X},{npc.Location.Y}";
            return true;
        }

        private bool ApplyObjectSpell(ZirconObjectSpellInfo spell, out string summary)
        {
            lock (syncRoot)
            {
                ZirconEntityState entity = GetOrCreate(spell.ObjectId, ZirconEntityKind.Spell);
                entity.ModelIndex = spell.Effect;
                entity.Location = spell.Location;
                entity.Direction = spell.Direction;
                entity.Health = spell.Power;
                entity.LastUpdatedUtc = DateTime.UtcNow;
            }

            summary = $"world spell id={spell.ObjectId} effect={spell.Effect} xy={spell.Location.X},{spell.Location.Y}";
            return true;
        }

        private bool ApplyObjectAttack(ZirconObjectAttackInfo attack, out string summary)
        {
            lock (syncRoot)
            {
                ZirconEntityState entity = GetOrCreate(attack.ObjectId, ZirconEntityKind.Unknown);
                entity.Location = attack.Location;
                entity.Direction = attack.Direction;
                entity.Action = ZirconMirAction.Attack;
                entity.ActionMagic = attack.AttackMagic;
                entity.ActionTargetId = attack.TargetId;
                entity.LastUpdatedUtc = DateTime.UtcNow;
                UpdateLocalIfMatching(entity);
            }

            summary = $"world attack id={attack.ObjectId} target={attack.TargetId} magic={attack.AttackMagic} xy={attack.Location.X},{attack.Location.Y}";
            return true;
        }
        private bool ApplyObjectMagic(ZirconObjectMagicInfo magic, out string summary)
        {
            lock (syncRoot)
            {
                ZirconEntityState entity = GetOrCreate(magic.ObjectId, ZirconEntityKind.Unknown);
                entity.Location = magic.Location;
                entity.Direction = magic.Direction;
                entity.Action = ZirconMirAction.Spell;
                entity.ActionMagic = magic.MagicType;
                entity.ActionTargetId = magic.Targets.Count > 0 ? magic.Targets[0] : 0;
                entity.LastUpdatedUtc = DateTime.UtcNow;
                UpdateLocalIfMatching(entity);
            }

            summary = $"world magic id={magic.ObjectId} type={magic.MagicType} cast={magic.Cast} targets={magic.Targets.Count}";
            return true;
        }

        private bool ApplyNewMagic(ZirconUserMagicInfo magic, out string summary)
        {
            lock (syncRoot)
                skills[magic.InfoIndex] = ZirconSkillState.FromPacket(magic);

            summary = $"world new magic infoIndex={magic.InfoIndex} level={magic.Level}";
            return true;
        }

        private bool ApplyMagicLeveled(ZirconMagicProgressInfo update, out string summary)
        {
            lock (syncRoot)
            {
                if (skills.TryGetValue(update.InfoIndex, out ZirconSkillState skill))
                {
                    skill.Level = update.Level;
                    skill.Experience = update.Experience;
                }
            }

            summary = $"world magic leveled infoIndex={update.InfoIndex} level={update.Level}";
            return true;
        }

        private bool ApplyMagicCooldown(ZirconMagicCooldownInfo update, out string summary)
        {
            lock (syncRoot)
            {
                if (skills.TryGetValue(update.InfoIndex, out ZirconSkillState skill))
                    skill.CooldownUntilUtc = DateTime.UtcNow.AddMilliseconds(update.Delay);
            }

            summary = $"world magic cooldown infoIndex={update.InfoIndex} delay={update.Delay}";
            return true;
        }

        private bool ApplyObjectMove(ZirconObjectMoveInfo move, out string summary)
        {
            lock (syncRoot)
            {
                ZirconEntityState entity = GetOrCreate(move.ObjectId, ZirconEntityKind.Unknown);
                entity.Location = move.Location;
                entity.Direction = move.Direction;
                entity.Action = ZirconMirAction.Moving;
                entity.LastUpdatedUtc = DateTime.UtcNow;
                UpdateLocalIfMatching(entity);
            }

            summary = $"world move id={move.ObjectId} xy={move.Location.X},{move.Location.Y} distance={move.Distance}";
            return true;
        }

        private bool ApplyObjectTurn(ZirconObjectTurnInfo turn, out string summary)
        {
            lock (syncRoot)
            {
                ZirconEntityState entity = GetOrCreate(turn.ObjectId, ZirconEntityKind.Unknown);
                entity.Location = turn.Location;
                entity.Direction = turn.Direction;
                entity.Action = ZirconMirAction.Standing;
                entity.LastUpdatedUtc = DateTime.UtcNow;
                UpdateLocalIfMatching(entity);
            }

            summary = $"world turn id={turn.ObjectId} xy={turn.Location.X},{turn.Location.Y}";
            return true;
        }

        private bool ApplyDataObjectLocation(ZirconDataObjectLocationInfo objectLocation, out string summary)
        {
            lock (syncRoot)
            {
                ZirconEntityState entity = GetOrCreate(objectLocation.ObjectId, ZirconEntityKind.Unknown);
                entity.MapIndex = objectLocation.MapIndex;
                entity.Location = objectLocation.Location;
                entity.LastUpdatedUtc = DateTime.UtcNow;
                UpdateLocalIfMatching(entity);
            }

            summary = $"world location id={objectLocation.ObjectId} map={objectLocation.MapIndex} xy={objectLocation.Location.X},{objectLocation.Location.Y}";
            return true;
        }

        private bool ApplyDataObjectMaxHealthMana(ZirconDataObjectMaxHealthManaInfo max, out string summary)
        {
            lock (syncRoot)
            {
                ZirconEntityState entity = GetOrCreate(max.ObjectId, ZirconEntityKind.Unknown);
                entity.MaxHealth = max.MaxHealth;
                entity.MaxMana = max.MaxMana;
                ReplaceStats(entity.Stats, max.Stats);
                entity.LastUpdatedUtc = DateTime.UtcNow;
                UpdateLocalIfMatching(entity);
            }

            summary = $"world max hp/mp id={max.ObjectId} hp={max.MaxHealth} mp={max.MaxMana}";
            return true;
        }

        private bool ApplyNpcResponse(ZirconNpcResponseInfo response, out string summary)
        {
            lock (syncRoot)
            {
                npcDialogOpen = true;
                npcObjectId = response.ObjectId;
                npcPageIndex = response.PageIndex;
            }

            summary = $"world npc response object={response.ObjectId} page={response.PageIndex}";
            return true;
        }

        private bool ApplyNpcClose(out string summary)
        {
            lock (syncRoot)
            {
                npcDialogOpen = false;
                npcObjectId = 0;
                npcPageIndex = 0;
            }

            summary = "world npc close";
            return true;
        }
        private bool ApplyStatsUpdate(ZirconStatsUpdateInfo statsUpdate, out string summary)
        {
            lock (syncRoot)
            {
                ReplaceStats(stats, statsUpdate.Stats);
            }

            summary = $"world stats count={statsUpdate.Stats.Count}";
            return true;
        }

        private bool ApplyChat(ZirconChatMessageInfo chat, out string summary)
        {
            lock (syncRoot)
            {
                chatMessages.Add(new ZirconChatLogEntry(chat.ObjectId, chat.MessageType, chat.Text, DateTime.UtcNow));
                while (chatMessages.Count > MaxChatMessages)
                    chatMessages.RemoveAt(0);
            }

            summary = $"world chat type={chat.MessageType} text={chat.Text}";
            return true;
        }

        private bool ApplyObjectRemove(ZirconObjectRemoveInfo remove, out string summary)
        {
            lock (syncRoot)
            {
                entities.Remove(remove.ObjectId);
                if (localPlayer != null && localPlayer.ObjectId == remove.ObjectId)
                    localPlayer = null;
            }

            summary = $"world remove id={remove.ObjectId}";
            return true;
        }

        private bool ApplyWeightUpdate(ZirconWeightUpdateInfo weight, out string summary)
        {
            lock (syncRoot)
            {
                bagWeight = weight.BagWeight;
                wearWeight = weight.WearWeight;
                handWeight = weight.HandWeight;
            }

            summary = $"world weight bag={weight.BagWeight} wear={weight.WearWeight} hand={weight.HandWeight}";
            return true;
        }

        private bool ApplyGoldChanged(ZirconCurrencyUpdateInfo update, out string summary)
        {
            lock (syncRoot)
            {
                gold = update.Value;
            }

            summary = $"world gold={update.Value}";
            return true;
        }

        private bool ApplyGameGoldChanged(ZirconCurrencyUpdateInfo update, out string summary)
        {
            lock (syncRoot)
            {
                gameGold = update.Value;
            }

            summary = $"world gameGold={update.Value}";
            return true;
        }

        private bool ApplyHuntGoldChanged(ZirconCurrencyUpdateInfo update, out string summary)
        {
            lock (syncRoot)
            {
                huntGold = update.Value;
            }

            summary = $"world huntGold={update.Value}";
            return true;
        }

        private bool ApplyAutoTimeChanged(ZirconAutoTimeChangedInfo update, out string summary)
        {
            lock (syncRoot)
            {
                autoTime = update.AutoTime;
            }

            summary = $"world autoTime={update.AutoTime}";
            return true;
        }

        private bool ApplySkillConfig(ZirconSkillConfigInfo config, out string summary)
        {
            lock (syncRoot)
            {
                skillLevelLimit = config.SkillLevelLimit;
            }

            summary = $"world skillLevelLimit={config.SkillLevelLimit}";
            return true;
        }
        private bool ApplyItemsGained(ZirconItemsGainedInfo gained, out string summary)
        {
            lock (syncRoot)
            {
                foreach (ZirconUserItemInfo packetItem in gained.Items)
                {
                    ZirconItemState item = ZirconItemState.FromPacket(packetItem);
                    items[ItemKey(item.Grid, item.Slot)] = item;
                }
            }

            summary = $"world items gained count={gained.Items.Count}";
            return true;
        }

        private bool ApplyItemMove(ZirconItemMoveInfo move, out string summary)
        {
            if (!move.Success)
            {
                summary = $"world item move rejected from={move.FromGrid}:{move.FromSlot} to={move.ToGrid}:{move.ToSlot}";
                return true;
            }

            lock (syncRoot)
            {
                int fromKey = ItemKey(move.FromGrid, move.FromSlot);
                int toKey = ItemKey(move.ToGrid, move.ToSlot);
                items.TryGetValue(fromKey, out ZirconItemState from);
                items.TryGetValue(toKey, out ZirconItemState to);

                if (move.MergeItem && from != null && to != null && from.InfoIndex == to.InfoIndex)
                {
                    to.Count += from.Count;
                    items.Remove(fromKey);
                }
                else
                {
                    if (from != null)
                    {
                        from.Grid = move.ToGrid;
                        from.Slot = move.ToSlot;
                        items[toKey] = from;
                    }
                    else
                    {
                        items.Remove(toKey);
                    }

                    if (to != null)
                    {
                        to.Grid = move.FromGrid;
                        to.Slot = move.FromSlot;
                        items[fromKey] = to;
                    }
                    else
                    {
                        items.Remove(fromKey);
                    }
                }
            }

            summary = $"world item move from={move.FromGrid}:{move.FromSlot} to={move.ToGrid}:{move.ToSlot} merge={move.MergeItem}";
            return true;
        }

        private bool ApplyItemChanged(ZirconItemChangedInfo changed, out string summary)
        {
            lock (syncRoot)
            {
                int key = ItemKey(changed.Link.Grid, changed.Link.Slot);
                if (changed.Success && items.TryGetValue(key, out ZirconItemState item))
                {
                    if (changed.Link.Count <= 0)
                        items.Remove(key);
                    else
                        item.Count = changed.Link.Count;
                }
            }

            summary = $"world item changed grid={changed.Link.Grid} slot={changed.Link.Slot} count={changed.Link.Count} success={changed.Success}";
            return true;
        }

        private bool ApplyItemLock(ZirconItemLockInfo update, out string summary)
        {
            lock (syncRoot)
            {
                if (items.TryGetValue(ItemKey(update.Grid, update.Slot), out ZirconItemState item))
                    item.Locked = update.Locked;
            }

            summary = $"world item lock grid={update.Grid} slot={update.Slot} locked={update.Locked}";
            return true;
        }

        private bool ApplyItemDurability(ZirconItemDurabilityInfo update, out string summary)
        {
            lock (syncRoot)
            {
                if (items.TryGetValue(ItemKey(update.Grid, update.Slot), out ZirconItemState item))
                    item.CurrentDurability = update.CurrentDurability;
            }

            summary = $"world item durability grid={update.Grid} slot={update.Slot} value={update.CurrentDurability}";
            return true;
        }

        private bool ApplySocialEvent(ZirconSocialEvent update, out string summary)
        {
            lock (syncRoot)
            {
                switch (update.Kind)
                {
                    case ZirconSocialEventKind.GroupSwitch: social.AllowGroup = update.Allow; break;
                    case ZirconSocialEventKind.GroupMember: social.GroupMembers[update.ObjectId] = update.Name; break;
                    case ZirconSocialEventKind.GroupRemove: social.GroupMembers.Remove(update.ObjectId); break;
                    case ZirconSocialEventKind.GroupInvite: social.PendingGroupInvite = update.Name; break;
                    case ZirconSocialEventKind.GuildInvite: social.PendingGuildInvite = update.Name; social.PendingGuildName = update.GuildName; break;
                    case ZirconSocialEventKind.TradeRequest: social.TradePartner = update.Name; break;
                    case ZirconSocialEventKind.TradeOpen: social.TradeOpen = true; social.TradeLocked = false; social.TradePartner = update.Name; social.PartnerItems.Clear(); social.OfferedGold = 0; social.PartnerGold = 0; break;
                    case ZirconSocialEventKind.TradeClose: social.TradeOpen = false; social.TradeLocked = false; break;
                    case ZirconSocialEventKind.TradeAddGold: social.OfferedGold = update.Gold; break;
                    case ZirconSocialEventKind.TradeGoldAdded: social.PartnerGold = update.Gold; break;
                    case ZirconSocialEventKind.TradeItemAdded: if (update.Item.HasValue) social.PartnerItems.Add(update.Item.Value); break;
                    case ZirconSocialEventKind.TradeUnlock: social.TradeLocked = false; break;
                }
            }
            summary = $"world social event={update.Kind}";
            return true;
        }

        private bool ApplyCommerceEvent(ZirconCommerceEvent update, out string summary)
        {
            lock (syncRoot)
            {
                switch (update.Kind)
                {
                    case ZirconCommerceEventKind.MailList: commerce.Mail.Clear(); commerce.Mail.AddRange(update.Mail); break;
                    case ZirconCommerceEventKind.MailNew: if (update.MailItem.HasValue) { RemoveMail(update.MailItem.Value.Index); commerce.Mail.Insert(0, update.MailItem.Value); } break;
                    case ZirconCommerceEventKind.MailDelete: RemoveMail(update.Index); break;
                    case ZirconCommerceEventKind.MarketConsignments:
                    case ZirconCommerceEventKind.MarketSearch: commerce.MarketResults.Clear(); commerce.MarketResults.AddRange(update.Listings); commerce.MarketResultCount = update.Count; break;
                    case ZirconCommerceEventKind.MarketSearchCount: commerce.MarketResultCount = update.Count; break;
                    case ZirconCommerceEventKind.MarketSearchIndex: if (update.Listing.HasValue) { RemoveListing(update.Listing.Value.Index); commerce.MarketResults.Add(update.Listing.Value); } break;
                    case ZirconCommerceEventKind.MarketConsignChanged: UpdateListingCount(update.Index, update.Amount); break;
                    case ZirconCommerceEventKind.MarketHistory: commerce.LastMarketPrice = update.LastPrice; commerce.AverageMarketPrice = update.AveragePrice; break;
                }
            }
            summary = $"world commerce event={update.Kind}";
            return true;
        }

        private void RemoveMail(int index) { for (int i = commerce.Mail.Count - 1; i >= 0; i--) if (commerce.Mail[i].Index == index) commerce.Mail.RemoveAt(i); }
        private void RemoveListing(int index) { for (int i = commerce.MarketResults.Count - 1; i >= 0; i--) if (commerce.MarketResults[i].Index == index) commerce.MarketResults.RemoveAt(i); }
        private void UpdateListingCount(int index, long count)
        {
            for (int i = 0; i < commerce.MarketResults.Count; i++)
            {
                ZirconMarketListingInfo listing = commerce.MarketResults[i];
                if (listing.Index != index || !listing.Item.HasValue) continue;
                ZirconUserItemInfo item = listing.Item.Value;
                var changed = new ZirconUserItemInfo(item.Index, item.InfoIndex, item.CurrentDurability, item.MaxDurability, count, item.Slot, item.Level, item.Flags);
                commerce.MarketResults[i] = new ZirconMarketListingInfo(listing.Index, changed, listing.Price, listing.Seller, listing.Message, listing.IsOwner);
                if (count <= 0) RemoveListing(index);
                return;
            }
        }
        private bool ApplyQuestChanged(ZirconUserQuestInfo update, out string summary)
        {
            lock (syncRoot)
                quests[update.Index] = ZirconQuestState.FromPacket(update);
            summary = $"world quest changed index={update.Index} quest={update.QuestIndex} tasks={update.Tasks.Count}";
            return true;
        }
        private bool ApplyItemsChanged(ZirconItemsChangedInfo update, out string summary)
        {
            lock (syncRoot)
            {
                if (update.Success)
                {
                    foreach (ZirconCellLinkInfo link in update.Links)
                    {
                        int key = ItemKey(link.Grid, link.Slot);
                        if (!items.TryGetValue(key, out ZirconItemState item)) continue;
                        if (link.Count >= item.Count) items.Remove(key);
                        else item.Count -= link.Count;
                    }
                }
            }
            summary = $"world items changed links={update.Links.Count} success={update.Success}";
            return true;
        }

        private bool ApplyNpcRepair(ZirconNpcRepairInfo update, out string summary)
        {
            lock (syncRoot)
            {
                if (update.Success)
                {
                    foreach (ZirconCellLinkInfo link in update.Links)
                    {
                        if (!items.TryGetValue(ItemKey(link.Grid, link.Slot), out ZirconItemState item)) continue;
                        if (!update.Special)
                            item.MaxDurability = Math.Max(0, item.MaxDurability - (item.MaxDurability - item.CurrentDurability) / 10);
                        item.CurrentDurability = item.MaxDurability;
                    }
                }
            }
            summary = $"world npc repair links={update.Links.Count} special={update.Special} success={update.Success}";
            return true;
        }
        private bool ApplyStorageSize(int value, out string summary)
        {
            lock (syncRoot)
                storageSize = Math.Max(0, value);
            summary = $"world storage size={storageSize}";
            return true;
        }
        private bool ApplyStorageItems(ZirconStorageItemsInfo update, out string summary)
        {
            lock (syncRoot)
            {
                RemoveGridItems(ZirconGridType.Storage);
                foreach (ZirconUserItemInfo packetItem in update.Items)
                {
                    ZirconItemState item = ZirconItemState.FromPacket(packetItem);
                    item.Grid = ZirconGridType.Storage;
                    items[ItemKey(item.Grid, item.Slot)] = item;
                }
            }

            summary = $"world storage sorted items={update.Items.Count}";
            return true;
        }
        private bool ApplyBuffAdd(ZirconBuffInfo packetBuff, out string summary)
        {
            lock (syncRoot)
                buffs[packetBuff.Index] = ZirconBuffState.FromPacket(packetBuff, DateTime.UtcNow);

            summary = $"world buff add index={packetBuff.Index} type={packetBuff.Type}";
            return true;
        }

        private bool ApplyBuffRemove(int index, out string summary)
        {
            lock (syncRoot)
                buffs.Remove(index);

            summary = $"world buff remove index={index}";
            return true;
        }

        private bool ApplyBuffChanged(ZirconBuffStatsInfo update, out string summary)
        {
            lock (syncRoot)
            {
                if (buffs.TryGetValue(update.Index, out ZirconBuffState buff))
                    buff.Stats = new Dictionary<int, int>(update.Stats);
            }

            summary = $"world buff changed index={update.Index} stats={update.Stats.Count}";
            return true;
        }

        private bool ApplyBuffTime(ZirconBuffTimeInfo update, out string summary)
        {
            lock (syncRoot)
            {
                if (buffs.TryGetValue(update.Index, out ZirconBuffState buff))
                {
                    buff.RemainingTime = update.RemainingTime;
                    buff.UpdatedUtc = DateTime.UtcNow;
                }
            }

            summary = $"world buff time index={update.Index} remainingMs={update.RemainingTime.TotalMilliseconds:0}";
            return true;
        }

        private bool ApplyBuffPaused(ZirconBuffPausedInfo update, out string summary)
        {
            lock (syncRoot)
            {
                if (buffs.TryGetValue(update.Index, out ZirconBuffState buff))
                {
                    DateTime now = DateTime.UtcNow;
                    buff.RemainingTime = buff.GetRemaining(now);
                    buff.UpdatedUtc = now;
                    buff.Paused = update.Paused;
                }
            }

            summary = $"world buff paused index={update.Index} paused={update.Paused}";
            return true;
        }

        private void RemoveGridItems(params ZirconGridType[] grids)
        {
            var remove = new List<int>();
            foreach (KeyValuePair<int, ZirconItemState> pair in items)
            {
                foreach (ZirconGridType grid in grids)
                {
                    if (pair.Value.Grid == grid)
                    {
                        remove.Add(pair.Key);
                        break;
                    }
                }
            }
            foreach (int key in remove)
                items.Remove(key);
        }
        private static int ItemKey(ZirconGridType grid, int slot)
        {
            return ((int)grid << 24) ^ slot;
        }
        private ZirconEntityState GetOrCreate(uint objectId, ZirconEntityKind preferredKind)
        {
            if (!entities.TryGetValue(objectId, out ZirconEntityState entity))
            {
                entity = new ZirconEntityState { ObjectId = objectId, Kind = preferredKind };
                entities.Add(objectId, entity);
                return entity;
            }

            if (entity.Kind == ZirconEntityKind.Unknown && preferredKind != ZirconEntityKind.Unknown)
                entity.Kind = preferredKind;

            return entity;
        }

        private void UpdateLocalIfMatching(ZirconEntityState entity)
        {
            if (localPlayer == null || localPlayer.ObjectId != entity.ObjectId)
                return;

            localPlayer = entity;
            mapIndex = entity.MapIndex;
            location = entity.Location;
        }

        private static void ReplaceStats(IDictionary<int, int> target, IReadOnlyDictionary<int, int> source)
        {
            target.Clear();
            if (source == null)
                return;

            foreach (KeyValuePair<int, int> pair in source)
                target[pair.Key] = pair.Value;
        }
    }
}


