using Bannerlord.Cannons;

namespace TOR_Core.Api;

public static class CannonSystemInitialiser
{
    public static void Initialise() => new SubModule().Inject();
}