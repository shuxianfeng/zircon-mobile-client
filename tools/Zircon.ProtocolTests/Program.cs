using System.Collections;
using System.Reflection;
using System.Text;
using UnityEngine;
using Zircon.Mobile.Core.Protocol;
using Zircon.Mobile.Game.Buffs;
using Zircon.Mobile.Game.Commerce;
using Zircon.Mobile.Game.Entities;
using Zircon.Mobile.Game.Input;
using Zircon.Mobile.Game.Items;
using Zircon.Mobile.Game.Quests;
using Zircon.Mobile.Game.Skills;
using Zircon.Mobile.Game.Social;
using Zircon.Mobile.Game.World;

internal static class Program
{
    private static int Main()
    {
        TestMovementRules();
        TestMovePacketsAndWorldDistance();
        TestTwoCellPredictionConfirmation();
        TestPredictionCorrectionDoesNotIdleGlide();
        TestClientMagic();
        TestSkillKeyAndStorageCommands();
        TestNpcServicePackets();
        TestQuestPackets();
        TestSocialPackets();
        TestCommercePackets();
        TestStartGameSkills();
        TestObjectMagic();
        TestGroundItemAndPickup();
        TestItemDrop();
        TestInventoryPackets();
        TestBuffPackets();
        TestNpcPackets();
        TestWorldInventoryAndBuffState();
        TestClientChat();
        TestMapTransitionPackets();
        Console.WriteLine("protocol tests passed=20");
        return 0;
    }

    private static void TestMovementRules()
    {
        Equal(2, ZirconMovementRules.ResolveRequestedDistance(1f, 0.85f),
            "full-strength joystick requests running");
        Equal(2, ZirconMovementRules.ResolveRequestedDistance(0.85f, 0.85f),
            "run threshold is inclusive");
        Equal(1, ZirconMovementRules.ResolveRequestedDistance(0.849f, 0.85f),
            "sub-threshold joystick requests walking");
        Equal(2, ZirconMovementRules.ResolveRequestedDistance(0.01f, 0f),
            "default mobile threshold runs whenever outside the dead zone");

        var blockers = new[]
        {
            new ZirconEntityState
            {
                ObjectId = 1u,
                Kind = ZirconEntityKind.Player,
                Location = new ZirconMapPoint(10, 10),
            },
            new ZirconEntityState
            {
                ObjectId = 2u,
                Kind = ZirconEntityKind.Monster,
                Location = new ZirconMapPoint(12, 10),
            },
            new ZirconEntityState
            {
                ObjectId = 3u,
                Kind = ZirconEntityKind.Item,
                Location = new ZirconMapPoint(13, 10),
            },
        };
        True(ZirconMovementRules.IsOccupiedByBlockingEntity(blockers, 1u, 12, 10),
            "living monster blocks local movement prediction");
        True(!ZirconMovementRules.IsOccupiedByBlockingEntity(blockers, 1u, 10, 10),
            "local player never blocks its own movement prediction");
        True(!ZirconMovementRules.IsOccupiedByBlockingEntity(blockers, 1u, 13, 10),
            "ground items do not block movement prediction");

        var visited = new List<int>();
        int openDistance = ZirconMovementRules.ResolveTraversableDistance(2, step =>
        {
            visited.Add(step);
            return false;
        });
        Equal(2, openDistance, "two open cells preserve running distance");
        Equal(2, visited.Count, "two-cell run checks both cells");
        Equal(1, visited[0], "run checks first cell first");
        Equal(2, visited[1], "run checks second cell second");

        visited.Clear();
        int secondBlocked = ZirconMovementRules.ResolveTraversableDistance(2, step =>
        {
            visited.Add(step);
            return step == 2;
        });
        Equal(1, secondBlocked, "blocked second cell falls back to one-cell walk");
        Equal(2, visited.Count, "second-cell fallback checks both cells");

        visited.Clear();
        int firstBlocked = ZirconMovementRules.ResolveTraversableDistance(2, step =>
        {
            visited.Add(step);
            return step == 1;
        });
        Equal(0, firstBlocked, "blocked first cell prevents movement");
        Equal(1, visited.Count, "blocked first cell short-circuits second-cell check");
    }

