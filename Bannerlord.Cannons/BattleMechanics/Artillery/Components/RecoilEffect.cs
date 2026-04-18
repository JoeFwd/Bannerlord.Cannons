using System;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.Cannons.BattleMechanics.Artillery.Components
{
    /// <summary>
    /// Animates cannon recoil and return with travel-based wheel rotation.
    /// Back-slide uses ease-out-cubic (maximum velocity at impact, decelerates to stop).
    /// Return uses smoothstep (gradual ease-in then ease-out, feels like a deliberate push).
    /// Wheel spin is derived from traveled recoil distance and wheel radius.
    /// </summary>
    public class RecoilEffect : IRecoilEffect
    {
        private readonly SynchedMissionObject _body;
        private readonly IWheelAnimator _wheelAnimator;
        private readonly Func<float> _recoilDuration;
        private readonly Func<float> _recoil2Duration;
        private readonly Func<float> _slideBackFrameFactor;
        private readonly Func<float> _wheelRadius;

        private MatrixFrame _slideBackFrameOrigGlobal;
        private MatrixFrame _slideBackFrameGlobal;
        private MatrixFrame _returnStartFrameGlobal;
        private float _slideDistance;
        private float _recoilTimer;
        private float _recoilPrevEased;
        private bool _active;
        private bool _returning;
        private float _returnTimer;
        private float _returnPrevEased;

        public RecoilEffect(
            SynchedMissionObject body,
            IWheelAnimator wheelAnimator,
            Func<float> recoilDuration,
            Func<float> recoil2Duration,
            Func<float> slideBackFrameFactor,
            Func<float> wheelRadius)
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
            _slideBackFrameOrigGlobal = _body.GameEntity.GetGlobalFrame();
            _slideBackFrameGlobal = _slideBackFrameOrigGlobal;

            float slideBackDistance = _slideBackFrameFactor();
            Vec2 planarForward = _slideBackFrameOrigGlobal.rotation.f.AsVec2;
            if (planarForward.Length > 0.0001f)
            {
                planarForward.Normalize();
                _slideBackFrameGlobal.origin += new Vec3(
                    -planarForward.x * slideBackDistance,
                    -planarForward.y * slideBackDistance,
                    0f);
            }
            else
            {
                _slideBackFrameGlobal.origin += new Vec3(0f, -slideBackDistance, 0f);
            }

            _slideDistance = (_slideBackFrameGlobal.origin - _slideBackFrameOrigGlobal.origin).Length;
            _recoilTimer = 0f;
            _recoilPrevEased = 0f;
            _active = true;
        }

        /// <inheritdoc/>
        public bool Update(float dt)
        {
            if (!_active) return false;

            _recoilTimer += dt;

            float recoilDuration = MathF.Max(0.0001f, _recoilDuration());
            float t = MathF.Min(_recoilTimer / recoilDuration, 1f);

            // Ease-out cubic: 1 - (1 - t)^3 — explosive start, slows toward the end.
            float oneMinusT = 1f - t;
            float tEased = 1f - (oneMinusT * oneMinusT * oneMinusT);

            MatrixFrame frame = MatrixFrame.Lerp(_slideBackFrameOrigGlobal, _slideBackFrameGlobal, tEased);
            _body.GameEntity.SetGlobalFrame(ref frame);

            RotateWheelByTravel(dt, tEased, _recoilPrevEased, 1f);
            _recoilPrevEased = tEased;

            if (t >= 1f)
            {
                _active = false;
                var finalFrame = _slideBackFrameGlobal;
                _body.GameEntity.SetGlobalFrame(ref finalFrame);
                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        public void BeginReturn()
        {
            _returnStartFrameGlobal = _body.GameEntity.GetGlobalFrame();
            _returnTimer = 0f;
            _returnPrevEased = 0f;
            _returning = true;
        }

        /// <inheritdoc/>
        public bool UpdateReturn(float dt)
        {
            if (!_returning) return false;

            _returnTimer += dt;

            float recoil2Duration = MathF.Max(0.0001f, _recoil2Duration());
            float t = MathF.Min(_returnTimer / recoil2Duration, 1f);

            // Smoothstep: 3t^2 - 2t^3 — eases in then out, feels like a deliberate push.
            float tEased = t * t * (3f - 2f * t);

            MatrixFrame frame = MatrixFrame.Lerp(_returnStartFrameGlobal, _slideBackFrameOrigGlobal, tEased);
            _body.GameEntity.SetGlobalFrame(ref frame);

            RotateWheelByTravel(dt, tEased, _returnPrevEased, -1f);
            _returnPrevEased = tEased;

            if (t >= 1f)
            {
                _returning = false;
                var orig = _slideBackFrameOrigGlobal;
                _body.GameEntity.SetGlobalFrame(ref orig);
                return true;
            }

            return false;
        }

        private void RotateWheelByTravel(float dt, float easedNow, float easedPrev, float direction)
        {
            if (dt <= 0f)
                return;

            float tDelta = easedNow - easedPrev;
            if (tDelta <= 0f || _slideDistance <= 0.0001f)
                return;

            // travel = recoilDistance * easedDelta ; angular = travel / radius
            float radius = MathF.Max(0.001f, _wheelRadius());
            float travel = _slideDistance * tDelta;
            float angularDelta = travel / radius;
            if (angularDelta <= 0f)
                return;

            float angularSpeed = angularDelta / dt;
            _wheelAnimator.Rotate(dt, direction, direction, angularSpeed);
        }
    }
}
