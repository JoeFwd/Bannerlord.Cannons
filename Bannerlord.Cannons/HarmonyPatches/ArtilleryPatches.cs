﻿using Bannerlord.Cannons.Logging;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.BattleMechanics.Artillery;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch]
    public class ArtilleryPatches
    {
        private static readonly ILogger Logger = new ConsoleLoggerFactory().CreateLogger<ArtilleryPatches>();
        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(RangedSiegeWeapon), "ShootProjectileAux")]
        public static bool OverrideArtilleryShooting(RangedSiegeWeapon __instance, ItemObject missileItem, Agent ____lastShooterAgent)
        {
            if(__instance is BaseFieldSiegeWeapon && ____lastShooterAgent != null && ____lastShooterAgent.IsAIControlled)
            {
                var fieldSiegeWeapon = __instance as BaseFieldSiegeWeapon;
                if (fieldSiegeWeapon == null || fieldSiegeWeapon.Target == null) return true;
                Vec3 launchVec = Vec3.Zero;
                float angle = fieldSiegeWeapon.GetTargetReleaseAngle(fieldSiegeWeapon.Target.SelectedWorldPosition, out launchVec);
                if (angle == float.NegativeInfinity)
                {
                    Logger.Error("Tried to shoot field siege weapon without a valid ballistics solution.");
                    return true;
                }

                Mat3 identity = Mat3.Identity;
                identity.f = launchVec;
                identity.Orthonormalize();

				Mission mission = Mission.Current;
				Agent lastShooterAgent = ____lastShooterAgent;
				mission.AddCustomMissile(lastShooterAgent, 
                    new MissionWeapon(missileItem, null, null, 1), 
                    fieldSiegeWeapon.ProjectileEntityCurrentGlobalPosition, 
                    identity.f, 
                    identity, 
                    8f, 
                    fieldSiegeWeapon.ProjectileVelocity, 
                    false, 
                    fieldSiegeWeapon, 
                    -1);
				return false;
            }
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ItemObject), "GetAirFrictionConstant")]
        public static void OverrideAirFrictionForCannonBall(ref float __result, WeaponClass weaponClass)
        {
            if (weaponClass == WeaponClass.Boulder) __result = 0;
        }
    }
}
