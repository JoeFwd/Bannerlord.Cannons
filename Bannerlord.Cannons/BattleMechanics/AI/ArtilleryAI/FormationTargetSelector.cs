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
            };
        }

        public Target FindBestTarget()
        {
            var candidates = BuildCandidates()
                .FindAll(t => t.Formation == null || t.Formation.GetCountOfUnitsWithCondition(a => a.IsActive()) > 0);
            return candidates.Count > 0 ? TaleWorlds.Core.Extensions.MaxBy(candidates, t => t.UtilityValue) : null;
        }

        private List<Target> BuildCandidates()
        {
            var list = new List<Target>();
            foreach (Formation formation in GetEnemyFormations())
            {
                Vec3 position = FindBestShootablePosition(formation);
                if (position == Vec3.Zero)
                    continue;

                Target target = ScoreFormation(formation);
                target.SelectedWorldPosition = position;
                if (target.UtilityValue != -1f && _weapon.IsTargetInRange(position))
                    list.Add(target);
            }
            return list;
        }

        /// <summary>
        /// Samples positions across the formation and returns the one closest to the
        /// weapon's current aim direction that is both in range and within the direction
        /// restriction. Returns Vec3.Zero if no shootable position exists.
        /// </summary>
        private Vec3 FindBestShootablePosition(Formation formation)
        {
            Vec3 best = Vec3.Zero;
            float bestAngle = float.MaxValue;

            foreach (Vec3 sample in SampleFormationPositions(formation))
            {
                if (sample == Vec3.Zero || !_weapon.IsTargetInRange(sample))
                    continue;
                if (!_weapon.TryGetAbsoluteHorizontalAngleToTarget(sample, out float angle))
                    continue;
                if (!_weapon.IsTargetWithinDirectionRestriction(sample))
                    continue;
                if (!_weapon.HasLineOfSightToTarget(sample))
                    continue;

                if (angle < bestAngle)
                {
                    bestAngle = angle;
                    best = sample;
                }
            }

            return best;
        }

        /// <summary>
        /// Yields a set of representative positions across the formation: centre,
        /// median agent, current position, and eight bounding-box corners.
        /// </summary>
        private IEnumerable<Vec3> SampleFormationPositions(Formation formation)
        {
            Vec2 avg2D = formation.GetAveragePositionOfUnits(false, false);
            Vec3 avg = avg2D.ToVec3();
            if (avg != Vec3.Zero)
                yield return avg;

            Agent median = formation.GetMedianAgent(false, false, avg2D);
            if (median != null)
                yield return median.Position;

            Vec3 current = formation.CurrentPosition.ToVec3();
            if (current != Vec3.Zero)
                yield return current;

            Vec2 forward = formation.QuerySystem.EstimatedDirection;
            if (forward.LengthSquared < 0.0001f)
                yield break;

            forward = forward.Normalized();
            Vec2 right = forward.RightVec().Normalized();
            float w = MathF.Max(formation.Width * 0.5f, 1f);
            float d = MathF.Max(formation.Depth * 0.5f, 1f);
            Vec3 anchor = avg != Vec3.Zero ? avg : current;
            if (anchor == Vec3.Zero)
                yield break;

            yield return anchor + right.ToVec3() * w;
            yield return anchor - right.ToVec3() * w;
            yield return anchor + forward.ToVec3() * d;
            yield return anchor - forward.ToVec3() * d;
            yield return anchor + right.ToVec3() * w + forward.ToVec3() * d;
            yield return anchor + right.ToVec3() * w - forward.ToVec3() * d;
            yield return anchor - right.ToVec3() * w + forward.ToVec3() * d;
            yield return anchor - right.ToVec3() * w - forward.ToVec3() * d;
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
