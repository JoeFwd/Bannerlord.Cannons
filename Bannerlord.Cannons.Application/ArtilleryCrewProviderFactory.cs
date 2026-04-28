namespace Bannerlord.Cannons;

public class ArtilleryCrewProviderFactory
{
    public static IArtilleryCrewProvider CreateArtilleryCrewProvider()
    {
        return new ArtilleryCrewProvider();
    }
}