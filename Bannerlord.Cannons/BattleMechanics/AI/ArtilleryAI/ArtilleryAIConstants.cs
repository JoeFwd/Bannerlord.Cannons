namespace Bannerlord.Cannons.BattleMechanics.AI.ArtilleryAI
{
    /// <summary>
    /// Shared tuning constants for artillery target selection.
    /// Centralised here so that both selectors stay consistent and the values
    /// can be found and adjusted in one place.
    /// </summary>
    internal static class ArtilleryAIConstants
    {
        /// <summary>
        /// Maximum range (metres) at which the AI will score and prioritise targets.
        /// Beyond this distance the cannon's accuracy drops to the point where targeting
        /// is not worthwhile. Both selectors use this value to cap scoring range.
        /// </summary>
        public const float MaxTargetRangeMetres = 300f;

        /// <summary>
        /// Utility ceiling for formation targets. Must remain strictly below
        /// <see cref="SiegeWeaponScoreFloor"/> so that any shootable siege weapon
        /// beats any formation in the priority race.
        /// </summary>
        public const float FormationUtilityCap = 0.85f;

        /// <summary>
        /// Minimum score returned for a siege weapon target (score floor = 1 − max penalty).
        /// Any value here must exceed <see cref="FormationUtilityCap"/>.
        /// </summary>
        public const float SiegeWeaponScoreFloor = 0.9f;
    }
}
