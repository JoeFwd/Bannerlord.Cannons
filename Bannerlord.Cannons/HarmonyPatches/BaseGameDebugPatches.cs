using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using TOR_Core.GameManagers;
using TaleWorlds.InputSystem;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch]
    public static class BaseGameDebugPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(HotKeyManager), "RegisterInitialContexts")]
        public static bool AddTorContext(ref IEnumerable<GameKeyContext> contexts)
        {
            List<GameKeyContext> newcontexts = contexts.ToList();
            if (!newcontexts.Any(x => x is TORGameKeyContext)) newcontexts.Add(new TORGameKeyContext());
            contexts = newcontexts;
            return true;
        }
    }
}
