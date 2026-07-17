using System.Collections.Generic;
using UnityEngine;
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
    public sealed class ZirconWorldPreviewBehaviour : MonoBehaviour
    {
        [SerializeField] private ZirconWorldDebugRenderer rendererTarget;
        [SerializeField] private ZirconMapDebugRenderer mapRenderer;
        [SerializeField] private bool animateWalk = true;

        private readonly List<ZirconEntityState> entities = new List<ZirconEntityState>();
        private ZirconEntityState localPlayer;

        private void Awake()
        {
            localPlayer = new ZirconEntityState
            {
                ObjectId = 1,
                Kind = ZirconEntityKind.Player,
                Name = "PreviewPlayer",
                MapIndex = 1,
                Location = new ZirconMapPoint(20, 20),
                Direction = 0,
                Health = 100,
                MaxHealth = 100,
                Mana = 50,
                MaxMana = 50,
            };

            entities.Add(localPlayer);
            entities.Add(new ZirconEntityState { ObjectId = 100, Kind = ZirconEntityKind.Monster, ModelIndex = 1, MapIndex = 1, Location = new ZirconMapPoint(22, 20), Direction = 0, Health = 50, MaxHealth = 50 });
            entities.Add(new ZirconEntityState { ObjectId = 101, Kind = ZirconEntityKind.Monster, ModelIndex = 1, MapIndex = 1, Location = new ZirconMapPoint(24, 23), Direction = 0, Health = 50, MaxHealth = 50 });
            entities.Add(new ZirconEntityState { ObjectId = 200, Kind = ZirconEntityKind.Npc, ModelIndex = 0, MapIndex = 1, Location = new ZirconMapPoint(18, 22), Direction = 0 });
            entities.Add(new ZirconEntityState { ObjectId = 300, Kind = ZirconEntityKind.Spell, ModelIndex = 0, MapIndex = 1, Location = new ZirconMapPoint(26, 21), Direction = 0 });

            if (mapRenderer != null)
                mapRenderer.RenderConfiguredMap();
        }

        private void Update()
        {
            if (rendererTarget == null)
                return;

            if (animateWalk)
            {
                int x = 20 + Mathf.RoundToInt(Mathf.Sin(Time.time) * 2f);
                localPlayer.Location = new ZirconMapPoint(x, 20);
            }

            var snapshot = new ZirconWorldSnapshot(
                localPlayer.Clone(),
                CloneEntities(),
                new List<ZirconChatLogEntry>(),
                new Dictionary<int, int>(),
                new List<ZirconSkillState>(),
                new List<ZirconItemState>(),
                new List<ZirconItemState>(),
                new List<ZirconItemState>(),
                100,
                new List<ZirconBuffState>(),
                new List<ZirconQuestState>(),
                new ZirconSocialState(),
                new ZirconCommerceState(),
                localPlayer.MapIndex,
                localPlayer.Location,
                true,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                false,
                0,
                0);

            rendererTarget.Render(snapshot);
        }

        private List<ZirconEntityState> CloneEntities()
        {
            var clone = new List<ZirconEntityState>(entities.Count);
            foreach (ZirconEntityState entity in entities)
                clone.Add(entity.Clone());

            return clone;
        }
    }
}
