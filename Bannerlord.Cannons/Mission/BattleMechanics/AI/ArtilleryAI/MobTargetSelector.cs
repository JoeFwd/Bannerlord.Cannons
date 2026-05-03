using System;
using System.Collections.Generic;
using System.Linq;
using Bannerlord.Cannons.BattleMechanics.AI.CommonAIFunctions;
using Bannerlord.Cannons.BattleMechanics.Artillery;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.Cannons.BattleMechanics.AI.ArtilleryAI
{
    /// <summary>
    /// Selects the densest reachable cluster of enemy soldiers as a cannon target,
    /// bypassing the formation system entirely.
    ///
    /// For each active enemy agent in range a "mob" is defined as all enemy agents
    /// within <see cref="MobClusterRadius"/> metres of that agent. The candidate
    /// agent and its mob are scored with two axes combined via geometric mean:
    /// <list type="number">
    ///   <item><description><b>Distance</b> — cubic curve rewarding ~150 m shots (same as formation selector).</description></item>
    ///   <item><description><b>Mob density</b> — linear 0–30 neighbours; prefers tightly packed groups.</description></item>
    /// </list>
    ///
    /// Scores are capped at <see cref="ArtilleryAIConstants.FormationUtilityCap"/> so that
    /// siege weapons scored by <see cref="SiegeWeaponTargetSelector"/> always take priority.
    ///
    /// Lead prediction uses the average velocity of all agents in the mob cluster.
    /// </summary>
    public class MobTargetSelector : ITargetSelector
    {
        /// <summary>
        /// Radius in metres within which agents are considered part of the same mob.
        /// Tuned to roughly cover a single tightly-packed infantry unit.
        /// </summary>
        private const float MobClusterRadius = 15f;

        /// <summary>Maximum mob density used as the scoring axis ceiling.</summary>
        private const float MaxMobDensity = 30f;

        private readonly BaseFieldSiegeWeapon _weapon;
        private readonly List<Axis<Target>> _axes;

        public MobTargetSelector(BaseFieldSiegeWeapon weapon)
        {
            _weapon = weapon;
            _axes = new List<Axis<Target>>
            {
                new Axis(0, ArtilleryAIConstants.MaxTargetRangeMetres,
                    x => ScoringFormulas.DistanceScore(x),
                    CommonAIDecisionFunctions.DistanceToTarget(() => _weapon.GameEntity.GlobalPosition)),

                new Axis(0, MaxMobDensity,
                    x => x,
                    t => t.MobAgents?.Count ?? 0),
            };
        }

        /// <summary>
        /// Evaluates all reachable enemy agents and returns the best target:
        /// <list type="number">
        ///   <item><description>The densest mob with a clear line of sight.</description></item>
        ///   <item><description>
        ///     If every mob is behind a destructible entity, a target aimed at the destructible
        ///     that shelters the highest-scored cluster — destroying it exposes the agents behind.
        ///   </description></item>
        /// </list>
        /// Returns <c>null</c> if no reachable enemy agents exist.
        /// </summary>
        public Target? FindBestTarget()
        {
            BuildCandidates(out var clearCandidates, out var blockedCandidates);

            if (clearCandidates.Count > 0)
                return TaleWorlds.Core.Extensions.MaxBy(clearCandidates, t => t.UtilityValue);

            if (blockedCandidates.Count > 0)
                return BuildDestructableTarget(blockedCandidates);

            return null;
        }

        /// <summary>
        /// For each enemy agent that passes range and direction checks, performs a LOS test
        /// via <see cref="BaseFieldSiegeWeapon.TryGetLineOfSightObstacle"/>. Agents with a
        /// clear path land in <paramref name="clearCandidates"/>; agents behind a destructible
        /// land in <paramref name="blockedCandidates"/> (paired with the blocking entity).
        /// Agents behind indestructible cover are silently skipped.
        /// </summary>
        private void BuildCandidates(
            out List<Target> clearCandidates,
            out List<(Target candidate, GameEntity blocking)> blockedCandidates)
        {
            clearCandidates   = new List<Target>();
            blockedCandidates = new List<(Target, GameEntity)>();

            var enemyAgents = GetAllEnemyAgents().ToList();
            if (enemyAgents.Count == 0)
                return;

            foreach (Agent agent in enemyAgents)
            {
                Vec3 position = agent.Position;

                if (!_weapon.IsTargetInRange(position))                          continue;
                if (!_weapon.IsTargetWithinDirectionRestriction(position))       continue;
                if (!_weapon.TryGetLineOfSightObstacle(position, out GameEntity? blocker)) continue;

                var mobAgents = enemyAgents
                    .Where(a => a.Position.Distance(position) <= MobClusterRadius)
                    .ToList();

                var target = new Target { Agent = agent, MobAgents = mobAgents, SelectedWorldPosition = position };
                target.UtilityValue = Math.Min(
                    _axes.GeometricMean(target),
                    ArtilleryAIConstants.FormationUtilityCap);

                if (target.UtilityValue <= 0f)
                    continue;

                if (blocker != null)
                    blockedCandidates.Add((target, blocker));
                else
                    clearCandidates.Add(target);
            }
        }

        /// <summary>
        /// Builds a <see cref="Target"/> aimed at the destructible entity that shelters the
        /// highest-scored mob cluster, so the cannon can break it open and expose the agents.
        /// </summary>
        private static Target BuildDestructableTarget(List<(Target candidate, GameEntity blocking)> blocked)
        {
            var best   = TaleWorlds.Core.Extensions.MaxBy(blocked, pair => pair.candidate.UtilityValue);
            GameEntity entity = best.blocking;
            Vec3 center = (entity.GlobalBoxMax + entity.GlobalBoxMin) * 0.5f;
            var target  = new Target { BlockingDestructable = entity, SelectedWorldPosition = center };
            target.UtilityValue = best.candidate.UtilityValue;
            return target;
        }

        private IEnumerable<Agent> GetAllEnemyAgents()
        {
            BattleSideEnum enemySide = _weapon.Side.GetOppositeSide();
            return Mission.Current.Agents
                .Where(a => a.Team != null
                         && a.Team.Side == enemySide
                         && a.IsActive()
                         && a.IsHuman);
        }
    }
}
