using Bannerlord.Cannons.BattleMechanics.AI.CommonAIFunctions;
using Bannerlord.Cannons.BattleMechanics.Artillery;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.Cannons.BattleMechanics.AI.ArtilleryAI
{
    /// <summary>
    /// Artillery AI state machine. Decides when to search for targets, aim, and fire.
    /// Target selection is delegated to <see cref="ITargetSelector"/>.
    /// </summary>
    public class FieldBattleWeaponAI : UsableMachineAIBase
    {
        private const float FindTargetInterval = 0.5f;

        private readonly BaseFieldSiegeWeapon _weapon;
        private readonly ITargetSelector _targetSelector;
        private Target _target;
        private Timer _findTargetTimer;

        public FieldBattleWeaponAI(BaseFieldSiegeWeapon weapon) : base(weapon)
        {
            _weapon = weapon;
            _targetSelector = new FormationTargetSelector(weapon);
            _findTargetTimer = new Timer(Mission.Current.CurrentTime, FindTargetInterval);
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

        private void TickWithTarget()
        {
            if (_weapon.Target != _target)
                _weapon.SetTarget(_target);
            if (_weapon.Target == null)
                return;
            if (_weapon.PilotAgent.Formation.FiringOrder.OrderType == OrderType.HoldFire)
                return;

            UpdateLeadPosition(_weapon.Target);
            Vec3 position = _weapon.Target.SelectedWorldPosition;

            bool safeToFire = _weapon.IsSafeToFire();
            if (position != Vec3.Zero && _weapon.AimAtThreat(_weapon.Target) && _weapon.IsTargetInRange(position) && safeToFire)
            {
                _weapon.AiRequestsShoot();
                _target = null;
            }
            else if (!safeToFire)
            {
                _target = null;
            }
        }

        private void TickWithoutTarget()
        {
            _weapon.ClearTarget();
            if (_findTargetTimer.Check(Mission.Current.CurrentTime))
                _target = _targetSelector.FindBestTarget();
        }

        /// <summary>
        /// Adjusts <paramref name="target"/>'s world position to account for formation
        /// velocity over the current estimated flight time. Sets position to Vec3.Zero
        /// if no valid agent can be resolved (blocks the shot).
        /// </summary>
        private void UpdateLeadPosition(Target target)
        {
            if (target.Formation == null)
            {
                target.SelectedWorldPosition = Vec3.Zero;
                return;
            }

            Agent agent = target.SelectedWorldPosition == Vec3.Zero
                ? CommonAIFunctions.CommonAIFunctions.GetRandomAgent(target.Formation)
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
