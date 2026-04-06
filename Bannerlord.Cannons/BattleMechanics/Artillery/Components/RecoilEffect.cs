using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.Cannons.BattleMechanics.Artillery.Components
{
    /// <summary>
    /// Animates the cannon body recoil with eased curves and velocity-proportional wheel spin.
    /// Back-slide uses ease-out-cubic (maximum velocity at impact, decelerates to stop).
    /// Return uses smoothstep (gradual ease-in then ease-out, feels like a deliberate push).
    /// Wheel angular velocity is derived from body linear velocity each tick so spin speed
    /// naturally tracks body speed.
    /// </summary>
    public class RecoilEffect : IRecoilEffect
    {
        private readonly SynchedMissionObject _body;
        private readonly IWheelAnimator _wheelAnimator;
        private readonly float _recoilDuration;
        private readonly float _recoil2Duration;
        private readonly float _slideBackFrameFactor;
        private readonly float _wheelRadius;

        private MatrixFrame _slideBackFrameOrig;
        private MatrixFrame _slideBackFrame;
        private float _slideDistance;
        private float _recoilTimer;
        private bool _active;
        private bool _returning;
        private float _returnTimer;

        public RecoilEffect(
            SynchedMissionObject body,
            IWheelAnimator wheelAnimator,
            float recoilDuration,
            float recoil2Duration,
            float slideBackFrameFactor,
            float wheelRadius)
        {
            _body = body;
            _wheelAnimator = wheelAnimator;
            _recoilDuration = recoilDuration;
            _recoil2Duration = recoil2Duration;
            _slideBackFrameFactor = slideBackFrameFactor;
            _wheelRadius = wheelRadius;
        }

        /// <inheritdoc/>
        public void Begin(MatrixFrame bodyFrame)
        {
            _slideBackFrameOrig = bodyFrame;
            _slideBackFrame = bodyFrame.Advance(-_slideBackFrameFactor);
            _slideDistance = (_slideBackFrame.origin - _slideBackFrameOrig.origin).Length;
            _recoilTimer = 0f;
            _active = true;
        }

        /// <inheritdoc/>
        public bool Update(float dt)
        {
            if (!_active) return false;

            _recoilTimer += dt;

            if (_recoilTimer >= _recoilDuration)
            {
                _active = false;
                return true;
            }

            // Smoothstep: 3t^2 - 2t^3 — eases in then out, mirrors the return push feel
            float t = _recoilTimer / _recoilDuration;
            float tEased = t * t * (3f - 2f * t);

            MatrixFrame frame = MatrixFrame.Lerp(_slideBackFrameOrig, _slideBackFrame, tEased);

            // Z-bounce: tent arch in position-space, peaks at midpoint of travel
            float zBounce = tEased < 0.5f ? tEased * 2f : (1f - tEased) * 2f;
            frame.origin.z += 0.2f * zBounce;

            _body.GameEntity.SetFrame(ref frame);

            // d/dt[smoothstep] via chain rule = 6t(1-t) / _recoilDuration
            float angularVelocity = _slideDistance * 6f * t * (1f - t) / (_recoilDuration * _wheelRadius);
            _wheelAnimator.Rotate(dt, 1f, 1f, angularVelocity);

            return false;
        }

        /// <inheritdoc/>
        public void BeginReturn()
        {
            _returnTimer = 0f;
            _returning = true;
        }

        /// <inheritdoc/>
        public bool UpdateReturn(float dt)
        {
            if (!_returning) return false;

            _returnTimer += dt;

            if (_returnTimer >= _recoil2Duration)
            {
                _returning = false;
                var orig = _slideBackFrameOrig;
                _body.GameEntity.SetFrame(ref orig);
                return true;
            }

            // Smoothstep: 3t^2 - 2t^3 — eases in then out, feels like a deliberate push
            float t = _returnTimer / _recoil2Duration;
            float tEased = t * t * (3f - 2f * t);

            MatrixFrame frame = MatrixFrame.Lerp(_slideBackFrame, _slideBackFrameOrig, tEased);
            _body.GameEntity.SetFrame(ref frame);

            // d/dt[smoothstep] via chain rule = 6t(1-t) / _recoil2Duration
            float angularVelocity = _slideDistance * 6f * t * (1f - t) / (_recoil2Duration * _wheelRadius);
            _wheelAnimator.Rotate(dt, -1f, -1f, angularVelocity);

            return false;
        }
    }
}
