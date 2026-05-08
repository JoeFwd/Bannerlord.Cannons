using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.Cannons.BattleMechanics.Artillery.Components
{
    public interface IAmmoPickupHandler
    {
        void Update(
            StandingPointWithWeaponRequirement? activePickupPoint,
            StandingPoint loadAmmoPoint,
            StandingPoint? reloaderOriginalPoint,
            ref Agent? reloaderAgent,
            ItemObject originalMissileItem,
            ItemObject loadedMissileItem,
            ActionIndexCache loadAmmoEndAction,
            UsableMachine machine);
    }
}
