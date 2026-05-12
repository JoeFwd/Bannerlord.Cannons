using TaleWorlds.Engine;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Objects.Usables;

namespace Bannerlord.Cannons.BattleMechanics.Artillery
{
    public class CannonBallPile : SiegeMachineStonePile
    {
        public override TextObject GetActionTextForStandingPoint(UsableMissionObject usableGameObject)
        {
            return new TextObject("{=!}Pick up a cannonball");
        }

        public override TextObject GetDescriptionText(WeakGameEntity gameEntity)
        {
            return new TextObject("{=!}Cannonball Pile");
        }
    }
}
