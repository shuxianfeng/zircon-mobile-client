using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Zircon.Mobile.Game.World;
using Zircon.Mobile.UI.Login;

namespace Zircon.Mobile.Game.Input
{
    public sealed class ZirconMobileGameplayControlsBehaviour : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private ZirconProtocolProbeBehaviour session;
        [SerializeField] private ZirconMapDebugRenderer mapRenderer;
        [SerializeField] private ZirconWorldDebugRenderer worldRenderer;
        [SerializeField] private ZirconTargetCombatBehaviour combat;
        [SerializeField] private Button selectButton;
        [SerializeField] private Button attackButton;
        [SerializeField] private Button pickupButton;
        [SerializeField] private RectTransform knob;
        [SerializeField] private float deadZonePixels = 24f;
        [SerializeField] private float knobRadiusPixels = 54f;
        [SerializeField] private float runThreshold = 0f;
        [SerializeField] private float repeatSeconds = 0.60f;

        private Vector2 origin;
        private Vector2 direction;
        private float inputStrength;
        private bool dragging;
        private float nextMoveTime;

        private void OnEnable()
        {
            selectButton?.onClick.AddListener(SelectNext);
            attackButton?.onClick.AddListener(Attack);
            pickupButton?.onClick.AddListener(Pickup);
        }

        private void OnDisable()
        {
            selectButton?.onClick.RemoveListener(SelectNext);
            attackButton?.onClick.RemoveListener(Attack);
            pickupButton?.onClick.RemoveListener(Pickup);
            ResetPad();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
                ResetPad();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
                ResetPad();
        }

        private void Update()
        {
            if (!dragging || direction.sqrMagnitude < 0.01f || session == null || !session.IsInGame || Time.unscaledTime < nextMoveTime)
                return;

            byte facing = ToMirDirection(direction);
            int requestedDistance = ZirconMovementRules.ResolveRequestedDistance(
                inputStrength, runThreshold);
            int moveDistance = ResolveMoveDistance(facing, requestedDistance);
            if (moveDistance <= 0)
            {
                ScheduleNextMove();
                return;
            }
            if (worldRenderer != null &&
                !worldRenderer.TryPredictLocalMove(facing, moveDistance))
                return;

            ScheduleNextMove();
            _ = session.SendMoveCommandAsync(facing, moveDistance);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            dragging = true;
            nextMoveTime = Time.unscaledTime;
            origin = eventData.position;
            direction = Vector2.zero;
            UpdatePad(eventData.position);
        }

        public void OnDrag(PointerEventData eventData) => UpdatePad(eventData.position);

        public void OnPointerUp(PointerEventData eventData) => ResetPad();

        private void UpdatePad(Vector2 position)
        {
            Vector2 delta = position - origin;
            float magnitude = delta.magnitude;
            bool outsideDeadZone = magnitude >= Mathf.Max(0f, deadZonePixels);
            direction = outsideDeadZone ? delta.normalized : Vector2.zero;
            inputStrength = outsideDeadZone
                ? Mathf.Clamp01(magnitude / Mathf.Max(1f, knobRadiusPixels))
                : 0f;
            if (knob != null)
                knob.anchoredPosition = Vector2.ClampMagnitude(delta, knobRadiusPixels);
        }

        private void ResetPad()
        {
            dragging = false;
            direction = Vector2.zero;
            inputStrength = 0f;
            if (knob != null)
                knob.anchoredPosition = Vector2.zero;
        }

        private void ScheduleNextMove()
        {
            float interval = Mathf.Max(0.05f, repeatSeconds);
            nextMoveTime = Time.unscaledTime + interval;
        }

        private void SelectNext() => combat?.SelectNextTarget();
        private void Attack() { if (combat != null) _ = combat.AttackSelectedTargetAsync(); }
        private void Pickup() { if (combat != null) _ = combat.PickUpNearbyAsync(); }

        private int ResolveMoveDistance(byte facing, int requestedDistance)
        {
            ZirconWorldSnapshot snapshot = session?.GetWorldSnapshot();
            if (snapshot == null || !snapshot.HasLocalPlayer)
                return requestedDistance;
            Vector2Int originCell = new Vector2Int(snapshot.Location.X, snapshot.Location.Y);
            if (worldRenderer != null &&
                worldRenderer.TryGetLocalPlayerPlannedCell(out Vector2Int plannedCell))
                originCell = plannedCell;
            Vector2Int delta = DirectionToDelta(facing);
            return ZirconMovementRules.ResolveTraversableDistance(
                requestedDistance,
                step =>
                {
                    int x = originCell.x + delta.x * step;
                    int y = originCell.y + delta.y * step;
                    return (mapRenderer != null && mapRenderer.IsBlocking(x, y)) ||
                           ZirconMovementRules.IsOccupiedByBlockingEntity(
                               snapshot.Entities,
                               snapshot.LocalPlayer.ObjectId,
                               x,
                               y);
                });
        }

        private static byte ToMirDirection(Vector2 value)
        {
            float angle = Mathf.Atan2(value.y, value.x) * Mathf.Rad2Deg;
            int result = Mathf.RoundToInt((90f - angle) / 45f) % 8;
            return (byte)(result < 0 ? result + 8 : result);
        }

        private static Vector2Int DirectionToDelta(byte direction)
        {
            switch (direction)
            {
                case 0: return new Vector2Int(0, -1);
                case 1: return new Vector2Int(1, -1);
                case 2: return new Vector2Int(1, 0);
                case 3: return new Vector2Int(1, 1);
                case 4: return new Vector2Int(0, 1);
                case 5: return new Vector2Int(-1, 1);
                case 6: return new Vector2Int(-1, 0);
                case 7: return new Vector2Int(-1, -1);
                default: return Vector2Int.zero;
            }
        }
    }
}
