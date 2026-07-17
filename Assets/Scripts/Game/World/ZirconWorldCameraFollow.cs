using UnityEngine;

namespace Zircon.Mobile.Game.World
{
    public sealed class ZirconWorldCameraFollow : MonoBehaviour
    {
        [SerializeField] private ZirconWorldDebugRenderer worldRenderer;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);
        [SerializeField] private float smoothTime = 0.12f;

        private Vector3 velocity;

        private void LateUpdate()
        {
            if (worldRenderer == null)
                return;

            Camera cameraToMove = targetCamera != null ? targetCamera : Camera.main;
            if (cameraToMove == null)
                return;

            if (!worldRenderer.TryGetLocalPlayerWorldPosition(out Vector3 playerPosition))
                return;

            Vector3 target = playerPosition + offset;
            cameraToMove.transform.position = Vector3.SmoothDamp(cameraToMove.transform.position, target, ref velocity, smoothTime);
        }
    }
}
