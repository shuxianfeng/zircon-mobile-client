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

                // A PC background tile is 96x64 pixels and represents a 2x2
                // cell block. The prototype world grid is square, so fill its
                // vertical cell span to avoid visible seams.
                child.localScale = new Vector3(1f, 1.5f, 1f);
                realTiles++;
            }

            Debug.Log("Readable world map: realTiles=" + realTiles + " fallbackBlocksRemoved=" + removed);
        }

        private void StabilizeEntitySprites()
        {
            if (worldRenderer == null)
                return;

            // The exported files are sparse validation samples, not a complete
            // action sequence. Keep one stable, front-facing frame instead of
            // cycling unrelated indices and producing a ghosting effect.
            FieldInfo dictionaryField = typeof(ZirconWorldDebugRenderer).GetField("spritesByKind", BindingFlags.Instance | BindingFlags.NonPublic);
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

            ZirconWorldSnapshot snapshot = session?.GetWorldSnapshot();
            if (snapshot == null)
                return;

            var kinds = new Dictionary<uint, ZirconEntityKind>();
            foreach (ZirconEntityState entity in snapshot.Entities)
                kinds[entity.ObjectId] = entity.Kind;

            for (int i = 0; i < worldRenderer.transform.childCount; i++)
            {
                Transform child = worldRenderer.transform.GetChild(i);
                SpriteRenderer spriteRenderer = child.GetComponent<SpriteRenderer>();
                Material runtimeMaterial = ZirconRuntimeSpriteMaterial.Shared;
                if (spriteRenderer != null && runtimeMaterial != null && spriteRenderer.sharedMaterial != runtimeMaterial)
                    spriteRenderer.sharedMaterial = runtimeMaterial;
                uint objectId = ParseObjectId(child.name);
                if (!kinds.TryGetValue(objectId, out ZirconEntityKind kind))
                    continue;

                float scale = 1f;
                if (snapshot.LocalPlayer != null && objectId == snapshot.LocalPlayer.ObjectId)
                    scale = 1f;
                else if (kind == ZirconEntityKind.Player)
                    scale = 1.55f;
                else if (kind == ZirconEntityKind.Npc)
                    scale = 1.5f;
                else if (kind == ZirconEntityKind.Monster)
                    scale = 0.9f;
                child.localScale = Vector3.one * scale;
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

        private static uint ParseObjectId(string objectName)
        {
            if (string.IsNullOrEmpty(objectName))
                return 0;
            int separator = objectName.LastIndexOf('_');
            return separator >= 0 && uint.TryParse(objectName.Substring(separator + 1), out uint value) ? value : 0;
        }
    }
}
