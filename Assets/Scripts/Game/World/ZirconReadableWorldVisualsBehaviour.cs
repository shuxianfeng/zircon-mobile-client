using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zircon.Mobile.Core.Network;
using Zircon.Mobile.Game.Entities;
using Zircon.Mobile.UI.Catalog;
using Zircon.Mobile.UI.Login;

namespace Zircon.Mobile.Game.World
{
    public sealed class ZirconReadableWorldVisualsBehaviour : MonoBehaviour
    {
        [SerializeField] private ZirconProtocolProbeBehaviour session;
        [SerializeField] private ZirconSystemCatalogBehaviour catalog;
        [SerializeField] private ZirconMapDebugRenderer mapRenderer;
        [SerializeField] private ZirconWorldDebugRenderer worldRenderer;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private bool showWorldDiagnosticStatus;

        private ZirconMapManifest cleanedManifest;
        private float nextStatusRefresh;
        private ZirconProductionEntityPresentationBehaviour productionPresentation;
        private readonly Dictionary<uint, ZirconEntityKind> entityKinds =
            new Dictionary<uint, ZirconEntityKind>();
        private readonly Dictionary<uint, EntityNameLabel> entityNameLabels =
            new Dictionary<uint, EntityNameLabel>();
        private readonly HashSet<uint> seenEntityLabelIds = new HashSet<uint>();
        private readonly List<uint> removedEntityLabelIds = new List<uint>();

        private void OnEnable()
        {
            productionPresentation = GetComponent<ZirconProductionEntityPresentationBehaviour>();
            if (productionPresentation == null)
                productionPresentation = gameObject.AddComponent<ZirconProductionEntityPresentationBehaviour>();
            productionPresentation.Configure(session, worldRenderer);
            RefreshStatus();
            nextStatusRefresh = Time.unscaledTime + 0.35f;
        }

        private void LateUpdate()
        {
            RemoveMapFallbackBlocks();
            StabilizeEntitySprites();
            UpdateEntityNameLabels();

            if (Time.unscaledTime >= nextStatusRefresh)
            {
                nextStatusRefresh = Time.unscaledTime + 0.35f;
                RefreshStatus();
            }
        }

        private void RemoveMapFallbackBlocks()
        {
            if (mapRenderer == null || mapRenderer.Manifest == null || mapRenderer.Manifest == cleanedManifest)
                return;

            cleanedManifest = mapRenderer.Manifest;
            int removed = 0;
            int realTiles = 0;
            for (int i = mapRenderer.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = mapRenderer.transform.GetChild(i);
                SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
                if (renderer == null)
                    continue;

                if (renderer.color != Color.white)
                {
                    Destroy(child.gameObject);
                    removed++;
                    continue;
                }

                // The world renderer already uses the original 48x32 cell
                // aspect ratio. Stretching these 96x64 tiles again tears the
                // ground apart and shifts every object placed on top of it.
                child.localScale = Vector3.one;
                realTiles++;
            }

            Debug.Log("Readable world map: realTiles=" + realTiles + " fallbackBlocksRemoved=" + removed);
        }

        private void StabilizeEntitySprites()
        {
            if (worldRenderer == null)
                return;

            ZirconWorldSnapshot snapshot = session?.GetWorldSnapshot();
            if (snapshot == null)
                return;

            entityKinds.Clear();
            foreach (ZirconEntityState entity in snapshot.Entities)
                entityKinds[entity.ObjectId] = entity.Kind;

            Material runtimeMaterial = ZirconRuntimeSpriteMaterial.Shared;
            foreach (KeyValuePair<uint, ZirconEntityKind> pair in entityKinds)
            {
                if (!worldRenderer.TryGetEntityRenderer(pair.Key, out SpriteRenderer spriteRenderer))
                    continue;
                if (spriteRenderer != null && runtimeMaterial != null && spriteRenderer.sharedMaterial != runtimeMaterial)
                    spriteRenderer.sharedMaterial = runtimeMaterial;

                // Unsupported monster models intentionally use the renderer's
                // small collision marker. Do not enlarge that marker to the
                // normal character scale while stabilizing formal sprites.
                if (worldRenderer.IsUsingFallbackMarker(pair.Key))
                    continue;

                float scale = 1f;
                if (snapshot.LocalPlayer != null && pair.Key == snapshot.LocalPlayer.ObjectId)
                    scale = 1f;
                else if (pair.Value == ZirconEntityKind.Player ||
                         pair.Value == ZirconEntityKind.Npc ||
                         pair.Value == ZirconEntityKind.Monster)
                    scale = 1f;
                spriteRenderer.transform.localScale = Vector3.one * scale;
            }
        }

