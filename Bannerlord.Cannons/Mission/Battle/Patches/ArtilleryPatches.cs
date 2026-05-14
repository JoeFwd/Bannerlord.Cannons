using Bannerlord.Cannons.BattleMechanics.Artillery;
using System.Reflection;
using Harmony.DependencyInjection.Patches;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.Cannons.Integration.Mission.Battle.Patches
{
    internal static class ArtilleryPatchHelpers
    {
        internal static Vec3 GetPlayerControlledLaunchDirection(BaseFieldSiegeWeapon fieldSiegeWeapon)
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

        private static bool Prefix(RangedSiegeWeapon __instance, ItemObject missileItem, Agent ___LastShooterAgent)
        {
            if (__instance is not BaseFieldSiegeWeapon fieldSiegeWeapon || ___LastShooterAgent is null)
                return true;

            Mat3 identity = Mat3.Identity;
            
            if (!___LastShooterAgent.IsAIControlled)
            {
                identity.f = ArtilleryPatchHelpers.GetPlayerControlledLaunchDirection(fieldSiegeWeapon);
            }
            else
            {
                // Battle AI: custom targeting sets Target.SelectedWorldPosition
                if (fieldSiegeWeapon.Target != null)
                {
                    Vec3 pos = fieldSiegeWeapon.Target.SelectedWorldPosition;
                    if (pos == Vec3.Zero) return true;
                    fieldSiegeWeapon.GetTargetReleaseAngle(pos, out Vec3 launchVec);
                    if (launchVec == Vec3.Zero) return true;
                    identity.f = launchVec;
                }
                // Siege AI: native RangedSiegeWeaponAi populates LastAiLaunchVector via AimAtThreat
                else
                {
                    if (fieldSiegeWeapon.LastAiLaunchVector == Vec3.Zero) return true;
                    identity.f = fieldSiegeWeapon.LastAiLaunchVector;
                }
            }
            
            identity.Orthonormalize();

            float projectileVelocity = fieldSiegeWeapon.ProjectileVelocity;
            float launchBaseSpeed = projectileVelocity;
            float launchSpeed = projectileVelocity;
            WeaponComponentData? currentUsageItem = missileItem.PrimaryWeapon;
            int missileItemBaseSpeed = currentUsageItem?.MissileSpeed ?? 0;
            int missileTotalDamage = currentUsageItem?.MissileDamage ?? 0;
            float speedRatio = launchBaseSpeed > 0f ? launchSpeed / launchBaseSpeed : 0f;
            float missileMagnitudeBeforeDamageModel = speedRatio * speedRatio * missileTotalDamage;

            TaleWorlds.MountAndBlade.Mission.Current.AddCustomMissile(___LastShooterAgent,
                new MissionWeapon(missileItem, null, null, 1),
                fieldSiegeWeapon.ProjectileEntityCurrentGlobalPosition,
                identity.f,
                identity,
                launchBaseSpeed,
                launchSpeed,
                false,
                fieldSiegeWeapon,
                -1);

            return false;
        }
    }

    public class ArtilleryCanShootAtPointPatch : IPatch
    {
        public MethodInfo TargetMethod =>
            AccessTools.Method(typeof(RangedSiegeWeapon), "CanShootAtPoint");

        public MethodInfo PatchMethod =>
            AccessTools.Method(typeof(ArtilleryCanShootAtPointPatch), nameof(Prefix));

        public PatchType PatchType => PatchType.Prefix;

        private static bool Prefix(RangedSiegeWeapon __instance, Vec3 target, ref bool __result)
        {
            if (__instance is not BaseFieldSiegeWeapon fieldSiegeWeapon)
                return true;

            __result = fieldSiegeWeapon.CanShootAtPointUsingAimFrame(target);
            return false;
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
