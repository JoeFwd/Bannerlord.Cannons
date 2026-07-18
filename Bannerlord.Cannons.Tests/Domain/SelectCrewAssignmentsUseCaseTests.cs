using System.Collections.Generic;
using Bannerlord.Cannons.Domain.Crew;
using FluentAssertions;
using Xunit;

namespace Bannerlord.Cannons.Tests.Domain
{
    public class SelectCrewAssignmentsUseCaseTests
    {
        private static readonly SelectCrewAssignmentsUseCase UseCase = new SelectCrewAssignmentsUseCase();

        private static CrewSeatSnapshot PilotSeat(int id, float x = 0f, float y = 0f, float z = 0f, bool occupied = false, bool reloaderVacated = false)
            => new CrewSeatSnapshot { SeatId = id, X = x, Y = y, Z = z, IsOccupied = occupied, IsReloaderVacated = reloaderVacated, IsPilotSeat = true };

        private static CrewSeatSnapshot ReloadSeat(int id, float x = 5f, float y = 0f, float z = 0f, bool occupied = false, bool reloaderVacated = false)
            => new CrewSeatSnapshot { SeatId = id, X = x, Y = y, Z = z, IsOccupied = occupied, IsReloaderVacated = reloaderVacated, IsPilotSeat = false };

        private static CrewCandidateSnapshot FreeCandidate(int id, float x = 0f, float y = 0f, float z = 0f)
            => new CrewCandidateSnapshot { AgentId = id, X = x, Y = y, Z = z, IsEligible = true };

        private static CrewCandidateSnapshot OwnedCandidate(int id, int seatId, bool onSeat, float x = 0f, float y = 0f, float z = 0f)
            => new CrewCandidateSnapshot
            {
                AgentId = id,
                X = x, Y = y, Z = z,
                IsEligible = true,
                OwnedByThisCannon = true,
                OwnedSeatId = seatId,
                IsOnOwnedSeat = onSeat
            };

        private static CrewCandidateSnapshot DeadOwnedCandidate(int id, int seatId)
            => new CrewCandidateSnapshot { AgentId = id, IsEligible = false, OwnedByThisCannon = true, OwnedSeatId = seatId };

        private static CrewCandidateSnapshot OtherOwnedCandidate(int id)
            => new CrewCandidateSnapshot { AgentId = id, IsEligible = true, OwnedByOther = true };

        // ── Assignments ──────────────────────────────────────────────────────

        [Fact]
        public void EmptyPilotSeat_NearestEligibleCandidate_IsAssigned()
        {
            var request = new CrewAssignmentRequest
            {
                Seats = new List<CrewSeatSnapshot> { PilotSeat(0, x: 0f) },
                Candidates = new List<CrewCandidateSnapshot> { FreeCandidate(1, x: 1f) }
            };

            var result = UseCase.Execute(request);

            result.Assignments.Should().ContainSingle(c => c.AgentId == 1 && c.SeatId == 0);
            result.Reassertions.Should().BeEmpty();
            result.Releases.Should().BeEmpty();
        }

        [Fact]
        public void PilotFirst_OneCandidate_TwoemptySeats_PilotSeatWins()
        {
            // One candidate, one pilot seat (id 0), one reload seat (id 1).
            // Candidate is equidistant; pilot seat must be filled first.
            var request = new CrewAssignmentRequest
            {
                Seats = new List<CrewSeatSnapshot>
                {
                    PilotSeat(0, x: 0f),
                    ReloadSeat(1, x: 0f)
                },
                Candidates = new List<CrewCandidateSnapshot> { FreeCandidate(1, x: 0f) }
            };

            var result = UseCase.Execute(request);

            result.Assignments.Should().ContainSingle(c => c.SeatId == 0, "pilot seat is filled first");
            result.Assignments.Should().NotContain(c => c.SeatId == 1, "candidate already used for pilot seat");
        }

        [Fact]
        public void ReloaderVacatedSeat_IsNotTreatedAsEmpty()
        {
            var request = new CrewAssignmentRequest
            {
                Seats = new List<CrewSeatSnapshot>
                {
                    ReloadSeat(0, reloaderVacated: true)
                },
                Candidates = new List<CrewCandidateSnapshot> { FreeCandidate(1) }
            };

            var result = UseCase.Execute(request);

            result.Assignments.Should().BeEmpty("reloader-vacated seat must not be poached");
        }

