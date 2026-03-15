using Bannerlord.Cannons.BattleMechanics.Artillery;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.Cannons.HarmonyPatches
{
    [HarmonyPatch]
    public class ArtilleryPatches
    {
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
