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
    /// Responsible for formation enumeration, position sampling, shootability
    /// filtering, and utility scoring. The AI controller calls FindBestTarget()
    /// and owns only the shoot/aim state machine.
    /// </summary>
    public class FormationTargetSelector : ITargetSelector
    {
        // Formation targets are capped below 1.0 so that siege weapons / rams (future
        // targeting tier) can always outscore any formation.
        private const float FormationUtilityCap = 0.85f;

        private readonly BaseFieldSiegeWeapon _weapon;
        private readonly List<Axis> _axes;

        public FormationTargetSelector(BaseFieldSiegeWeapon weapon)
        {
            _weapon = weapon;
            _axes = new List<Axis>
            {
                new Axis(0, 300, x => 0.7f - 3 * (float)Math.Pow(x - 0.3f, 3) + (float)Math.Pow(x, 2),
                    CommonAIDecisionFunctions.DistanceToTarget(() => _weapon.GameEntity.GlobalPosition)),
                new Axis(0, 70, x => x, CommonAIDecisionFunctions.UnitCount()),
                new Axis(0, 10, x => x, CommonAIDecisionFunctions.TargetDistanceToHostiles()),
                new Axis(0f, 20f, x => x, CommonAIDecisionFunctions.ExpectedCasualties(() => _weapon.GameEntity.GlobalPosition)),
            };
        }

        public Target FindBestTarget()
        {
            var candidates = BuildCandidates();
            return candidates.Count > 0 ? TaleWorlds.Core.Extensions.MaxBy(candidates, t => t.UtilityValue) : null;
        }

        private List<Target> BuildCandidates()
        {
            var list = new List<Target>();
            foreach (Formation formation in GetEnemyFormations())
            {
                if (formation.GetCountOfUnitsWithCondition(a => a.IsActive()) == 0)
                    continue;

                Vec3 position = FindBestShootablePosition(formation);
                if (position == Vec3.Zero)
                    continue;

                Target target = ScoreFormation(formation);
                target.SelectedWorldPosition = position;
                if (target.UtilityValue == -1f || !_weapon.IsTargetInRange(position))
                    continue;

                target.UtilityValue = Math.Min(target.UtilityValue, FormationUtilityCap);
                list.Add(target);
            }
            return list;
        }

        /// <summary>
        /// Returns the formation's centre position (median agent) if it is in range,
        /// within the direction restriction, and has clear line of sight.
        /// Returns Vec3.Zero if the centre fails any check.
        /// </summary>
        private Vec3 FindBestShootablePosition(Formation formation)
        {
            Vec2 avg2D = formation.GetAveragePositionOfUnits(false, false);
            Agent center = formation.GetMedianAgent(false, false, avg2D);
            if (center == null) return Vec3.Zero;

            Vec3 position = center.Position;
            if (!_weapon.IsTargetInRange(position)) return Vec3.Zero;
            if (!_weapon.IsTargetWithinDirectionRestriction(position)) return Vec3.Zero;
            if (!_weapon.HasLineOfSightToTarget(position)) return Vec3.Zero;

            return position;
        }

        private Target ScoreFormation(Formation formation)
        {
            var target = new Target { Formation = formation };
            target.UtilityValue = _axes.GeometricMean(target);
            return target;
        }

        private IEnumerable<Formation> GetEnemyFormations()
        {
            return Mission.Current.Teams
                .Where(t => t.Side.GetOppositeSide() == _weapon.Side)
                .SelectMany(t => t.GetFormationsIncludingSpecial());
        }
    }
}
