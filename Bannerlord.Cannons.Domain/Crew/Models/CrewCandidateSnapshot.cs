namespace Bannerlord.Cannons.Domain.Crew
{
    public sealed class CrewCandidateSnapshot
    {
        public int AgentId { get; init; }
        public float X { get; init; }
        public float Y { get; init; }
        public float Z { get; init; }
        public bool IsEligible { get; init; }
        public bool OwnedByThisCannon { get; init; }
        public bool OwnedByOther { get; init; }
        public int OwnedSeatId { get; init; }
        public bool IsOnOwnedSeat { get; init; }
    }
}
