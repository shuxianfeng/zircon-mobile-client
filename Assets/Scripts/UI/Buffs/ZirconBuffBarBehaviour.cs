using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zircon.Mobile.Game.Buffs;
using Zircon.Mobile.Game.World;
using Zircon.Mobile.UI.Login;

namespace Zircon.Mobile.UI.Buffs
{
    public sealed class ZirconBuffBarBehaviour : MonoBehaviour
    {
        [SerializeField] private ZirconProtocolProbeBehaviour session;
        [SerializeField] private RectTransform content;
        [SerializeField] private Button buffTemplate;
        [SerializeField] private TMP_Text detailText;

        private readonly List<Entry> entries = new List<Entry>();
        private float nextRefresh;

        private void Update()
        {
            if (Time.unscaledTime < nextRefresh)
                return;
            nextRefresh = Time.unscaledTime + 1f;

            ZirconWorldSnapshot snapshot = session?.GetWorldSnapshot();
            if (snapshot == null)
                return;

            var buffs = new List<ZirconBuffState>(snapshot.Buffs ?? Array.Empty<ZirconBuffState>());
            buffs.Sort((left, right) => left.Index.CompareTo(right.Index));
            EnsureEntryCount(buffs.Count);
            for (int i = 0; i < entries.Count; i++)
            {
                ZirconBuffState buff = buffs[i];
                Entry entry = entries[i];
                entry.Buff = buff;
                if (entry.Label != null)
                {
                    string label = $"{BuffName(buff.Type)}\n{FormatTime(buff.RemainingTime, buff.Paused)}";
                    if (entry.Label.text != label)
                        entry.Label.text = label;
                }
            }
        }

        private void EnsureEntryCount(int count)
        {
            if (content == null || buffTemplate == null)
                return;

            while (entries.Count < count)
            {
                Button button = Instantiate(buffTemplate, content);
                button.gameObject.SetActive(true);
                var entry = new Entry(button, button.GetComponentInChildren<TMP_Text>());
                button.onClick.AddListener(() => ShowDetails(entry.Buff));
                entries.Add(entry);
            }

            while (entries.Count > count)
            {
                int last = entries.Count - 1;
                if (entries[last].Button != null)
                    Destroy(entries[last].Button.gameObject);
                entries.RemoveAt(last);
            }
        }

        private void ShowDetails(ZirconBuffState buff)
        {
            if (detailText == null || buff == null)
                return;

            var lines = new List<string> { BuffName(buff.Type), FormatTime(buff.RemainingTime, buff.Paused) };
            foreach (KeyValuePair<int, int> stat in buff.Stats)
                lines.Add($"{stat.Key}: {stat.Value}");
            detailText.text = string.Join("\n", lines);
        }

        private static string FormatTime(TimeSpan time, bool paused)
        {
            if (time == TimeSpan.MaxValue || time.TotalDays >= 3650)
                return "永久";
            if (time <= TimeSpan.Zero)
                return paused ? "暂停" : string.Empty;
            if (time.TotalHours >= 1)
                return $"{(int)time.TotalHours}:{time.Minutes:00}";
            return $"{(int)time.TotalMinutes}:{time.Seconds:00}";
        }

        private static string BuffName(int type)
        {
            switch (type)
            {
                case 1: return "Server";
                case 2: return "Hunt Gold";
                case 5: return "PK Point";
                case 10: return "Item Buff";
                case 11: return "Permanent";
                case 100: return "Defiance";
                case 101: return "Might";
                case 102: return "Endurance";
                case 103: return "Reflect";
                case 200: return "Renounce";
                case 201: return "Magic Shield";
                case 202: return "Judgement";
                case 300: return "Heal";
                case 301: return "Invisibility";
                case 302: return "Magic Resist";
                case 303: return "Resilience";
                case 305: return "Blood Lust";
                case 306: return "Faith";
                case 309: return "Life Steal";
                case 404: return "Cloak";
                case 405: return "Ghost Walk";
                case 408: return "Dragon Repulse";
                case 409: return "Evasion";
                case 411: return "Frost Bite";
                default: return $"Buff {type}";
            }
        }

        private sealed class Entry
        {
            public Entry(Button button, TMP_Text label) { Button = button; Label = label; }
            public Button Button { get; }
            public TMP_Text Label { get; }
            public ZirconBuffState Buff { get; set; }
        }
    }
}
