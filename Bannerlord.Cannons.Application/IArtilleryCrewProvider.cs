using TaleWorlds.MountAndBlade;

namespace Bannerlord.Cannons;

public interface IArtilleryCrewProvider
{
    int GetArtilleryTroopNumber();

    bool IsArtilleryCrew(Agent agent);
}