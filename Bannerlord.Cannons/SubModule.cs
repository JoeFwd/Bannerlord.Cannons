using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;
using Module = TaleWorlds.MountAndBlade.Module;
using Path = System.IO.Path;

namespace Bannerlord.Cannons
{
    public class SubModule : MBSubModuleBase
    {
        private static readonly Harmony Harmony = new Harmony("mod.harmony.bannerlord.cannons");

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            InitialiseHarmonyPatches();
        }

        public override void OnMissionBehaviorInitialize(Mission mission)
        {
            GetMissionLogics().ForEach(missionLogic => mission.AddMissionBehavior(missionLogic));
        }

#if !IS_MULTIPLAYER_BUILD && !RELEASE
        public override void OnGameInitializationFinished(Game game)
        {
            if (!(game.GameType is Campaign)) return;

            LoadDadgBattleScenes();
        }
#endif

        public void Inject()
        {
            Module.CurrentModule.SubModules.Add(this);
            InitialiseScriptTypes();
            InitialiseHarmonyPatches();
        }

        private static void LoadDadgBattleScenes()
        {
            var modulePath = ModuleHelper.GetModuleFullPath("Bannerlord.Cannons");
            var battleScenesFileName = "battle_scenes.xml";
            GameSceneDataManager.Instance?.LoadSPBattleScenes(Path.Combine(modulePath, "ModuleData",
                battleScenesFileName));
        }

        private static void InitialiseScriptTypes()
        {
            var types = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => typeof(ScriptComponentBehavior).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
                .ToDictionary(t => t.Name, t => t);

            Managed.AddTypes(types);
        }

        private static void InitialiseHarmonyPatches()
        {
            Harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        private List<MissionLogic> GetMissionLogics()
        {
            return Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => typeof(MissionLogic).IsAssignableFrom(t) && !t.IsAbstract && t.GetConstructor(Type.EmptyTypes) != null)
                .Select(t => Activator.CreateInstance(t) as MissionLogic)
                .Where(instance => instance != null)
                .ToList();
        }
    }
}