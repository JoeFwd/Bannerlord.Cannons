namespace Bannerlord.Cannons.BattleMechanics.Artillery.Components
{
    /// <summary>
    /// Enables or disables the physics state of the cannonball pile's collider entities.
    /// </summary>
    /// <remarks>
    /// The engine's <c>SetPhysicsState</c> is a native call that drives both the rigid body and
    /// the entity's visibility — vanilla relies on that coupling in <c>DeploymentPoint</c>, where
    /// the same flag is named <c>visible</c>. Deactivating the colliders therefore hides the pile.
    /// </remarks>
    public interface IAmmoPileColliderActivator
    {
        /// <summary>
        /// Applies <paramref name="isActive"/> to every collider on the pile and its children.
        /// </summary>
        void SetActive(bool isActive);
    }
}
