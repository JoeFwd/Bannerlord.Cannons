using Bannerlord.Cannons.BattleMechanics.AI.CommonAIFunctions;
using Bannerlord.Cannons.BattleMechanics.Artillery;
using Microsoft.Extensions.Logging;
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

        /// <summary>How long (in seconds) a target the cannon could not safely fire at is skipped.</summary>
        private const float BlockedTargetCooldown = 10f;

        /// <summary>How long (in seconds) the cannon may keep slewing onto a target before giving up on it.</summary>
        private const float MaxAimSeconds = 5f;

        private readonly BaseFieldSiegeWeapon _weapon;
        private readonly ITargetSelector _siegeWeaponSelector;
        private readonly ITargetSelector _formationSelector;
        private readonly ILogger _logger;
        private Target? _target;
        private Timer _findTargetTimer;
        private ITargetable? _blockedTargetable;
        private float _blockedTargetableUntil;
        private float _aimDeadline;

        public FieldBattleWeaponAI(BaseFieldSiegeWeapon weapon, ILoggerFactory loggerFactory) : base(weapon)
        {
            _weapon = weapon ?? throw new System.ArgumentNullException(nameof(weapon));
            if (loggerFactory == null) throw new System.ArgumentNullException(nameof(loggerFactory));
        
            _siegeWeaponSelector = new SiegeWeaponTargetSelector(weapon, loggerFactory);
            _formationSelector   = new MobTargetSelector(weapon);
            _logger = loggerFactory.CreateLogger<FieldBattleWeaponAI>();
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
        /// Called each tick while a target is held. Updates the lead position, aims,
        /// then checks the remaining fire conditions and fires if they all pass.
        ///
        /// The target is cleared after a successful shot so the next tick starts a
        /// fresh selection cycle. A target the cannon cannot safely fire at is dropped
        /// *and* blacklisted.
        /// </summary>
        private void TickWithTarget()
        {
            Target? target = _target;
            if (target == null)
                return;

            if (_weapon.Target != target)
                _weapon.SetTarget(target);
            if (_weapon.Target == null)
                return;

            UpdateLeadPosition(target);
            Vec3 aimPoint = target.SelectedWorldPosition;
            if (aimPoint == Vec3.Zero)
            {
                _target = null;
                return;
            }

            // Lead prediction can drift the aim point out of the traverse arc after selection.
            // Aiming anyway would slew the barrel to the arc limit, so drop the target instead.
            if (!_weapon.IsTargetWithinDirectionRestriction(aimPoint))
            {
                BlockCurrentTarget();
                _target = null;
                return;
            }

            // Aim before testing safety: IsSafeToFire() traces along the barrel's current
            // ShootingDirection, which only points at the target once AimAtTargetWorldUp has run.
            if (!ReadyToFire(aimPoint))
            {
                // Still slewing — retry next tick, but do not hold a target that never converges.
                // No blacklisting: aiming is normally slow because the weapon was reloading.
                if (Mission.Current.CurrentTime >= _aimDeadline)
                    _target = null;
                return;
            }

            if (!_weapon.IsSafeToFire())
            {
                BlockCurrentTarget();
                _target = null;
                return;
            }

            _weapon.AiRequestsShoot();
            _target = null; // consumed — next tick will search for a new target
        }

        /// <summary>
        /// Suppresses the current target for <see cref="BlockedTargetCooldown"/> seconds, so the
        /// selector falls through to another one instead of re-picking the same top score.
        /// </summary>
        private void BlockCurrentTarget()
        {
            if (_target?.TargetableObject == null)
                return;

            _blockedTargetable = _target.TargetableObject;
            _blockedTargetableUntil = Mission.Current.CurrentTime + BlockedTargetCooldown;
        }

        private bool IsBlocked(Target target)
            => target.TargetableObject != null
               && target.TargetableObject == _blockedTargetable
               && Mission.Current.CurrentTime < _blockedTargetableUntil;

        private bool ReadyToFire(Vec3 aimPoint)
            => aimPoint != Vec3.Zero
               && _weapon.AimAtTargetWorldUp(aimPoint)
               && _weapon.IsTargetInRange(aimPoint);

        /// <summary>
        /// Called each tick while no target is held. Clears any stale weapon target and
        /// polls the selectors every <see cref="FindTargetInterval"/> seconds.
        /// Siege weapons have absolute priority; formations are the fallback.
        /// </summary>
        private void TickWithoutTarget()
        {
            _weapon.ClearTarget();
            if (!_findTargetTimer.Check(Mission.Current.CurrentTime))
                return;

            Target? siegeWeaponTarget = _siegeWeaponSelector.FindBestTarget();
            if (siegeWeaponTarget != null && !IsBlocked(siegeWeaponTarget))
            {
                TrySetSelectedTarget(siegeWeaponTarget, "SiegeWeapon");
                if (_target != null)
                    return;
            }

            Target? formationTarget = _formationSelector.FindBestTarget();
            if (formationTarget != null)
                TrySetSelectedTarget(formationTarget, "Formation");
        }

        private void TrySetSelectedTarget(Target target, string targetKind)
        {
            _target = target;
            _aimDeadline = Mission.Current.CurrentTime + MaxAimSeconds;
            LogSelectedTarget(_target, targetKind);
        }

        private void LogSelectedTarget(Target target, string targetKind)
        {
            _logger.LogInformation(
                "Cannon selected target: Cannon={CannonName}, CannonEntity={CannonEntity}, CannonSide={CannonSide}, TargetKind={TargetKind}, FormationIndex={FormationIndex}, UnitCount={UnitCount}, ContainsPlayer={ContainsPlayer}, SiegeTargetEntity={SiegeTargetEntity}, Score={Score}.",
                GetCannonName(),
                (_weapon.GameEntity.IsValid ? _weapon.GameEntity.Name : null) ?? string.Empty,
                _weapon.Side,
                targetKind,
                target.Formation?.Index,
                target.Formation?.CountOfUnits,
                target.Formation != null && FormationTargetSelector.ShouldFilterOutPlayerFormation(target.Formation),
                GetTargetEntityName(target.TargetableObject),
                target.UtilityValue);
        }

        private string GetCannonName()
            => _weapon is ArtilleryRangedSiegeWeapon artillery
                ? artillery.DisplayName
                : _weapon.GetType().Name;

        private static string GetTargetEntityName(ITargetable? targetable)
        {
            if (targetable == null) return string.Empty;
            var entity = targetable.GetTargetEntity();
            return entity.IsValid ? entity.Name : string.Empty;
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
            if (target.TargetableObject != null)
            {
                WeakGameEntity entity = target.TargetableObject.GetTargetEntity();
                target.SelectedWorldPosition = entity.IsValid
                    ? (entity.GlobalBoxMax + entity.GlobalBoxMin) * 0.5f
                    : Vec3.Zero;
                return;
            }

            // Destructable target: refresh centre each tick; zero out when already destroyed.
            if (target.BlockingDestructable != null)
            {
                DestructableComponent? dc = target.BlockingDestructable
                    .GetFirstScriptOfTypeInFamily<DestructableComponent>();
                target.SelectedWorldPosition = (dc != null && !dc.IsDestroyed)
                    ? (target.BlockingDestructable.GlobalBoxMax + target.BlockingDestructable.GlobalBoxMin) * 0.5f
                    : Vec3.Zero;
                return;
            }

            // Mob target: agent is fixed at selection time; apply average mob velocity as lead.
            if (target.MobAgents != null)
            {
                Agent? mobAgent = target.Agent;
                target.SelectedWorldPosition = mobAgent != null && mobAgent.IsActive()
                    ? mobAgent.Position + target.GetVelocity() * _weapon.GetEstimatedCurrentFlightTime()
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
