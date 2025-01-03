using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;
using TOR_Core.Extensions;
using TOR_Core.BattleMechanics.AI.CastingAI.AgentCastingBehavior;
using TOR_Core.BattleMechanics.AI.CommonAIFunctions;

namespace TOR_Core.BattleMechanics.AI.CastingAI
{
    public static class AgentCastingBehaviorConfiguration
    {
        public static List<Target> FindTargets(Agent agent, AbilityTemplate abilityTemplate)
        {
            if (abilityTemplate.AbilityTargetType == AbilityTargetType.AlliesInAOE ||
                abilityTemplate.AbilityTargetType == AbilityTargetType.SingleAlly)
                return agent.Team.GetAllyTeams()
                    .SelectMany(team => team.GetFormations())
                    .Select(form => new Target {Formation = form})
                    .ToList();
            if (abilityTemplate.AbilityTargetType == AbilityTargetType.Self)
                return new List<Target>()
                {
                    new Target
                    {
                        Formation = agent.Formation,
                        Agent = agent
                    }
                };

            return agent.Team.GetEnemyTeams()
                .SelectMany(team => team.GetFormations())
                .Select(form => new Target {Formation = form})
                .ToList();
        }

        public static readonly Dictionary<Type, Func<AbstractAgentCastingBehavior, List<Axis>>> UtilityByType =
            new Dictionary<Type, Func<AbstractAgentCastingBehavior, List<Axis>>>
            {
                {typeof(ArtilleryPlacementCastingBehavior), CreateArtilleryPlacementAxis()},
            };

        public static List<AbstractAgentCastingBehavior> PrepareCastingBehaviors(Agent agent)
        {
            var castingBehaviors = new List<AbstractAgentCastingBehavior>();
            var index = 0;
            foreach (var knownAbilityTemplate in agent.GetComponent<AbilityComponent>().GetKnownAbilityTemplates())
            {
                castingBehaviors.Add(new ArtilleryPlacementCastingBehavior(agent, knownAbilityTemplate, index));
                index++;
            }

            return castingBehaviors;
        }

        private static Func<AbstractAgentCastingBehavior, List<Axis>> CreateArtilleryPlacementAxis()
        {
            return behaviour =>
            {
                var axes = new List<Axis>();

                axes.Add(new Axis(0, 100f, x => 1 - x, CommonAIDecisionFunctions.DistanceToTarget(() => behaviour.Agent.Team.QuerySystem.MedianPosition.GetGroundVec3MT())));
                axes.Add(new Axis(0, 70f, x => x, CommonAIDecisionFunctions.TargetDistanceToHostiles(behaviour.Agent.Team)));
                axes.Add(new Axis(0, 1, x => x, CommonAIDecisionFunctions.AssessPositionForArtillery()));

                return axes;
            };
        }
    }
}