        private void UpdateEntityNameLabels()
        {
            ZirconWorldSnapshot snapshot = session?.GetWorldSnapshot();
            if (snapshot == null || worldRenderer == null)
                return;

            seenEntityLabelIds.Clear();
            foreach (ZirconEntityState entity in snapshot.Entities)
            {
                bool localPlayer = snapshot.LocalPlayer != null &&
                                   entity != null &&
                                   entity.ObjectId == snapshot.LocalPlayer.ObjectId;
                bool shouldLabel = entity != null && !entity.Dead &&
                                   (entity.Kind == ZirconEntityKind.Npc ||
                                    entity.Kind == ZirconEntityKind.Monster ||
                                    (entity.Kind == ZirconEntityKind.Player && !localPlayer));
                if (!shouldLabel ||
                    !worldRenderer.TryGetEntityRenderer(entity.ObjectId, out SpriteRenderer renderer) ||
                    renderer == null)
                    continue;

                seenEntityLabelIds.Add(entity.ObjectId);
                if (!entityNameLabels.TryGetValue(entity.ObjectId, out EntityNameLabel entry) || entry.Root == null)
                {
                    entry = CreateEntityNameLabel(entity.ObjectId, renderer);
                    entityNameLabels[entity.ObjectId] = entry;
                }

                string name;
                Color color;
                if (entity.Kind == ZirconEntityKind.Npc)
                {
                    ZirconSystemCatalogBehaviour.NpcEntry npc = catalog?.GetNpc(entity.ModelIndex);
                    name = !string.IsNullOrWhiteSpace(npc?.Name)
                        ? npc.Name
                        : "NPC " + entity.ModelIndex;
                    color = new Color(0.18f, 1f, 0.55f, 1f);
                }
                else if (entity.Kind == ZirconEntityKind.Monster)
                {
                    bool known = ZirconProductionEntityPresentationBehaviour.TryGetKnownMonster(
                        entity.ModelIndex, out string monsterName, out _);
                    ZirconSystemCatalogBehaviour.MonsterEntry monster =
                        catalog?.GetMonster(entity.ModelIndex);
                    name = !string.IsNullOrWhiteSpace(entity.PetOwner)
                        ? entity.PetOwner + "的宠物"
                        : !string.IsNullOrWhiteSpace(monster?.Name)
                            ? monster.Name
                            : known ? monsterName : "怪物 #" + entity.ModelIndex;
                    color = string.IsNullOrWhiteSpace(entity.PetOwner)
                        ? new Color(1f, 0.38f, 0.28f, 1f)
                        : new Color(0.56f, 0.94f, 1f, 1f);
                }
                else
                {
                    name = !string.IsNullOrWhiteSpace(entity.Name)
                        ? entity.Name
                        : "玩家 #" + entity.ObjectId;
                    color = new Color(0.32f, 0.82f, 1f, 1f);
                }
                if (entry.Label.text != name)
                    entry.Label.text = name;
                entry.Label.color = color;

                float spriteTop = renderer.sprite != null
                    ? renderer.sprite.bounds.max.y * Mathf.Abs(renderer.transform.localScale.y)
                    : 0.76f;
                if (entity.Kind == ZirconEntityKind.Monster &&
                    ZirconProductionEntityPresentationBehaviour.TryGetKnownMonster(
                        entity.ModelIndex, out _, out float monsterHeight))
                    spriteTop = monsterHeight;
                entry.Root.localPosition = renderer.transform.localPosition +
                                           new Vector3(0f, spriteTop + 0.10f, 0f);
                entry.Canvas.sortingOrder = renderer.sortingOrder + 100;
            }

            removedEntityLabelIds.Clear();
            foreach (KeyValuePair<uint, EntityNameLabel> pair in entityNameLabels)
                if (!seenEntityLabelIds.Contains(pair.Key))
                    removedEntityLabelIds.Add(pair.Key);

            foreach (uint objectId in removedEntityLabelIds)
            {
                if (entityNameLabels.TryGetValue(objectId, out EntityNameLabel entry) && entry.Root != null)
                    Destroy(entry.Root.gameObject);
                entityNameLabels.Remove(objectId);
            }
        }

