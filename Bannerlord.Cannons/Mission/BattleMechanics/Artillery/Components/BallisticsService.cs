using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.Cannons.BattleMechanics.Artillery.Components
{
    /// <summary>
    /// Wraps the <see cref="Ballistics"/> static utility and extracts the Mat3 angular-error
    /// logic from <c>BaseFieldSiegeWeapon</c> to provide a dependency-injectable ballistics
    /// service.
    /// </summary>
    public class BallisticsService : IBallisticsService
    {
        /// <inheritdoc/>
        public bool TryGetReleaseAngle(
            Vec3 startPos,
            float speed,
            Vec3 target,
            bool preferHighAngle,
            out float angle,
            out Vec3 launchVec)
        {
            Vec3 low = Vec3.Zero;
            Vec3 high = Vec3.Zero;
            launchVec = Vec3.Zero;
            angle = float.NaN;

            int numSolutions = Ballistics.GetLaunchVectorForProjectileToHitTarget(
                startPos, speed, target, out low, out high);

            if (numSolutions <= 0) return false;

            if (numSolutions == 2)
            {
                launchVec = preferHighAngle ? high : low;
            }
            else
            {
                launchVec = low != Vec3.Zero ? low : high;
            }

            Vec3 forward = launchVec.NormalizedCopy();
            forward.z = 0;
            Vec3 dir = launchVec.NormalizedCopy();
            Vec3 diff = dir - forward;
            float zDiff = diff.z;
            angle = Vec3.AngleBetweenTwoVectors(forward, dir);
            if (zDiff < 0) angle = -angle;

            return true;
        }

        /// <inheritdoc/>
        public bool IsTargetInRange(Vec3 startPos, float speed, Vec3 targetPos)
        {
            Vec3 diff = targetPos - startPos;
            float maxRange = Ballistics.GetMaximumRange(speed, diff.z);
            diff.z = 0;
            return diff.Length < maxRange;
        }

        /// <inheritdoc/>
        public float GetEstimatedFlightTime(float speed, float angle, Vec3 startPos, Vec3 targetPos)
        {
            Vec3 diff = targetPos - startPos;
            return Ballistics.GetTimeOfProjectileFlight(speed, angle, diff.Length);
        }

        /// <inheritdoc/>
        public Vec3 GetBallisticErrorAppliedDirection(Vec3 shootingDirection, float errorAmount)
        {
            // Copied from BaseFieldSiegeWeapon / RangedSiegeWeapon
            Mat3 mat3 = new Mat3()
            {
                f = shootingDirection,
                u = Vec3.Up
            };
            mat3.Orthonormalize();
            float a = MBRandom.RandomFloat * 6.28318548f;
            mat3.RotateAboutForward(a);
            float f = errorAmount * MBRandom.RandomFloat;
            mat3.RotateAboutSide(f.ToRadians());
            return mat3.f;
        }
    }
}
