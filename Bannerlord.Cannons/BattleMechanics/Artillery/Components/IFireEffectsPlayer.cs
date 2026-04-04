using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace Bannerlord.Cannons.BattleMechanics.Artillery.Components
{
    /// <summary>
    /// Plays muzzle-blast particle effects and fire sounds when the cannon discharges.
    /// </summary>
    public interface IFireEffectsPlayer
    {
        /// <summary>
        /// Resolves the two fire-sound IDs to engine sound indices and stores the
        /// explosion-effect particle name and scene reference for later use.
        /// </summary>
        /// <param name="fireSoundId1">First fire-sound string ID (e.g. "mortar_shot_1").</param>
        /// <param name="fireSoundId2">Second fire-sound string ID (e.g. "mortar_shot_2").</param>
        /// <param name="explosionEffect">Particle-system name for the muzzle blast.</param>
        /// <param name="scene">The mission scene used to create the <see cref="SoundEvent"/>.</param>
        void Initialise(string fireSoundId1, string fireSoundId2, string explosionEffect, Scene scene);

        /// <summary>
        /// Bursts the muzzle-blast particle at <paramref name="muzzleFrame"/> and plays a
        /// randomly-selected fire sound at <paramref name="position"/>.
        /// </summary>
        /// <param name="muzzleFrame">World-space frame of the projectile starting position.</param>
        /// <param name="position">World-space position used for positional audio.</param>
        void Play(MatrixFrame muzzleFrame, Vec3 position);

        /// <summary>
        /// Releases any active <see cref="SoundEvent"/> so it does not play over into the
        /// next firing cycle.
        /// </summary>
        void Stop();
    }
}
