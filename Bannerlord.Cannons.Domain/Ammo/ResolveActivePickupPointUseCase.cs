using System.Collections.Generic;

namespace Bannerlord.Cannons.Domain.Ammo
{
    public sealed class ResolveActivePickupPointUseCase
    {
        public ResolveActivePickupPointResult Execute(ResolveActivePickupPointRequest request)
        {
            var pickupPoints = request.PickupPoints;
            if (pickupPoints == null || pickupPoints.Count == 0)
                return new ResolveActivePickupPointResult
                {
                    ActivePointId = null,
                    ActivationCommands = new AmmoPickupPointActivationCommand[0]
                };

            var shouldEnablePickup = request.WeaponState == AmmoWeaponState.LoadingAmmo
                                     && request.HasAmmo
                                     && !request.LoadAmmoPointHasUser
                                     && !request.LoadAmmoPointHasAIMovingTo;

            var activePointId = shouldEnablePickup
                ? ResolveSingleActivePointId(pickupPoints)
                : null;

            var activationCommands = new List<AmmoPickupPointActivationCommand>(pickupPoints.Count);
            foreach (var point in pickupPoints)
            {
                var isActive = activePointId.HasValue && point.PointId == activePointId.Value;
                var shouldBeDeactivated = !isActive;
                if (point.IsDeactivated != shouldBeDeactivated)
                    activationCommands.Add(
                        new AmmoPickupPointActivationCommand
                        {
                            PointId = point.PointId,
                            ShouldBeDeactivated = shouldBeDeactivated
                        });
            }

            return new ResolveActivePickupPointResult
            {
                ActivePointId = activePointId,
                ActivationCommands = activationCommands
            };
        }

        private static int? ResolveSingleActivePointId(IReadOnlyList<AmmoPickupPointSnapshot> pickupPoints)
        {
            var assignedPointId = FindPointAssignedToReloaderId(pickupPoints);
            if (assignedPointId.HasValue)
                return assignedPointId.Value;

            foreach (var point in pickupPoints)
            {
                if (point.HasUser || point.HasAIMovingTo)
                    return point.PointId;
            }

            return pickupPoints[0].PointId;
        }

        private static int? FindPointAssignedToReloaderId(IReadOnlyList<AmmoPickupPointSnapshot> pickupPoints)
        {
            foreach (var point in pickupPoints)
            {
                if (point.IsAssignedToReloader)
                    return point.PointId;
            }

            return null;
        }
    }
}
