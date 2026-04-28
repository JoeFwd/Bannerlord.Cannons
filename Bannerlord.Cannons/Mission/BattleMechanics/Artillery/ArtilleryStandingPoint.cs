using TaleWorlds.MountAndBlade;

namespace Bannerlord.Cannons.BattleMechanics.Artillery
{
    public class ArtilleryStandingPoint : StandingPoint
    {
        private readonly IArtilleryCrewProvider _artilleryCrewProvider = ArtilleryCrewProviderFactory.CreateArtilleryCrewProvider();
        
        public override bool IsDisabledForAgent(Agent agent)
        {
            if (agent == null || !agent.IsActive() || agent.Team == null)
                return true;

            return !_artilleryCrewProvider.IsArtilleryCrew(agent) || base.IsDisabledForAgent(agent);
        }
    }
}
