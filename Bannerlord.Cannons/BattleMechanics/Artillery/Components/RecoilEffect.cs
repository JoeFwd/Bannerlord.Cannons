using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.Cannons.BattleMechanics.Artillery.Components
{
    /// <summary>
    /// Lerps the cannon body backward then back to its origin to simulate barrel recoil,
    /// and drives the wheel spin via <see cref="IWheelAnimator"/> during the animation.
    /// </summary>
    public class RecoilEffect : IRecoilEffect
    {
        private readonly SynchedMissionObject _body;
        private readonly IWheelAnimator _wheelAnimator;
        private readonly float _recoilDuration;
        private readonly float _recoil2Duration;
        private readonly float _slideBackFrameFactor;

        private MatrixFrame _slideBackFrameOrig;
        private MatrixFrame _slideBackFrame;
        private float _recoilTimer;
        private bool _active;

        public RecoilEffect(
            SynchedMissionObject body,
            IWheelAnimator wheelAnimator,
            float recoilDuration,
            float recoil2Duration,
            float slideBackFrameFactor)
        {
            _body = body;
            _wheelAnimator = wheelAnimator;
            _recoilDuration = recoilDuration;
            _recoil2Duration = recoil2Duration;
            _slideBackFrameFactor = slideBackFrameFactor;
        }

        /// <inheritdoc/>
        public void Begin(MatrixFrame bodyFrame)
        {
            _slideBackFrameOrig = bodyFrame;
            _slideBackFrame = bodyFrame.Advance(_slideBackFrameFactor);
            _recoilTimer = 0f;
            _active = true;
        }

        /// <inheritdoc/>
        public bool Update(float dt)
        {
            if (!_active) return false;

            _recoilTimer += dt;

            if (_recoilTimer > _recoilDuration + _recoil2Duration)
            {
                _active = false;
                return true; // signal caller to transition to LoadingAmmo
            }

            if (_recoilTimer < _recoilDuration)
            {
                float amount = _recoilTimer / _recoilDuration;
                MatrixFrame frame = MatrixFrame.Lerp(_slideBackFrameOrig, _slideBackFrame, amount);

                if (amount < 0.5f)
                    frame.origin.z = MBMath.Lerp(frame.origin.z, frame.origin.z + 0.2f, amount * 2f);
                else
                    frame.origin.z = MBMath.Lerp(frame.origin.z, frame.origin.z + 0.2f, 1f - amount);

                _body.GameEntity.SetFrame(ref frame);
                _wheelAnimator.Rotate(dt, 1f, 1f, 5f);
            }
            else if (_recoilTimer < _recoil2Duration)
            {
                float amount = (_recoilTimer - _recoilDuration) / _recoil2Duration;
                MatrixFrame frame = MatrixFrame.Lerp(_slideBackFrame, _slideBackFrameOrig, amount);
                _body.GameEntity.SetFrame(ref frame);
                _wheelAnimator.Rotate(dt, 1f, 1f);
            }

            return false;
        }
    }
}
