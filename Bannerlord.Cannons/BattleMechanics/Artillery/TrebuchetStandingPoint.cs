using TaleWorlds.MountAndBlade;

namespace TOR_Core.BattleMechanics.Artillery;

public class TrebuchetStandingPoint : StandingPoint
{
    public override bool IsDisabledForAgent(Agent agent)
    {
        return agent.IsPlayerControlled ? true : base.IsDisabledForAgent(agent);
    }
}