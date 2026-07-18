namespace Bannerlord.Cannons.Domain.Crew
{
    public sealed class CrewSeatSnapshot
    {
        public int SeatId { get; init; }
        public float X { get; init; }
        public float Y { get; init; }
        public float Z { get; init; }
        public bool IsOccupied { get; init; }
        public bool IsReloaderVacated { get; init; }
        public bool IsPilotSeat { get; init; }
    }
}
