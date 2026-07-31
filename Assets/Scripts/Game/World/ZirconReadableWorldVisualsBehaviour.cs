using System.Collections;
using System.Collections.Generic;
using System.Reflection;
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

        private ZirconMapManifest cleanedManifest;
        private float nextStatusRefresh;
        private ZirconProductionEntityPresentationBehaviour productionPresentation;
        private bool spriteSequencesStabilized;
        private readonly Dictionary<uint, ZirconEntityKind> entityKinds =
            new Dictionary<uint, ZirconEntityKind>();

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

            if (!spriteSequencesStabilized)
            {
                // The exported fallback files are sparse validation samples,
                // not complete action sequences. Stabilize them once after the
                // world renderer has performed its first load.
                FieldInfo dictionaryField = typeof(ZirconWorldDebugRenderer).GetField(
                    "spritesByKind", BindingFlags.Instance | BindingFlags.NonPublic);
                if (dictionaryField?.GetValue(worldRenderer) is IDictionary dictionary)
                {
                    foreach (DictionaryEntry entry in dictionary)
                        if (entry.Key is ZirconEntityKind kind &&
                            kind != ZirconEntityKind.Monster &&
                            kind != ZirconEntityKind.Spell &&
                            entry.Value is IList frames)
                            while (frames.Count > 1)
                                frames.RemoveAt(frames.Count - 1);
                }
                spriteSequencesStabilized = true;
            }

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

                float scale = 1f;
                if (snapshot.LocalPlayer != null && pair.Key == snapshot.LocalPlayer.ObjectId)
                    scale = 1f;
                else if (pair.Value == ZirconEntityKind.Player)
                    scale = 1.55f;
                else if (pair.Value == ZirconEntityKind.Npc)
                    scale = 1.5f;
                else if (pair.Value == ZirconEntityKind.Monster)
                    scale = 0.9f;
                spriteRenderer.transform.localScale = Vector3.one * scale;
            }
        }

        private void RefreshStatus()
        {
            if (statusText == null)
                return;

            ZirconConnectionState state = session?.ConnectionState ?? ZirconConnectionState.Disconnected;
            bool worldUiActive = state == ZirconConnectionState.LoadingMap || state == ZirconConnectionState.InGame;
            Graphic statusBackground = statusText.transform.parent?.GetComponent<Graphic>();
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
                : "附近NPC：" + npcCount + "（金色NPC标记）";
            statusText.text = mapName + "   坐标：" + snapshot.Location.X + "," + snapshot.Location.Y + "   " + npc;
        }

    }
}
