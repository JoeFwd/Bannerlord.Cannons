using System.Collections.Generic;
using System.Reflection;
using Harmony.DependencyInjection.Patches;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.Cannons.Integration.Mission.Battle.Patches
{
    /// <summary>
    /// Records the missile indices of cannonballs spawned by our siege weapons so the merlon
    /// pass-through patch can tell our projectiles apart from every other missile in the mission.
    /// An index is dropped again as soon as the engine removes the missile, so a recycled index can
    /// never be mistaken for a live cannonball.
    /// </summary>
    internal static class CannonMissileRegistry
    {
        private static readonly HashSet<int> Active = new HashSet<int>();

        internal static void Register(int missileIndex) => Active.Add(missileIndex);

        internal static void Unregister(int missileIndex) => Active.Remove(missileIndex);

        internal static bool Contains(int missileIndex) => Active.Contains(missileIndex);
    }

    /// <summary>
    /// v1.3 regression fix. In v1.2 a freshly-fired defender cannonball passed cleanly through the
    /// cannon's own wall merlon for its first few metres; in v1.3 the merlon's physics material no
    /// longer carries <c>AttacksCanPassThrough</c>, so a steeply-aimed ball collides with it and
    /// detonates ~1.5-2.2 m from the muzzle. This prefix restores the v1.2 behaviour reactively: when
    /// one of our cannonballs reports an early collision with a static structure (no agent victim)
    /// within <see cref="MaxPassThroughDistance"/> of its launch point, it is treated as the cannon's
    /// own merlon and the missile is kept alive instead of being detonated. Returning
    /// <c>__result = false</c> is the engine's own "PassThrough" outcome (it keeps the missile flying);
    /// skipping the original only bypasses no-damage hit notifications, which is harmless here.
    /// </summary>
    public class ArtilleryMerlonPassThroughPatch : IPatch
    {
        // The cannon's own merlon/parapet is the only solid thing this close to the muzzle. A full
        // siege capture showed our cannonballs strike that merlon anywhere from 1.6 m (shallow shots)
        // out to 5.1 m (the steepest allowed depression), while the nearest real obstacle - an
        // attacker pavise - is 11.5 m away. 8 m clears every merlon strike with margin while staying
        // well short of any legitimate downrange target (enemies and siege engines are further still).
        private const float MaxPassThroughDistance = 8.0f;

        public MethodInfo TargetMethod =>
            AccessTools.Method(typeof(TaleWorlds.MountAndBlade.Mission), "MissileHitCallback");

        public MethodInfo PatchMethod =>
            AccessTools.Method(typeof(ArtilleryMerlonPassThroughPatch), nameof(Prefix));

        public PatchType PatchType => PatchType.Prefix;

        private static bool Prefix(
            out int extraHitParticleIndex,
            ref AttackCollisionData collisionData,
            Vec3 missileStartingPosition,
            Vec3 missilePosition,
            Agent victim,
            GameEntity hitEntity,
            ref bool __result)
        {
            extraHitParticleIndex = -1;

            try
            {
                int missileIndex = collisionData.AffectorWeaponSlotOrMissileIndex;
                if (!CannonMissileRegistry.Contains(missileIndex))
                    return true; // not one of our cannonballs -> let the engine handle it normally

                float travel = (missilePosition - missileStartingPosition).Length;
                bool earlyStructureHit = victim == null && hitEntity != null && travel < MaxPassThroughDistance;

                // DIAGNOSTIC (remove later): record whether each of our cannonballs is passed through its
                // own merlon or detonates normally, and how far it had travelled from the muzzle.
                CannonMissileHitLog.Log(
                    $"[{System.DateTime.Now:HH:mm:ss.fff}] {(earlyStructureHit ? "PASS" : "HIT ")} idx={missileIndex}" +
                    $" travel={travel:F2}m victim={(victim != null ? victim.Name : "none")}" +
                    $" hit={(hitEntity != null ? hitEntity.Name : "TERRAIN/none")}" +
                    $" hitRoot={(hitEntity != null && hitEntity.Root != null ? hitEntity.Root.Name : "?")}\n");

                if (!earlyStructureHit)
                    return true; // genuine downrange impact -> detonate as the engine intended

                // Keep the missile alive and flying (the engine's PassThrough outcome): no blow, no
                // removal. This restores the v1.2 fly-through past the cannon's own merlon.
                __result = false;
                return false;
            }
            catch
            {
                // A fault here must never break the engine's missile-collision pipeline.
                return true;
            }
        }
    }

    /// <summary>
    /// Drops a missile index from <see cref="CannonMissileRegistry"/> once the engine removes the
    /// missile, so recycled indices can never be mistaken for a live cannonball.
    /// </summary>
    public class CannonMissileUnregisterPatch : IPatch
    {
        public MethodInfo TargetMethod =>
            AccessTools.Method(typeof(TaleWorlds.MountAndBlade.Mission), "OnMissileRemoved");

        public MethodInfo PatchMethod =>
            AccessTools.Method(typeof(CannonMissileUnregisterPatch), nameof(Postfix));

        public PatchType PatchType => PatchType.Postfix;

        private static void Postfix(int missileIndex) => CannonMissileRegistry.Unregister(missileIndex);
    }
}
