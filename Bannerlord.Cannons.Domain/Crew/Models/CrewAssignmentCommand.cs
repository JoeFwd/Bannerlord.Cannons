namespace Bannerlord.Cannons.Domain.Crew
{
    public sealed class CrewAssignmentCommand
    {
        public int AgentId { get; init; }
        public int SeatId { get; init; }
    }
}
