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

            // A gun still holding a round is worth attaching, it needs a pilot to fire it; only a
            // finished one is refused, and it stays refused because its weight is float.MinValue,
            // so attaching it would seat nobody. IsDisabledForBattleSideAI is deliberately not
            // consulted: it holds while any enemy is within 5m, permanent on a wall gun.
            if (userFormations.Count == 0 && machine is not BaseFieldSiegeWeapon { IsSpent: true })
                artilleryFormation.StartUsingMachine(machine, team.IsPlayerTeam);

            // Deployment discards cannons with SetDisabledSynched, which does not tear down
            // detachments, and a disabled machine stops ticking so it cannot drop itself.
            // A live cannon has to prune them or they hold crew and inflate the count below.
            // Finished cannons go the same way: UsableMachine.IsAgentEligible is always true, so a
            // float.MinValue weight stops further assignments but never hands back the men already
            // on the gun. Detaching is what un-seats them.
            foreach (var deadMachine in artilleryFormation.Detachments.OfType<UsableMachine>()
                         .Where(m => m.IsDeactivated || m is BaseFieldSiegeWeapon { IsSpent: true }).ToList())
                artilleryFormation.StopUsingMachine(deadMachine, team.IsPlayerTeam);

            // Count the cannons, not the detachments: Detachments also holds every StrategicArea
            // the formation joined. Every cannon ticks this same team-wide check, so the whole
            // deficit is drafted at once — the assignment below is synchronous, and surplus crew
            // is what makes agents contest the same seats. Finished cannons were just detached, so
            // every cannon still attached needs its seats: one disabled by a nearby enemy, and one
            // down to the round in its barrel, both still have to be crewed.
            var cannonCount = artilleryFormation.Detachments.OfType<BaseFieldSiegeWeapon>().Count();
            var deficit = cannonCount * SeatsPerCannon - artilleryFormation.CountOfUnits;
            if (deficit < 0) ReleaseSurplusCrew(team, artilleryFormation, -deficit);
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

        /// <summary>
        /// Send crew back once their cannon is destroyed or spent. StopUsingMachine un-seats them
        /// but leaves them in the formation, since vanilla borrows crew via detachments and never
        /// moves anyone. Bodyguard gets no tactic behavior, so men left here stand idle while
        /// still counting against the seats of the cannons still firing.
        /// </summary>
        private static void ReleaseSurplusCrew(Team team, Formation artilleryFormation, int surplus)
        {
            // Men already attached to a machine go last: they are the ones working a live gun.
            var released = team.ActiveAgents
                .Where(a => a.Formation != null && a.Formation.Index == artilleryFormation.Index)
                .OrderBy(a => a.IsDetachedFromFormation)
                .Take(surplus)
                .ToList();

            foreach (var agent in released)
            {
                // Back to the formation the spawner would have put him in. Detaching first
                // clears the machine's hold; assigning Formation alone would not.
                agent.TryAttachToFormation();
                var receivingFormation = team.GetFormation(
                    Mission.Current.GetAgentTroopClass(team.Side, agent.Character));
                agent.Formation = receivingFormation;
                team.TriggerOnFormationsChanged(receivingFormation);
            }

            if (released.Count > 0) team.TriggerOnFormationsChanged(artilleryFormation);
        }
    }
}
