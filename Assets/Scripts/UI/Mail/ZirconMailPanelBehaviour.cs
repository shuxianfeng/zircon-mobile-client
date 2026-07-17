using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zircon.Mobile.Core.Protocol;
using Zircon.Mobile.UI.Login;

namespace Zircon.Mobile.UI.Mail
{
    public sealed class ZirconMailPanelBehaviour : MonoBehaviour
    {
        [SerializeField] private ZirconProtocolProbeBehaviour session;
        [SerializeField] private RectTransform mailContent;
        [SerializeField] private Button mailTemplate;
        [SerializeField] private TMP_Text senderText;
        [SerializeField] private TMP_Text subjectText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private TMP_Text attachmentText;
        [SerializeField] private Button collectButton;
        [SerializeField] private Button deleteButton;
        [SerializeField] private TMP_InputField recipientInput;
        [SerializeField] private TMP_InputField composeSubjectInput;
        [SerializeField] private TMP_InputField composeMessageInput;
        [SerializeField] private TMP_InputField goldInput;
        [SerializeField] private TMP_InputField attachmentSlotInput;
        [SerializeField] private TMP_InputField attachmentCountInput;
        [SerializeField] private Button sendButton;

        private readonly List<MailCell> cells = new List<MailCell>();
        private int selectedIndex = -1;
        private float nextRefresh;

        private void Awake()
        {
            collectButton?.onClick.AddListener(CollectSelected);
            deleteButton?.onClick.AddListener(DeleteSelected);
            sendButton?.onClick.AddListener(SendMail);
        }

        private void Update()
        {
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + 0.2f;
            Refresh(session?.GetWorldSnapshot());
        }

        private void Refresh(Zircon.Mobile.Game.World.ZirconWorldSnapshot snapshot)
        {
            IReadOnlyList<ZirconMailInfo> mail = snapshot?.Commerce?.Mail;
            EnsureRows(mail?.Count ?? 0);
            for (int i = 0; i < cells.Count; i++)
            {
                bool active = mail != null && i < mail.Count;
                cells[i].Button.gameObject.SetActive(active);
                if (!active) continue;
                ZirconMailInfo item = mail[i]; cells[i].Index = item.Index;
                if (cells[i].Label != null) cells[i].Label.text = (item.Opened ? string.Empty : "* ") + item.Subject + "\n" + item.Sender;
            }
            ZirconMailInfo? selected = FindMail(mail, selectedIndex);
            if (senderText != null) senderText.text = selected?.Sender ?? string.Empty;
            if (subjectText != null) subjectText.text = selected?.Subject ?? string.Empty;
            if (messageText != null) messageText.text = selected?.Message ?? string.Empty;
            if (attachmentText != null) attachmentText.text = selected.HasValue ? $"Gold {selected.Value.Gold}  Items {selected.Value.Items.Count}" : string.Empty;
            if (collectButton != null) collectButton.interactable = selected.HasValue && (selected.Value.HasItem || selected.Value.Gold > 0);
            if (deleteButton != null) deleteButton.interactable = selected.HasValue;
        }

        private void EnsureRows(int count)
        {
            if (mailContent == null || mailTemplate == null) return;
            while (cells.Count < count)
            {
                Button button = Instantiate(mailTemplate, mailContent); var cell = new MailCell(button, button.GetComponentInChildren<TMP_Text>());
                button.onClick.AddListener(() => SelectMail(cell.Index)); cells.Add(cell);
            }
        }

        private void SelectMail(int index)
        {
            selectedIndex = index;
            Send(ZirconClientPackets.MailOpened(index), "mail opened");
        }

        private void CollectSelected()
        {
            ZirconMailInfo? mail = FindMail(session?.GetWorldSnapshot()?.Commerce?.Mail, selectedIndex);
            if (!mail.HasValue) return;
            if (mail.Value.Items.Count == 0) Send(ZirconClientPackets.MailGetItem(mail.Value.Index, -1), "mail gold");
            else for (int slot = 0; slot < mail.Value.Items.Count; slot++) Send(ZirconClientPackets.MailGetItem(mail.Value.Index, slot), "mail item");
        }

        private void DeleteSelected() { if (selectedIndex >= 0) Send(ZirconClientPackets.MailDelete(selectedIndex), "mail delete"); }

        private void SendMail()
        {
            long gold = goldInput != null && long.TryParse(goldInput.text, out long parsedGold) ? System.Math.Max(0, parsedGold) : 0;
            var links = new List<ZirconCellLinkInfo>();
            if (attachmentSlotInput != null && int.TryParse(attachmentSlotInput.text, out int slot))
            {
                long count = attachmentCountInput != null && long.TryParse(attachmentCountInput.text, out long parsedCount) ? System.Math.Max(1, parsedCount) : 1;
                links.Add(new ZirconCellLinkInfo(ZirconGridType.Inventory, slot, count));
            }
            Send(ZirconClientPackets.MailSend(links, recipientInput?.text, composeSubjectInput?.text, composeMessageInput?.text, gold), "mail send");
        }

        private void Send(byte[] packet, string label) => _ = session?.SendGamePacketCommandAsync(packet, label);
        private static ZirconMailInfo? FindMail(IReadOnlyList<ZirconMailInfo> mail, int index) { if (mail != null) foreach (ZirconMailInfo item in mail) if (item.Index == index) return item; return null; }
        private sealed class MailCell { public MailCell(Button button, TMP_Text label) { Button = button; Label = label; } public int Index { get; set; } public Button Button { get; } public TMP_Text Label { get; } }
    }
}