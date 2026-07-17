using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zircon.Mobile.Game.World;
using Zircon.Mobile.UI.Login;

namespace Zircon.Mobile.UI.Chat
{
    public sealed class ZirconChatPanelBehaviour : MonoBehaviour
    {
        [SerializeField] private ZirconProtocolProbeBehaviour session;
        [SerializeField] private RectTransform messageContent;
        [SerializeField] private Button messageTemplate;
        [SerializeField] private TMP_InputField input;
        [SerializeField] private TMP_InputField whisperTarget;
        [SerializeField] private TMP_Dropdown channelDropdown;
        [SerializeField] private Button sendButton;
        [SerializeField] private int visibleMessageCount = 40;

        private readonly List<Button> rows = new List<Button>();
        private float nextRefresh;

        private void Awake()
        {
            if (sendButton != null)
                sendButton.onClick.AddListener(SendCurrent);
        }

        private void OnDestroy()
        {
            if (sendButton != null)
                sendButton.onClick.RemoveListener(SendCurrent);
        }

        private void Update()
        {
            if (Time.unscaledTime < nextRefresh)
                return;
            nextRefresh = Time.unscaledTime + 0.2f;
            Refresh(session?.GetWorldSnapshot());
        }

        private void Refresh(ZirconWorldSnapshot snapshot)
        {
            int count = snapshot?.ChatMessages.Count ?? 0;
            int start = count > visibleMessageCount ? count - visibleMessageCount : 0;
            EnsureRows(count - start);

            for (int row = 0; row < rows.Count; row++)
            {
                bool active = row < count - start;
                rows[row].gameObject.SetActive(active);
                if (!active)
                    continue;

                ZirconChatLogEntry entry = snapshot.ChatMessages[start + row];
                TMP_Text label = rows[row].GetComponentInChildren<TMP_Text>();
                if (label != null)
                    label.text = entry.Text;
            }
        }

        private void EnsureRows(int count)
        {
            if (messageContent == null || messageTemplate == null)
                return;
            while (rows.Count < count)
            {
                Button row = Instantiate(messageTemplate, messageContent);
                row.gameObject.SetActive(true);
                rows.Add(row);
            }
        }

        private void SendCurrent()
        {
            string text = input?.text?.Trim();
            if (string.IsNullOrEmpty(text))
                return;

            string payload = BuildPayload(channelDropdown?.value ?? 0, whisperTarget?.text, text);
            if (string.IsNullOrEmpty(payload))
                return;

            _ = session?.SendChatCommandAsync(payload);
            input.text = string.Empty;
        }

        internal static string BuildPayload(int channel, string target, string text)
        {
            switch (channel)
            {
                case 1: return "!!" + text;
                case 2: return "!~" + text;
                case 3: return "!" + text;
                case 4: return "!@" + text;
                case 5:
                    string name = target?.Trim();
                    return string.IsNullOrEmpty(name) ? string.Empty : "/" + name + " " + text;
                default: return text;
            }
        }
    }
}