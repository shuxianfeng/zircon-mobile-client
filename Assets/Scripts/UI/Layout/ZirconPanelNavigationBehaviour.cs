using UnityEngine;
using UnityEngine.UI;

namespace Zircon.Mobile.UI.Layout
{
    public sealed class ZirconPanelNavigationBehaviour : MonoBehaviour
    {
        [SerializeField] private Button[] openButtons;
        [SerializeField] private GameObject[] panels;
        [SerializeField] private Button[] closeButtons;

        private void OnEnable()
        {
            int openCount = Mathf.Min(openButtons?.Length ?? 0, panels?.Length ?? 0);
            for (int i = 0; i < openCount; i++)
            {
                int index = i;
                openButtons[i]?.onClick.AddListener(() => Open(index));
            }

            int closeCount = Mathf.Min(closeButtons?.Length ?? 0, panels?.Length ?? 0);
            for (int i = 0; i < closeCount; i++)
            {
                int index = i;
                closeButtons[i]?.onClick.AddListener(() => Close(index));
            }
        }

        private void OnDisable()
        {
            foreach (Button button in openButtons ?? new Button[0])
                button?.onClick.RemoveAllListeners();
            foreach (Button button in closeButtons ?? new Button[0])
                button?.onClick.RemoveAllListeners();
        }

        public void Open(int index)
        {
            for (int i = 0; i < (panels?.Length ?? 0); i++)
                if (panels[i] != null) panels[i].SetActive(i == index);
        }

        public void Close(int index)
        {
            if (panels != null && index >= 0 && index < panels.Length && panels[index] != null)
                panels[index].SetActive(false);
        }
    }
}
