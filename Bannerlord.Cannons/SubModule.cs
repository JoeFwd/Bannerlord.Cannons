using TOR_Core.GameManagers;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI.Mission;
using TOR_Core.AbilitySystem;
using TOR_Core.Battle.CrosshairMissionBehavior;
using TOR_Core.BattleMechanics.Firearms;
using TOR_Core.BattleMechanics.TriggeredEffect;
using TOR_Core.Extensions.ExtendedInfoSystem;


namespace Bannerlord.Cannons
{
    public class SubModule : MBSubModuleBase
    {
        public static Harmony HarmonyInstance { get; private set; }
        
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();

            HarmonyInstance = new Harmony("mod.harmony.bannerlord.cannons");
            HarmonyInstance.PatchAll();

            TORKeyInputManager.Initialize();
            TriggeredEffectManager.LoadTemplates();
            AbilityFactory.LoadTemplates();
        }

        protected override void InitializeGameStarter(Game game, IGameStarter starterObject)
        {
            if(Game.Current.GameType is Campaign && starterObject is CampaignGameStarter)
            {
                var starter = starterObject as CampaignGameStarter;
                starter.AddBehavior(new ExtendedInfoManager());
            }
        }

        public override void OnMissionBehaviorInitialize(Mission mission)
        {
            var toRemove = mission.GetMissionBehavior<MissionGauntletCrosshair>();
            if(toRemove != null) mission.RemoveMissionBehavior(toRemove);

            mission.AddMissionBehavior(new AbilityManagerMissionLogic());
            mission.AddMissionBehavior(new AbilityHUDMissionView());
            mission.AddMissionBehavior(new CustomCrosshairMissionBehavior());
            // mission.AddMissionBehavior(new WeaponEffectMissionLogic());
            // mission.AddMissionBehavior(new CustomBannerMissionLogic());
            // mission.AddMissionBehavior(new DismembermentMissionLogic());
            // mission.AddMissionBehavior(new MoraleMissionLogic());
            mission.AddMissionBehavior(new FirearmsMissionLogic());
            // mission.AddMissionBehavior(new ForceAtmosphereMissionLogic());
            // mission.AddMissionBehavior(new AnimationTriggerMissionLogic());
            // mission.AddMissionBehavior(new DualWieldMissionLogic());
            // mission.AddMissionBehavior(new BattleShoutsMissionLogic());
        }
    }
}