        [Fact]
        public void OccupiedSeat_IsNotAssigned()
        {
            var request = new CrewAssignmentRequest
            {
                Seats = new List<CrewSeatSnapshot>
                {
                    PilotSeat(0, occupied: true)
                },
                Candidates = new List<CrewCandidateSnapshot> { FreeCandidate(1) }
            };

            var result = UseCase.Execute(request);

            result.Assignments.Should().BeEmpty("seat already has a user");
        }

        [Fact]
        public void SameCandidate_NotAssignedToTwoSeats()
        {
            var request = new CrewAssignmentRequest
            {
                Seats = new List<CrewSeatSnapshot>
                {
                    PilotSeat(0, x: 0f),
                    ReloadSeat(1, x: 0f)
                },
                Candidates = new List<CrewCandidateSnapshot>
                {
                    // Only one candidate available
                    FreeCandidate(1, x: 0f)
                }
            };

            var result = UseCase.Execute(request);

            result.Assignments.Should().HaveCount(1, "one candidate can only fill one seat");
        }

        [Fact]
        public void OwnedByOther_CandidateIsNotPoached()
        {
            var request = new CrewAssignmentRequest
            {
                Seats = new List<CrewSeatSnapshot> { PilotSeat(0) },
                Candidates = new List<CrewCandidateSnapshot>
                {
                    OtherOwnedCandidate(1)
                }
            };

            var result = UseCase.Execute(request);

            result.Assignments.Should().BeEmpty("candidate owned by another cannon must not be reassigned");
        }

        // ── Reassertions ─────────────────────────────────────────────────────

        [Fact]
        public void OwnedEligibleAgentDriftedOffSeat_ReassertionEmitted()
        {
            var request = new CrewAssignmentRequest
            {
                Seats = new List<CrewSeatSnapshot> { PilotSeat(0) },
                Candidates = new List<CrewCandidateSnapshot>
                {
                    OwnedCandidate(id: 1, seatId: 0, onSeat: false)
                }
            };

            var result = UseCase.Execute(request);

            result.Reassertions.Should().ContainSingle(c => c.AgentId == 1 && c.SeatId == 0);
            result.Assignments.Should().BeEmpty();
            result.Releases.Should().BeEmpty();
        }

        [Fact]
        public void OwnedEligibleAgentOnSeat_NoReassertionOrAssignment()
        {
            var request = new CrewAssignmentRequest
            {
                Seats = new List<CrewSeatSnapshot>
                {
                    PilotSeat(0, occupied: true)
                },
                Candidates = new List<CrewCandidateSnapshot>
                {
                    OwnedCandidate(id: 1, seatId: 0, onSeat: true)
                }
            };

            var result = UseCase.Execute(request);

            result.Reassertions.Should().BeEmpty();
            result.Assignments.Should().BeEmpty();
            result.Releases.Should().BeEmpty();
        }

        // ── Releases ─────────────────────────────────────────────────────────

        [Fact]
        public void OwnedIneligibleAgent_ReleaseEmitted()
        {
            var request = new CrewAssignmentRequest
            {
                Seats = new List<CrewSeatSnapshot> { PilotSeat(0) },
                Candidates = new List<CrewCandidateSnapshot>
                {
                    DeadOwnedCandidate(id: 1, seatId: 0)
                }
            };

            var result = UseCase.Execute(request);

            result.Releases.Should().ContainSingle(id => id == 1);
            result.Reassertions.Should().BeEmpty();
        }

        // ── Nearest-candidate selection ───────────────────────────────────────

        [Fact]
        public void NearestCandidateSelected_NotFarthest()
        {
            var request = new CrewAssignmentRequest
            {
                Seats = new List<CrewSeatSnapshot>
                {
                    PilotSeat(0, x: 0f)
                },
                Candidates = new List<CrewCandidateSnapshot>
                {
                    FreeCandidate(id: 1, x: 100f),
                    FreeCandidate(id: 2, x: 1f)   // nearest
                }
            };

            var result = UseCase.Execute(request);

            result.Assignments.Should().ContainSingle(c => c.AgentId == 2 && c.SeatId == 0,
                "the nearest candidate (id=2) should be picked, not the far one (id=1)");
        }
    }
}
