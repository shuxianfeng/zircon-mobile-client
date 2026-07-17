using UnityEngine;
using UnityEngine.UI;
using Zircon.Mobile.UI.Login;

namespace Zircon.Mobile.UI.Layout
{
    /// <summary>
    /// Shows the opaque device shell behind login/character selection and hides it
    /// once the live world is active so the world camera remains visible.
    /// </summary>
    public sealed class ZirconAdaptiveDeviceBackgroundBehaviour : MonoBehaviour
    {
        [SerializeField] private ZirconProtocolProbeBehaviour session;
        [SerializeField] private Graphic background;

        private void OnEnable() => Refresh();

        private void Update() => Refresh();

        private void Refresh()
        {
            if (background == null)
                return;

            bool shouldShow = session == null || !session.IsInGame;
            if (background.enabled != shouldShow)
                background.enabled = shouldShow;
        }
    }
}
