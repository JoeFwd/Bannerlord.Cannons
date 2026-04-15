using System;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.Cannons.BattleMechanics.AI.CommonAIFunctions
{
    /// <summary>
    /// General-purpose AI helper methods that do not belong to a specific decision
    /// or state domain.
    /// </summary>
    public static class CommonAIUtilities
    {
        private static readonly Random _random = new();

        /// <summary>
        /// Returns an agent near a random position within the target formation, biased
        /// toward the formation's median agent. The random offset spans the full depth
        /// and 90 % of the width so the sampled position stays plausibly inside the
        /// formation boundary.
        ///
        /// Returns <c>null</c> if the formation has no valid median agent.
        /// </summary>
        public static Agent? GetRandomAgent(Formation targetFormation)
        {
            Vec2 averagePos = targetFormation.GetAveragePositionOfUnits(true, false);
            Agent? medianAgent = targetFormation?.GetMedianAgent(true, false, averagePos);
            if (medianAgent == null) return null;

            Vec2 direction = targetFormation!.QuerySystem.EstimatedDirection;
            Vec2 rightVec  = direction.RightVec();

            Vec3 sampledPos = medianAgent.Position;
            sampledPos += direction.ToVec3() * (float)(_random.NextDouble() * targetFormation.Depth - targetFormation.Depth / 2);

            float sampledWidth = targetFormation.Width * 0.90f;
            sampledPos += rightVec.ToVec3() * (float)(_random.NextDouble() * sampledWidth - sampledWidth / 2);

            return targetFormation.GetMedianAgent(true, false, sampledPos.AsVec2);
        }

        /// <summary>
        /// Returns <c>true</c> when the ray from <paramref name="from"/> to
        /// <paramref name="to"/> is unobstructed for at least <paramref name="minDistance"/>
        /// metres, i.e. no entity or terrain is hit before that distance.
        /// </summary>
        public static bool HasLineOfSight(Vec3 from, Vec3 to, float minDistance = 70f)
        {
            float distanceToObstacle;
            using (new TWSharedMutexReadLock(Scene.PhysicsAndRayCastLock))
            {
                Mission.Current.Scene.RayCastForClosestEntityOrTerrainMT(from, to, out distanceToObstacle, out GameEntity _);
            }
            return distanceToObstacle > minDistance;
        }
    }
}
