using TaleWorlds.MountAndBlade;

namespace Bannerlord.Cannons.BattleMechanics.Artillery;

public class TrebuchetStandingPoint : StandingPoint
{
    public override bool IsDisabledForAgent(Agent agent)
    {
        if (agent == null || !agent.IsActive() || agent.Team == null)
            return true;

        return agent.IsPlayerControlled ? true : base.IsDisabledForAgent(agent);
    }
}
