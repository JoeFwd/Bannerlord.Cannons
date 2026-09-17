using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.Cannons.BattleMechanics.Artillery.Components
{
    /// <summary>
    /// Drives the per-frame load animation for the agent standing at the cannon's
    /// ammo-load standing-point. Returns <see langword="true"/> when the loading
    /// sequence has completed and the caller should transition to
    /// <c>WaitingBeforeIdle</c>.
    /// </summary>
    public class AmmoLoadHandler : IAmmoLoadHandler
    {
        /// <inheritdoc/>
        public bool Update(
            StandingPoint loadAmmoPoint,
            ref Agent? lastLoaderAgent,
            ActionIndexCache loadAmmoBeginAction,
            ActionIndexCache loadAmmoEndAction,
            ItemObject originalMissileItem)
        {
            if (loadAmmoPoint is null) return false;

            if (loadAmmoPoint.HasUser)
            {
                var user = loadAmmoPoint.UserAgent;
                lastLoaderAgent = user;

                if (user.GetCurrentAction(1) == loadAmmoEndAction)
                {
                    EquipmentIndex wieldedItemIndex = user.GetPrimaryWieldedItemIndex();
                    if (wieldedItemIndex != EquipmentIndex.None &&
                        user.Equipment[wieldedItemIndex].CurrentUsageItem.WeaponClass ==
                        originalMissileItem.PrimaryWeapon.WeaponClass)
                    {
                        user.RemoveEquippedWeapon(wieldedItemIndex);
                        user.StopUsingGameObject(true, Agent.StopUsingGameObjectFlags.None);
                        // Signal caller to set State = WeaponState.WaitingBeforeIdle
                        return true;
                    }
                    user.StopUsingGameObject(true);
                }
                else
                {
                    if (user.GetCurrentAction(1) != loadAmmoBeginAction &&
                        !loadAmmoPoint.UserAgent.SetActionChannel(1, loadAmmoBeginAction))
                    {
                        for (EquipmentIndex ei = EquipmentIndex.WeaponItemBeginSlot;
                             ei < EquipmentIndex.NumAllWeaponSlots;
                             ei++)
                        {
                            if (!user.Equipment[ei].IsEmpty &&
                                user.Equipment[ei].CurrentUsageItem.WeaponClass ==
                                originalMissileItem.PrimaryWeapon.WeaponClass)
                            {
                                user.RemoveEquippedWeapon(ei);
                            }
                        }
                        user.StopUsingGameObject(true);
                    }
                }
            }
            else if (loadAmmoPoint.HasAIMovingTo)
            {
                // An agent that lost its cannonball on the way here can never seat: the load point
                // is a StandingPointWithWeaponRequirement, so IsDisabledForAgent refuses anyone not
                // carrying the projectile. Left alone the point stays latched as HasAIMovingTo for
                // the rest of the battle, which also stops it being offered to anyone else, and the
                // cannon sits in LoadingAmmo forever. Vanilla's trebuchet releases the mover for the
                // same reason; StopUsingGameObject clears the move latch so it can go for ammo again.
                Agent movingAgent = loadAmmoPoint.MovingAgent;
                EquipmentIndex movingWieldedItemIndex = movingAgent.GetPrimaryWieldedItemIndex();
                if (movingWieldedItemIndex == EquipmentIndex.None ||
                    movingAgent.Equipment[movingWieldedItemIndex].CurrentUsageItem.WeaponClass !=
                    originalMissileItem.PrimaryWeapon.WeaponClass)
                {
                    movingAgent.StopUsingGameObject(true, Agent.StopUsingGameObjectFlags.None);
                }
            }

            return false;
        }
    }
}
