namespace Bannerlord.Cannons.Domain.Ammo
{
    public sealed class AmmoPickupPointActivationCommand
    {
        public int PointId { get; init; }
        public bool ShouldBeDeactivated { get; init; }
    }
}
