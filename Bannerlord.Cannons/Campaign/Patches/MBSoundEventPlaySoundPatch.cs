using System.Reflection;
using Harmony.DependencyInjection.Patches;
using HarmonyLib;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.Cannons.Integration.Campaign.Patches
{
    public class MBSoundEventPlaySoundPatch : IPatch
    {
        private static PendingCannonFireSound _pendingFireSound = null!;

        public MBSoundEventPlaySoundPatch(PendingCannonFireSound pendingFireSound)
        {
            _pendingFireSound = pendingFireSound;
        }

        public MethodInfo TargetMethod => AccessTools.Method(
            typeof(MBSoundEvent),
            nameof(MBSoundEvent.PlaySound),
            new[] { typeof(int), typeof(Vec3) });

        public MethodInfo PatchMethod =>
            AccessTools.Method(typeof(MBSoundEventPlaySoundPatch), nameof(Prefix));

        public PatchType PatchType => PatchType.Prefix;

        private static bool Prefix(int soundCodeId, Vec3 position, ref bool __result)
        {
            if (soundCodeId != MiscSoundContainer.SoundCodeAmbientNodeSiegeTrebuchetFire ||
                !_pendingFireSound.TryTake(out var cannonFireSoundId))
                return true;

            __result = MBSoundEvent.PlaySound(cannonFireSoundId, position);
            return false;
        }
    }
}
