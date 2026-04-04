namespace Bannerlord.Cannons.BattleMechanics.Artillery.Components
{
    /// <summary>
    /// Applies per-frame rotational delta to the cannon's left and right wheel entities.
    /// </summary>
    public interface IWheelAnimator
    {
        /// <summary>
        /// Rotates both wheels by <paramref name="dt"/> * <paramref name="speed"/> radians.
        /// </summary>
        /// <param name="dt">Frame delta time in seconds.</param>
        /// <param name="leftDir">Rotation sign for the left wheel (e.g. +1 or -1).</param>
        /// <param name="rightDir">Rotation sign for the right wheel (e.g. +1 or -1).</param>
        /// <param name="speed">Multiplier applied on top of <paramref name="dt"/>.</param>
        void Rotate(float dt, float leftDir, float rightDir, float speed = 1f);
    }
}
