using TaleWorlds.MountAndBlade;

namespace Bannerlord.Cannons.BattleMechanics.AI.ArtilleryAI
{
    /// <summary>
    /// Thin subclass of the engine's <see cref="RangedSiegeWeaponAi"/> for siege-mode
    /// cannon AI. Currently adds no custom behaviour — it exists as a hook point so that
    /// siege-mode overrides (e.g. different target selection, fire cadence, crew orders)
    /// can be added here without touching vanilla engine code.
    /// </summary>
    public class FieldSiegeWeaponAI : RangedSiegeWeaponAi
    {
        public FieldSiegeWeaponAI(RangedSiegeWeapon weapon) : base(weapon) { }
    }
}
