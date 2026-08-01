using System;
using System.Collections.Generic;
using Zircon.Mobile.Game.Entities;

namespace Zircon.Mobile.Game.Input
{
    /// <summary>
    /// Pure movement policy shared by the mobile controls and offline tests.
    /// Zircon walking and running use the same packet; distance 1 walks and
    /// distance 2 runs. A blocked second cell safely falls back to walking.
    /// </summary>
    public static class ZirconMovementRules
    {
        public const int WalkDistance = 1;
        public const int RunDistance = 2;

        public static int ResolveRequestedDistance(float strength, float runThreshold)
        {
            float threshold = Math.Max(0.01f, Math.Min(1f, runThreshold));
            return strength >= threshold ? RunDistance : WalkDistance;
        }

        public static int ResolveTraversableDistance(
            int requestedDistance,
            Func<int, bool> isStepBlocked)
        {
            int requested = Math.Max(0, Math.Min(RunDistance, requestedDistance));
            if (requested == 0 || isStepBlocked == null)
                return requested;

            int traversable = 0;
            for (int step = 1; step <= requested; step++)
            {
                if (isStepBlocked(step))
                    break;
                traversable = step;
            }
            return traversable;
        }

        public static bool IsOccupiedByBlockingEntity(
            IEnumerable<ZirconEntityState> entities,
            uint localPlayerObjectId,
            int x,
            int y)
        {
            if (entities == null)
                return false;

            foreach (ZirconEntityState entity in entities)
            {
                if (entity == null || entity.Dead || entity.ObjectId == localPlayerObjectId ||
                    entity.Location.X != x || entity.Location.Y != y)
                    continue;

                if (entity.Kind == ZirconEntityKind.Player ||
                    entity.Kind == ZirconEntityKind.Monster ||
                    entity.Kind == ZirconEntityKind.Npc)
                    return true;
            }

            return false;
        }
    }
}
