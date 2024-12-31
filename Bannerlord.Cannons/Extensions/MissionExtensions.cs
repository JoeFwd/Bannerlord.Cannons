using System.Collections.Generic;
using System.Linq;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;

namespace TOR_Core.Extensions
{
    public static class MissionExtensions
    {
        public static List<Team> GetEnemyTeamsOf(this Mission mission, Team team)
        {
            return mission.Teams.Where(x => x.IsEnemyOf(team)).ToList();
        }

        public static int GetArtillerySlotsLeftForTeam(this Mission mission, Team team)
        {
            int slotsLeft = 0;
            var manager = mission.GetMissionBehavior<AbilityManagerMissionLogic>();
            if (manager != null)
            {
                slotsLeft = manager.GetArtillerySlotsLeftForTeam(team);
            }

            return slotsLeft;
        }
    }
}