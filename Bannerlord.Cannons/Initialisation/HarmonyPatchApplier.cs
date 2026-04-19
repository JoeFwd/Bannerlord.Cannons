using System.Reflection;
using HarmonyLib;

namespace Bannerlord.Cannons.Initialisation
{
    public class HarmonyPatchApplier
    {
        private static readonly Harmony Harmony = new Harmony("mod.harmony.bannerlord.cannons");
        private static bool _isPatched;

        public void Apply()
        {
            if (_isPatched)
                return;

            Harmony.PatchAll(Assembly.GetExecutingAssembly());
            _isPatched = true;
        }
    }
}
