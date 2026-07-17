namespace Zircon.Mobile.Core.Protocol
{
    public static class ZirconPacketSummary
    {
        public static string Describe(ZirconPacketFrame frame)
        {
            if (ZirconServerPacketDecoder.TryDecodeLogin(frame, out ZirconDecodedLogin login))
                return $"Login result={login.Result} characters={login.Characters.Count}";

            if (ZirconServerPacketDecoder.TryDecodeStartGame(frame, out ZirconDecodedStartGame start))
                return $"StartGame result={start.Result} hasStartInfo={start.StartInformation.HasValue} items={start.StartInformation?.Items.Count ?? 0} skills={start.StartInformation?.Magics.Count ?? 0} buffs={start.StartInformation?.Buffs.Count ?? 0}";

            if (ZirconInGamePacketDecoder.TryDecodeChat(frame, out ZirconChatMessageInfo chat))
                return $"Chat type={chat.MessageType} text={chat.Text}";

            if (ZirconInGamePacketDecoder.TryDecodeObjectAttack(frame, out ZirconObjectAttackInfo attack))
                return $"ObjectAttack id={attack.ObjectId} dir={attack.Direction} target={attack.TargetId} magic={attack.AttackMagic} element={attack.AttackElement} xy={attack.Location.X},{attack.Location.Y}";

            if (ZirconInGamePacketDecoder.TryDecodeCombatTime(frame))
                return "CombatTime";

            if (ZirconInGamePacketDecoder.TryDecodeObjectMagic(frame, out ZirconObjectMagicInfo objectMagic))
                return $"ObjectMagic id={objectMagic.ObjectId} type={objectMagic.MagicType} cast={objectMagic.Cast} targets={objectMagic.Targets.Count} xy={objectMagic.Location.X},{objectMagic.Location.Y}";

            if (ZirconInGamePacketDecoder.TryDecodeObjectItem(frame, out ZirconObjectItemInfo objectItem))
                return $"ObjectItem id={objectItem.ObjectId} itemInfo={objectItem.Item?.InfoIndex ?? 0} xy={objectItem.Location.X},{objectItem.Location.Y}";

            if (ZirconInGamePacketDecoder.TryDecodeNewMagic(frame, out ZirconUserMagicInfo newMagic))
                return $"NewMagic infoIndex={newMagic.InfoIndex} level={newMagic.Level}";

            if (ZirconInGamePacketDecoder.TryDecodeMagicLeveled(frame, out ZirconMagicProgressInfo leveled))
                return $"MagicLeveled infoIndex={leveled.InfoIndex} level={leveled.Level} exp={leveled.Experience}";

            if (ZirconInGamePacketDecoder.TryDecodeMagicCooldown(frame, out ZirconMagicCooldownInfo cooldown))
                return $"MagicCooldown infoIndex={cooldown.InfoIndex} delay={cooldown.Delay}";

            if (ZirconInventoryPacketDecoder.TryDecodeItemMove(frame, out ZirconItemMoveInfo itemMove))
                return $"ItemMove {itemMove.FromGrid}:{itemMove.FromSlot} -> {itemMove.ToGrid}:{itemMove.ToSlot} success={itemMove.Success}";

            if (ZirconInventoryPacketDecoder.TryDecodeItemChanged(frame, out ZirconItemChangedInfo itemChanged))
                return $"ItemChanged grid={itemChanged.Link.Grid} slot={itemChanged.Link.Slot} count={itemChanged.Link.Count} success={itemChanged.Success}";

            if (ZirconInventoryPacketDecoder.TryDecodeItemLock(frame, out ZirconItemLockInfo itemLock))
                return $"ItemLock grid={itemLock.Grid} slot={itemLock.Slot} locked={itemLock.Locked}";

            if (ZirconInventoryPacketDecoder.TryDecodeItemDurability(frame, out ZirconItemDurabilityInfo durability))
                return $"ItemDurability grid={durability.Grid} slot={durability.Slot} value={durability.CurrentDurability}";

            if (ZirconBuffPacketDecoder.TryDecodeBuffAdd(frame, out ZirconBuffInfo buffAdd))
                return $"BuffAdd index={buffAdd.Index} type={buffAdd.Type} remainingMs={buffAdd.RemainingTime.TotalMilliseconds:0}";

            if (ZirconBuffPacketDecoder.TryDecodeBuffRemove(frame, out int buffRemove))
                return $"BuffRemove index={buffRemove}";

            if (ZirconBuffPacketDecoder.TryDecodeBuffChanged(frame, out ZirconBuffStatsInfo buffChanged))
                return $"BuffChanged index={buffChanged.Index} stats={buffChanged.Stats.Count}";

            if (ZirconBuffPacketDecoder.TryDecodeBuffTime(frame, out ZirconBuffTimeInfo buffTime))
                return $"BuffTime index={buffTime.Index} remainingMs={buffTime.RemainingTime.TotalMilliseconds:0}";

            if (ZirconBuffPacketDecoder.TryDecodeBuffPaused(frame, out ZirconBuffPausedInfo buffPaused))
                return $"BuffPaused index={buffPaused.Index} paused={buffPaused.Paused}";
            if (ZirconInGamePacketDecoder.TryDecodeObjectMove(frame, out ZirconObjectMoveInfo move))
                return $"ObjectMove id={move.ObjectId} dir={move.Direction} xy={move.Location.X},{move.Location.Y} distance={move.Distance}";

            if (ZirconInGamePacketDecoder.TryDecodeObjectTurn(frame, out ZirconObjectTurnInfo turn))
                return $"ObjectTurn id={turn.ObjectId} dir={turn.Direction} xy={turn.Location.X},{turn.Location.Y}";

            if (ZirconInGamePacketDecoder.TryDecodeObjectMonster(frame, out ZirconObjectMonsterInfo monster))
                return $"ObjectMonster id={monster.ObjectId} monster={monster.MonsterIndex} xy={monster.Location.X},{monster.Location.Y} dead={monster.Dead}";

            if (ZirconInGamePacketDecoder.TryDecodeObjectNpc(frame, out ZirconObjectNpcInfo npc))
                return $"ObjectNPC id={npc.ObjectId} npc={npc.NpcIndex} xy={npc.Location.X},{npc.Location.Y}";

            if (ZirconInGamePacketDecoder.TryDecodeObjectSpell(frame, out ZirconObjectSpellInfo spell))
                return $"ObjectSpell id={spell.ObjectId} effect={spell.Effect} xy={spell.Location.X},{spell.Location.Y}";

            if (ZirconInGamePacketDecoder.TryDecodeDataObjectPlayer(frame, out ZirconDataObjectPlayerInfo player))
                return $"DataObjectPlayer id={player.ObjectId} name={player.Name} map={player.MapIndex} xy={player.Location.X},{player.Location.Y}";

            if (ZirconInGamePacketDecoder.TryDecodeDataObjectMonster(frame, out ZirconDataObjectMonsterInfo dataMonster))
                return $"DataObjectMonster id={dataMonster.ObjectId} monster={dataMonster.MonsterIndex} map={dataMonster.MapIndex} xy={dataMonster.Location.X},{dataMonster.Location.Y}";

            if (ZirconInGamePacketDecoder.TryDecodeItemsGained(frame, out ZirconItemsGainedInfo gained))
                return $"ItemsGained items={gained.Items.Count}";

            if (ZirconInGamePacketDecoder.TryDecodeDataObjectItem(frame, out ZirconDataObjectItemInfo dataItem))
                return $"DataObjectItem id={dataItem.ObjectId} itemInfo={dataItem.ItemInfoIndex} map={dataItem.MapIndex} xy={dataItem.Location.X},{dataItem.Location.Y}";

            if (ZirconInGamePacketDecoder.TryDecodeDataObjectLocation(frame, out ZirconDataObjectLocationInfo location))
                return $"DataObjectLocation id={location.ObjectId} map={location.MapIndex} xy={location.Location.X},{location.Location.Y}";

            if (ZirconInGamePacketDecoder.TryDecodeDataObjectMaxHealthMana(frame, out ZirconDataObjectMaxHealthManaInfo max))
                return $"DataObjectMaxHealthMana id={max.ObjectId} hp={max.MaxHealth} mp={max.MaxMana}";

            if (ZirconInGamePacketDecoder.TryDecodeNpcResponse(frame, out ZirconNpcResponseInfo npcResponse))
                return $"NPCResponse object={npcResponse.ObjectId} page={npcResponse.PageIndex}";

            if (ZirconInGamePacketDecoder.TryDecodeNpcClose(frame))
                return "NPCClose";

            if (ZirconInGamePacketDecoder.TryDecodeStatsUpdate(frame, out ZirconStatsUpdateInfo stats))
                return $"StatsUpdate stats={stats.Stats.Count} hermitStats={stats.HermitStats.Count} hermitPoints={stats.HermitPoints}";

            if (ZirconStatusPacketDecoder.TryDecodeObjectRemove(frame, out ZirconObjectRemoveInfo remove))
                return $"ObjectRemove id={remove.ObjectId}";

            if (ZirconStatusPacketDecoder.TryDecodeWeightUpdate(frame, out ZirconWeightUpdateInfo weight))
                return $"WeightUpdate bag={weight.BagWeight} wear={weight.WearWeight} hand={weight.HandWeight}";

            if (ZirconStatusPacketDecoder.TryDecodeGoldChanged(frame, out ZirconCurrencyUpdateInfo gold))
                return $"GoldChanged gold={gold.Value}";

            if (ZirconStatusPacketDecoder.TryDecodeGameGoldChanged(frame, out ZirconCurrencyUpdateInfo gameGold))
                return $"GameGoldChanged gameGold={gameGold.Value}";

            if (ZirconStatusPacketDecoder.TryDecodeHuntGoldChanged(frame, out ZirconCurrencyUpdateInfo huntGold))
                return $"HuntGoldChanged huntGold={huntGold.Value}";

            if (ZirconStatusPacketDecoder.TryDecodeAutoTimeChanged(frame, out ZirconAutoTimeChangedInfo autoTime))
                return $"AutoTimeChanged autoTime={autoTime.AutoTime}";

            if (ZirconStatusPacketDecoder.TryDecodeSkillConfig(frame, out ZirconSkillConfigInfo skillConfig))
                return $"SkillConfig skillLevelLimit={skillConfig.SkillLevelLimit}";
            return $"Packet id={frame.PacketId} length={frame.Length}";
        }
    }
}

