using TaleWorlds.Library;

namespace Bannerlord.Cannons.BattleMechanics.Artillery.Components
{
    /// <summary>
    /// Provides ballistic calculations used by the cannon AI and firing logic.
    /// </summary>
    public interface IBallisticsService
    {
        /// <summary>
        /// Computes the launch vector (and derived elevation angle) needed to hit
        /// <paramref name="target"/> from <paramref name="startPos"/> at the given
        /// <paramref name="speed"/>.
        /// </summary>
        /// <param name="startPos">World-space muzzle position.</param>
        /// <param name="speed">Projectile launch speed (m/s).</param>
        /// <param name="target">World-space target position.</param>
        /// <param name="preferHighAngle">When two solutions exist, selects the high-arc trajectory.</param>
        /// <param name="angle">Output elevation angle in radians; <c>NaN</c> when no solution exists.</param>
        /// <param name="launchVec">Output normalised launch direction vector.</param>
        /// <returns><see langword="true"/> when at least one ballistic solution exists.</returns>
        bool TryGetReleaseAngle(Vec3 startPos, float speed, Vec3 target, bool preferHighAngle, out float angle, out Vec3 launchVec);

        /// <summary>
        /// Returns <see langword="true"/> when <paramref name="targetPos"/> lies within the
        /// maximum range achievable at the given <paramref name="speed"/> from
        /// <paramref name="startPos"/>.
        /// </summary>
        bool IsTargetInRange(Vec3 startPos, float speed, Vec3 targetPos);

        /// <summary>
        /// Estimates the projectile flight time at the given <paramref name="speed"/> and
        /// release <paramref name="angle"/> over the horizontal distance between
        /// <paramref name="startPos"/> and <paramref name="targetPos"/>.
        /// </summary>
        float GetEstimatedFlightTime(float speed, float angle, Vec3 startPos, Vec3 targetPos);

        /// <summary>
        /// Applies a random angular error of magnitude <paramref name="errorAmount"/> to
        /// <paramref name="shootingDirection"/> using a random azimuth rotation followed by
        /// a lateral tilt, matching the vanilla <c>RangedSiegeWeapon</c> spread logic.
        /// </summary>
        Vec3 GetBallisticErrorAppliedDirection(Vec3 shootingDirection, float errorAmount);
    }
}
