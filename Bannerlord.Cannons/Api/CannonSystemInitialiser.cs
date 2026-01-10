namespace Bannerlord.Cannons.Api;

public static class CannonSystemInitialiser
{
    public static void Initialise() => new SubModule().Inject();
}