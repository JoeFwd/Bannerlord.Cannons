namespace Bannerlord.Cannons.BattleMechanics.Artillery.Components
{
    /// <summary>
    /// Simple readiness policy: once reloading completes, enforce a fixed cooldown
    /// before the cannon can proceed to its post-reload action.
    /// </summary>
    public class FixedDelayPostReloadReadinessPolicy : IPostReloadReadinessPolicy
    {
        private const float DelaySeconds = 1.5f;
        private float _elapsedSeconds;
        private float _delaySeconds;
        private bool _isArmed;

        /// <inheritdoc/>
        public void MarkReloadCompleted()
        {
            _delaySeconds = DelaySeconds < 0f ? 0f : DelaySeconds;
            _elapsedSeconds = 0f;
            _isArmed = true;
        }

        /// <inheritdoc/>
        public void Update(float dt)
        {
            if (!_isArmed)
                return;

            _elapsedSeconds += dt;
        }

        /// <inheritdoc/>
        public bool IsDelayElapsed => _isArmed && _elapsedSeconds >= _delaySeconds;

        /// <inheritdoc/>
        public void Reset()
        {
            _isArmed = false;
            _elapsedSeconds = 0f;
            _delaySeconds = 0f;
        }
    }
}
