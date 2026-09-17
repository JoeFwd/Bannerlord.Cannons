using System;
using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.Cannons.BattleMechanics.Artillery.Components
{
    /// <summary>
    /// Activates the tagged collider entities of the cannonball pile supplied at construction.
    /// </summary>
    /// <remarks>
    /// The pile is resolved lazily: the weapon's ammo pick-up points — and therefore its pile —
    /// only exist once <c>OnInit</c> has run, which is after the components are built.
    /// </remarks>
    public class AmmoPileColliderActivator : IAmmoPileColliderActivator
    {
        private const string PileColliderTag = "pile_collider";
        private static readonly List<GameEntity> NoColliders = new();

        private readonly Func<GameEntity?> _pileEntity;
        private List<GameEntity>? _colliders;

        public AmmoPileColliderActivator(Func<GameEntity?> pileEntity)
        {
            _pileEntity = pileEntity;
        }

        /// <inheritdoc/>
        public void SetActive(bool isActive)
        {
            foreach (var collider in GetColliders())
            {
                collider.SetPhysicsState(isActive, setChildren: true);
            }
        }

        private List<GameEntity> GetColliders()
        {
            if (_colliders != null)
                return _colliders;

            var pile = _pileEntity();
            if (pile == null)
                return NoColliders;

            _colliders = pile.CollectChildrenEntitiesWithTag(PileColliderTag);
            return _colliders;
        }
    }
}
