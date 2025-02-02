using Bannerlord.Cannons.Logging;
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
            if (__instance is not BaseFieldSiegeWeapon fieldSiegeWeapon || ____lastShooterAgent is null)
                return true;

            Mat3 identity = Mat3.Identity;
            
            if (!____lastShooterAgent.IsAIControlled)
            {
                identity.f = fieldSiegeWeapon.GetBallisticErrorAppliedDirection(1f);
            }
            else            
            {
                if (fieldSiegeWeapon.Target == null) return true;
                float angle = fieldSiegeWeapon.GetTargetReleaseAngle(fieldSiegeWeapon.Target.SelectedWorldPosition, out Vec3 launchVec);
                if (angle == float.NegativeInfinity)
                {
                    Logger.Error("Tried to shoot field siege weapon without a valid ballistics solution.");
                    return true;
                }

                identity.f = launchVec;
            }
            
            identity.Orthonormalize();

            Mission.Current.AddCustomMissile(____lastShooterAgent, 
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

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ItemObject), "GetAirFrictionConstant")]
        public static void OverrideAirFrictionForCannonBall(ref float __result, WeaponClass weaponClass)
        {
            if (weaponClass == WeaponClass.Boulder) __result = 0;
        }
    }
}
