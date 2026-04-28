using System.Collections.Generic;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.Cannons.BattleMechanics.Artillery.Components
{
    /// <summary>
    /// Manages the AI team-formation assignment for artillery weapons, ensuring an
    /// appropriate infantry formation is ordered to man the gun on non-player teams and
    /// that the correct crew formation is used on the player's team.
    /// </summary>
    public interface IAIFormationManager
    {
        /// <summary>
        /// Evaluates the current state of <paramref name="userFormations"/> relative to
        /// <paramref name="team"/> and issues the necessary
        /// <see cref="Formation.StartUsingMachine"/> / <see cref="Formation.StopUsingMachine"/>
        /// calls.
        /// </summary>
        /// <param name="team">The team that owns this artillery piece.</param>
        /// <param name="userFormations">Formations currently assigned to the weapon.</param>
        /// <param name="machine">The <see cref="UsableMachine"/> to assign or unassign.</param>
        void Update(Team team, IReadOnlyList<Formation> userFormations, UsableMachine machine);
    }
}
