using System;
using System.Collections.Generic;
using System.Linq;
using Bannerlord.Cannons.BattleMechanics.AI.CommonAIFunctions;
using Bannerlord.Cannons.BattleMechanics.Artillery;
using TaleWorlds.Core;
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
        /// Evaluates all reachable enemy agents and returns the one at the centre of
        /// the densest in-range mob, or <c>null</c> if no valid target exists.
        /// </summary>
        public Target? FindBestTarget()
        {
            var candidates = BuildCandidates();
            return candidates.Count > 0
                ? TaleWorlds.Core.Extensions.MaxBy(candidates, t => t.UtilityValue)
                : null;
        }

        /// <summary>
        /// For each enemy agent that passes the shootability checks, builds a
        /// <see cref="Target"/> whose <see cref="Target.MobAgents"/> contains all
        /// agents within <see cref="MobClusterRadius"/> of it, then scores and
        /// caps the result.
        /// </summary>
        private List<Target> BuildCandidates()
        {
            var list         = new List<Target>();
            var enemyAgents  = GetAllEnemyAgents().ToList();
            if (enemyAgents.Count == 0)
                return list;

            foreach (Agent agent in enemyAgents)
            {
                Vec3 position = agent.Position;

                if (!_weapon.IsTargetInRange(position))                   continue;
                if (!_weapon.IsTargetWithinDirectionRestriction(position)) continue;
                if (!_weapon.HasLineOfSightToTarget(position))             continue;

                var mobAgents = enemyAgents
                    .Where(a => a.Position.Distance(position) <= MobClusterRadius)
                    .ToList();

                var target = new Target { Agent = agent, MobAgents = mobAgents, SelectedWorldPosition = position };
                target.UtilityValue = Math.Min(
                    _axes.GeometricMean(target),
                    ArtilleryAIConstants.FormationUtilityCap);

                if (target.UtilityValue > 0f)
                    list.Add(target);
            }

            return list;
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
