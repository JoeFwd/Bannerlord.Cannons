using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bannerlord.Cannons.Domain;
using Bannerlord.Cannons.Infrastructure;
using Bannerlord.Cannons.Infrastructure.Icons;
using Bannerlord.Cannons.Infrastructure.Registry;
using Bannerlord.Cannons.Integration.Campaign;
using Bannerlord.Cannons.Integration.Mission.Spawn;
using Bannerlord.Cannons.Integration.UI;
using Bannerlord.Cannons.Logging;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.Core;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
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

            // 1. Load + validate cannons from all modules
            var loggerFactory = new ConsoleLoggerFactory();
            var reader = new XmlCannonConfigurationReader(loggerFactory);
            var validator = new ValidateCannonsUseCase(loggerFactory);
            var validCannons = validator.GetValidCannons(reader.LoadCannons()).ToList();

            // 2. Build registry with dynamic types
            var registry = new CannonRegistry();
            foreach (var cannon in validCannons)
            {
                var dynType = CannonTypeEmitter.EmitCannonType(cannon.Id);
                registry.RegisterCannon(cannon, new GenericCannonFactory(cannon.Id, dynType));
            }
            CannonRegistry.Initialize(registry);

            // 3. Register script types (dynamic cannon types + spawner + static types)
            var scriptTypes = new Dictionary<string, Type>();
            foreach (var cannon in validCannons)
                scriptTypes[CannonTypeEmitter.GetTypeName(cannon.Id)] =
                    CannonRegistry.Instance.GetFactory(cannon.Id)!.CannonScriptType;
            var spawnerType = SpawnerTypeEmitter.EmitSpawnerType();
            scriptTypes[spawnerType.Name] = spawnerType;
            Managed.AddTypes(scriptTypes);

            // 4. Register icons into UI brushes
            RegisterCannonIcons();

            // 5. Harmony patches (auto-discovers all [HarmonyPatch] in assembly)
            InitialiseHarmonyPatches();
        }

        protected override void InitializeGameStarter(Game game, IGameStarter starterObject)
        {
            if (game.GameType is not Campaign ||
                starterObject is not CampaignGameStarter campaignGameStarter) return;

            campaignGameStarter.AddModel(
                new CannonSiegeEventModel(
                    campaignGameStarter.Models.OfType<SiegeEventModel>().Last()));
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

        private static void RegisterCannonIcons()
        {
            var brushExtender = new BrushStyleExtender(
                UIResourceManager.BrushFactory, UIResourceManager.SpriteData);
            var deployEnricher = new SiegeEngineDeploymentIconEnricher(brushExtender);
            var campaignEnricher = new CampaignMapSiegeEngineDeploymentIconEnricher(brushExtender);
            var iconProvider = new CannonIconProvider(CannonRegistry.Instance);
            foreach (var icon in iconProvider.GetSiegeEngineIcons())
            {
                deployEnricher.AddSiegeEngineDeploymentIcon(icon.Name, icon.SiegeDeploymentSelectionIconSpriteId);
                campaignEnricher.AddCampaignMapSiegeEngineDeploymentIcon(icon.Name, icon.CampaignMapSelectionIconSpriteId);
            }
        }
    }
}
