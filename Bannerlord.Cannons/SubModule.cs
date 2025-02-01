﻿using HarmonyLib;
using TaleWorlds.MountAndBlade;
using TOR_Core.BattleMechanics.Firearms;


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
        }

        public override void OnMissionBehaviorInitialize(Mission mission)
        {
            // mission.AddMissionBehavior(new WeaponEffectMissionLogic());
            // mission.AddMissionBehavior(new CustomBannerMissionLogic());
            // mission.AddMissionBehavior(new DismembermentMissionLogic());
            // mission.AddMissionBehavior(new MoraleMissionLogic());
            mission.AddMissionBehavior(new CannonballExplosionMissionLogic());
            // mission.AddMissionBehavior(new ForceAtmosphereMissionLogic());
            // mission.AddMissionBehavior(new AnimationTriggerMissionLogic());
            // mission.AddMissionBehavior(new DualWieldMissionLogic());
            // mission.AddMissionBehavior(new BattleShoutsMissionLogic());
        }
    }
}