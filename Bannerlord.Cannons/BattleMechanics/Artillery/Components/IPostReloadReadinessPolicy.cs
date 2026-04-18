namespace Bannerlord.Cannons.BattleMechanics.Artillery.Components
{
    /// <summary>
    /// Tracks post-reload delay progression and exposes whether the delay has elapsed.
    /// </summary>
    public interface IPostReloadReadinessPolicy
    {
        /// <summary>
        /// Arms the delay right after a reload completes.
        /// </summary>
        void MarkReloadCompleted();

        /// <summary>
        /// Advances delay progression.
        /// </summary>
        /// <param name="dt">Elapsed time in seconds for this tick.</param>
        void Update(float dt);

        /// <summary>
        /// True once the configured post-reload delay has elapsed.
        /// </summary>
        bool IsDelayElapsed { get; }

        /// <summary>
        /// Clears any in-progress readiness timing.
        /// </summary>
        void Reset();
    }
}
