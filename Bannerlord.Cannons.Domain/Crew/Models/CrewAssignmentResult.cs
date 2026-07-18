using System;
using System.Collections.Generic;

namespace Bannerlord.Cannons.Domain.Crew
{
    public sealed class CrewAssignmentResult
    {
        public IReadOnlyList<CrewAssignmentCommand> Assignments { get; init; } = Array.Empty<CrewAssignmentCommand>();
        public IReadOnlyList<CrewAssignmentCommand> Reassertions { get; init; } = Array.Empty<CrewAssignmentCommand>();
        public IReadOnlyList<int> Releases { get; init; } = Array.Empty<int>();
    }
}