    private static void TestMovePacketsAndWorldDistance()
    {
        ZirconPacketFrame clientMove = ReadFrame(ZirconClientPackets.Move(3, 2));
        Equal(ZirconPacketIds.Client.Move, clientMove.PacketId, "Client.Move id");
        using (BinaryReader reader = PayloadReader(clientMove))
        {
            Equal((byte)3, reader.ReadByte(), "Client.Move direction");
            Equal(2, reader.ReadInt32(), "Client.Move running distance");
            Equal(reader.BaseStream.Length, reader.BaseStream.Position,
                "Client.Move payload consumed");
        }

        TimeSpan slow = TimeSpan.FromMilliseconds(375);
        byte[] payload = WritePayload(writer =>
        {
            writer.Write(901u);
            writer.Write((byte)3);
            writer.Write(162);
            writer.Write(231);
            writer.Write(2);
            writer.Write(slow.Ticks);
        });
        var frame = new ZirconPacketFrame(
            payload.Length + 6,
            ZirconPacketIds.Server.ObjectMove,
            payload,
            Array.Empty<byte>());

        True(ZirconInGamePacketDecoder.TryDecodeObjectMove(frame, out ZirconObjectMoveInfo decoded),
            "ObjectMove decode");
        Equal(901u, decoded.ObjectId, "ObjectMove object id");
        Equal((byte)3, decoded.Direction, "ObjectMove direction");
        Equal(new ZirconMapPoint(162, 231), decoded.Location, "ObjectMove location");
        Equal(2, decoded.Distance, "ObjectMove distance");
        Equal(slow, decoded.Slow, "ObjectMove slow");

        var world = new ZirconWorldState();
        True(world.ApplyPacket(frame, out string summary), "world apply ObjectMove");
        True(summary.Contains("distance=2", StringComparison.Ordinal),
            "ObjectMove summary retains distance");
        ZirconWorldSnapshot snapshot = world.GetSnapshot();
        var entity = snapshot.Entities.Single(value => value.ObjectId == 901u);
        Equal(2, entity.MoveDistance, "world snapshot retains ObjectMove distance");
        Equal(slow, entity.MoveSlow, "world snapshot retains ObjectMove slow");
    }

    private static void TestTwoCellPredictionConfirmation()
    {
        Time.time = 10f;
        Time.unscaledTime = 10f;
        var player = new ZirconEntityState
        {
            ObjectId = 77u,
            Kind = ZirconEntityKind.Player,
            MapIndex = 1,
            Location = new ZirconMapPoint(10, 10),
            Direction = 2,
            PositionSequence = 1,
        };
        var renderer = new ZirconWorldDebugRenderer();
        renderer.Render(CreateMovementSnapshot(player));

        True(renderer.TryPredictLocalMove(2, 2), "two-cell local prediction accepted");
        True(renderer.TryGetLocalPlayerPlannedCell(out Vector2Int predicted),
            "two-cell local prediction exposes planned cell");
        Equal(new Vector2Int(12, 10), predicted,
            "two-cell prediction reaches run destination");
        True(renderer.TryGetVisualMoveDistance(77u, out int visualDistance),
            "two-cell prediction exposes visual distance");
        Equal(2, visualDistance, "two-cell prediction preserves visual distance");
        Near(0.60f, GetMovementDuration(renderer, 77u), 0.001f,
            "two-cell run uses one action duration");

        ZirconEntityState confirmed = player.Clone();
        confirmed.Location = new ZirconMapPoint(12, 10);
        confirmed.Direction = 2;
        confirmed.PositionSequence = 2;
        confirmed.MoveDistance = 2;
        renderer.Render(CreateMovementSnapshot(confirmed));

        True(renderer.TryGetLocalPlayerPlannedCell(out Vector2Int plannedAfterConfirm),
            "confirmed run retains planned cell");
        Equal(new Vector2Int(12, 10), plannedAfterConfirm,
            "authoritative run destination confirms two-cell prediction");
        Equal(0, GetUnconfirmedPredictionCount(renderer, 77u),
            "authoritative run destination clears pending prediction");
        True(renderer.TryPredictLocalMove(2, 2),
            "renderer accepts another run after two-cell confirmation");
    }

    private static void TestPredictionCorrectionDoesNotIdleGlide()
    {
        Time.time = 20f;
        Time.unscaledTime = 20f;
        var player = new ZirconEntityState
        {
            ObjectId = 88u,
            Kind = ZirconEntityKind.Player,
            MapIndex = 1,
            Location = new ZirconMapPoint(10, 10),
            Direction = 2,
            PositionSequence = 1,
        };
        var renderer = new ZirconWorldDebugRenderer();
        renderer.Render(CreateMovementSnapshot(player));
        True(renderer.TryPredictLocalMove(2, 2),
            "prediction correction test accepts run");

        Time.time = 20.60f;
        Time.unscaledTime = 20.60f;
        InvokeRendererLateUpdate(renderer);
        True(renderer.TryGetEntityRenderer(88u, out SpriteRenderer marker),
            "prediction correction test exposes marker");
        Near(12f * renderer.TileScale, marker.transform.localPosition.x, 0.001f,
            "prediction reaches visual destination");

        Time.time = 21.30f;
        Time.unscaledTime = 21.30f;
        InvokeRendererLateUpdate(renderer);
        Near(10f * renderer.TileScale, marker.transform.localPosition.x, 0.001f,
            "timed-out prediction snaps to authoritative cell");
        True(!renderer.IsObjectMoving(88u),
            "timed-out correction never presents as idle movement");

        Time.time = 30f;
        Time.unscaledTime = 30f;
        renderer = new ZirconWorldDebugRenderer();
        renderer.Render(CreateMovementSnapshot(player));
        True(renderer.TryPredictLocalMove(2, 2),
            "rejected prediction test accepts run");
        Time.time = 30.60f;
        Time.unscaledTime = 30.60f;
        InvokeRendererLateUpdate(renderer);

        ZirconEntityState corrected = player.Clone();
        corrected.Location = new ZirconMapPoint(11, 10);
        corrected.Direction = 2;
        corrected.PositionSequence = 2;
        corrected.MoveDistance = 1;
        renderer.Render(CreateMovementSnapshot(corrected));
        True(renderer.TryGetEntityRenderer(88u, out marker),
            "rejected prediction marker remains available");
        Near(11f * renderer.TileScale, marker.transform.localPosition.x, 0.001f,
            "unexpected authoritative cell snaps without gliding");
        True(!renderer.IsObjectMoving(88u),
            "unexpected authoritative correction remains visually idle");
    }

