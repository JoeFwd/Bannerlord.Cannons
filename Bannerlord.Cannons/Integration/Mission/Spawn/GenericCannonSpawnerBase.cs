using System;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Objects.Siege;

namespace Bannerlord.Cannons.Integration.Mission.Spawn
{
    public class GenericCannonSpawnerBase : SpawnerBase
    {
        [EditorVisibleScriptComponentVariable(true)]
        public string Team = "Attacker";

        [EditorVisibleScriptComponentVariable(true)]
        public string SiegeEngineId = "";

        protected override void OnPreInit()
        {
            base.OnPreInit();
            _spawnerMissionHelper = new SpawnerEntityMissionHelper(this);
        }

        public override void AssignParameters(SpawnerEntityMissionHelper spawnerMissionHelper)
        {
            var cannon = spawnerMissionHelper.SpawnedEntity.GetFirstScriptInFamilyDescending<GenericCannon>();
            cannon.SetSide(string.Equals(Team, "Attacker", StringComparison.OrdinalIgnoreCase) ? BattleSideEnum.Attacker : BattleSideEnum.Defender);
            CopyFieldsToCannon(cannon);
        }

        private void CopyFieldsToCannon(GenericCannon cannon)
        {
            var cannonType = cannon.GetType();
            foreach (var field in GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (field.Name == nameof(Team)) continue;
                var cannonField = cannonType.GetField(field.Name, BindingFlags.Public | BindingFlags.Instance);
                cannonField?.SetValue(cannon, field.GetValue(this));
            }
        }
    }
}
