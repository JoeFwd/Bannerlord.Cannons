using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace Bannerlord.Cannons.BattleMechanics.Artillery.Components
{
    /// <summary>
    /// Drives the barrel-recoil animation (slide back, then explicit return) after a shot is fired.
    /// The two phases are decoupled: <see cref="Update"/> drives only the back-slide and signals
    /// completion; the caller then triggers the return phase via <see cref="BeginReturn"/> once
    /// the crew push-back action has been performed.
    /// </summary>
    public interface IRecoilEffect
    {
        /// <summary>
        /// Captures the current body frame and begins the recoil back-slide.
        /// Call this when the weapon enters <c>WaitingAfterShooting</c>.
        /// </summary>
        /// <param name="bodyFrame">The current local frame of the cannon body entity.</param>
        void Begin(MatrixFrame bodyFrame);

        /// <summary>
        /// Advances the back-slide phase by <paramref name="dt"/> seconds.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> when the back-slide has completed and the caller should
        /// transition to <c>LoadingAmmo</c>. The body remains in the recoiled position
        /// until <see cref="BeginReturn"/> is called.
        /// </returns>
        bool Update(float dt);

        /// <summary>
        /// Begins the return phase, lerping the body back to its original firing position.
        /// Call this when the crew push-back action completes.
        /// </summary>
        void BeginReturn();

        /// <summary>
        /// Advances the return phase by <paramref name="dt"/> seconds.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> when the body has fully returned to its firing position.
        /// </returns>
        bool UpdateReturn(float dt);
    }
}
