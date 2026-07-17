using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zircon.Mobile.Game.Skills;
using Zircon.Mobile.Game.World;
using Zircon.Mobile.UI.Catalog;
using Zircon.Mobile.UI.Login;

namespace Zircon.Mobile.UI.Skills
{
    public sealed class ZirconSkillBookPanelBehaviour : MonoBehaviour
    {
        [SerializeField] private ZirconProtocolProbeBehaviour session;
        [SerializeField] private ZirconSystemCatalogBehaviour catalog;
        [SerializeField] private RectTransform skillContent;
        [SerializeField] private Button skillTemplate;
        [SerializeField] private Button[] hotbarAssignButtons;
        [SerializeField] private ZirconMobileSkillButtonBehaviour[] hotbarCasters;
        [SerializeField] private TMP_Text selectedNameText;
        [SerializeField] private TMP_Text selectedDetailText;
        [SerializeField] private Color selectedColor = new Color(0.95f, 0.72f, 0.18f, 1f);

        private readonly List<SkillCell> cells = new List<SkillCell>();
        private int selectedInfoIndex = -1;
        private float nextRefresh;

        private void Awake()
        {
            for (int index = 0; hotbarAssignButtons != null && index < hotbarAssignButtons.Length; index++)
            {
                int captured = index;
                hotbarAssignButtons[index]?.onClick.AddListener(() => _ = BindSelectedAsync(captured));
            }
        }

        private void OnDestroy()
        {
            foreach (SkillCell cell in cells)
                cell.Button.onClick.RemoveListener(cell.SelectAction);
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
            IReadOnlyList<ZirconSkillState> skills = snapshot?.Skills;
            EnsureCells(skills?.Count ?? 0);
            for (int index = 0; index < cells.Count; index++)
            {
                bool active = skills != null && index < skills.Count;
                SkillCell cell = cells[index];
                cell.Button.gameObject.SetActive(active);
                if (!active)
                    continue;

                ZirconSkillState skill = skills[index];
                cell.InfoIndex = skill.InfoIndex;
                ZirconSystemCatalogBehaviour.MagicEntry info = catalog?.GetMagic(skill.InfoIndex);
                if (cell.Label != null)
                    cell.Label.text = $"{info?.Name ?? $"#{skill.InfoIndex}"}\nLv.{skill.Level}";
                ColorBlock colors = cell.BaseColors;
                if (skill.InfoIndex == selectedInfoIndex)
                    colors.normalColor = selectedColor;
                cell.Button.colors = colors;
            }

            ZirconSkillState selected = FindSkill(snapshot, selectedInfoIndex);
            ZirconSystemCatalogBehaviour.MagicEntry selectedInfo = selected == null ? null : catalog?.GetMagic(selected.InfoIndex);
            if (selectedNameText != null)
                selectedNameText.text = selectedInfo?.Name ?? string.Empty;
            if (selectedDetailText != null)
                selectedDetailText.text = selected == null ? string.Empty : $"Lv.{selected.Level}  EXP {selected.Experience}\n{selectedInfo?.Description ?? string.Empty}";

            RefreshHotbar(snapshot);
        }

        private void EnsureCells(int count)
        {
            if (skillContent == null || skillTemplate == null)
                return;
            while (cells.Count < count)
            {
                Button button = Instantiate(skillTemplate, skillContent);
                var cell = new SkillCell(button, button.GetComponentInChildren<TMP_Text>(), button.colors);
                cell.SelectAction = () => selectedInfoIndex = cell.InfoIndex;
                button.onClick.AddListener(cell.SelectAction);
                button.gameObject.SetActive(true);
                cells.Add(cell);
            }
        }

        private void RefreshHotbar(ZirconWorldSnapshot snapshot)
        {
            if (hotbarAssignButtons == null)
                return;
            for (int index = 0; index < hotbarAssignButtons.Length; index++)
            {
                byte key = (byte)(index + 1);
                ZirconSkillState skill = FindSkillByKey(snapshot, key);
                ZirconSystemCatalogBehaviour.MagicEntry info = skill == null ? null : catalog?.GetMagic(skill.InfoIndex);
                TMP_Text label = hotbarAssignButtons[index]?.GetComponentInChildren<TMP_Text>();
                if (label != null)
                    label.text = info?.Name ?? string.Empty;
                if (hotbarCasters != null && index < hotbarCasters.Length && hotbarCasters[index] != null && info != null)
                    hotbarCasters[index].Configure(info.MagicType, info.Mode, info.Delay);
            }
        }

        private async Task BindSelectedAsync(int hotbarIndex)
        {
            ZirconWorldSnapshot snapshot = session?.GetWorldSnapshot();
            ZirconSkillState selected = FindSkill(snapshot, selectedInfoIndex);
            ZirconSystemCatalogBehaviour.MagicEntry info = selected == null ? null : catalog?.GetMagic(selected.InfoIndex);
            if (selected == null || info == null || hotbarIndex < 0 || hotbarIndex > 3)
                return;

            byte key = (byte)(hotbarIndex + 1);
            ZirconSkillState occupied = FindSkillByKey(snapshot, key);
            if (occupied != null && occupied.InfoIndex != selected.InfoIndex)
            {
                ZirconSystemCatalogBehaviour.MagicEntry occupiedInfo = catalog?.GetMagic(occupied.InfoIndex);
                if (occupiedInfo != null)
                    await session.SendMagicKeyCommandAsync(occupied.InfoIndex, occupiedInfo.MagicType, 0, occupied.Set2Key, occupied.Set3Key, occupied.Set4Key);
            }

            await session.SendMagicKeyCommandAsync(selected.InfoIndex, info.MagicType, key, selected.Set2Key, selected.Set3Key, selected.Set4Key);
        }

        private static ZirconSkillState FindSkill(ZirconWorldSnapshot snapshot, int infoIndex)
        {
            if (snapshot == null)
                return null;
            foreach (ZirconSkillState skill in snapshot.Skills)
                if (skill.InfoIndex == infoIndex) return skill;
            return null;
        }

        private static ZirconSkillState FindSkillByKey(ZirconWorldSnapshot snapshot, byte key)
        {
            if (snapshot == null)
                return null;
            foreach (ZirconSkillState skill in snapshot.Skills)
                if (skill.Set1Key == key) return skill;
            return null;
        }

        private sealed class SkillCell
        {
            public SkillCell(Button button, TMP_Text label, ColorBlock colors)
            { Button = button; Label = label; BaseColors = colors; }
            public int InfoIndex { get; set; }
            public Button Button { get; }
            public TMP_Text Label { get; }
            public ColorBlock BaseColors { get; }
            public UnityEngine.Events.UnityAction SelectAction { get; set; }
        }
    }
}