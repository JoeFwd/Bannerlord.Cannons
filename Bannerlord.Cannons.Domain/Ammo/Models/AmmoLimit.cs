using System;

namespace Bannerlord.Cannons.Domain.Ammo
{
    // DDD: owned part of the Cannon aggregate — its identity and lifetime are inseparable from
    // the cannon that holds it. Modelled here as a standalone class because the cannon domain
    // object does not exist yet: BaseFieldSiegeWeapon is locked inside Bannerlord's inheritance
    // hierarchy and cannot carry domain logic directly.
    public sealed class AmmoLimit
    {
        private readonly Action _onAmmoConsumed;
        private readonly AmmoState _state = new();

        public AmmoLimit(Action onAmmoConsumed)
        {
            _onAmmoConsumed = onAmmoConsumed ?? throw new ArgumentNullException(nameof(onAmmoConsumed));
        }

        public int AmmoCount => _state.AmmoCount;
        public bool HasAmmo => _state.HasAmmo;

        public void SetHasAmmo(bool hasAmmo) => _state.SetHasAmmo(hasAmmo);

        public bool TrySetAmmo(int ammoLeft)
        {
            var clamped = Math.Max(0, ammoLeft);
            if (_state.AmmoCount == clamped && _state.HasAmmo == (clamped > 0))
                return false;
            _state.SetAmmoCount(clamped);
            return true;
        }

        public bool TryConsumeAmmo()
        {
            if (!_state.HasAmmo || _state.AmmoCount <= 0)
                return false;
            _state.SetAmmoCount(_state.AmmoCount - 1);
            _onAmmoConsumed();
            return true;
        }

        public void SyncFromWeapon(int ammoCount) => _state.SetAmmoCount(ammoCount);

        public bool CheckAmmo()
        {
            if (_state.AmmoCount > 0)
                return false;
            _state.SetHasAmmo(false);
            return true;
        }
    }
}