    private static void InvokeRendererLateUpdate(ZirconWorldDebugRenderer renderer)
    {
        MethodInfo lateUpdate = typeof(ZirconWorldDebugRenderer).GetMethod(
            "LateUpdate",
            BindingFlags.Instance | BindingFlags.NonPublic);
        True(lateUpdate != null, "renderer LateUpdate is available");
        lateUpdate.Invoke(renderer, null);
    }

    private static int GetUnconfirmedPredictionCount(
        ZirconWorldDebugRenderer renderer,
        uint objectId)
    {
        object movement = GetMovementState(renderer, objectId);
        FieldInfo unconfirmedField = movement.GetType().GetField(
            "Unconfirmed",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        True(unconfirmedField != null, "unconfirmed prediction list is available");
        var unconfirmed = unconfirmedField.GetValue(movement) as ICollection;
        True(unconfirmed != null, "unconfirmed prediction list is readable");
        return unconfirmed.Count;
    }

    private static float GetMovementDuration(
        ZirconWorldDebugRenderer renderer,
        uint objectId)
    {
        object movement = GetMovementState(renderer, objectId);
        FieldInfo durationField = movement.GetType().GetField(
            "Duration",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        True(durationField != null, "movement duration is available");
        return (float)durationField.GetValue(movement);
    }

    private static object GetMovementState(
        ZirconWorldDebugRenderer renderer,
        uint objectId)
    {
        FieldInfo movementsField = typeof(ZirconWorldDebugRenderer).GetField(
            "movementByObjectId",
            BindingFlags.Instance | BindingFlags.NonPublic);
        True(movementsField != null, "movement state dictionary is available");
        var movements = movementsField.GetValue(renderer) as IDictionary;
        True(movements != null && movements.Contains(objectId),
            "local movement state is available");
        return movements[objectId];
    }

    private static ZirconWorldSnapshot CreateMovementSnapshot(ZirconEntityState player)
    {
        return new ZirconWorldSnapshot(
            localPlayer: player,
            entities: new[] { player },
            chatMessages: Array.Empty<ZirconChatLogEntry>(),
            stats: new Dictionary<int, int>(),
            skills: Array.Empty<ZirconSkillState>(),
            inventory: Array.Empty<ZirconItemState>(),
            equipment: Array.Empty<ZirconItemState>(),
            storage: Array.Empty<ZirconItemState>(),
            storageSize: 100,
            buffs: Array.Empty<ZirconBuffState>(),
            quests: Array.Empty<ZirconQuestState>(),
            social: new ZirconSocialState(),
            commerce: new ZirconCommerceState(),
            mapIndex: player.MapIndex,
            location: player.Location,
            hasLocalPlayer: true,
            bagWeight: 0,
            wearWeight: 0,
            handWeight: 0,
            gold: 0,
            gameGold: 0,
            huntGold: 0,
            autoTime: 0,
            skillLevelLimit: 0,
            npcDialogOpen: false,
            npcObjectId: 0,
            npcPageIndex: 0);
    }

    private static void Near(float expected, float actual, float tolerance, string name)
    {
        if (MathF.Abs(expected - actual) > tolerance)
            throw new InvalidOperationException(
                $"Assertion failed: {name}. expected={expected} actual={actual}");
    }


    private static void TestClientMagic()
    {
        byte[] raw = ZirconClientPackets.Magic(2, 105, 77, new ZirconMapPoint(162, 229));
        ZirconPacketFrame frame = ReadFrame(raw);
        Equal(ZirconPacketIds.Client.Magic, frame.PacketId, "Client.Magic id");
        using var reader = PayloadReader(frame);
        Equal((byte)2, reader.ReadByte(), "magic direction");
        Equal((byte)ZirconMirAction.Spell, reader.ReadByte(), "magic action");
        Equal(105, reader.ReadInt32(), "magic type");
        Equal(77u, reader.ReadUInt32(), "magic target");
        Equal(162, reader.ReadInt32(), "magic x");
        Equal(229, reader.ReadInt32(), "magic y");
    }

    private static void TestSkillKeyAndStorageCommands()
    {
        ZirconPacketFrame magicKey = ReadFrame(ZirconClientPackets.MagicKey(111, 1, 2, 3, 4));
        Equal(ZirconPacketIds.Client.MagicKey, magicKey.PacketId, "Client.MagicKey id");
        using (BinaryReader reader = PayloadReader(magicKey))
        {
            Equal(111, reader.ReadInt32(), "MagicKey type");
            Equal((byte)1, reader.ReadByte(), "MagicKey set 1");
            Equal((byte)2, reader.ReadByte(), "MagicKey set 2");
            Equal((byte)3, reader.ReadByte(), "MagicKey set 3");
            Equal((byte)4, reader.ReadByte(), "MagicKey set 4");
        }

        ZirconPacketFrame sort = ReadFrame(ZirconClientPackets.SortStorageItem());
        Equal(ZirconPacketIds.Client.SortStorageItem, sort.PacketId, "Client.SortStorageItem id");
        Equal(0, sort.Payload.Length, "SortStorageItem empty payload");

        byte[] sortedPayload = WritePayload(writer =>
        {
            writer.Write(true); writer.Write(1); writer.Write(true); WriteUserItem(writer, 950, 137, 8);
        });
        var sorted = new ZirconPacketFrame(sortedPayload.Length + 6, ZirconPacketIds.Server.SortStorageItem, sortedPayload, Array.Empty<byte>());
        True(ZirconInventoryPacketDecoder.TryDecodeSortStorageItems(sorted, out ZirconStorageItemsInfo storage), "SortStorageItem decode");
        Equal(1, storage.Items.Count, "sorted storage count");
        var world = new ZirconWorldState();
        True(world.ApplyPacket(sorted, out _), "world apply sorted storage");
        Equal(1, world.GetSnapshot().Storage.Count, "world storage count");

        byte[] sizePayload = WritePayload(writer => writer.Write(120));
        var sizeFrame = new ZirconPacketFrame(sizePayload.Length + 6, ZirconPacketIds.Server.StorageSize, sizePayload, Array.Empty<byte>());
        True(ZirconInventoryPacketDecoder.TryDecodeStorageSize(sizeFrame, out int storageSize), "StorageSize decode");
        Equal(120, storageSize, "storage size");
        True(world.ApplyPacket(sizeFrame, out _), "world apply storage size");
        Equal(120, world.GetSnapshot().StorageSize, "world storage size");
    }
    private static void TestNpcServicePackets()
    {
        var links = new[] { new ZirconCellLinkInfo(ZirconGridType.Inventory, 4, 3) };
        ZirconPacketFrame sell = ReadFrame(ZirconClientPackets.NpcSell(links));
        Equal(ZirconPacketIds.Client.NpcSell, sell.PacketId, "NPCSell id");
        using (BinaryReader reader = PayloadReader(sell))
        {
            True(reader.ReadBoolean(), "NPCSell list"); Equal(1, reader.ReadInt32(), "NPCSell count"); True(reader.ReadBoolean(), "NPCSell link");
            Equal(1, reader.ReadInt32(), "NPCSell grid"); Equal(4, reader.ReadInt32(), "NPCSell slot"); Equal(3L, reader.ReadInt64(), "NPCSell amount");
        }
        ZirconPacketFrame repair = ReadFrame(ZirconClientPackets.NpcRepair(links, true, false));
        Equal(ZirconPacketIds.Client.NpcRepair, repair.PacketId, "NPCRepair id");

        byte[] changedPayload = WritePayload(writer => { writer.Write(true); writer.Write(1); writer.Write(true); writer.Write(1); writer.Write(4); writer.Write(3L); writer.Write(true); });
        var changed = new ZirconPacketFrame(changedPayload.Length + 6, ZirconPacketIds.Server.ItemsChanged, changedPayload, Array.Empty<byte>());
        True(ZirconInventoryPacketDecoder.TryDecodeItemsChanged(changed, out ZirconItemsChangedInfo result), "ItemsChanged decode");
        True(result.Success, "ItemsChanged success"); Equal(3L, result.Links[0].Count, "ItemsChanged amount");
    }

    private static void TestQuestPackets()
    {
        ZirconPacketFrame track = ReadFrame(ZirconClientPackets.QuestTrack(77, true));
        Equal(ZirconPacketIds.Client.QuestTrack, track.PacketId, "QuestTrack id");
        using (BinaryReader reader = PayloadReader(track)) { Equal(77, reader.ReadInt32(), "QuestTrack index"); True(reader.ReadBoolean(), "QuestTrack value"); }

        byte[] payload = WritePayload(writer =>
        {
            writer.Write(true); writer.Write(9); writer.Write(77); writer.Write(true); writer.Write(false); writer.Write(0);
            writer.Write(true); writer.Write(1); writer.Write(true); writer.Write(10); writer.Write(200); writer.Write(5L);
        });
        var frame = new ZirconPacketFrame(payload.Length + 6, ZirconPacketIds.Server.QuestChanged, payload, Array.Empty<byte>());
        True(ZirconQuestPacketDecoder.TryDecodeQuestChanged(frame, out ZirconUserQuestInfo quest), "QuestChanged decode");
        Equal(77, quest.QuestIndex, "quest info index"); Equal(1, quest.Tasks.Count, "quest task count");
        var world = new ZirconWorldState(); True(world.ApplyPacket(frame, out _), "world quest apply"); Equal(1, world.GetSnapshot().Quests.Count, "world quest count");
    }

    private static void TestSocialPackets()
    {
        ZirconPacketFrame invite = ReadFrame(ZirconClientPackets.GroupInvite("Raphael"));
        Equal(ZirconPacketIds.Client.GroupInvite, invite.PacketId, "GroupInvite id");
        byte[] payload = WritePayload(writer => { writer.Write(123u); writer.Write("Raphael"); });
        var member = new ZirconPacketFrame(payload.Length + 6, ZirconPacketIds.Server.GroupMember, payload, Array.Empty<byte>());
        True(ZirconSocialPacketDecoder.TryDecode(member, out ZirconSocialEvent social), "GroupMember decode");
        Equal(123u, social.ObjectId, "GroupMember object"); Equal("Raphael", social.Name, "GroupMember name");
        var world = new ZirconWorldState(); True(world.ApplyPacket(member, out _), "world group apply"); Equal(1, world.GetSnapshot().Social.GroupMembers.Count, "world group count");
    }

    private static void TestCommercePackets()
    {
        ZirconPacketFrame search = ReadFrame(ZirconClientPackets.MarketSearch("Sword", false, 0, 1));
        Equal(ZirconPacketIds.Client.MarketSearch, search.PacketId, "MarketSearch id");
        byte[] mailPayload = WritePayload(writer =>
        {
            writer.Write(true); writer.Write(1); writer.Write(true); writer.Write(5); writer.Write(false); writer.Write(true);
            writer.Write(DateTime.UtcNow.ToBinary()); writer.Write("System"); writer.Write("Subject"); writer.Write("Body"); writer.Write(100); writer.Write(false);
        });
        var mailFrame = new ZirconPacketFrame(mailPayload.Length + 6, ZirconPacketIds.Server.MailList, mailPayload, Array.Empty<byte>());
        True(ZirconCommercePacketDecoder.TryDecode(mailFrame, out ZirconCommerceEvent mail), "MailList decode"); Equal(1, mail.Mail.Count, "mail count");
        var world = new ZirconWorldState(); True(world.ApplyPacket(mailFrame, out _), "world mail apply"); Equal(1, world.GetSnapshot().Commerce.Mail.Count, "world mail count");

        byte[] marketPayload = WritePayload(writer =>
        {
            writer.Write(1); writer.Write(true); writer.Write(1); writer.Write(true); writer.Write(8); writer.Write(true); WriteUserItem(writer, 50, 137, 2);
            writer.Write(999); writer.Write("Seller"); writer.Write("Good"); writer.Write(false);
        });
        var marketFrame = new ZirconPacketFrame(marketPayload.Length + 6, ZirconPacketIds.Server.MarketSearch, marketPayload, Array.Empty<byte>());
        True(ZirconCommercePacketDecoder.TryDecode(marketFrame, out ZirconCommerceEvent market), "Market search decode"); Equal(1, market.Listings.Count, "market result count");
    }
    private static void TestStartGameSkills()
    {
        byte[] payload = WritePayload(writer =>
        {
            writer.Write((byte)5); writer.Write(string.Empty); writer.Write(0L); writer.Write(true);
            writer.Write(1); writer.Write(900u); writer.Write("Tester"); writer.Write(unchecked((int)0xffffffff));
            writer.Write(string.Empty); writer.Write(string.Empty); writer.Write((byte)0); writer.Write((byte)1);
            writer.Write(10); writer.Write(20); writer.Write((byte)2); writer.Write(1); writer.Write(100L);
            writer.Write(5); writer.Write(42);
            for (int i = 0; i < 7; i++) writer.Write(0);
            writer.Write(0m); writer.Write(1000); writer.Write(200);
            writer.Write((byte)0); writer.Write((byte)0); writer.Write(0); writer.Write(0f); writer.Write(true);
            writer.Write(true); writer.Write(1); writer.Write(true); WriteUserItem(writer, 700, 55, 10);
            writer.Write(false); writer.Write(false);
            writer.Write(true); writer.Write(1); writer.Write(true);
            WriteMagic(writer, 7, 6, 65, 0, 0, 0, 3, 1234, 2500);
            writer.Write(false); // Buffs
            writer.Write(0); writer.Write(false); writer.Write(false); writer.Write(false); writer.Write((byte)0); writer.Write(0); writer.Write(0);
            writer.Write(true); writer.Write(1); writer.Write(true); writer.Write(9); writer.Write(77); writer.Write(true); writer.Write(false); writer.Write(0);
            writer.Write(true); writer.Write(1); writer.Write(true); writer.Write(10); writer.Write(200); writer.Write(5L);
        });

        var frame = new ZirconPacketFrame(payload.Length + 6, ZirconPacketIds.Server.StartGame, payload, Array.Empty<byte>());
        True(ZirconServerPacketDecoder.TryDecodeStartGame(frame, out ZirconDecodedStartGame start), "StartGame decode");
        True(start.StartInformation.HasValue, "StartInformation");
        Equal(1, start.StartInformation.Value.Magics.Count, "skill count");
        Equal(6, start.StartInformation.Value.Magics[0].InfoIndex, "skill info index");
        Equal(3, start.StartInformation.Value.Magics[0].Level, "skill level");
        Equal(1, start.StartInformation.Value.Items.Count, "inventory count");
        Equal(10L, start.StartInformation.Value.Items[0].Count, "inventory item count");
        Equal(1, start.StartInformation.Value.Quests.Count, "start quest count");
    }

    private static void TestObjectMagic()
    {
        byte[] payload = WritePayload(writer =>
        {
            writer.Write(900u); writer.Write((byte)2); writer.Write(162); writer.Write(229); writer.Write(105);
            writer.Write(true); writer.Write(1); writer.Write(77u);
            writer.Write(true); writer.Write(1); writer.Write(163); writer.Write(229);
            writer.Write(true); writer.Write(500L);
        });

        var frame = new ZirconPacketFrame(payload.Length + 6, ZirconPacketIds.Server.ObjectMagic, payload, Array.Empty<byte>());
        True(ZirconInGamePacketDecoder.TryDecodeObjectMagic(frame, out ZirconObjectMagicInfo magic), "ObjectMagic decode");
        Equal(105, magic.MagicType, "ObjectMagic type");
        Equal(77u, magic.Targets[0], "ObjectMagic target");
        True(magic.Cast, "ObjectMagic cast");
    }

    private static void TestGroundItemAndPickup()
    {
        byte[] groundPayload = WritePayload(writer =>
        {
            writer.Write(400u); writer.Write(1); writer.Write(161); writer.Write(229); writer.Write(55);
        });
        var groundFrame = new ZirconPacketFrame(groundPayload.Length + 6, ZirconPacketIds.Server.DataObjectItem, groundPayload, Array.Empty<byte>());
        True(ZirconInGamePacketDecoder.TryDecodeDataObjectItem(groundFrame, out ZirconDataObjectItemInfo ground), "DataObjectItem decode");
        Equal(55, ground.ItemInfoIndex, "ground item info");

        byte[] gainedPayload = WritePayload(writer =>
        {
            writer.Write(true); writer.Write(1); writer.Write(true); WriteUserItem(writer, 700, 55, 2);
        });
        var gainedFrame = new ZirconPacketFrame(gainedPayload.Length + 6, ZirconPacketIds.Server.ItemsGained, gainedPayload, Array.Empty<byte>());
        True(ZirconInGamePacketDecoder.TryDecodeItemsGained(gainedFrame, out ZirconItemsGainedInfo gained), "ItemsGained decode");
        Equal(1, gained.Items.Count, "gained item count");
        Equal(2L, gained.Items[0].Count, "gained stack count");
    }

    private static void TestItemDrop()
    {
        ZirconPacketFrame frame = ReadFrame(ZirconClientPackets.ItemDrop(7, 1));
        Equal(ZirconPacketIds.Client.ItemDrop, frame.PacketId, "Client.ItemDrop id");
        using var reader = PayloadReader(frame);
        True(reader.ReadBoolean(), "ItemDrop link present");
        Equal(1, reader.ReadInt32(), "ItemDrop inventory grid");
        Equal(7, reader.ReadInt32(), "ItemDrop slot");
        Equal(1L, reader.ReadInt64(), "ItemDrop count");
    }
    private static void TestInventoryPackets()
    {
        ZirconPacketFrame moveClient = ReadFrame(ZirconClientPackets.ItemMove(ZirconGridType.Inventory, ZirconGridType.Equipment, 4, 2, false));
        Equal(ZirconPacketIds.Client.ItemMove, moveClient.PacketId, "Client.ItemMove id");
        using (BinaryReader reader = PayloadReader(moveClient))
        {
            Equal(1, reader.ReadInt32(), "ItemMove from grid");
            Equal(2, reader.ReadInt32(), "ItemMove to grid");
            Equal(4, reader.ReadInt32(), "ItemMove from slot");
            Equal(2, reader.ReadInt32(), "ItemMove to slot");
            True(!reader.ReadBoolean(), "ItemMove merge false");
        }

        byte[] changedPayload = WritePayload(writer =>
        {
            writer.Write(true); writer.Write(1); writer.Write(6); writer.Write(749L); writer.Write(true);
        });
        var changedFrame = new ZirconPacketFrame(changedPayload.Length + 6, ZirconPacketIds.Server.ItemChanged, changedPayload, Array.Empty<byte>());
        True(ZirconInventoryPacketDecoder.TryDecodeItemChanged(changedFrame, out ZirconItemChangedInfo changed), "ItemChanged decode");
        Equal(6, changed.Link.Slot, "ItemChanged slot");
        Equal(749L, changed.Link.Count, "ItemChanged count");
        True(changed.Success, "ItemChanged success");
    }

    private static void TestBuffPackets()
    {
        byte[] payload = WritePayload(writer =>
        {
            writer.Write(true); writer.Write(77); writer.Write(201); writer.Write(TimeSpan.FromSeconds(30).Ticks);
            writer.Write(TimeSpan.FromSeconds(1).Ticks); writer.Write(true); writer.Write(true); writer.Write(1);
            writer.Write(10); writer.Write(25); writer.Write(false); writer.Write(0);
        });
        var frame = new ZirconPacketFrame(payload.Length + 6, ZirconPacketIds.Server.BuffAdd, payload, Array.Empty<byte>());
        True(ZirconBuffPacketDecoder.TryDecodeBuffAdd(frame, out ZirconBuffInfo buff), "BuffAdd decode");
        Equal(77, buff.Index, "Buff index");
        Equal(201, buff.Type, "Buff type");
        Equal(25, buff.Stats[10], "Buff stat");
    }

    private static void TestNpcPackets()
    {
        ZirconPacketFrame button = ReadFrame(ZirconClientPackets.NpcButton(42));
        Equal(ZirconPacketIds.Client.NpcButton, button.PacketId, "NPCButton id");
        using (BinaryReader reader = PayloadReader(button))
            Equal(42, reader.ReadInt32(), "NPCButton value");

        ZirconPacketFrame buy = ReadFrame(ZirconClientPackets.NpcBuy(137, 3));
        Equal(ZirconPacketIds.Client.NpcBuy, buy.PacketId, "NPCBuy id");
        using (BinaryReader reader = PayloadReader(buy))
        {
            Equal(137, reader.ReadInt32(), "NPCBuy item");
            Equal(3L, reader.ReadInt64(), "NPCBuy amount");
            True(!reader.ReadBoolean(), "NPCBuy personal funds");
        }

        var world = new ZirconWorldState();
        byte[] responsePayload = WritePayload(writer =>
        {
            writer.Write((uint)901);
            writer.Write(42);
        });
        var response = new ZirconPacketFrame(
            responsePayload.Length + 6,
            ZirconPacketIds.Server.NpcResponse,
            responsePayload,
            Array.Empty<byte>());
        True(world.ApplyPacket(response, out _), "world apply NPC response");
        True(world.GetSnapshot().NpcDialogOpen, "NPC response opens dialog");
        Equal((uint)901, world.GetSnapshot().NpcObjectId, "NPC response object");
        Equal(42, world.GetSnapshot().NpcPageIndex, "NPC response page");

        world.DismissNpcDialog();
        True(!world.GetSnapshot().NpcDialogOpen, "local NPC dismissal closes dialog");
        Equal((uint)0, world.GetSnapshot().NpcObjectId, "local NPC dismissal clears object");
        Equal(0, world.GetSnapshot().NpcPageIndex, "local NPC dismissal clears page");
    }

    private static void TestClientChat()
    {
        ZirconPacketFrame frame = ReadFrame(ZirconClientPackets.Chat("@MAP 0"));
        Equal(ZirconPacketIds.Client.Chat, frame.PacketId, "Client.Chat id");
        using BinaryReader reader = PayloadReader(frame);
        Equal("@MAP 0", reader.ReadString(), "Client.Chat text");
        Equal(reader.BaseStream.Length, reader.BaseStream.Position, "Client.Chat deployed schema");
    }

    private static void TestMapTransitionPackets()
    {
        var world = new ZirconWorldState();

        byte[] oldPayload = WritePayload(writer => writer.Write(6));
        var oldFrame = new ZirconPacketFrame(oldPayload.Length + 6, ZirconPacketIds.Server.MapChanged, oldPayload, Array.Empty<byte>());
        True(ZirconMapPacketDecoder.TryDecodeMapChanged(oldFrame, out ZirconMapChangedInfo oldMap), "old MapChanged decode");
        Equal(6, oldMap.MapIndex, "old MapChanged map");
        Equal(-1, oldMap.InstanceIndex, "old MapChanged instance");
        True(world.ApplyPacket(oldFrame, out _), "world apply old MapChanged");
        Equal(6, world.GetSnapshot().MapIndex, "world old MapChanged map");

        byte[] newPayload = WritePayload(writer => { writer.Write(1); writer.Write(2); });
        var newFrame = new ZirconPacketFrame(newPayload.Length + 6, ZirconPacketIds.Server.MapChanged, newPayload, Array.Empty<byte>());
        True(ZirconMapPacketDecoder.TryDecodeMapChanged(newFrame, out ZirconMapChangedInfo newMap), "new MapChanged decode");
        Equal(1, newMap.MapIndex, "new MapChanged map");
        Equal(2, newMap.InstanceIndex, "new MapChanged instance");

        byte[] locationPayload = WritePayload(writer => { writer.Write((byte)3); writer.Write(79); writer.Write(149); });
        var locationFrame = new ZirconPacketFrame(locationPayload.Length + 6, ZirconPacketIds.Server.UserLocation, locationPayload, Array.Empty<byte>());
        True(ZirconMapPacketDecoder.TryDecodeUserLocation(locationFrame, out ZirconUserLocationInfo location), "UserLocation decode");
        True(world.ApplyPacket(locationFrame, out _), "world apply UserLocation");
        Equal(new ZirconMapPoint(79, 149), world.GetSnapshot().Location, "world UserLocation");
    }

    private static void TestWorldInventoryAndBuffState()
    {
        var world = new ZirconWorldState();
        world.SetItemStackSizeResolver(infoIndex => infoIndex == 137 ? 20 : 1);
        byte[] gainedPayload = WritePayload(writer =>
        {
            writer.Write(true); writer.Write(1); writer.Write(true); WriteUserItem(writer, 801, 137, 12, 27);
        });
        var gained = new ZirconPacketFrame(gainedPayload.Length + 6, ZirconPacketIds.Server.ItemsGained, gainedPayload, Array.Empty<byte>());
        True(world.ApplyPacket(gained, out _), "world apply gained");

        byte[] stackedPayload = WritePayload(writer =>
        {
            writer.Write(true); writer.Write(1); writer.Write(true); WriteUserItem(writer, 802, 137, 5, 31);
        });
        var stacked = new ZirconPacketFrame(stackedPayload.Length + 6, ZirconPacketIds.Server.ItemsGained, stackedPayload, Array.Empty<byte>());
        True(world.ApplyPacket(stacked, out _), "world apply gained stack increment");

        byte[] buffPayload = WritePayload(writer =>
        {
            writer.Write(true); writer.Write(88); writer.Write(101); writer.Write(TimeSpan.FromMinutes(2).Ticks);
            writer.Write(0L); writer.Write(false); writer.Write(false); writer.Write(0);
        });
        var buff = new ZirconPacketFrame(buffPayload.Length + 6, ZirconPacketIds.Server.BuffAdd, buffPayload, Array.Empty<byte>());
        True(world.ApplyPacket(buff, out _), "world apply buff");

        ZirconWorldSnapshot snapshot = world.GetSnapshot();
        Equal(1, snapshot.Inventory.Count, "world inventory count");
        Equal(17L, snapshot.Inventory[0].Count, "world inventory stack increment");
        Equal(0, snapshot.Inventory[0].Slot, "world gained ignores transient packet slot");
        Equal(1, snapshot.Buffs.Count, "world buff count");
        Equal(101, snapshot.Buffs[0].Type, "world buff type");
    }
    private static void WriteMagic(BinaryWriter writer, int index, int infoIndex, byte key1, byte key2, byte key3, byte key4, int level, long experience, long cooldownTicks)
    {
        writer.Write(index); writer.Write(infoIndex); writer.Write(key1); writer.Write(key2);
        writer.Write(key3); writer.Write(key4); writer.Write(level); writer.Write(experience); writer.Write(cooldownTicks);
    }

    private static void WriteUserItem(BinaryWriter writer, int index, int infoIndex, long count, int slot = 0)
    {
        writer.Write(index); writer.Write(infoIndex); writer.Write(10); writer.Write(10); writer.Write(count);
        writer.Write(slot); writer.Write(1); writer.Write(0m); writer.Write(0); writer.Write(0L); writer.Write(0L);
        writer.Write(true); writer.Write(true); writer.Write(1); writer.Write(10); writer.Write(25); writer.Write(0); writer.Write(0L);
    }

    private static ZirconPacketFrame ReadFrame(byte[] raw)
    {
        var buffer = raw.ToList();
        True(ZirconBinary.TryReadFrame(buffer, out ZirconPacketFrame frame), "frame decode");
        Equal(0, buffer.Count, "frame buffer consumed");
        return frame;
    }

    private static BinaryReader PayloadReader(ZirconPacketFrame frame) => new BinaryReader(new MemoryStream(frame.Payload), Encoding.UTF8);

    private static byte[] WritePayload(Action<BinaryWriter> write)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8, true);
        write(writer); writer.Flush(); return stream.ToArray();
    }

    private static void True(bool value, string name)
    {
        if (!value) throw new InvalidOperationException($"Assertion failed: {name}");
    }

    private static void Equal<T>(T expected, T actual, string name)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"Assertion failed: {name}. expected={expected} actual={actual}");
    }
}
