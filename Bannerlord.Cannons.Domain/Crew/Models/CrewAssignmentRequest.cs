using System;
using System.Collections.Generic;

namespace Bannerlord.Cannons.Domain.Crew
{
    public sealed class CrewAssignmentRequest
    {
        public IReadOnlyList<CrewSeatSnapshot> Seats { get; init; } = Array.Empty<CrewSeatSnapshot>();
        public IReadOnlyList<CrewCandidateSnapshot> Candidates { get; init; } = Array.Empty<CrewCandidateSnapshot>();
    }
}
