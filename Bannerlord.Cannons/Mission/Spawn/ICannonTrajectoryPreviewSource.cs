namespace Bannerlord.Cannons.Integration.Mission.Spawn
{
    public interface ICannonTrajectoryPreviewSource
    {
        float GetTrajectoryPreviewBaseMuzzleVelocity();
        float GetTrajectoryPreviewBottomReleaseAngleRestriction();
        float GetTrajectoryPreviewTopReleaseAngleRestriction();
        float GetTrajectoryPreviewDirectionRestrictionDegrees();
    }
}
