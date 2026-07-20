using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zircon.Mobile.Core.Protocol;
using Zircon.Mobile.Game.Entities;
using Zircon.Mobile.Game.Input;
using Zircon.Mobile.Game.World;
using Zircon.Mobile.UI.Login;

namespace Zircon.Mobile.Game.Skills
{
    [RequireComponent(typeof(Button))]
    public sealed class ZirconMobileSkillButtonBehaviour : MonoBehaviour
    {
        [SerializeField] private ZirconProtocolProbeBehaviour protocolProbe;
        [SerializeField] private ZirconTargetCombatBehaviour targetCombat;
        [SerializeField] private Button button;
        [SerializeField] private Image cooldownOverlay;
        [SerializeField] private TMP_Text cooldownText;
        [SerializeField] private int magicType = 111;
        [SerializeField] private int magicMode = 1;
        [SerializeField] private int cooldownMilliseconds;
        [SerializeField] private byte fallbackDirection;

        private float cooldownEndsAt;
        private bool sending;
        private bool configured;

        public int MagicType => magicType;
        public bool IsConfigured => configured;
        public bool IsReady => configured && !sending && Time.unscaledTime >= cooldownEndsAt;

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            button?.onClick.AddListener(OnPressed);
        }

        private void OnDisable()
        {
            button?.onClick.RemoveListener(OnPressed);
            sending = false;
        }

        private void Update()
        {
            float remaining = Mathf.Max(0f, cooldownEndsAt - Time.unscaledTime);
            float duration = Mathf.Max(0.001f, cooldownMilliseconds / 1000f);

            if (cooldownOverlay != null)
                cooldownOverlay.fillAmount = Mathf.Clamp01(remaining / duration);

            if (cooldownText != null)
                cooldownText.text = remaining > 0f ? Mathf.CeilToInt(remaining).ToString() : string.Empty;

            if (button != null)
                button.interactable = protocolProbe != null && protocolProbe.IsInGame && IsReady;
        }

        public void Configure(int type, int mode, int delayMilliseconds)
        {
            magicType = type;
            magicMode = Mathf.Clamp(mode, 0, 4);
            cooldownMilliseconds = Mathf.Max(0, delayMilliseconds);
            configured = type > 0;
        }

        public void ClearConfiguration()
        {
            configured = false;
            magicType = 0;
            magicMode = 0;
            cooldownMilliseconds = 0;
            cooldownEndsAt = 0f;
        }

        public async Task CastAsync()
        {
            if (!configured || !IsReady || protocolProbe == null || !protocolProbe.IsInGame)
                return;

            ZirconWorldSnapshot snapshot = protocolProbe.GetWorldSnapshot();
            if (snapshot == null || !snapshot.HasLocalPlayer)
                return;

            uint targetId = 0;
            ZirconMapPoint targetLocation = snapshot.Location;
            byte direction = fallbackDirection;

            if (magicMode == 2 && !TryGetSelectedTarget(snapshot, out ZirconEntityState selected))
                return;

            if (targetCombat != null && targetCombat.HasSelectedObject && TryGetSelectedTarget(snapshot, out selected))
            {
                targetId = selected.ObjectId;
                targetLocation = selected.Location;
                direction = DirectionFromPoints(snapshot.Location, selected.Location);
            }

            sending = true;
            try
            {
                await protocolProbe.SendMagicCommandAsync(direction, magicType, targetId, targetLocation);
                cooldownEndsAt = Time.unscaledTime + cooldownMilliseconds / 1000f;
            }
            finally
            {
                sending = false;
            }
        }

        private void OnPressed()
        {
            _ = CastAsync();
        }

        private bool TryGetSelectedTarget(ZirconWorldSnapshot snapshot, out ZirconEntityState target)
        {
            target = null;
            if (targetCombat == null || !targetCombat.HasSelectedObject)
                return false;

            foreach (ZirconEntityState entity in snapshot.Entities)
            {
                if (entity.ObjectId != targetCombat.SelectedObjectId || entity.Dead)
                    continue;

                target = entity;
                return true;
            }

            return false;
        }

        private static byte DirectionFromPoints(ZirconMapPoint origin, ZirconMapPoint target)
        {
            int x = target.X.CompareTo(origin.X);
            int y = target.Y.CompareTo(origin.Y);

            if (x == 0 && y < 0) return 0;
            if (x > 0 && y < 0) return 1;
            if (x > 0 && y == 0) return 2;
            if (x > 0 && y > 0) return 3;
            if (x == 0 && y > 0) return 4;
            if (x < 0 && y > 0) return 5;
            if (x < 0 && y == 0) return 6;
            if (x < 0 && y < 0) return 7;
            return 0;
        }
    }
}
