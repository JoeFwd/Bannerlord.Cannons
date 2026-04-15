using TaleWorlds.MountAndBlade;

namespace Bannerlord.Cannons.BattleMechanics.AI.CommonAIFunctions
{
    /// <summary>
    /// Utility predicates that query the movement/behaviour state of an agent or
    /// formation. Used by AI controllers to decide whether acting is appropriate.
    /// </summary>
    public static class CommonAIStateFunctions
    {
        /// <summary>
        /// Returns <c>true</c> when the agent's formation is in a state that allows
        /// free movement — specifically a Charge or ChargeWithTarget order, or when
        /// the active formation behaviour is a skirmish behaviour.
        ///
        /// Cannon crews should only leave their weapon to act when they can move freely;
        /// for example, retreating or holding formations should keep everyone in place.
        /// </summary>
        public static bool CanAgentMoveFreely(Agent agent)
        {
            var movementOrder = agent?.Formation?.GetReadonlyMovementOrderReference();
            return movementOrder.HasValue
                && (movementOrder.Value.OrderType == OrderType.Charge
                    || movementOrder.Value.OrderType == OrderType.ChargeWithTarget
                    || agent?.Formation?.AI?.ActiveBehavior?.GetType().Name.Contains("Skirmish") == true);
        }
    }
}
