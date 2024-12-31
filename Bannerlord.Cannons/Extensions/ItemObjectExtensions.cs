using TaleWorlds.Core;

namespace TOR_Core.Extensions
{
    public static class ItemObjectExtensions
    {
        public static bool IsGunPowderWeapon(this WeaponComponentData weapon)
        {
            if (weapon == null || !weapon.IsRangedWeapon) return false;
            return weapon.WeaponClass == WeaponClass.Cartridge || weapon.AmmoClass == WeaponClass.Cartridge;
        }
    }
}