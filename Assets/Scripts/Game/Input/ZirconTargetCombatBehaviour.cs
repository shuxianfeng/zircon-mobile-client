using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using Zircon.Mobile.Core.Protocol;
using Zircon.Mobile.Game.Entities;
using Zircon.Mobile.Game.World;
using Zircon.Mobile.UI.Login;

namespace Zircon.Mobile.Game.Input
{
    public sealed class ZirconTargetCombatBehaviour : MonoBehaviour
    {
        [SerializeField] private ZirconProtocolProbeBehaviour protocolProbe;
        [SerializeField] private ZirconWorldDebugRenderer worldRenderer;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private bool enableMouseAndKeyboardInEditor = true;
        [SerializeField] private float selectionRadiusWorld = 0.8f;
        [SerializeField] private float maximumTapTravelPixels = 24f;
        [SerializeField] private float doubleTapSeconds = 0.35f;
        [SerializeField] private float attackRepeatSeconds = 0.45f;
        [SerializeField] private float npcRepeatSeconds = 1f;
        [SerializeField] private float autoApproachRepeatSeconds = 0.34f;

        private int activeFingerId = -1;
        private Vector2 touchStart;
        private bool touchStartedOverUi;
        private uint selectedObjectId;
        private bool hasSelectedObject;
        private uint lastTappedObjectId;
        private float lastTapTime = float.NegativeInfinity;
        private float nextAttackTime;
        private float nextNpcTime;
        private uint autoApproachObjectId;
        private float nextAutoApproachTime;
        private bool autoApproachSending;

        public bool HasSelectedObject => hasSelectedObject;
        public uint SelectedObjectId => selectedObjectId;

        private void Update()
        {
            if (protocolProbe == null || !protocolProbe.IsInGame)
            {
                ClearSelection();
                return;
            }

            ValidateSelection();
            HandleTouch();
            if (autoApproachObjectId != 0 && Time.unscaledTime >= nextAutoApproachTime && !autoApproachSending)
                _ = AdvanceAutoApproachAsync();

            if (!enableMouseAndKeyboardInEditor || !Application.isEditor)
                return;

            if (UnityEngine.Input.GetMouseButtonUp(0) && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
                HandleTap(UnityEngine.Input.mousePosition);

            if (UnityEngine.Input.GetKeyDown(KeyCode.Tab))
                SelectNextTarget();

            if (UnityEngine.Input.GetKeyDown(KeyCode.Space))
                _ = AttackSelectedTargetAsync();

            if (UnityEngine.Input.GetKeyDown(KeyCode.P))
                _ = PickUpNearbyAsync();
        }

        public bool SelectNextTarget()
        {
            ZirconWorldSnapshot snapshot = protocolProbe?.GetWorldSnapshot();
            if (snapshot == null || !snapshot.HasLocalPlayer)
                return false;

            ZirconEntityState best = null;
            long bestDistance = long.MaxValue;
            foreach (ZirconEntityState entity in snapshot.Entities)
            {
                if (!IsAttackable(entity, snapshot))
                    continue;

                long dx = entity.Location.X - snapshot.Location.X;
                long dy = entity.Location.Y - snapshot.Location.Y;
                long distance = dx * dx + dy * dy;
                if (hasSelectedObject && entity.ObjectId == selectedObjectId)
                    continue;

                if (distance < bestDistance)
                {
                    best = entity;
                    bestDistance = distance;
                }
            }

            if (best == null)
                return false;

            SetSelection(best.ObjectId);
            return true;
        }

        public async Task AttackSelectedTargetAsync()
        {
            if (Time.unscaledTime < nextAttackTime || protocolProbe == null || !protocolProbe.IsInGame)
                return;

            ZirconWorldSnapshot snapshot = protocolProbe.GetWorldSnapshot();
            if (!TryGetSelectedTarget(snapshot, out ZirconEntityState target))
            {
                ClearSelection();
                return;
            }

            byte direction = DirectionFromPoints(snapshot.Location, target.Location);
            if (ChebyshevDistance(snapshot.Location, target.Location) > 1)
            {
                autoApproachObjectId = target.ObjectId;
                nextAutoApproachTime = Time.unscaledTime;
                await AdvanceAutoApproachAsync();
                return;
            }

            autoApproachObjectId = 0;
            nextAttackTime = Time.unscaledTime + attackRepeatSeconds;
            await protocolProbe.SendAttackCommandAsync(direction);
        }

        private async Task AdvanceAutoApproachAsync()
        {
            if (autoApproachSending || protocolProbe == null || !protocolProbe.IsInGame)
                return;
            ZirconWorldSnapshot snapshot = protocolProbe.GetWorldSnapshot();
            if (!TryFindAttackable(snapshot, autoApproachObjectId, out ZirconEntityState target))
            {
                autoApproachObjectId = 0;
                return;
            }

            autoApproachSending = true;
            try
            {
                byte direction = DirectionFromPoints(snapshot.Location, target.Location);
                if (ChebyshevDistance(snapshot.Location, target.Location) <= 1)
                {
                    autoApproachObjectId = 0;
                    nextAttackTime = Time.unscaledTime + attackRepeatSeconds;
                    await protocolProbe.SendAttackCommandAsync(direction);
                    Debug.Log("Combat auto-approach reached target=" + target.ObjectId + " and attacked");
                }
                else
                {
                    nextAutoApproachTime = Time.unscaledTime + autoApproachRepeatSeconds;
                    await protocolProbe.SendMoveCommandAsync(direction, 1);
                    Debug.Log("Combat auto-approach target=" + target.ObjectId + " direction=" + direction);
                }
            }
            finally
            {
                autoApproachSending = false;
            }
        }

        public async Task PickUpNearbyAsync()
        {
            if (protocolProbe == null || !protocolProbe.IsInGame)
                return;

            await protocolProbe.SendPickUpCommandAsync(0);
        }
        public void ClearSelection()
        {
            hasSelectedObject = false;
            autoApproachObjectId = 0;
            worldRenderer?.ClearSelectedObject();
        }

        private void HandleTouch()
        {
            for (int i = 0; i < UnityEngine.Input.touchCount; i++)
            {
                Touch touch = UnityEngine.Input.GetTouch(i);
                if (activeFingerId == -1 && touch.phase == TouchPhase.Began)
                {
                    activeFingerId = touch.fingerId;
                    touchStart = touch.position;
                    touchStartedOverUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId);
                }

                if (touch.fingerId != activeFingerId)
                    continue;

                if (touch.phase == TouchPhase.Canceled)
                {
                    activeFingerId = -1;
                    touchStartedOverUi = false;
                    return;
                }

                if (touch.phase != TouchPhase.Ended)
                    continue;

                activeFingerId = -1;
                bool ignoreTap = touchStartedOverUi;
                touchStartedOverUi = false;
                if (!ignoreTap && Vector2.Distance(touchStart, touch.position) <= maximumTapTravelPixels)
                    HandleTap(touch.position);
                return;
            }
        }

