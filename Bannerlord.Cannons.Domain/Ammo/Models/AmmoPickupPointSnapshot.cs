namespace Bannerlord.Cannons.Domain.Ammo
{
    public sealed class AmmoPickupPointSnapshot
    {
        public int PointId { get; init; }
        public bool HasUser { get; init; }
        public bool HasAIMovingTo { get; init; }
        public bool IsDeactivated { get; init; }
        public bool IsAssignedToReloader { get; init; }
    }
}
