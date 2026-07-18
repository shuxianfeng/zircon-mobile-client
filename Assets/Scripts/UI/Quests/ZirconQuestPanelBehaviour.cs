using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zircon.Mobile.Game.Quests;
using Zircon.Mobile.Game.World;
using Zircon.Mobile.UI.Catalog;
using Zircon.Mobile.UI.Login;

namespace Zircon.Mobile.UI.Quests
{
    public sealed class ZirconQuestPanelBehaviour : MonoBehaviour
    {
        [SerializeField] private ZirconProtocolProbeBehaviour session;
        [SerializeField] private ZirconSystemCatalogBehaviour catalog;
        [SerializeField] private RectTransform questContent;
        [SerializeField] private Button questTemplate;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private TMP_InputField rewardChoiceInput;
        [SerializeField] private Button trackButton;
        [SerializeField] private TMP_Text trackButtonText;
        [SerializeField] private Button completeButton;

        private readonly List<QuestCell> cells = new List<QuestCell>();
        private readonly Dictionary<int, bool> pendingTrackStates = new Dictionary<int, bool>();
        private int selectedIndex = -1;
        private float nextRefresh;
        private float nextTrackCommandTime;

        private void Awake()
        {
            trackButton?.onClick.AddListener(ToggleTrack);
            completeButton?.onClick.AddListener(CompleteSelected);
        }

        private void Update()
        {
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + 0.2f;
            Refresh(session?.GetWorldSnapshot());
        }

        private void Refresh(ZirconWorldSnapshot snapshot)
        {
            IReadOnlyList<ZirconQuestState> quests = snapshot?.Quests;
            EnsureRows(quests?.Count ?? 0);
            for (int i = 0; i < cells.Count; i++)
            {
                bool active = quests != null && i < quests.Count;
                cells[i].Button.gameObject.SetActive(active);
                if (!active) continue;
                ZirconQuestState quest = quests[i];
                if (pendingTrackStates.TryGetValue(quest.Index, out bool pendingTrack) && pendingTrack == quest.Track)
                    pendingTrackStates.Remove(quest.Index);
                cells[i].QuestIndex = quest.Index;
                if (cells[i].Label != null)
                    cells[i].Label.text = (catalog?.GetQuest(quest.QuestIndex)?.Name ?? $"Quest #{quest.QuestIndex}") + (quest.Completed ? "\nCompleted" : EffectiveTrack(quest) ? "\nTracked" : string.Empty);
            }

            ZirconQuestState selected = FindQuest(snapshot, selectedIndex);
            if (titleText != null) titleText.text = selected == null ? string.Empty : catalog?.GetQuest(selected.QuestIndex)?.Name ?? $"Quest #{selected.QuestIndex}";
            if (progressText != null) progressText.text = BuildProgress(selected);
            if (trackButton != null) trackButton.interactable = selected != null && !selected.Completed && Time.unscaledTime >= nextTrackCommandTime;
            if (completeButton != null) completeButton.interactable = selected != null && !selected.Completed;
            if (trackButtonText != null) trackButtonText.text = selected != null && EffectiveTrack(selected) ? "Untrack" : "Track";
        }

        private void EnsureRows(int count)
        {
            if (questContent == null || questTemplate == null) return;
            while (cells.Count < count)
            {
                Button button = Instantiate(questTemplate, questContent);
                var cell = new QuestCell(button, button.GetComponentInChildren<TMP_Text>());
                button.onClick.AddListener(() => selectedIndex = cell.QuestIndex);
                cells.Add(cell);
            }
        }

        private void ToggleTrack()
        {
            if (Time.unscaledTime < nextTrackCommandTime) return;
            ZirconQuestState quest = FindQuest(session?.GetWorldSnapshot(), selectedIndex);
            if (quest == null) return;
            bool value = !EffectiveTrack(quest);
            pendingTrackStates[quest.Index] = value;
            nextTrackCommandTime = Time.unscaledTime + .75f;
            _ = session.SendQuestTrackCommandAsync(quest.Index, value);
        }

        private bool EffectiveTrack(ZirconQuestState quest)
        {
            if (quest == null) return false;
            return pendingTrackStates.TryGetValue(quest.Index, out bool value) ? value : quest.Track;
        }

        private void CompleteSelected()
        {
            ZirconQuestState quest = FindQuest(session?.GetWorldSnapshot(), selectedIndex);
            if (quest == null) return;
            int choice = rewardChoiceInput != null && int.TryParse(rewardChoiceInput.text, out int value) ? value : 0;
            _ = session.SendQuestCompleteCommandAsync(quest.QuestIndex, choice);
        }

        private string BuildProgress(ZirconQuestState quest)
        {
            if (quest == null) return string.Empty;
            if (quest.Tasks.Count == 0) return quest.Completed ? "Completed" : "In progress";
            var lines = new System.Text.StringBuilder();
            foreach (ZirconQuestTaskState task in quest.Tasks)
            {
                ZirconSystemCatalogBehaviour.QuestTaskEntry info = catalog?.GetQuestTask(task.TaskIndex);
                string name = !string.IsNullOrEmpty(info?.MobDescription) ? info.MobDescription : info?.ItemInfoIndex > 0 ? catalog?.GetItem(info.ItemInfoIndex)?.Name : $"Task #{task.TaskIndex}";
                lines.Append(name).Append(": ").Append(task.Amount).Append(" / ").Append(info?.Amount ?? 0).AppendLine();
            }
            return lines.ToString().TrimEnd();
        }

        private static ZirconQuestState FindQuest(ZirconWorldSnapshot snapshot, int index)
        {
            if (snapshot == null) return null;
            foreach (ZirconQuestState quest in snapshot.Quests)
                if (quest.Index == index) return quest;
            return null;
        }

        private sealed class QuestCell
        {
            public QuestCell(Button button, TMP_Text label) { Button = button; Label = label; }
            public int QuestIndex { get; set; }
            public Button Button { get; }
            public TMP_Text Label { get; }
        }
    }
}
