using System.Collections.Generic;
using Bannerlord.Cannons.Domain.Ammo;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.Cannons.BattleMechanics.Artillery.Components
{
    internal sealed class BannerlordAmmoPickupPointPort : IAmmoPickupPointPort
    {
        private readonly StandingPoint _loadAmmoPoint;
        private readonly IReadOnlyList<StandingPointWithWeaponRequirement> _pickupPoints;
        private readonly Agent? _reloaderAgent;

        public BannerlordAmmoPickupPointPort(
            StandingPoint loadAmmoPoint,
            IReadOnlyList<StandingPointWithWeaponRequirement> pickupPoints,
            Agent? reloaderAgent)
        {
            _loadAmmoPoint = loadAmmoPoint;
            _pickupPoints = pickupPoints;
            _reloaderAgent = reloaderAgent;
        }

        public ResolveActivePickupPointRequest CreateResolveRequest(AmmoWeaponState weaponState, bool hasAmmo)
        {
            var snapshots = new List<AmmoPickupPointSnapshot>(_pickupPoints.Count);
            for (var i = 0; i < _pickupPoints.Count; i++)
            {
                var point = _pickupPoints[i];
                var isAssignedToReloader = _reloaderAgent != null
                    && ((point.HasUser && point.UserAgent == _reloaderAgent)
                        || (point.HasAIMovingTo && point.MovingAgent == _reloaderAgent));
                snapshots.Add(new AmmoPickupPointSnapshot
                {
                    PointId = i,
                    HasUser = point.HasUser,
                    HasAIMovingTo = point.HasAIMovingTo,
                    IsDeactivated = point.IsDeactivated,
                    IsAssignedToReloader = isAssignedToReloader
                });
            }

            return new ResolveActivePickupPointRequest
            {
                WeaponState = weaponState,
                HasAmmo = hasAmmo,
                LoadAmmoPointHasUser = _loadAmmoPoint.HasUser,
                LoadAmmoPointHasAIMovingTo = _loadAmmoPoint.HasAIMovingTo,
                PickupPoints = snapshots
            };
        }

        public void ApplyAvailability(IReadOnlyList<AmmoPickupPointActivationCommand> activationCommands)
        {
            foreach (var command in activationCommands)
            {
                var id = command.PointId;
                if (id < 0 || id >= _pickupPoints.Count)
                    continue;
                var point = _pickupPoints[id];
                if (point.IsDeactivated != command.ShouldBeDeactivated)
                    point.SetIsDeactivatedSynched(command.ShouldBeDeactivated);
            }
        }

        public StandingPointWithWeaponRequirement? ResolveStandingPoint(int? pointId)
        {
            if (!pointId.HasValue)
                return null;
            var value = pointId.Value;
            if (value < 0 || value >= _pickupPoints.Count)
                return null;
            return _pickupPoints[value];
        }
    }
}
