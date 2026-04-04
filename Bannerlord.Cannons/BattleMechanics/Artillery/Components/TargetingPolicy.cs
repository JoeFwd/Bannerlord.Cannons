using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.Cannons.BattleMechanics.Artillery.Components
{
    public sealed class TargetingPolicy : ITargetingPolicy
    {
        public TargetFlags BuildFlags(bool isDestroyed, bool isDeactivated, BattleSideEnum side)
        {
            TargetFlags targetFlags = (TargetFlags)(0 | 2 | 8 | 16);
            if (isDestroyed || isDeactivated)
                targetFlags |= TargetFlags.NotAThreat;
            if (side == BattleSideEnum.Attacker && DebugSiegeBehavior.DebugDefendState == DebugSiegeBehavior.DebugStateDefender.DebugDefendersToMangonels)
                targetFlags |= TargetFlags.DebugThreat;
            if (side == BattleSideEnum.Defender && DebugSiegeBehavior.DebugAttackState == DebugSiegeBehavior.DebugStateAttacker.DebugAttackersToMangonels)
                targetFlags |= TargetFlags.DebugThreat;
            return targetFlags;
        }

        public float ComputeBaseTargetValue(float userMultiplier, float distanceMultiplier, float hitPointMultiplier)
        {
            return 40f * userMultiplier * distanceMultiplier * hitPointMultiplier;
        }

        public float ProcessTargetValue(float baseValue, TargetFlags flags)
        {
            if (flags.HasAnyFlag(TargetFlags.NotAThreat))
            {
                return -1000f;
            }
            if (flags.HasAnyFlag(TargetFlags.IsSiegeEngine))
            {
                baseValue *= 0.2f;
            }
            if (flags.HasAnyFlag(TargetFlags.IsStructure))
            {
                baseValue *= 0.05f;
            }
            if (flags.HasAnyFlag(TargetFlags.DebugThreat))
            {
                baseValue *= 10000f;
            }
            return baseValue;
        }
    }
}