        private void HandleTap(Vector2 screenPosition)
        {
            if (!TryFindEntity(screenPosition, out ZirconEntityState target))
            {
                ClearSelection();
                return;
            }

            if (target.Kind == ZirconEntityKind.Npc)
            {
                ClearSelection();
                if (Time.unscaledTime >= nextNpcTime)
                {
                    nextNpcTime = Time.unscaledTime + npcRepeatSeconds;
                    _ = protocolProbe.SendNpcCallCommandAsync(target.ObjectId);
                }
                return;
            }

            bool isDoubleTap = target.ObjectId == lastTappedObjectId && Time.unscaledTime - lastTapTime <= doubleTapSeconds;
            SetSelection(target.ObjectId);
            lastTappedObjectId = target.ObjectId;
            lastTapTime = Time.unscaledTime;

            if (isDoubleTap)
                _ = AttackSelectedTargetAsync();
        }

        private bool TryFindEntity(Vector2 screenPosition, out ZirconEntityState target)
        {
            target = null;
            ZirconWorldSnapshot snapshot = protocolProbe?.GetWorldSnapshot();
            Camera cameraToUse = worldCamera != null ? worldCamera : Camera.main;
            if (snapshot == null || cameraToUse == null || worldRenderer == null)
                return false;

            var plane = new Plane(Vector3.forward, worldRenderer.transform.position);
            Ray ray = cameraToUse.ScreenPointToRay(screenPosition);
            if (!plane.Raycast(ray, out float enter))
                return false;

            Vector3 tapWorld = ray.GetPoint(enter);
            float bestDistance = selectionRadiusWorld;
            foreach (ZirconEntityState entity in snapshot.Entities)
            {
                if (!IsInteractive(entity, snapshot))
                    continue;

                Vector3 entityWorld;
                bool hasRenderer = worldRenderer.TryGetEntityRenderer(entity.ObjectId, out SpriteRenderer entityRenderer) &&
                                   entityRenderer != null;
                if (!worldRenderer.TryGetEntityWorldPosition(entity.ObjectId, out entityWorld))
                    entityWorld = new Vector3(entity.Location.X * worldRenderer.TileScale, -entity.Location.Y * worldRenderer.TileScale, 0f);

                float distance = Vector2.Distance(tapWorld, entityWorld);
                if (hasRenderer)
                {
                    Bounds bounds = entityRenderer.bounds;
                    bounds.Expand(selectionRadiusWorld * 0.35f);
                    if (bounds.Contains(new Vector3(tapWorld.x, tapWorld.y, bounds.center.z)))
                        distance = Vector2.Distance(tapWorld, entityWorld) * 0.01f;
                }
                if (distance > bestDistance)
                    continue;

                target = entity;
                bestDistance = distance;
            }

            return target != null;
        }

