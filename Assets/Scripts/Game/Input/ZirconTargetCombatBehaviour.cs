using System.Threading.Tasks;
using UnityEngine;
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

        private int activeFingerId = -1;
        private Vector2 touchStart;
        private uint selectedObjectId;
        private bool hasSelectedObject;
        private uint lastTappedObjectId;
        private float lastTapTime = float.NegativeInfinity;
        private float nextAttackTime;
        private float nextNpcTime;

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

            if (!enableMouseAndKeyboardInEditor || !Application.isEditor)
                return;

            if (UnityEngine.Input.GetMouseButtonUp(0))
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
            nextAttackTime = Time.unscaledTime + attackRepeatSeconds;
            await protocolProbe.SendAttackCommandAsync(direction);
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
                }

                if (touch.fingerId != activeFingerId)
                    continue;

                if (touch.phase == TouchPhase.Canceled)
                {
                    activeFingerId = -1;
                    return;
                }

                if (touch.phase != TouchPhase.Ended)
                    continue;

                activeFingerId = -1;
                if (Vector2.Distance(touchStart, touch.position) <= maximumTapTravelPixels)
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
                if (!worldRenderer.TryGetEntityWorldPosition(entity.ObjectId, out entityWorld))
                    entityWorld = new Vector3(entity.Location.X * worldRenderer.TileScale, -entity.Location.Y * worldRenderer.TileScale, 0f);

                float distance = Vector2.Distance(tapWorld, entityWorld);
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