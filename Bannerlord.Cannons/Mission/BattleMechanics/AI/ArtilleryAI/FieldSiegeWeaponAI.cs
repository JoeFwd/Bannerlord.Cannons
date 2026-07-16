using TaleWorlds.MountAndBlade;

namespace Bannerlord.Cannons.BattleMechanics.AI.ArtilleryAI
{
    /// <summary>
    /// Thin subclass of the engine's <see cref="RangedSiegeWeaponAi"/> for siege-mode
    /// cannon AI. Adds no custom targeting — it exists as a hook point so that
    /// siege-mode overrides (e.g. different target selection, fire cadence, crew orders)
    /// can be added here without touching vanilla engine code.
    /// </summary>
    public class FieldSiegeWeaponAI : RangedSiegeWeaponAi
    {
        public FieldSiegeWeaponAI(RangedSiegeWeapon weapon) : base(weapon)
        {
            // v1.4 moved the ThreatSeeker's target-list initialisation out of its
            // constructor into InitializeTargetableObjects(), which the engine only
            // invokes via RangedSiegeWeapon.OnDeploymentFinished. Cannons spawned
            // dynamically (i.e. not present when the deployment phase ends) never
            // receive that call, leaving _potentialTargetObjects null so the AI can
            // never acquire a target. Initialise it here so target acquisition works
            // regardless of when the cannon enters the mission. (No-op-safe if the
            // engine later re-initialises it at OnDeploymentFinished.)
            InitializeThreatSeeker();
        }
    }
}
