using System;
using System.IO;
using System.Reflection;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ModuleManager;
using Path = System.IO.Path;

namespace Bannerlord.Cannons.Initialisation
{
    public class DadgBattleSceneLoader
    {
        private const string ModuleId = "Bannerlord.Cannons";
        private const string BattleScenesFileName = "battle_scenes.xml";

        public void Load()
        {
            var modulePath = ModuleHelper.GetModuleFullPath(ModuleId);
            var filePath = Path.Combine(modulePath, "ModuleData", BattleScenesFileName);

            var managerType =
                Type.GetType("TaleWorlds.Core.GameSceneDataManager, TaleWorlds.Core") ??
                Type.GetType("TaleWorlds.Engine.GameSceneDataManager, TaleWorlds.Engine");

            if (managerType == null)
                return;

            var instance = managerType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)
                ?.GetValue(null);

            managerType.GetMethod("LoadSPBattleScenes", new[] { typeof(string) })
                ?.Invoke(instance, new object[] { filePath });
        }
    }
}
