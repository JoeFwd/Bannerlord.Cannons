using System.Collections.Generic;
using Bannerlord.Cannons.Domain.Crew;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.Cannons.BattleMechanics.Artillery.Components
{
    internal sealed class BannerlordCrewAssignmentPort : ICrewAssignmentPort
    {
        private readonly IReadOnlyList<StandingPoint> _crewSeats;
        private readonly StandingPoint _pilotStandingPoint;
        private readonly Team _team;
        private readonly Agent? _reloaderAgent;
        private readonly StandingPoint? _reloaderOriginalPoint;
        private readonly IArtilleryCrewProvider _crewProvider;
        private readonly UsableMachine _machine;
        private readonly BattleSideEnum _side;
        private readonly CannonCrewRegistry _registry;

        // Kept to resolve agent index→Agent in ApplyAssignments without a second scan.
        private readonly List<Agent> _activeAgentSnapshot = new List<Agent>();

        public BannerlordCrewAssignmentPort(
            IReadOnlyList<StandingPoint> crewSeats,
            StandingPoint pilotStandingPoint,
            Team team,
            Agent? reloaderAgent,
            StandingPoint? reloaderOriginalPoint,
            IArtilleryCrewProvider crewProvider,
            UsableMachine machine,
            BattleSideEnum side,
            CannonCrewRegistry registry)
        {
            _crewSeats = crewSeats;
            _pilotStandingPoint = pilotStandingPoint;
            _team = team;
            _reloaderAgent = reloaderAgent;
            _reloaderOriginalPoint = reloaderOriginalPoint;
            _crewProvider = crewProvider;
            _machine = machine;
            _side = side;
            _registry = registry;
        }

        public CrewAssignmentRequest CreateRequest()
        {
            var seats = new List<CrewSeatSnapshot>(_crewSeats.Count);
            for (var i = 0; i < _crewSeats.Count; i++)
            {
                var sp = _crewSeats[i];
                var pos = sp.GameEntity.GlobalPosition;
                var isReloaderVacated = _reloaderOriginalPoint != null
                    && sp == _reloaderOriginalPoint
                    && _reloaderAgent != null
                    && _reloaderAgent.IsActive();
                seats.Add(new CrewSeatSnapshot
                {
                    SeatId = i,
                    X = pos.X,
                    Y = pos.Y,
                    Z = pos.Z,
                    IsOccupied = sp.HasUser || sp.HasAIMovingTo,
                    IsReloaderVacated = isReloaderVacated,
                    IsPilotSeat = sp == _pilotStandingPoint
                });
            }

            _activeAgentSnapshot.Clear();
            var candidates = new List<CrewCandidateSnapshot>();

            if (_team != null)
            {
                foreach (var agent in _team.ActiveAgents)
                {
                    if (!agent.IsActive() || !agent.IsAIControlled || agent.Team?.Side != _side)
                        continue;
                    if (!_crewProvider.IsArtilleryCrew(agent))
                        continue;

                    _activeAgentSnapshot.Add(agent);
                    var pos = agent.Position;
                    var ownedByThis = _registry.IsOwnedBy(agent.Index, _machine, out var ownedSeatId);
                    var ownedByOther = !ownedByThis && _registry.IsOwnedByOther(agent.Index, _machine);

                    bool isOnOwnedSeat = false;
                    if (ownedByThis && ownedSeatId >= 0 && ownedSeatId < _crewSeats.Count)
                    {
                        var ownedSp = _crewSeats[ownedSeatId];
                        isOnOwnedSeat = (ownedSp.HasUser && ownedSp.UserAgent == agent)
                                        || (ownedSp.HasAIMovingTo && ownedSp.MovingAgent == agent);
                    }

                    candidates.Add(new CrewCandidateSnapshot
                    {
                        AgentId = agent.Index,
                        X = pos.X,
                        Y = pos.Y,
                        Z = pos.Z,
                        IsEligible = true,
                        OwnedByThisCannon = ownedByThis,
                        OwnedByOther = ownedByOther,
                        OwnedSeatId = ownedByThis ? ownedSeatId : -1,
                        IsOnOwnedSeat = isOnOwnedSeat
                    });
                }
            }

            return new CrewAssignmentRequest
            {
                Seats = seats,
                Candidates = candidates
            };
        }

        public void ApplyAssignments(CrewAssignmentResult result)
        {
            foreach (var agentId in result.Releases)
            {
                _registry.Release(agentId);
            }

            foreach (var command in result.Reassertions)
            {
                var agent = FindAgent(command.AgentId);
                if (agent == null)
                    continue;
                var seat = ResolveSeat(command.SeatId);
                if (seat == null)
                    continue;
                DetachFromFormationIfSafe(agent);
                agent.AIMoveToGameObjectEnable(seat, _machine, Agent.AIScriptedFrameFlags.NoAttack);
                _registry.Claim(command.AgentId, _machine, command.SeatId);
            }

            foreach (var command in result.Assignments)
            {
                var agent = FindAgent(command.AgentId);
                if (agent == null)
                    continue;
                var seat = ResolveSeat(command.SeatId);
                if (seat == null)
                    continue;
                DetachFromFormationIfSafe(agent);
                agent.AIMoveToGameObjectEnable(seat, _machine, Agent.AIScriptedFrameFlags.NoAttack);
                _registry.Claim(command.AgentId, _machine, command.SeatId);
            }
        }

        // Calling Formation.DetachUnit on a deployment formation (IsDeployment == true) corrupts
        // LineFormation's internal MBList2D and throws IndexOutOfRangeException during siege startup.
        // Skip the call in that case; AIMoveToGameObjectEnable handles the transition safely.
        private static void DetachFromFormationIfSafe(Agent agent)
        {
            var f = agent.Formation;
            if (f != null && !f.IsDeployment)
                f.DetachUnit(agent, isLoose: true);
        }

        private Agent? FindAgent(int agentId)
        {
            foreach (var agent in _activeAgentSnapshot)
            {
                if (agent.Index == agentId)
                    return agent;
            }
            return null;
        }

        private StandingPoint? ResolveSeat(int seatId)
        {
            if (seatId < 0 || seatId >= _crewSeats.Count)
                return null;
            return _crewSeats[seatId];
        }
    }
}
