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
    public class FieldBattleWeaponAI : UsableMachineAIBase
    {
        private const float FindTargetInterval = 0.5f;

        private readonly BaseFieldSiegeWeapon _fieldSiegeWeapon;
        private Target _target;
        private List<Axis> _targetDecisionFunctions;
        private Timer _findTargetTimer;

        public FieldBattleWeaponAI(BaseFieldSiegeWeapon usableMachine) : base(usableMachine)
        {
            _fieldSiegeWeapon = usableMachine;
            _targetDecisionFunctions = CreateTargetingFunctions();
            _findTargetTimer = new Timer(Mission.Current.CurrentTime, FindTargetInterval);
        }

        protected override void OnTick(Agent agentToCompareTo, Formation formationToCompareTo, Team potentialUsersTeam, float dt)
        {
            base.OnTick(agentToCompareTo, formationToCompareTo, potentialUsersTeam, dt);
            if (_fieldSiegeWeapon.PilotAgent != null && _fieldSiegeWeapon.PilotAgent.IsAIControlled)
            {
                if (_fieldSiegeWeapon.State == RangedSiegeWeapon.WeaponState.Idle)
                {
                    if (_target != null)
                    {
                        if (_fieldSiegeWeapon.Target != _target)
                            _fieldSiegeWeapon.SetTarget(_target);

                        if (_fieldSiegeWeapon.Target != null && _fieldSiegeWeapon.PilotAgent.Formation.FiringOrder.OrderType != OrderType.HoldFire)
                        {
                            var position = GetAdjustedTargetPosition(_fieldSiegeWeapon.Target);
                            bool safeToFire = _fieldSiegeWeapon.IsSafeToFire();
                            if (position != Vec3.Zero && _fieldSiegeWeapon.AimAtThreat(_fieldSiegeWeapon.Target) && _fieldSiegeWeapon.IsTargetInRange(position) && safeToFire)
                            {
                                _fieldSiegeWeapon.AiRequestsShoot();
                                _target = null;
                            }
                            else if (!safeToFire)
                            {
                                _target = null;
                            }
                        }
                    }
                    else
                    {
                        _fieldSiegeWeapon.ClearTarget();
                        if (_findTargetTimer.Check(Mission.Current.CurrentTime))
                            _target = FindNewTarget();
                    }
                }
            }
        }

        private Vec3 GetAdjustedTargetPosition(Target target)
        {
            if (target?.Formation == null) return Vec3.Zero;

            var targetAgent = target.SelectedWorldPosition == Vec3.Zero
                ? CommonAIFunctions.CommonAIFunctions.GetRandomAgent(target.Formation)
                : target.Agent;

            if (targetAgent == null) return Vec3.Zero;
            target.Agent = targetAgent;

            Vec3 velocity = target.GetVelocity();
            float time = _fieldSiegeWeapon.GetEstimatedCurrentFlightTime();

            target.SelectedWorldPosition = target.Position + velocity * time;
            return target.SelectedWorldPosition;
        }

        private Target FindNewTarget()
        {
            var candidates = GetAllThreats()
                .FindAll(target => target.Formation == null || target.Formation.GetCountOfUnitsWithCondition(x => x.IsActive()) > 0);
            return candidates.Count > 0 ? TaleWorlds.Core.Extensions.MaxBy(candidates, target => target.UtilityValue) : null;
        }

        private List<Target> GetAllThreats()
        {
            List<Target> list = new List<Target>();
            foreach (Formation formation in GetUnemployedEnemyFormations())
            {
                Vec3 candidatePosition = GetCandidateTargetPosition(formation);
                if (candidatePosition == Vec3.Zero)
                    continue;

                Target targetFormation = GetTargetValueOfFormation(formation);
                targetFormation.SelectedWorldPosition = candidatePosition;
                if (targetFormation.UtilityValue != -1f && _fieldSiegeWeapon.IsTargetInRange(candidatePosition))
                    list.Add(targetFormation);
            }

            return list;
        }

        private Vec3 GetCandidateTargetPosition(Formation formation)
        {
            if (formation == null)
                return Vec3.Zero;

            Vec3 bestPosition = Vec3.Zero;
            float bestHorizontalAngle = float.MaxValue;

            foreach (Vec3 sample in GetFormationTargetSamples(formation))
            {
                if (sample == Vec3.Zero || !_fieldSiegeWeapon.IsTargetInRange(sample))
                    continue;

                if (!_fieldSiegeWeapon.TryGetAbsoluteHorizontalAngleToTarget(sample, out float absoluteLocalTargetDirection))
                    continue;

                if (!_fieldSiegeWeapon.IsTargetWithinDirectionRestriction(sample))
                    continue;

                if (absoluteLocalTargetDirection < bestHorizontalAngle)
                {
                    bestHorizontalAngle = absoluteLocalTargetDirection;
                    bestPosition = sample;
                }
            }

            return bestPosition;
        }

        private IEnumerable<Vec3> GetFormationTargetSamples(Formation formation)
        {
            Vec2 averagePosition2D = formation.GetAveragePositionOfUnits(false, false);
            Vec3 averagePosition = averagePosition2D.ToVec3();
            if (averagePosition != Vec3.Zero)
                yield return averagePosition;

            Agent medianAgent = formation.GetMedianAgent(false, false, averagePosition2D);
            if (medianAgent != null)
                yield return medianAgent.Position;

            Vec3 currentPosition = formation.CurrentPosition.ToVec3();
            if (currentPosition != Vec3.Zero)
                yield return currentPosition;

            Vec2 forward = formation.QuerySystem.EstimatedDirection;
            if (forward.LengthSquared < 0.0001f)
                yield break;

            forward = forward.Normalized();
            Vec2 right = forward.RightVec().Normalized();

            float widthStep = MathF.Max(formation.Width * 0.5f, 1f);
            float depthStep = MathF.Max(formation.Depth * 0.5f, 1f);
            Vec3 anchor = averagePosition != Vec3.Zero ? averagePosition : currentPosition;
            if (anchor == Vec3.Zero)
                yield break;

            yield return anchor + right.ToVec3() * widthStep;
            yield return anchor - right.ToVec3() * widthStep;
            yield return anchor + forward.ToVec3() * depthStep;
            yield return anchor - forward.ToVec3() * depthStep;
            yield return anchor + right.ToVec3() * widthStep + forward.ToVec3() * depthStep;
            yield return anchor + right.ToVec3() * widthStep - forward.ToVec3() * depthStep;
            yield return anchor - right.ToVec3() * widthStep + forward.ToVec3() * depthStep;
            yield return anchor - right.ToVec3() * widthStep - forward.ToVec3() * depthStep;
        }

        private Target GetTargetValueOfFormation(Formation formation)
        {
            var target = new Target {Formation = formation};
            target.UtilityValue = _targetDecisionFunctions.GeometricMean(target);
            return target;
        }

        private IEnumerable<Formation> GetUnemployedEnemyFormations()
        {
            return from f in (from t in Mission.Current.Teams where t.Side.GetOppositeSide() == _fieldSiegeWeapon.Side select t)
                    .SelectMany((Team t) => t.GetFormationsIncludingSpecial())
                where f.CountOfUnits > 0
                select f;
        }

        private List<Axis> CreateTargetingFunctions()
        {
            return new List<Axis>
            {
                new Axis(0, 300, x => 0.7f - 3 * (float) Math.Pow(x - 0.3f, 3) + (float) Math.Pow(x, 2), CommonAIDecisionFunctions.DistanceToTarget(() => _fieldSiegeWeapon.GameEntity.GlobalPosition)),
                new Axis(0, 70, x => x, CommonAIDecisionFunctions.UnitCount()),
                new Axis(0, 10, x => x, CommonAIDecisionFunctions.TargetDistanceToHostiles()),
            };
        }
    }
}