        private void ValidateSelection()
        {
            if (!hasSelectedObject)
                return;

            ZirconWorldSnapshot snapshot = protocolProbe.GetWorldSnapshot();
            if (!TryGetSelectedTarget(snapshot, out _))
                ClearSelection();
        }

        private bool TryGetSelectedTarget(ZirconWorldSnapshot snapshot, out ZirconEntityState target)
        {
            target = null;
            if (!hasSelectedObject || snapshot == null)
                return false;

            foreach (ZirconEntityState entity in snapshot.Entities)
            {
                if (entity.ObjectId == selectedObjectId && IsAttackable(entity, snapshot))
                {
                    target = entity;
                    return true;
                }
            }

            return false;
        }

        private void SetSelection(uint objectId)
        {
            if (selectedObjectId != objectId)
                autoApproachObjectId = 0;
            selectedObjectId = objectId;
            hasSelectedObject = true;
            worldRenderer?.SetSelectedObject(objectId);
        }

        private static bool IsInteractive(ZirconEntityState entity, ZirconWorldSnapshot snapshot)
        {
            if (entity == null || entity.Dead)
                return false;

            if (snapshot.LocalPlayer != null && entity.ObjectId == snapshot.LocalPlayer.ObjectId)
                return false;

            return entity.Kind == ZirconEntityKind.Monster || entity.Kind == ZirconEntityKind.Player || entity.Kind == ZirconEntityKind.Npc;
        }

        private static bool IsAttackable(ZirconEntityState entity, ZirconWorldSnapshot snapshot)
        {
            if (entity == null || entity.Dead || (entity.Kind != ZirconEntityKind.Monster && entity.Kind != ZirconEntityKind.Player))
                return false;

            return snapshot.LocalPlayer == null || entity.ObjectId != snapshot.LocalPlayer.ObjectId;
        }

        private static bool TryFindAttackable(ZirconWorldSnapshot snapshot, uint objectId, out ZirconEntityState target)
        {
            target = null;
            if (snapshot == null || objectId == 0) return false;
            foreach (ZirconEntityState entity in snapshot.Entities)
                if (entity.ObjectId == objectId && IsAttackable(entity, snapshot)) { target = entity; return true; }
            return false;
        }

        private static int ChebyshevDistance(ZirconMapPoint origin, ZirconMapPoint target)
        {
            return Mathf.Max(Mathf.Abs(target.X - origin.X), Mathf.Abs(target.Y - origin.Y));
        }

        private static byte DirectionFromPoints(Zircon.Mobile.Core.Protocol.ZirconMapPoint origin, Zircon.Mobile.Core.Protocol.ZirconMapPoint target)
        {
            int x = target.X.CompareTo(origin.X);
            int y = target.Y.CompareTo(origin.Y);

            if (x == 0 && y < 0) return 0;
            if (x > 0 && y < 0) return 1;
            if (x > 0 && y == 0) return 2;
            if (x > 0 && y > 0) return 3;
            if (x == 0 && y > 0) return 4;
            if (x < 0 && y > 0) return 5;
            if (x < 0 && y == 0) return 6;
            if (x < 0 && y < 0) return 7;
            return 0;
        }

        private void OnDisable()
        {
            ClearSelection();
        }
    }
}
