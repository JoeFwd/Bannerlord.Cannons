using System;
using Bannerlord.Cannons.Extensions;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.Cannons.BattleMechanics.AI.CommonAIFunctions
{
    public static class CommonAIDecisionFunctions
    {
        public static Func<Target, float> TargetDistanceToHostiles(Team team = null)
        {
            return target =>
            {
                if (team != null)
                {
                    var distance = target.TacticalPosition.Position.AsVec2.Distance(team.QuerySystem.AverageEnemyPosition);
                    return distance;
                }

                if (target.Formation != null)
                {
                    var querySystemClosestEnemyFormation = target.Formation.QuerySystem.ClosestEnemyFormation;
                    if (querySystemClosestEnemyFormation == null)
                    {
                        return float.MaxValue;
                    }

                    return target.GetPositionPrioritizeCalculated().AsVec2.Distance(querySystemClosestEnemyFormation.AveragePosition);
                }

                return 0f;
            };
        }

        public static Func<Target, float> TargetDistanceToOwnArmy(Team team = null)
        {
            return target =>
            {
                if (team != null)
                {
                    var distance = target.TacticalPosition.Position.AsVec2.Distance(team.QuerySystem.AveragePosition);
                    return distance;
                }

                return 0f;
            };
        }

        public static Func<Target, float> DistanceToTarget(Func<Vec3> provider)
        {
            return target => provider.Invoke().Distance(target.GetPosition());
        }

        public static Func<Target, float> FormationPower()
        {
            return target => target.Formation.QuerySystem.FormationPower;
        }

        public static float CalculateEnemyTotalPower(Team chosenTeam)
        {
            float power = 0;
            foreach (var team in Mission.Current.GetEnemyTeamsOf(chosenTeam))
            {
                power += team.QuerySystem.TeamPower;
            }

            return power;
        }

        public static Func<Target, float> AssessPositionForArtillery()
        {
            return target =>
            {
                var value = 0.2f;
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


        public static Func<Target, float> PositionHeight()
        {
            return target =>
            {
                return target.TacticalPosition.Position.GetGroundZ();
            };
        }

        public static Func<Target, float> UnitCount()
        {
            return target => target.Formation?.CountOfUnits ?? 1;
        }

        /// <summary>
        /// Returns the unit density of the formation (units per square metre of formation area).
        /// A tightly-packed blob maximises cannonball casualties, so higher density → higher score.
        /// Range is 0–1.5 u/m²; anything denser is treated as maximum.
        /// </summary>
        public static Func<Target, float> FormationDensity()
        {
            return target =>
            {
                if (target.Formation == null) return 0f;
                float area = Math.Max(target.Formation.Width * target.Formation.Depth, 1f);
                return target.Formation.CountOfUnits / area;
            };
        }

        /// <summary>
        /// Returns how well the cannon-to-formation direction aligns with the formation's
        /// depth axis (forward/backward). 1.0 = enfilading fire (cannonball travels through
        /// every rank); 0.0 = shooting across the width (hits only one rank).
        /// Range 0–1, so the axis can be used directly with a linear curve.
        /// </summary>
        public static Func<Target, float> EnfiladeAlignment(Func<Vec3> weaponPosition)
        {
            return target =>
            {
                if (target.Formation == null) return 0f;

                Vec2 forward = target.Formation.QuerySystem.EstimatedDirection;
                if (forward.LengthSquared < 0.0001f) return 0f;
                forward = forward.Normalized();

                Vec2 formationCenter = target.Formation.GetAveragePositionOfUnits(false, false);
                Vec2 toFormation = formationCenter - weaponPosition().AsVec2;
                if (toFormation.LengthSquared < 0.0001f) return 0f;
                toFormation = toFormation.Normalized();

                // |cos θ| between shot direction and formation forward
                float dot = toFormation.x * forward.x + toFormation.y * forward.y;
                return Math.Abs(dot);
            };
        }
    }

    public static class CommonAIStateFunctions
    {
        public static bool CanAgentMoveFreely(Agent agent)
        {
            var movementOrder = agent?.Formation?.GetReadonlyMovementOrderReference();
            return movementOrder.HasValue && (movementOrder.Value.OrderType == OrderType.Charge || movementOrder.Value.OrderType == OrderType.ChargeWithTarget || agent?.Formation?.AI?.ActiveBehavior?.GetType().Name.Contains("Skirmish") == true);
        }
    }

    public static class CommonAIFunctions
    {
        private static readonly Random _random = new();

        public static Agent GetRandomAgent(Formation targetFormation)
        {
            var medianAgent = targetFormation?.GetMedianAgent(true, false, targetFormation.GetAveragePositionOfUnits(true, false));

            if (medianAgent == null) return null;

            var adjustedPosition = medianAgent.Position;

            var direction = targetFormation.QuerySystem.EstimatedDirection;
            var rightVec = direction.RightVec();

            adjustedPosition += direction.ToVec3() * (float)(_random.NextDouble() * targetFormation.Depth - targetFormation.Depth / 2);
            var widthToTarget = targetFormation.Width * 0.90f;
            adjustedPosition += rightVec.ToVec3() * (float)(_random.NextDouble() * widthToTarget - widthToTarget / 2);

            return targetFormation.GetMedianAgent(true, false, adjustedPosition.AsVec2);
        }

        public static bool HasLineOfSight(Vec3 from, Vec3 to, float atLeast = 70)
        {
            float distanceE;
            using (new TWSharedMutexReadLock(Scene.PhysicsAndRayCastLock))
            {
                Mission.Current.Scene.RayCastForClosestEntityOrTerrainMT(from, to, out distanceE, out GameEntity entity);
            }
            return distanceE > atLeast;
        }
    }
}
