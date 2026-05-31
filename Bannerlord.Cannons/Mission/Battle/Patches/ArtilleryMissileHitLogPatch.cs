namespace Bannerlord.Cannons.Integration.Mission.Battle.Patches
{
    /// <summary>
    /// TEMPORARY DIAGNOSTIC. Thin append-only file logger used to prove the v1.3 cannonball/merlon
    /// fix: <see cref="ArtilleryShootProjectileAuxPatch"/> writes a FIRE line per steeply-aimed shot
    /// and <see cref="ArtilleryMerlonPassThroughPatch"/> writes a PASS or HIT line per collision,
    /// correlated by missile index. Delete this file together with its callers once the fix is verified.
    /// </summary>
    internal static class CannonMissileHitLog
    {
        private const string LogPath = @"C:\Users\Joe\Documents\cannons_debug.log";

        internal static void Log(string line) => System.IO.File.AppendAllText(LogPath, line);
    }
}
