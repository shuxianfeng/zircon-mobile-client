using UnityEngine;
using Zircon.Mobile.Game.World;
using Zircon.Mobile.UI.Login;

namespace Zircon.Mobile.Game.Input
{
    public sealed class ZirconTouchMovementBehaviour : MonoBehaviour
    {
        [SerializeField] private ZirconProtocolProbeBehaviour protocolProbe;
        [SerializeField] private ZirconMapDebugRenderer mapRenderer;
        [SerializeField] private bool enableKeyboardInEditor = true;
        [SerializeField] private bool respectLocalBlocking = true;
        [SerializeField] private float touchDeadZonePixels = 42f;
        [SerializeField] private float repeatSeconds = 0.28f;
        [SerializeField] private int moveDistance = 1;

        private int activeFingerId = -1;
        private Vector2 touchStart;
        private float nextSendTime;

        private void Update()
        {
            if (protocolProbe == null || !protocolProbe.IsInGame)
                return;

            if (TryGetKeyboardVector(out Vector2 keyboardVector))
            {
                TrySendMove(keyboardVector);
                return;
            }

            if (TryGetTouchVector(out Vector2 touchVector))
                TrySendMove(touchVector);
        }

        private bool TryGetKeyboardVector(out Vector2 direction)
        {
            direction = Vector2.zero;
            if (!enableKeyboardInEditor)
                return false;

            if (UnityEngine.Input.GetKey(KeyCode.W) || UnityEngine.Input.GetKey(KeyCode.UpArrow))
                direction.y += 1f;
            if (UnityEngine.Input.GetKey(KeyCode.S) || UnityEngine.Input.GetKey(KeyCode.DownArrow))
                direction.y -= 1f;
            if (UnityEngine.Input.GetKey(KeyCode.D) || UnityEngine.Input.GetKey(KeyCode.RightArrow))
                direction.x += 1f;
            if (UnityEngine.Input.GetKey(KeyCode.A) || UnityEngine.Input.GetKey(KeyCode.LeftArrow))
                direction.x -= 1f;

            return direction.sqrMagnitude > 0.01f;
        }

        private bool TryGetTouchVector(out Vector2 direction)
        {
            direction = Vector2.zero;
            if (UnityEngine.Input.touchCount == 0)
            {
                activeFingerId = -1;
                return false;
            }

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

                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    activeFingerId = -1;
                    return false;
                }

                Vector2 delta = touch.position - touchStart;
                if (delta.magnitude < touchDeadZonePixels)
                    return false;

                direction = delta.normalized;
                return true;
            }

            return false;
        }

        private void TrySendMove(Vector2 direction)
        {
            if (Time.time < nextSendTime)
                return;

            nextSendTime = Time.time + repeatSeconds;
            byte mirDirection = ToMirDirection(direction);
            if (!CanMoveTo(mirDirection))
                return;

            _ = protocolProbe.SendMoveCommandAsync(mirDirection, moveDistance);
        }

        private bool CanMoveTo(byte mirDirection)
        {
            if (!respectLocalBlocking || mapRenderer == null)
                return true;

            ZirconWorldSnapshot snapshot = protocolProbe.GetWorldSnapshot();
            if (snapshot == null || !snapshot.HasLocalPlayer)
                return true;

            Vector2Int delta = DirectionToDelta(mirDirection);
            int targetX = snapshot.Location.X + delta.x * moveDistance;
            int targetY = snapshot.Location.Y + delta.y * moveDistance;
            return !mapRenderer.IsBlocking(targetX, targetY);
        }

        private static byte ToMirDirection(Vector2 direction)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            int value = Mathf.RoundToInt((90f - angle) / 45f);
            value %= 8;
            if (value < 0)
                value += 8;

            return (byte)value;
        }

        private static Vector2Int DirectionToDelta(byte direction)
        {
            switch (direction)
            {
                case 0:
                    return new Vector2Int(0, -1);
                case 1:
                    return new Vector2Int(1, -1);
                case 2:
                    return new Vector2Int(1, 0);
                case 3:
                    return new Vector2Int(1, 1);
                case 4:
                    return new Vector2Int(0, 1);
                case 5:
                    return new Vector2Int(-1, 1);
                case 6:
                    return new Vector2Int(-1, 0);
                case 7:
                    return new Vector2Int(-1, -1);
                default:
                    return Vector2Int.zero;
            }
        }
    }
}
