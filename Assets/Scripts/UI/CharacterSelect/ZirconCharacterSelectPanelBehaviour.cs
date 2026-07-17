using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zircon.Mobile.Core.Protocol;
using Zircon.Mobile.UI.Login;

namespace Zircon.Mobile.UI.CharacterSelect
{
    public sealed class ZirconCharacterSelectPanelBehaviour : MonoBehaviour
    {
        [SerializeField] private ZirconProtocolProbeBehaviour session;
        [SerializeField] private RectTransform content;
        [SerializeField] private Button characterButtonTemplate;
        [SerializeField] private TMP_Text emptyText;

        private readonly List<Button> spawnedButtons = new List<Button>();

        private void OnEnable()
        {
            if (session != null)
            {
                session.CharactersChanged += Rebuild;
                Rebuild(session.Characters);
            }
        }

        private void OnDisable()
        {
            if (session != null)
                session.CharactersChanged -= Rebuild;
            Clear();
        }

        private void Rebuild(IReadOnlyList<ZirconCharacterSelectInfo> characters)
        {
            Clear();
            bool hasCharacters = characters != null && characters.Count > 0;
            if (emptyText != null)
                emptyText.gameObject.SetActive(!hasCharacters);
            if (!hasCharacters || characterButtonTemplate == null || content == null)
                return;

            for (int i = 0; i < characters.Count; i++)
            {
                ZirconCharacterSelectInfo character = characters[i];
                Button button = Instantiate(characterButtonTemplate, content);
                button.gameObject.SetActive(true);
                TMP_Text label = button.GetComponentInChildren<TMP_Text>();
                if (label != null)
                    label.text = $"{character.Name}  Lv.{character.Level}  {ClassName(character.CharacterClass)}";

                int characterIndex = character.Index;
                button.onClick.AddListener(() => _ = session.StartCharacterAsync(characterIndex));
                spawnedButtons.Add(button);
            }
        }

        private void Clear()
        {
            foreach (Button button in spawnedButtons)
            {
                if (button != null)
                    Destroy(button.gameObject);
            }
            spawnedButtons.Clear();
        }

        private static string ClassName(byte characterClass)
        {
            switch (characterClass)
            {
                case 0: return "战士";
                case 1: return "法师";
                case 2: return "道士";
                case 3: return "刺客";
                default: return "未知";
            }
        }
    }
}