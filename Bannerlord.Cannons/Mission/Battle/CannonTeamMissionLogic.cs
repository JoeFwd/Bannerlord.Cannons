using System.Linq;
using Bannerlord.Cannons.Integration.Mission.Spawn;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.Cannons.Integration.Mission.Battle
{
    public class CannonTeamMissionLogic : MissionLogic
    {
        // Triggered on the Deployment -> Battle transition, which happens exactly once when the
        // deployment phase ends. This guarantees that we add the team and forced use flag to deployed cannons.
        public override void OnMissionModeChange(MissionMode oldMissionMode, bool atStart)
        {
            base.OnMissionModeChange(oldMissionMode, atStart);

            if (oldMissionMode != MissionMode.Deployment || Mission.Mode != MissionMode.Battle)
                return;

            TaleWorlds.MountAndBlade.Mission.Current.ActiveMissionObjects
                .OfType<GenericCannon>()
                .Where(script => script.Side.Equals(BattleSideEnum.Attacker))
                .ToList()
                .ForEach(script =>
                {
                    script.Team = Mission.Teams.Attacker;
                    script.SetForcedUse(true);
                });

            TaleWorlds.MountAndBlade.Mission.Current.ActiveMissionObjects
                .OfType<GenericCannon>()
                .Where(script => script.Side.Equals(BattleSideEnum.Defender))
                .ToList()
                .ForEach(script =>
                {
                    script.Team = Mission.Teams.Defender;
                    script.SetForcedUse(true);
                });
        }
    }
}
