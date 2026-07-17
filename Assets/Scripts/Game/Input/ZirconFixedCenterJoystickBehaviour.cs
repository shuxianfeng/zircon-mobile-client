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
        [SerializeField] private RectTransform pad;
        [SerializeField] private RectTransform knob;
        [SerializeField] private float deadZonePixels = 18f;
        [SerializeField] private float knobRadiusPixels = 54f;
        [SerializeField] private float repeatSeconds = 0.30f;

        private Vector2 direction;
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
            held = false;
            direction = Vector2.zero;
            if (knob != null) knob.anchoredPosition = Vector2.zero;
            Debug.Log("P0 joystick released");
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
            direction = local.magnitude < deadZonePixels ? Vector2.zero : local.normalized;
            if (knob != null) knob.anchoredPosition = Vector2.ClampMagnitude(local, knobRadiusPixels);
        }

        private void TryMove()
        {
            if (direction.sqrMagnitude < 0.01f || session == null || !session.IsInGame || Time.unscaledTime < nextMove)
                return;
            nextMove = Time.unscaledTime + repeatSeconds;
            byte facing = ToMirDirection(direction);
            ZirconWorldSnapshot snapshot = session.GetWorldSnapshot();
            if (snapshot == null || !snapshot.HasLocalPlayer)
                return;
            Vector2Int delta = DirectionToDelta(facing);
            int x = snapshot.Location.X + delta.x;
            int y = snapshot.Location.Y + delta.y;
            if (mapRenderer != null && mapRenderer.IsBlocking(x, y))
            {
                Debug.Log($"P0 joystick blocked direction={facing} target={x},{y}");
                return;
            }
            Debug.Log($"P0 joystick move direction={facing} from={snapshot.Location.X},{snapshot.Location.Y} to={x},{y}");
            _ = session.SendMoveCommandAsync(facing, 1);
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
