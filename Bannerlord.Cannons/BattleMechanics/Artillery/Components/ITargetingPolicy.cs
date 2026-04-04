using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.Cannons.BattleMechanics.Artillery.Components
{
    public interface ITargetingPolicy
    {
        TargetFlags BuildFlags(bool isDestroyed, bool isDeactivated, BattleSideEnum side);
        float ComputeBaseTargetValue(float userMultiplier, float distanceMultiplier, float hitPointMultiplier);
        float ProcessTargetValue(float baseValue, TargetFlags flags);
    }
}
