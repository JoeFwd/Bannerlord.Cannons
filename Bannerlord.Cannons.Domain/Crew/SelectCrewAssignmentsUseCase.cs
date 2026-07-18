using System.Collections.Generic;

namespace Bannerlord.Cannons.Domain.Crew
{
    public sealed class SelectCrewAssignmentsUseCase
    {
        public CrewAssignmentResult Execute(CrewAssignmentRequest request)
        {
            var seats = request.Seats;
            var candidates = request.Candidates;

            var releases = new List<int>();
            var reassertions = new List<CrewAssignmentCommand>();
            var assignments = new List<CrewAssignmentCommand>();

            // Releases: owned agents that are no longer eligible (dead, inactive, side-changed).
            foreach (var candidate in candidates)
            {
                if (candidate.OwnedByThisCannon && !candidate.IsEligible)
                    releases.Add(candidate.AgentId);
            }

            // Reassertions: owned+eligible agents that have drifted off their assigned seat.
            // Track which seats are already covered by a reassertion to exclude them from
            // the assignment pass.
            var reassertedSeatIds = new HashSet<int>();
            foreach (var candidate in candidates)
            {
                if (candidate.OwnedByThisCannon && candidate.IsEligible && !candidate.IsOnOwnedSeat)
                {
                    reassertions.Add(new CrewAssignmentCommand
                    {
                        AgentId = candidate.AgentId,
                        SeatId = candidate.OwnedSeatId
                    });
                    reassertedSeatIds.Add(candidate.OwnedSeatId);
                }
            }

            // Assignments: fill empty seats (not vacated by reloader, not already targeted by
            // a reassertion) with the nearest unclaimed eligible candidate.
            // Pilot seat is served first.
            var orderedSeats = new List<CrewSeatSnapshot>(seats.Count);
            foreach (var seat in seats)
            {
                if (!seat.IsPilotSeat)
                    continue;
                var isEmpty = !seat.IsOccupied && !seat.IsReloaderVacated && !reassertedSeatIds.Contains(seat.SeatId);
                if (isEmpty)
                    orderedSeats.Add(seat);
            }
            foreach (var seat in seats)
            {
                if (seat.IsPilotSeat)
                    continue;
                var isEmpty = !seat.IsOccupied && !seat.IsReloaderVacated && !reassertedSeatIds.Contains(seat.SeatId);
                if (isEmpty)
                    orderedSeats.Add(seat);
            }

            // Track which candidates have been claimed in this pass.
            var claimedAgentIds = new HashSet<int>();

            foreach (var seat in orderedSeats)
            {
                var bestAgentId = -1;
                var bestDistSq = float.MaxValue;

                foreach (var candidate in candidates)
                {
                    if (!candidate.IsEligible)
                        continue;
                    if (candidate.OwnedByThisCannon || candidate.OwnedByOther)
                        continue;
                    if (claimedAgentIds.Contains(candidate.AgentId))
                        continue;

                    var dx = candidate.X - seat.X;
                    var dy = candidate.Y - seat.Y;
                    var dz = candidate.Z - seat.Z;
                    var distSq = dx * dx + dy * dy + dz * dz;

                    if (distSq < bestDistSq)
                    {
                        bestDistSq = distSq;
                        bestAgentId = candidate.AgentId;
                    }
                }

                if (bestAgentId >= 0)
                {
                    assignments.Add(new CrewAssignmentCommand
                    {
                        AgentId = bestAgentId,
                        SeatId = seat.SeatId
                    });
                    claimedAgentIds.Add(bestAgentId);
                }
            }

            return new CrewAssignmentResult
            {
                Assignments = assignments,
                Reassertions = reassertions,
                Releases = releases
            };
        }
    }
}
