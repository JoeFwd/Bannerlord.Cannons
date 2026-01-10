using TaleWorlds.MountAndBlade;

namespace Bannerlord.Cannons.BattleMechanics.Artillery;

public class TrebuchetStandingPoint : StandingPoint
{
    public override bool IsDisabledForAgent(Agent agent)
    {
        return agent.IsPlayerControlled ? true : base.IsDisabledForAgent(agent);
    }
}