        private EntityNameLabel CreateEntityNameLabel(uint objectId, SpriteRenderer renderer)
        {
            var rootObject = new GameObject("EntityName_" + objectId, typeof(RectTransform), typeof(Canvas));
            rootObject.layer = renderer.gameObject.layer;
            RectTransform root = rootObject.GetComponent<RectTransform>();
            root.SetParent(worldRenderer.transform, false);
            root.sizeDelta = new Vector2(260f, 54f);
            root.localScale = Vector3.one * 0.003f;

            Canvas canvas = rootObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;

            var textObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.layer = renderer.gameObject.layer;
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.SetParent(root, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
            if (statusText != null && statusText.font != null)
                label.font = statusText.font;
            label.fontSize = 28f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Overflow;
            label.raycastTarget = false;
            label.outlineColor = new Color32(0, 0, 0, 235);
            label.outlineWidth = 0.18f;
            return new EntityNameLabel(root, canvas, label);
        }

        private void RefreshStatus()
        {
            if (statusText == null)
                return;

            Graphic statusBackground = statusText.transform.parent?.GetComponent<Graphic>();
            if (!showWorldDiagnosticStatus)
            {
                if (statusBackground != null && statusBackground.enabled)
                    statusBackground.enabled = false;
                if (statusText.gameObject.activeSelf)
                    statusText.gameObject.SetActive(false);
                return;
            }

            ZirconConnectionState state = session?.ConnectionState ?? ZirconConnectionState.Disconnected;
            bool worldUiActive = state == ZirconConnectionState.LoadingMap || state == ZirconConnectionState.InGame;
            if (statusBackground != null && statusBackground.enabled != worldUiActive)
                statusBackground.enabled = worldUiActive;
            if (statusText.gameObject.activeSelf != worldUiActive)
                statusText.gameObject.SetActive(worldUiActive);
            if (!worldUiActive)
                return;

            ZirconWorldSnapshot snapshot = session?.GetWorldSnapshot();
            if (snapshot == null || !snapshot.HasLocalPlayer)
            {
                statusText.text = "正在读取地图与角色信息…";
                return;
            }

            int npcCount = 0;
            long nearestSquared = long.MaxValue;
            foreach (ZirconEntityState entity in snapshot.Entities)
            {
                if (entity == null || entity.Dead || entity.Kind != ZirconEntityKind.Npc)
                    continue;
                npcCount++;
                long dx = entity.Location.X - snapshot.Location.X;
                long dy = entity.Location.Y - snapshot.Location.Y;
                long distance = dx * dx + dy * dy;
                if (distance < nearestSquared)
                    nearestSquared = distance;
            }

            ZirconSystemCatalogBehaviour.MapEntry map = catalog?.GetMap(snapshot.MapIndex);
            string mapName = map?.Description ?? ("地图 " + snapshot.MapIndex);
            string npc = npcCount == 0
                ? "附近NPC：无"
                : "附近NPC：" + npcCount + "（青绿色名称）";
            statusText.text = mapName + "   坐标：" + snapshot.Location.X + "," + snapshot.Location.Y + "   " + npc;
        }

        private sealed class EntityNameLabel
        {
            public EntityNameLabel(RectTransform root, Canvas canvas, TextMeshProUGUI label)
            {
                Root = root;
                Canvas = canvas;
                Label = label;
            }

            public RectTransform Root { get; }
            public Canvas Canvas { get; }
            public TextMeshProUGUI Label { get; }
        }

    }
}
