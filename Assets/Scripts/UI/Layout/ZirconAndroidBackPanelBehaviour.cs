using UnityEngine;
using UnityEngine.EventSystems;

namespace Zircon.Mobile.UI.Layout
{
    /// <summary>
    /// Gives Android's Back action predictable in-game panel navigation.
    /// The soft keyboard consumes the first Back action itself; once it is
    /// hidden, Escape closes the currently visible feature panel.
    /// </summary>
    public sealed class ZirconAndroidBackPanelBehaviour : MonoBehaviour
    {
        [SerializeField] private GameObject[] panels;

        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Escape))
                return;

            for (int i = (panels?.Length ?? 0) - 1; i >= 0; i--)
            {
                GameObject panel = panels[i];
                if (panel == null || !panel.activeInHierarchy)
                    continue;

                if (EventSystem.current != null)
                    EventSystem.current.SetSelectedGameObject(null);
                panel.SetActive(false);
                Debug.Log("Android Back closed panel=" + panel.name);
                return;
            }
        }
    }
}
