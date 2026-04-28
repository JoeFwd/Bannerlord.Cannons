using System;
using System.Collections.Generic;
using System.Linq;
using Bannerlord.Cannons.BattleMechanics.AI.CommonAIFunctions;
using Bannerlord.Cannons.BattleMechanics.Artillery;
using Bannerlord.Cannons.Extensions;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.Cannons.BattleMechanics.AI.ArtilleryAI
{
    /// <summary>
    /// Selects the best enemy formation target for a field artillery piece.
    ///
    /// Scoring is driven by four <see cref="Axis"/> dimensions combined with a
    /// geometric mean (see <see cref="AxisExtensions.GeometricMean"/>):
    /// <list type="number">
    ///   <item><description><b>Distance</b> — cubic curve rewarding ~150 m shots.</description></item>
    ///   <item><description><b>Unit count</b> — more soldiers = better target.</description></item>
    ///   <item><description><b>Hostile proximity</b> — prefer formations already in melee.</description></item>
    ///   <item><description><b>Expected casualties</b> — enfilade angle and formation density.</description></item>
    /// </list>
    ///
    /// The final utility is capped at <see cref="ArtilleryAIConstants.FormationUtilityCap"/> so that
    /// siege weapons (scored 0.9–1.0 by <see cref="SiegeWeaponTargetSelector"/>)
    /// always take priority over infantry formations.
    ///
    /// Candidate filtering (range, direction restriction, line of sight) is performed
    /// before scoring so that the selector never returns an unshootable target.
    /// </summary>
    public class FormationTargetSelector : ITargetSelector
    {
        // Axis input ranges — these define the "full score" ceiling for each metric.
        // Values above the maximum are clamped to the maximum by the Axis class.
        private const float MaxUnitCount          = 70f;  // formations rarely exceed this size
        private const float MaxHostileProximityM  = 10f;  // metres to the nearest enemy formation
        private const float MaxExpectedCasualties = 20f;  // practical ceiling for enfilade score

        private readonly BaseFieldSiegeWeapon _weapon;
        private readonly List<Axis<Target>> _axes;

        public FormationTargetSelector(BaseFieldSiegeWeapon weapon)
        {
            _weapon = weapon;
            _axes = new List<Axis<Target>>
            {
                // Distance axis: cubic curve — rewards mid-range (~150 m), penalises
                // point-blank and max-range shots. See ScoringFormulas.DistanceScore.
                new Axis(0, ArtilleryAIConstants.MaxTargetRangeMetres,
                    x => ScoringFormulas.DistanceScore(x),
                    CommonAIDecisionFunctions.DistanceToTarget(() => _weapon.GameEntity.GlobalPosition)),

                // Unit count axis: linear — more soldiers = better target.
                new Axis(0, MaxUnitCount,
                    x => x,
                    CommonAIDecisionFunctions.UnitCount()),

                // Hostile proximity axis: linear — low distance to nearest enemy means the
                // formation is actively fighting; cannonballs land in a dense, mixed area.
                new Axis(0, MaxHostileProximityM,
                    x => x,
                    CommonAIDecisionFunctions.TargetDistanceToHostiles()),

                // Expected casualties axis: enfilade scoring — combines formation density and
                // the angle of attack. See ScoringFormulas.EnfiladeScore for the formula.
                new Axis(0f, MaxExpectedCasualties,
                    x => x,
                    CommonAIDecisionFunctions.ExpectedCasualties(() => _weapon.GameEntity.GlobalPosition)),
            };
        }

        /// <summary>
        /// Evaluates all enemy formations and returns the highest-scoring shootable one,
        /// or <c>null</c> if no valid target exists.
        /// </summary>
        public Target FindBestTarget()
        {
            var candidates = BuildCandidates();
            return candidates.Count > 0
                ? TaleWorlds.Core.Extensions.MaxBy(candidates, t => t.UtilityValue)
                : null;
        }

        /// <summary>
        /// Builds a list of scored, shootable formation targets.
        /// Formations with no active units or that fail any shootability check are excluded.
        /// </summary>
        private List<Target> BuildCandidates()
        {
            var list = new List<Target>();
            foreach (Formation formation in GetEnemyFormations())
            {
                if (formation.GetCountOfUnitsWithCondition(a => a.IsActive()) == 0)
                    continue;

                Vec3 position = FindShootableFormationCenter(formation);
                if (position == Vec3.Zero)
                    continue;

                Target target = ScoreFormation(formation);
                target.SelectedWorldPosition = position;

                if (!_weapon.IsTargetInRange(position))
                    continue;

                // Cap below siege-weapon tier so siege weapons always win.
                target.UtilityValue = Math.Min(target.UtilityValue, ArtilleryAIConstants.FormationUtilityCap);
                list.Add(target);
            }
            return list;
        }

        /// <summary>
        /// Returns the formation's median-agent position if it passes all three
        /// shootability checks (range, direction restriction, line of sight).
        /// Returns <see cref="Vec3.Zero"/> if the formation cannot be shot at.
        /// </summary>
        private Vec3 FindShootableFormationCenter(Formation formation)
        {
            Vec2 averagePos = formation.GetAveragePositionOfUnits(false, false);
            Agent? center   = formation.GetMedianAgent(false, false, averagePos);
            if (center == null) return Vec3.Zero;

            Vec3 position = center.Position;
            if (!_weapon.IsTargetInRange(position))                  return Vec3.Zero;
            if (!_weapon.IsTargetWithinDirectionRestriction(position)) return Vec3.Zero;
            if (!_weapon.HasLineOfSightToTarget(position))            return Vec3.Zero;

            return position;
        }

        private Target ScoreFormation(Formation formation)
        {
            var target = new Target { Formation = formation };
            target.UtilityValue = _axes.GeometricMean(target);
            return target;
        }

        /// <summary>
        /// Enumerates all formations belonging to teams that are on the opposite side
        /// of the cannon. Includes special formations (e.g. bodyguard, garrison).
        /// </summary>
        private IEnumerable<Formation> GetEnemyFormations()
        {
            return Mission.Current.Teams
                .Where(t => t.Side.GetOppositeSide() == _weapon.Side)
                .SelectMany(t => t.GetFormationsIncludingSpecial());
        }
    }
}
