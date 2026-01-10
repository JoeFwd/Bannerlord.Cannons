using TaleWorlds.MountAndBlade;

namespace Bannerlord.Cannons.BattleMechanics.Artillery
{
    public class ArtilleryStandingPoint : StandingPoint
    {
        private readonly IArtilleryCrewProvider _artilleryCrewProvider = ArtilleryCrewProviderFactory.CreateArtilleryCrewProvider();
        
        public override bool IsDisabledForAgent(Agent agent)
        {
            return !_artilleryCrewProvider.IsArtilleryCrew(agent) || base.IsDisabledForAgent(agent);
        }
    }
}
