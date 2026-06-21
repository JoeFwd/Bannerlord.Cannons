using System.Reflection;
using Bannerlord.Cannons.DI;
using Microsoft.Extensions.Logging;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Objects.Siege;

namespace Bannerlord.Cannons.Integration.Mission.Spawn
{
    public enum Team { Attacker, Defender }

    public class GenericCannonSpawnerBase : SpawnerBase
    {
        [EditorVisibleScriptComponentVariable(true)]
        public Team Team = Team.Attacker;

        [EditorVisibleScriptComponentVariable(true)]
        public string SiegeEngineId = "";

        [SpawnerPermissionField]
        public MatrixFrame projectile_pile = MatrixFrame.Zero;

        [EditorVisibleScriptComponentVariable(true)]
        public string AddOnDeployTag = "";

        [EditorVisibleScriptComponentVariable(true)]
        public string RemoveOnDeployTag = "";

        [EditorVisibleScriptComponentVariable(true)]
        public bool ammo_pos_a_enabled = true;

        [EditorVisibleScriptComponentVariable(true)]
        public bool ammo_pos_b_enabled = true;

        [EditorVisibleScriptComponentVariable(true)]
        public bool ammo_pos_c_enabled = true;

        [EditorVisibleScriptComponentVariable(true)]
        public bool ammo_pos_d_enabled = true;

        [EditorVisibleScriptComponentVariable(true)]
        public bool ammo_pos_e_enabled = true;

        [EditorVisibleScriptComponentVariable(true)]
        public bool ammo_pos_f_enabled = true;

        [EditorVisibleScriptComponentVariable(true)]
        public bool ammo_pos_g_enabled = true;

        [EditorVisibleScriptComponentVariable(true)]
        public bool ammo_pos_h_enabled = true;

        protected override void OnEditorInit()
        {
            base.OnEditorInit();
            _spawnerEditorHelper = new SpawnerEntityEditorHelper(this);
        }

        protected override void OnEditorTick(float dt)
        {
            base.OnEditorTick(dt);
            _spawnerEditorHelper?.Tick(dt);
        }
        
        protected override void OnPreInit()
        {
            base.OnPreInit();
            var logger = CannonsRuntimeServices
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger(nameof(CannonSpawnerEntityMissionHelper));
            _spawnerMissionHelper = CannonSpawnerEntityMissionHelper.Create(this, logger);
        }

        public override void AssignParameters(SpawnerEntityMissionHelper spawnerMissionHelper)
        {
            var cannon = spawnerMissionHelper.SpawnedEntity.GetFirstScriptInFamilyDescending<GenericCannon>();
            cannon.SetSide(Team == Team.Attacker ? BattleSideEnum.Attacker : BattleSideEnum.Defender);
            CopyFieldsToCannon(cannon);
        }

        private void CopyFieldsToCannon(GenericCannon cannon)
        {
            var cannonType = cannon.GetType();
            foreach (var field in GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (field.Name == nameof(Team) || field.Name.Equals(nameof(projectile_pile))) continue;
                var cannonField = cannonType.GetField(field.Name, BindingFlags.Public | BindingFlags.Instance);
                cannonField?.SetValue(cannon, field.GetValue(this));
            }
        }
    }
}
