using UnityEngine;
using UnityEngine.EventSystems;
using Zircon.Mobile.Game.World;
using Zircon.Mobile.UI.Login;

namespace Zircon.Mobile.Game.Input
{
    /// <summary>
    /// Fixed-position mobile joystick. Direction is measured from the visible
    /// pad centre, so the first touch is meaningful and does not depend on a
    /// child Image forwarding drag events in a particular way.
    /// </summary>
    public sealed class ZirconFixedCenterJoystickBehaviour : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private ZirconProtocolProbeBehaviour session;
        [SerializeField] private ZirconMapDebugRenderer mapRenderer;
        [SerializeField] private ZirconWorldDebugRenderer worldRenderer;
        [SerializeField] private RectTransform pad;
        [SerializeField] private RectTransform knob;
        [SerializeField] private float deadZonePixels = 18f;
        [SerializeField] private float knobRadiusPixels = 54f;
        // Mobile movement defaults to running once the stick leaves its dead zone.
        // A positive threshold can still be configured later for an optional
        // walk/run analogue mode.
        [SerializeField] private float runThreshold = 0f;
        [SerializeField] private float repeatSeconds = 0.60f;

        private Vector2 direction;
        private float inputStrength;
        private bool held;
        private float nextMove;

        public void OnPointerDown(PointerEventData eventData)
        {
            held = true;
            UpdateDirection(eventData);
            nextMove = 0f;
            TryMove();
        }

        public void OnDrag(PointerEventData eventData)
        {
            UpdateDirection(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            ResetPad();
        }

        private void OnDisable()
        {
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

        private void ResetPad()
        {
            held = false;
            direction = Vector2.zero;
            inputStrength = 0f;
            if (knob != null) knob.anchoredPosition = Vector2.zero;
        }

        private void Update()
        {
            if (held) TryMove();
        }

        private void UpdateDirection(PointerEventData eventData)
        {
            if (pad == null) return;
            Camera camera = eventData.pressEventCamera;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(pad, eventData.position, camera, out Vector2 local))
                return;
            float magnitude = local.magnitude;
            bool outsideDeadZone = magnitude >= Mathf.Max(0f, deadZonePixels);
            direction = outsideDeadZone ? local.normalized : Vector2.zero;
            inputStrength = outsideDeadZone
                ? Mathf.Clamp01(magnitude / Mathf.Max(1f, knobRadiusPixels))
                : 0f;
            if (knob != null) knob.anchoredPosition = Vector2.ClampMagnitude(local, knobRadiusPixels);
        }

        private void TryMove()
        {
            if (direction.sqrMagnitude < 0.01f || session == null || !session.IsInGame || Time.unscaledTime < nextMove)
                return;
            byte facing = ToMirDirection(direction);
            int requestedDistance = ZirconMovementRules.ResolveRequestedDistance(
                inputStrength, runThreshold);
            int moveDistance = ResolveMoveDistance(facing, requestedDistance);
            if (moveDistance <= 0)
            {
                nextMove = Time.unscaledTime + Mathf.Max(0.05f, repeatSeconds);
                return;
            }
            if (worldRenderer != null &&
                !worldRenderer.TryPredictLocalMove(facing, moveDistance))
                return;
            ScheduleNextMove();
            _ = session.SendMoveCommandAsync(facing, moveDistance);
        }

        private int ResolveMoveDistance(byte facing, int requestedDistance)
        {
            ZirconWorldSnapshot snapshot = session?.GetWorldSnapshot();
            if (snapshot == null || !snapshot.HasLocalPlayer)
                return requestedDistance;
            Vector2Int origin = new Vector2Int(snapshot.Location.X, snapshot.Location.Y);
            if (worldRenderer != null &&
                worldRenderer.TryGetLocalPlayerPlannedCell(out Vector2Int plannedCell))
                origin = plannedCell;
            Vector2Int delta = DirectionToDelta(facing);
            return ZirconMovementRules.ResolveTraversableDistance(
                requestedDistance,
                step =>
                {
                    int x = origin.x + delta.x * step;
                    int y = origin.y + delta.y * step;
                    return (mapRenderer != null && mapRenderer.IsBlocking(x, y)) ||
                           ZirconMovementRules.IsOccupiedByBlockingEntity(
                               snapshot.Entities,
                               snapshot.LocalPlayer.ObjectId,
                               x,
                               y);
                });
        }

        private void ScheduleNextMove()
        {
            float interval = Mathf.Max(0.05f, repeatSeconds);
            nextMove = Time.unscaledTime + interval;
        }

        private static byte ToMirDirection(Vector2 value)
        {
            float angle = Mathf.Atan2(value.y, value.x) * Mathf.Rad2Deg;
            int result = Mathf.RoundToInt((90f - angle) / 45f) % 8;
            return (byte)(result < 0 ? result + 8 : result);
        }

        private static Vector2Int DirectionToDelta(byte value)
        {
            switch (value)
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
