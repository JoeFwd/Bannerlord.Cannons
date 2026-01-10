using System.Collections.Generic;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.Cannons.Extensions
{
    public static class TeamExtensions
    {
        public static List<Formation> GetFormations(this Team team)
        {
            return team.FormationsIncludingEmpty.FindAll(form => form.CountOfUnits > 0);
        }

        public static List<Formation> GetFormationsIncludingSpecial(this Team team)
        {
            return team.FormationsIncludingSpecialAndEmpty.FindAll(form => form.CountOfUnits > 0);
        }
    }
}
