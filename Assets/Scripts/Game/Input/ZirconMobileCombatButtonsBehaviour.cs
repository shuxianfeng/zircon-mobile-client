using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Zircon.Mobile.Game.Entities;
using Zircon.Mobile.Game.World;
using Zircon.Mobile.UI.Login;

namespace Zircon.Mobile.Game.Input
{
    /// <summary>
    /// Owns the explicit mobile target/attack buttons. It runs after the generic
    /// world-touch handler so a UI touch cannot clear the target selected by the
    /// same pointer release.
    /// </summary>
    [DefaultExecutionOrder(1000)]
    public sealed class ZirconMobileCombatButtonsBehaviour : MonoBehaviour
    {
        [SerializeField] private ZirconProtocolProbeBehaviour session;
        [SerializeField] private ZirconTargetCombatBehaviour targetCombat;
        [SerializeField] private ZirconWorldDebugRenderer worldRenderer;
        [SerializeField] private Button selectButton;
        [SerializeField] private Button attackButton;

        private uint selectedObjectId;
        private bool hasSelectedObject;
        private float nextAttackTime;

        private void OnEnable()
        {
            selectButton?.onClick.AddListener(SelectNext);
            attackButton?.onClick.AddListener(Attack);
        }

        private void OnDisable()
        {
            selectButton?.onClick.RemoveListener(SelectNext);
            attackButton?.onClick.RemoveListener(Attack);
            hasSelectedObject = false;
        }

        private void Update()
        {
            ZirconWorldSnapshot snapshot = session?.GetWorldSnapshot();
            if (!TryGetSelected(snapshot, out _))
            {
                hasSelectedObject = false;
                return;
            }

            // The generic handler may see the same UI pointer release as a world
            // tap and clear itself. Restore it after that handler has updated so
            // targeted skills and the selection highlight stay in sync.
            if (targetCombat != null &&
                (!targetCombat.HasSelectedObject || targetCombat.SelectedObjectId != selectedObjectId) &&
                targetCombat.SelectNextTarget())
            {
                selectedObjectId = targetCombat.SelectedObjectId;
            }

            worldRenderer?.SetSelectedObject(selectedObjectId);
        }

        private void SelectNext()
        {
            if (targetCombat == null || !targetCombat.SelectNextTarget())
                return;

            selectedObjectId = targetCombat.SelectedObjectId;
            hasSelectedObject = true;
            worldRenderer?.SetSelectedObject(selectedObjectId);
        }

        private void Attack()
        {
            if (Time.unscaledTime < nextAttackTime)
                return;

            ZirconWorldSnapshot snapshot = session?.GetWorldSnapshot();
            if (!TryGetSelected(snapshot, out ZirconEntityState target))
                return;

            nextAttackTime = Time.unscaledTime + .45f;
            byte direction = DirectionFromPoints(snapshot.Location, target.Location);
            _ = SendAttackAsync(direction);
        }

        private async Task SendAttackAsync(byte direction)
        {
            if (session != null)
                await session.SendAttackCommandAsync(direction);
        }

        private bool TryGetSelected(ZirconWorldSnapshot snapshot, out ZirconEntityState selected)
        {
            selected = null;
            if (!hasSelectedObject || snapshot == null)
                return false;

            foreach (ZirconEntityState entity in snapshot.Entities)
            {
                if (entity.ObjectId != selectedObjectId || entity.Dead)
                    continue;
                if (entity.Kind != ZirconEntityKind.Monster && entity.Kind != ZirconEntityKind.Player)
                    continue;
                if (snapshot.LocalPlayer != null && entity.ObjectId == snapshot.LocalPlayer.ObjectId)
                    continue;
                selected = entity;
                return true;
            }

            return false;
        }

        private static byte DirectionFromPoints(Zircon.Mobile.Core.Protocol.ZirconMapPoint origin, Zircon.Mobile.Core.Protocol.ZirconMapPoint target)
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
