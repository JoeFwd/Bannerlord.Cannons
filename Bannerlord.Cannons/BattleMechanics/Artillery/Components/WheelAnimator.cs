using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.Cannons.BattleMechanics.Artillery.Components
{
    /// <summary>
    /// Applies per-frame rotational delta to the cannon's left and right wheel entities on the
    /// configured axis.
    /// </summary>
    public class WheelAnimator : IWheelAnimator
    {
        private readonly SynchedMissionObject _wheelL;
        private readonly SynchedMissionObject _wheelR;
        private readonly WheelRotationAxis _axis;

        public WheelAnimator(
            SynchedMissionObject wheelL,
            SynchedMissionObject wheelR,
            WheelRotationAxis axis)
        {
            _wheelL = wheelL;
            _wheelR = wheelR;
            _axis = axis;
        }

        /// <inheritdoc/>
        public void Rotate(float dt, float leftDir, float rightDir, float speed = 1f)
        {
            MatrixFrame frameL = _wheelL.GameEntity.GetFrame();
            MatrixFrame frameR = _wheelR.GameEntity.GetFrame();

            if (_axis == WheelRotationAxis.Y)
            {
                frameL.rotation.RotateAboutForward(leftDir  * dt * speed);
                frameR.rotation.RotateAboutForward(rightDir * dt * speed);
            }
            else // X
            {
                frameL.rotation.RotateAboutSide(leftDir  * dt * speed);
                frameR.rotation.RotateAboutSide(rightDir * dt * speed);
            }

            _wheelL.GameEntity.SetFrame(ref frameL);
            _wheelR.GameEntity.SetFrame(ref frameR);
        }
    }
}
