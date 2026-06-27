using Bannerlord.Cannons.BattleMechanics.Artillery;
using System.Reflection;
using Harmony.DependencyInjection.Patches;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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

        /// <summary>
        /// Finds the highest ancestor MissionObject that the spawning missile should ignore.
        /// Walks up <c>fieldSiegeWeapon.GameEntity</c>'s parent chain, returning the highest
        /// ancestor that still carries a MissionObject script. Stops when a parent has no
        /// MissionObject (i.e. we've left the cannon prefab and entered the scene container).
        /// This is required for DADG cannons where the cannon script sits on a child of the
        /// prefab root (e.g. veuglaire_cannon_body) while the physics body lives on a sibling
        /// (e.g. clean). Native missile collision ignore only propagates to descendants of the
        /// passed entity, so we need the actual prefab root, not just <c>this</c>.
        /// </summary>
        internal static MissionObject FindPrefabRootMissionObject(BaseFieldSiegeWeapon fieldSiegeWeapon)
        {
            MissionObject best = fieldSiegeWeapon;
            var current = fieldSiegeWeapon.GameEntity;
            while (current.IsValid)
            {
                var parentWeak = current.Parent;
                if (!parentWeak.IsValid) break;
                var parent = GameEntity.CreateFromWeakEntity(parentWeak);
                var parentMissionObject = parent.GetFirstScriptOfType<MissionObject>();
                if (parentMissionObject == null) break;
                best = parentMissionObject;
                current = parentWeak;
            }
            return best;
        }
    }

    public class ArtilleryShootProjectileAuxPatch : IPatch
    {
        // Static because Harmony Prefix must be static. Populated by DI constructor below.
        private static ILogger _logger = NullLogger.Instance;

        public ArtilleryShootProjectileAuxPatch(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ArtilleryShootProjectileAuxPatch>();
        }

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

            // Walk up to the highest ancestor with a MissionObject (the prefab root).
            // Cannot use GameEntity.Root: scenes wrap prefabs in a container that has no
            // MissionObject script, so Root.GetFirstScriptOfType<MissionObject>() returns null
            // and falls back to `this`, defeating the descendant-ignore propagation.
            MissionObject missionObjectToIgnore = ArtilleryPatchHelpers.FindPrefabRootMissionObject(fieldSiegeWeapon);

            // Spawn at the muzzle-exit entity (projectile_leaving_position) when available —
            // it sits ~4cm further forward than the projectile entity and just outside the
            // barrel collision mesh. Avoids clipping the barrel at low pitch.
            Vec3 spawnPos = fieldSiegeWeapon.MuzzleExitPosition;
            _logger.LogInformation(
                "Cannon shoot: IsAI={IsAI}, Cannon={Cannon}, IgnoreEntity={IgnoreEntity}, SpawnPos={SpawnPos}, ProjectilePos={ProjectilePos}, Direction={Direction}, Speed={Speed}.",
                ___LastShooterAgent.IsAIControlled,
                fieldSiegeWeapon.GameEntity.Name,
                missionObjectToIgnore.GameEntity.Name,
                spawnPos,
                fieldSiegeWeapon.ProjectileEntityCurrentGlobalPosition,
                identity.f,
                launchSpeed);

            TaleWorlds.MountAndBlade.Mission.Current.AddCustomMissile(___LastShooterAgent,
                new MissionWeapon(missileItem, null, null, 1),
                spawnPos,
                identity.f,
                identity,
                launchBaseSpeed,
                launchSpeed,
                false,
                missionObjectToIgnore,
                -1);

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
