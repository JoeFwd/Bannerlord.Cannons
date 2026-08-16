using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.Cannons;

public class ArtilleryCrewProvider : IArtilleryCrewProvider
{
    public int GetArtilleryTroopNumber()
    {
        return 20;
    }

    public bool IsArtilleryCrew(Agent agent)
    {
        return agent.IsPlayerControlled || agent.Formation?.FormationIndex == FormationClass.Bodyguard;
    }
}