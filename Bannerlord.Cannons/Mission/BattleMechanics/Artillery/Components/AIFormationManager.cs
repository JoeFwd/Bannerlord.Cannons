using System.Collections.Generic;
using System.Linq;
using Bannerlord.Cannons.Extensions;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.Cannons.BattleMechanics.Artillery.Components
{
    /// <summary>
    /// Manages the AI team-formation assignment for artillery weapons.
    /// On AI-controlled teams the infantry formation is assigned; on the player's team
    /// the formation whose crew contains more than two artillery crew agents is selected.
    /// </summary>
    public class AIFormationManager : IAIFormationManager
    {
        private readonly IArtilleryCrewProvider _crewProvider;

        public AIFormationManager(IArtilleryCrewProvider crewProvider)
        {
            _crewProvider = crewProvider;
        }

        /// <inheritdoc/>
        public void Update(Team team, IReadOnlyList<Formation> userFormations, UsableMachine machine)
        {
            if (!team?.IsPlayerTeam ?? false)
            {
                if (userFormations.Count > 0 && userFormations.All(f => f.Index != (int)FormationClass.Infantry))
                    userFormations[0]?.StopUsingMachine(machine);

                if (userFormations.Count == 0)
                    team.FormationsIncludingSpecialAndEmpty.ToList()
                        .FirstOrDefault(f => f.Index == (int)FormationClass.Infantry)
                        ?.StartUsingMachine(machine);
            }
            else if (team?.IsPlayerTeam ?? false)
            {
                if (userFormations.Count == 0)
                {
                    var form = team.GetFormations().ToList()
                        .FirstOrDefault(f => f.Arrangement.GetAllUnits()
                            .FindAll(u => _crewProvider.IsArtilleryCrew((Agent)u)).Count() > 2);

                    if (form != null)
                        form.StartUsingMachine(machine, true);
                }
            }
        }
    }
}
