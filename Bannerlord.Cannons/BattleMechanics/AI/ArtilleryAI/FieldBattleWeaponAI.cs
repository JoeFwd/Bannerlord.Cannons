using Bannerlord.Cannons.BattleMechanics.AI.CommonAIFunctions;
using Bannerlord.Cannons.BattleMechanics.Artillery;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.Cannons.BattleMechanics.AI.ArtilleryAI
{
    /// <summary>
    /// Artillery AI state machine. Decides when to search for targets, aim, and fire.
    /// Target selection is fully delegated to <see cref="ITargetSelector"/> implementations.
    ///
    /// Each tick the machine is in one of two modes:
    /// <list type="bullet">
    ///   <item><description>
    ///     <b>No target</b> (<see cref="TickWithoutTarget"/>): polls the target selectors
    ///     every <see cref="FindTargetInterval"/> seconds. Siege weapons are tried first;
    ///     infantry formations are the fallback.
    ///   </description></item>
    ///   <item><description>
    ///     <b>Has target</b> (<see cref="TickWithTarget"/>): updates the lead position,
    ///     aims, verifies all fire conditions, and fires. The target is consumed
    ///     immediately after firing — the next shot goes through a fresh selection cycle.
    ///   </description></item>
    /// </list>
    /// </summary>
    public class FieldBattleWeaponAI : UsableMachineAIBase
    {
        /// <summary>How often (in seconds) a new target is searched for when idle.</summary>
        private const float FindTargetInterval = 0.5f;

        private readonly BaseFieldSiegeWeapon _weapon;
        private readonly ITargetSelector _siegeWeaponSelector;
        private readonly ITargetSelector _formationSelector;
        private Target? _target;
        private Timer _findTargetTimer;

        public FieldBattleWeaponAI(BaseFieldSiegeWeapon weapon) : base(weapon)
        {
            _weapon = weapon;
            _siegeWeaponSelector = new SiegeWeaponTargetSelector(weapon);
            _formationSelector   = new FormationTargetSelector(weapon);
            _findTargetTimer     = new Timer(Mission.Current.CurrentTime, FindTargetInterval);
        }

        protected override void OnTick(Agent agentToCompareTo, Formation formationToCompareTo, Team potentialUsersTeam, float dt)
        {
            base.OnTick(agentToCompareTo, formationToCompareTo, potentialUsersTeam, dt);

            if (_weapon.PilotAgent == null || !_weapon.PilotAgent.IsAIControlled)
                return;
            if (_weapon.State != RangedSiegeWeapon.WeaponState.Idle)
                return;

            if (_target != null)
                TickWithTarget();
            else
                TickWithoutTarget();
        }

        /// <summary>
        /// Called each tick while a target is held. Updates the lead position, checks
        /// all fire conditions (aim, range, safety), and fires if they all pass.
        ///
        /// The target is cleared after a successful shot so the next tick starts a
        /// fresh selection cycle. It is also cleared when the cannon is unsafe to fire
        /// (e.g. friendlies in the way) — the AI will re-select next interval rather
        /// than waiting indefinitely for the obstruction to clear.
        /// </summary>
        private void TickWithTarget()
        {
            if (_weapon.Target != _target)
                _weapon.SetTarget(_target);
            if (_weapon.Target == null)
                return;
            if (_weapon.PilotAgent.Formation.FiringOrder.OrderType == OrderType.HoldFire)
                return;

            UpdateLeadPosition(_weapon.Target);
            Vec3 aimPoint = _weapon.Target.SelectedWorldPosition;

            bool safeToFire = _weapon.IsSafeToFire();
            if (aimPoint != Vec3.Zero && _weapon.AimAtThreat(_weapon.Target) && _weapon.IsTargetInRange(aimPoint) && safeToFire)
            {
                _weapon.AiRequestsShoot();
                _target = null; // consumed — next tick will search for a new target
            }
            else if (!safeToFire)
            {
                // Don't wait: a friendly is in the way. Clear and re-select next interval.
                _target = null;
            }
        }

        /// <summary>
        /// Called each tick while no target is held. Clears any stale weapon target and
        /// polls the selectors every <see cref="FindTargetInterval"/> seconds.
        /// Siege weapons have absolute priority; formations are the fallback.
        /// </summary>
        private void TickWithoutTarget()
        {
            _weapon.ClearTarget();
            if (_findTargetTimer.Check(Mission.Current.CurrentTime))
                _target = _siegeWeaponSelector.FindBestTarget() ?? _formationSelector.FindBestTarget();
        }

        /// <summary>
        /// Updates <paramref name="target"/>'s <c>SelectedWorldPosition</c> to the
        /// position the cannon should aim at on this tick.
        ///
        /// <b>Siege weapon targets</b>: position is refreshed to the entity's current
        /// bounding-box centre. No lead is applied — rams and towers move so slowly
        /// that prediction adds no meaningful accuracy.
        ///
        /// <b>Formation targets</b>: a random agent within the formation is chosen and
        /// its position is nudged forward by <c>velocity × estimatedFlightTime</c> to
        /// account for the formation moving during the shell's flight.
        ///
        /// Sets <c>SelectedWorldPosition</c> to <see cref="Vec3.Zero"/> when no valid
        /// agent can be resolved (e.g. formation was wiped out) — this blocks firing.
        /// </summary>
        private void UpdateLeadPosition(Target target)
        {
            if (target.WeaponEntity != null)
            {
                GameEntity entity = target.WeaponEntity.GetTargetEntity();
                target.SelectedWorldPosition = entity != null
                    ? (entity.GlobalBoxMax + entity.GlobalBoxMin) * 0.5f
                    : Vec3.Zero;
                return;
            }

            if (target.Formation == null)
            {
                target.SelectedWorldPosition = Vec3.Zero;
                return;
            }

            // Pick a fresh random agent on the first tick (SelectedWorldPosition == Zero),
            // then keep the same agent on subsequent ticks so the aim is stable.
            Agent? agent = target.SelectedWorldPosition == Vec3.Zero
                ? CommonAIUtilities.GetRandomAgent(target.Formation)
                : target.Agent;

            if (agent == null)
            {
                target.SelectedWorldPosition = Vec3.Zero;
                return;
            }

            target.Agent = agent;
            target.SelectedWorldPosition = target.Position + target.GetVelocity() * _weapon.GetEstimatedCurrentFlightTime();
        }
    }
}
