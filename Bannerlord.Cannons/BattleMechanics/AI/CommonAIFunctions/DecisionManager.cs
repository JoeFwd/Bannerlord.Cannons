using System.Collections.Generic;
using System.Linq;

namespace TOR_Core.BattleMechanics.AI.CommonAIFunctions
{
    public static class DecisionManager
    {
        public static BehaviorOption? EvaluateCastingBehaviors(List<IAgentBehavior> behaviors)
        {
            var options = behaviors
                .SelectMany(behavior => behavior.CalculateUtility())
                .ToList();

            if (!options.Any())
            {
                return null;
            }

            return TaleWorlds.Core.Extensions.MaxBy(options, option => option?.Target?.UtilityValue ?? float.MinValue);
        }
    }
}