using Bannerlord.Cannons.BattleMechanics.Artillery;
using System.Reflection;
using Harmony.DependencyInjection.Patches;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.Engine;
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

            // Spawn past any nearby static obstacles (e.g. merlons) along the trajectory.
            Vec3 muzzlePos = fieldSiegeWeapon.ProjectileEntityCurrentGlobalPosition;
            Vec3 spawnPos = GetClearedSpawnPosition(muzzlePos, identity.f, fieldSiegeWeapon);

            var spawnedMissile = TaleWorlds.MountAndBlade.Mission.Current.AddCustomMissile(___LastShooterAgent,
                new MissionWeapon(missileItem, null, null, 1),
                spawnPos,
                identity.f,
                identity,
                launchBaseSpeed,
                launchSpeed,
                false,
                fieldSiegeWeapon,
                -1);
            
            CannonMissileRegistry.Register(spawnedMissile.Index);

            // DIAGNOSTIC (remove later): record the launch angle of steeply-aimed shots.
            if (identity.f.z < -0.30f)
            {
                float offset = (spawnPos - muzzlePos).Length;
                CannonMissileHitLog.Log(
                    $"[{System.DateTime.Now:HH:mm:ss.fff}] FIRE idx={spawnedMissile.Index}" +
                    $" dir.z={identity.f.z:F4} muzzle.z={muzzlePos.z:F2}" +
                    $" offset={offset:F2} cannonRoot={RootName(fieldSiegeWeapon)}\n");
            }

            return false;
        }

        /// <summary>
        /// If a non-scripted obstacle (e.g. merlon) is within a short distance along the
        /// launch direction, return a spawn point just past it; otherwise return the muzzle.
        /// </summary>
        private static Vec3 GetClearedSpawnPosition(Vec3 muzzlePos, Vec3 launchDir, BaseFieldSiegeWeapon weapon)
        {
            const float checkDistance = 7f;
            const float clearance = 0.5f;

            Vec3 rayEnd = muzzlePos + launchDir * checkDistance;

            // Use the basic raycast (no IgnoreEntity) with default exclude flags.
            bool rayHit = TaleWorlds.MountAndBlade.Mission.Current.Scene
                .RayCastForClosestEntityOrTerrain(
                    muzzlePos, rayEnd,
                    out float hitDist, out Vec3 hitPoint, out WeakGameEntity hitWeakEntity,
                    0.01f, BodyFlags.CommonFocusRayCastExcludeFlags);

            // DIAGNOSTIC (remove later): log every raycast result.
            GameEntity hitEntity = hitWeakEntity.IsValid
                ? GameEntity.CreateFromWeakEntity(hitWeakEntity)
                : null;
            string hitName = hitEntity != null ? hitEntity.Name : "none";
            bool hasMO = hitEntity != null && hitEntity.GetFirstScriptOfType<MissionObject>() != null;
            CannonMissileHitLog.Log(
                $"[{System.DateTime.Now:HH:mm:ss.fff}] RAY hit={rayHit} dist={hitDist:F2}" +
                $" entity={hitName} hasMO={hasMO}" +
                $" dir=({launchDir.x:F3},{launchDir.y:F3},{launchDir.z:F3})" +
                $" muzzle=({muzzlePos.x:F1},{muzzlePos.y:F1},{muzzlePos.z:F1})\n");

            if (rayHit && hitEntity != null && !hasMO)
            {
                return muzzlePos + launchDir * (hitDist + clearance);
            }

            return muzzlePos;
        }

        // DIAGNOSTIC (remove later): the cannon's root entity name, for correlating FIRE log lines.
        private static string RootName(BaseFieldSiegeWeapon w)
        {
            try
            {
                GameEntity e = GameEntity.CreateFromWeakEntity(w.GameEntity);
                return e != null && e.Root != null ? e.Root.Name : "?";
            }
            catch { return "?"; }
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
