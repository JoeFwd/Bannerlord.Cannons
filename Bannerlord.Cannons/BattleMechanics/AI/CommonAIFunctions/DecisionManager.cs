using System.Collections.Generic;
using System.Linq;

namespace TOR_Core.BattleMechanics.AI.CommonAIFunctions
{
    public static class DecisionManager
    {
        public static BehaviorOption EvaluateCastingBehaviors(List<IAgentBehavior> behaviors)
        {
            return TaleWorlds.Core.Extensions.MaxBy(behaviors
                .SelectMany(behavior => behavior.CalculateUtility()), option => option.Target.UtilityValue);
        }
    }
}