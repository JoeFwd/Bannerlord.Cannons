using Bannerlord.Cannons.BattleMechanics.Artillery;
using System.Reflection;
using Harmony.DependencyInjection.Patches;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.Cannons.Integration.Mission.Battle.Patches
{
    internal static class ArtilleryPatchHelpers
    {
        internal static bool TrySetupProjectileToShoot(
            BaseFieldSiegeWeapon fieldSiegeWeapon,
            Agent shooter,
            bool randomizeMissileSpeed,
            out Vec3 direction,
            out Mat3 orientation,
            out float missileBaseSpeed,
            out float missileShootingSpeed)
        {
            direction = Vec3.Zero;
            orientation = Mat3.Identity;
            missileBaseSpeed = fieldSiegeWeapon.ProjectileVelocity;
            missileShootingSpeed = 0f;

            if (!TryGetLaunchDirection(fieldSiegeWeapon, shooter, out Vec3 launchDirection))
                return false;

            orientation.f = launchDirection;
            orientation.Orthonormalize();

            float muzzleSpeed = missileBaseSpeed;
            if (randomizeMissileSpeed)
                muzzleSpeed *= MBRandom.RandomFloatRanged(0.9f, 1.1f);

            direction = muzzleSpeed * orientation.f + fieldSiegeWeapon.GetGlobalVelocity();
            missileShootingSpeed = direction.Normalize();
            return true;
        }

        private static bool TryGetLaunchDirection(BaseFieldSiegeWeapon fieldSiegeWeapon, Agent shooter, out Vec3 direction)
        {
            direction = Vec3.Zero;

            if (!shooter.IsAIControlled)
            {
                direction = GetPlayerControlledLaunchDirection(fieldSiegeWeapon);
                return direction != Vec3.Zero;
            }

            // Battle AI: custom targeting sets Target.SelectedWorldPosition
            if (fieldSiegeWeapon.Target != null)
            {
                Vec3 pos = fieldSiegeWeapon.Target.SelectedWorldPosition;
                if (pos == Vec3.Zero) return false;
                float releaseAngle = fieldSiegeWeapon.GetTargetReleaseAngle(pos, out direction);
                return direction != Vec3.Zero
                       && fieldSiegeWeapon.IsReleaseAngleWithinRestrictions(releaseAngle);
            }

            // Siege AI: native RangedSiegeWeaponAi populates LastAiLaunchVector via AimAtThreat
            direction = fieldSiegeWeapon.LastAiLaunchVector;
            return direction != Vec3.Zero;
        }

        private static Vec3 GetPlayerControlledLaunchDirection(BaseFieldSiegeWeapon fieldSiegeWeapon)
        {
            // Keep ballistic spread routed through BaseFieldSiegeWeapon's component abstraction.
            return fieldSiegeWeapon.GetBallisticErrorAppliedDirection(1f);
        }
    }

    public class ArtilleryShootProjectileAuxPatch : IPatch
    {
        public MethodInfo TargetMethod =>
            AccessTools.Method(typeof(RangedSiegeWeapon), "ShootProjectileAux");

        public MethodInfo PatchMethod =>
            AccessTools.Method(typeof(ArtilleryShootProjectileAuxPatch), nameof(Prefix));

        public PatchType PatchType => PatchType.Prefix;

        private static bool Prefix(RangedSiegeWeapon __instance, ItemObject missileItem, bool randomizeMissileSpeed, Agent ___LastShooterAgent)
        {
            if (__instance is not BaseFieldSiegeWeapon fieldSiegeWeapon || ___LastShooterAgent is not { } shooter)
                return true;

            if (!ArtilleryPatchHelpers.TrySetupProjectileToShoot(
                    fieldSiegeWeapon,
                    shooter,
                    randomizeMissileSpeed,
                    out Vec3 direction,
                    out Mat3 orientation,
                    out float missileBaseSpeed,
                    out float missileShootingSpeed))
                return true;

            TaleWorlds.MountAndBlade.Mission.Current.AddCustomMissile(shooter,
                new MissionWeapon(missileItem, null, shooter.Origin?.Banner, 1),
                fieldSiegeWeapon.ProjectileEntityCurrentGlobalPosition,
                direction,
                orientation,
                missileShootingSpeed,
                missileBaseSpeed,
                false,
                fieldSiegeWeapon,
                -1);

            return false;
        }
    }

    public class ArtilleryOnDeploymentFinishedPatch : IPatch
    {
        public MethodInfo TargetMethod =>
            AccessTools.Method(typeof(RangedSiegeWeapon), nameof(RangedSiegeWeapon.OnDeploymentFinished));

        public MethodInfo PatchMethod =>
            AccessTools.Method(typeof(ArtilleryOnDeploymentFinishedPatch), nameof(Prefix));

        public PatchType PatchType => PatchType.Prefix;

        private static bool Prefix(RangedSiegeWeapon __instance)
        {
            return ShouldRunNativeDeployment(__instance);
        }

        internal static bool ShouldRunNativeDeployment(RangedSiegeWeapon weapon)
        {
            return weapon is not BaseFieldSiegeWeapon || weapon.Ai is RangedSiegeWeaponAi;
        }
    }

    public class ArtilleryGetAirFrictionConstantPatch : IPatch
    {
        public MethodInfo TargetMethod =>
            AccessTools.Method(typeof(ItemObject), "GetAirFrictionConstant");

        public MethodInfo PatchMethod =>
            AccessTools.Method(typeof(ArtilleryGetAirFrictionConstantPatch), nameof(Postfix));

        public PatchType PatchType => PatchType.Postfix;

        private static void Postfix(ref float __result, WeaponClass weaponClass)
        {
            if (weaponClass == WeaponClass.Boulder) __result = 0;
        }
    }
}
