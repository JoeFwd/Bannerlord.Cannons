using System;
using Bannerlord.Cannons.BattleMechanics.AI.ArtilleryAI;
using Bannerlord.Cannons.Extensions;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.Cannons.BattleMechanics.AI.CommonAIFunctions
{
    /// <summary>
    /// Factory methods that return <c>Func&lt;Target, float&gt;</c> delegates for use as
    /// <see cref="Axis"/> parameter functions. Each method captures whatever external
    /// context it needs (e.g. a weapon position provider) and returns a closure that
    /// computes the relevant metric from a <see cref="Target"/> at evaluation time.
    /// </summary>
    public static class CommonAIDecisionFunctions
    {
        /// <summary>
        /// Returns the 2D distance from the target formation's position to the
        /// closest enemy formation. A low value (formations fighting nearby) scores
        /// higher on the "hostile proximity" axis — cannonballs into a melee do more
        /// damage than those aimed at a formation that is still marching.
        /// </summary>
        public static Func<Target, float> TargetDistanceToHostiles(Team? team = null)
        {
            return target =>
            {
                if (team != null)
                    return target.TacticalPosition.Position.AsVec2.Distance(team.QuerySystem.AverageEnemyPosition);

                if (target.Formation == null)
                    return 0f;

                var closestEnemy = target.Formation.QuerySystem.ClosestEnemyFormation;
                if (closestEnemy == null)
                    return float.MaxValue;

                return target.GetPositionPrioritizeCalculated().AsVec2.Distance(closestEnemy.AveragePosition);
            };
        }

        /// <summary>
        /// Returns the 2D distance from the target tactical position to the own
        /// team's average position. Intended for positional scoring; unused in the
        /// current formation targeting flow.
        /// </summary>
        public static Func<Target, float> TargetDistanceToOwnArmy(Team? team = null)
        {
            return target =>
            {
                if (team != null)
                    return target.TacticalPosition.Position.AsVec2.Distance(team.QuerySystem.AveragePosition);

                return 0f;
            };
        }

        /// <summary>
        /// Returns the 3D distance from a dynamically-evaluated cannon position to
        /// the target. Use <paramref name="weaponPositionProvider"/> to capture the
        /// weapon's <c>GlobalPosition</c> at evaluation time rather than at
        /// selector construction time.
        /// </summary>
        public static Func<Target, float> DistanceToTarget(Func<Vec3> weaponPositionProvider)
            => target => weaponPositionProvider.Invoke().Distance(target.GetPosition());

        /// <summary>Returns the formation's combat power (TaleWorlds QuerySystem value).</summary>
        public static Func<Target, float> FormationPower()
            => target => target.Formation.QuerySystem.FormationPower;

        /// <summary>Sums the QuerySystem team power of every enemy team.</summary>
        public static float CalculateEnemyTotalPower(Team chosenTeam)
        {
            float power = 0;
            foreach (var team in Mission.Current.GetEnemyTeamsOf(chosenTeam))
                power += team.QuerySystem.TeamPower;
            return power;
        }

        /// <summary>
        /// Scores a tactical position for artillery placement based on terrain type
        /// and region membership. Higher ground, cliffs, and chokepoints score well;
        /// forests and difficult terrain penalise the score.
        /// </summary>
        public static Func<Target, float> AssessPositionForArtillery()
        {
            return target =>
            {
                float value = 0.2f;

                if (target.TacticalPosition.TacticalPositionType == TacticalPosition.TacticalPositionTypeEnum.HighGround)
                    value += 0.6f;
                if (target.TacticalPosition.TacticalPositionType == TacticalPosition.TacticalPositionTypeEnum.Cliff)
                    value += 0.6f;
                if (target.TacticalPosition.TacticalPositionType == TacticalPosition.TacticalPositionTypeEnum.ChokePoint)
                    value += 0.6f;

                if (target.TacticalPosition.TacticalRegionMembership == TacticalRegion.TacticalRegionTypeEnum.Opening)
                    value += 0.2f;
                if (target.TacticalPosition.TacticalRegionMembership == TacticalRegion.TacticalRegionTypeEnum.Forest)
                    value -= 0.1f;
                if (target.TacticalPosition.TacticalRegionMembership == TacticalRegion.TacticalRegionTypeEnum.DifficultTerrain)
                    value -= 0.05f;

                return value;
            };
        }

        /// <summary>Returns the world-space ground height of a target tactical position.</summary>
        public static Func<Target, float> PositionHeight()
            => target => target.TacticalPosition.Position.GetGroundZ();

        /// <summary>Returns the number of units currently in the target formation.</summary>
        public static Func<Target, float> UnitCount()
            => target => target.Formation?.CountOfUnits ?? 1;

        /// <summary>
        /// Estimates how many units a cannonball would hit given the formation's size
        /// and the angle of the shot. Delegates the core calculation to
        /// <see cref="ScoringFormulas.EnfiladeScore"/>; see that method for the full
        /// formula and rationale.
        ///
        /// Falls back to plain density (N / W×D) when direction information is unavailable.
        /// </summary>
        /// <param name="weaponPositionProvider">
        /// Provides the cannon's current world position at evaluation time.
        /// </param>
        public static Func<Target, float> ExpectedCasualties(Func<Vec3> weaponPositionProvider)
        {
            return target =>
            {
                if (target.Formation == null) return 0f;

                float n = target.Formation.CountOfUnits;
                float w = Math.Max(target.Formation.Width, 1f);
                float d = Math.Max(target.Formation.Depth, 1f);

                Vec2 forward        = target.Formation.QuerySystem.EstimatedDirection;
                Vec2 formationCenter = target.Formation.GetAveragePositionOfUnits(false, false);
                Vec2 toFormation    = formationCenter - weaponPositionProvider.Invoke().AsVec2;

                if (forward.LengthSquared < 0.0001f || toFormation.LengthSquared < 0.0001f)
                    return n / (w * d); // no direction info — fall back to plain density

                forward     = forward.Normalized();
                toFormation = toFormation.Normalized();

                float cosAlpha = Math.Abs(toFormation.x * forward.x + toFormation.y * forward.y);
                return ScoringFormulas.EnfiladeScore(n, w, d, cosAlpha);
            };
        }
    }
}
