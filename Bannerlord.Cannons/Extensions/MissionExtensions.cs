using System.Collections.Generic;
using System.Linq;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.Extensions
{
    public static class MissionExtensions
    {
        public static List<Team> GetEnemyTeamsOf(this Mission mission, Team team)
        {
            return mission.Teams.Where(x => x.IsEnemyOf(team)).ToList();
        }
    }
}