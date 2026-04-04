using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.Cannons.BattleMechanics.Artillery.Components
{
    /// <summary>
    /// Performs the forward ray-cast safety check before the cannon fires, ensuring
    /// no friendly agents or near terrain is in the line of fire.
    /// </summary>
    public interface IFireSafetyChecker
    {
        /// <summary>
        /// Returns <see langword="true"/> when it is safe to fire along
        /// <paramref name="shootingDirection"/> from <paramref name="muzzlePos"/>.
        /// Mirrors the ray-cast logic in <c>BaseFieldSiegeWeapon.IsSafeToFire()</c>.
        /// </summary>
        /// <param name="scene">Mission scene used for thread-safe ray-cast locking.</param>
        /// <param name="muzzlePos">World-space starting position of the ray-cast.</param>
        /// <param name="shootingDirection">Normalised or un-normalised firing direction.</param>
        /// <param name="pilotAgent">The agent manning the cannon; friendly check is relative to this agent.</param>
        bool IsSafeToFire(Scene scene, Vec3 muzzlePos, Vec3 shootingDirection, Agent pilotAgent);
    }
}
