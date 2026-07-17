using UnityEngine;
using Zircon.Mobile.Core.Network;
using Zircon.Mobile.UI.Login;

namespace Zircon.Mobile.UI
{
    public sealed class ZirconUiFlowBehaviour : MonoBehaviour
    {
        [SerializeField] private ZirconProtocolProbeBehaviour session;
        [SerializeField] private GameObject loginRoot;
        [SerializeField] private GameObject characterSelectRoot;
        [SerializeField] private GameObject hudRoot;

        private void OnEnable()
        {
            if (session != null)
                session.ConnectionStateChanged += Apply;
            Apply(session?.ConnectionState ?? ZirconConnectionState.Disconnected);
        }

        private void OnDisable()
        {
            if (session != null)
                session.ConnectionStateChanged -= Apply;
        }

        private void Update()
        {
            if (session != null)
                Apply(session.ConnectionState);
        }

        private void Apply(ZirconConnectionState state)
        {
            bool selecting = state == ZirconConnectionState.SelectingCharacter;
            bool inGame = state == ZirconConnectionState.LoadingMap || state == ZirconConnectionState.InGame;
            SetActive(loginRoot, !selecting && !inGame);
            SetActive(characterSelectRoot, selecting);
            SetActive(hudRoot, inGame);
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
                target.SetActive(active);
        }
    }
}