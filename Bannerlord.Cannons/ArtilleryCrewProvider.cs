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
        return true;
    }
}