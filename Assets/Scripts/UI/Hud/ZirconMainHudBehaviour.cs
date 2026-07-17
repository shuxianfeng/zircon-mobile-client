using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zircon.Mobile.Game.World;
using Zircon.Mobile.UI.Login;

namespace Zircon.Mobile.UI.HUD
{
    public sealed class ZirconMainHudBehaviour : MonoBehaviour
    {
        [SerializeField] private ZirconProtocolProbeBehaviour session;
        [SerializeField] private TMP_Text nameLevelText;
        [SerializeField] private TMP_Text healthText;
        [SerializeField] private TMP_Text manaText;
        [SerializeField] private TMP_Text currencyText;
        [SerializeField] private TMP_Text locationText;
        [SerializeField] private Slider healthBar;
        [SerializeField] private Slider manaBar;
        [SerializeField] private GameObject inGameRoot;

        private void Update()
        {
            ZirconWorldSnapshot snapshot = session?.GetWorldSnapshot();
            bool visible = snapshot != null && snapshot.HasLocalPlayer && snapshot.LocalPlayer != null;
            if (inGameRoot != null && inGameRoot.activeSelf != visible)
                inGameRoot.SetActive(visible);
            if (!visible)
                return;

            int health = snapshot.LocalPlayer.Health;
            int maxHealth = snapshot.LocalPlayer.MaxHealth;
            int mana = snapshot.LocalPlayer.Mana;
            int maxMana = snapshot.LocalPlayer.MaxMana;

            SetText(nameLevelText, $"{snapshot.LocalPlayer.Name}  Lv.{snapshot.LocalPlayer.Level}");
            SetText(healthText, $"{health}/{maxHealth}");
            SetText(manaText, $"{mana}/{maxMana}");
            SetText(currencyText, $"{snapshot.Gold:N0}");
            SetText(locationText, $"{snapshot.MapIndex}  {snapshot.Location.X},{snapshot.Location.Y}");

            SetBar(healthBar, health, maxHealth);
            SetBar(manaBar, mana, maxMana);
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null)
                target.text = value;
        }

        private static void SetBar(Slider bar, int value, int maximum)
        {
            if (bar == null)
                return;

            bar.minValue = 0f;
            bar.maxValue = Mathf.Max(1, maximum);
            bar.value = Mathf.Clamp(value, 0, Mathf.Max(1, maximum));
        }
    }
}