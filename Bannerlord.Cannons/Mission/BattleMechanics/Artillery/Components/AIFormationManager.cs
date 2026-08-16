using System.Collections.Generic;
using System.Linq;
using Bannerlord.Cannons.Extensions;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.Cannons.BattleMechanics.Artillery.Components
{
    /// <summary>
    /// Manages the AI team-formation assignment for artillery weapons.
    /// Both teams draw dedicated cannon crew from their own Bodyguard formation,
    /// capped at SeatsPerCannon units per cannon currently drawing from it.
    /// </summary>
    public class AIFormationManager : IAIFormationManager
    {
        private const int SeatsPerCannon = 2;

        /// <inheritdoc/>
        public void Update(Team team, IReadOnlyList<Formation> userFormations, UsableMachine machine)
        {
            if (team == null) return;

            // Bodyguard is outside Team.FormationsIncludingEmpty, so vanilla tactics never
            // assign it a behavior and never pull its units off cannon duty.
            var artilleryFormation = team.GetFormation(FormationClass.Bodyguard);

            if (userFormations.Count > 0 && userFormations.All(f => f.Index != artilleryFormation.Index))
                userFormations[0]?.StopUsingMachine(machine);

            if (userFormations.Count == 0)
                artilleryFormation.StartUsingMachine(machine, team.IsPlayerTeam);

            // Deployment discards cannons with SetDisabledSynched, which does not tear down
            // detachments, and a disabled machine stops ticking so it cannot drop itself.
            // A live cannon has to prune them or they hold crew and inflate the count below.
            foreach (var deadMachine in artilleryFormation.Detachments.OfType<UsableMachine>()
                         .Where(m => m.IsDeactivated).ToList())
                artilleryFormation.StopUsingMachine(deadMachine, team.IsPlayerTeam);

            // Count the cannons, not the detachments: Detachments also holds every StrategicArea
            // the formation joined. Every cannon ticks this same team-wide check, so the whole
            // deficit is drafted at once — the assignment below is synchronous, and surplus crew
            // is what makes agents contest the same seats.
            var cannonCount = artilleryFormation.Detachments.OfType<BaseFieldSiegeWeapon>().Count();
            var deficit = cannonCount * SeatsPerCannon - artilleryFormation.CountOfUnits;
            if (deficit <= 0) return;

            // Draft the men standing closest to the cannon, whichever formation they are in, so
            // replacements do not have to cross the map under fire. Formations the AI may not
            // split are skipped, matching the guard Formation.TransferUnitsAux applies.
            var cannonPosition = machine.GameEntity.GlobalPosition;
            var recruits = team.ActiveAgents
                .Where(a => a.Formation != null
                            && a.Formation.Index != artilleryFormation.Index
                            && a.Formation.IsSplittableByAI)
                .OrderBy(a => a.Position.DistanceSquared(cannonPosition))
                .Take(deficit)
                .ToList();
            if (recruits.Count == 0) return;

            // Assign each man directly. Formation.TransferUnits would pop whoever is nearest the
            // target formation's OrderPosition instead, discarding the selection made above.
            foreach (var recruit in recruits)
            {
                var donorFormation = recruit.Formation;
                recruit.Formation = artilleryFormation;
                team.TriggerOnFormationsChanged(donorFormation);
            }

            team.TriggerOnFormationsChanged(artilleryFormation);
        }
    }
}
