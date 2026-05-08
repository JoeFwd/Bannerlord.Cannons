using System;
using System.Collections.Generic;
using Bannerlord.Cannons.Domain.Ammo;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.Cannons.BattleMechanics.Artillery.Components
{
    /// <summary>
    /// Drives the per-frame logic for crew members picking up cannon balls from
    /// ammo-pile standing points and returning them to the load position.
    /// </summary>
    public class AmmoPickupHandler : IAmmoPickupHandler
    {
        private static readonly ActionIndexCache act_pickup_boulder_begin =
            ActionIndexCache.Create("act_pickup_boulder_begin");

        private static readonly ActionIndexCache act_pickup_boulder_end =
            ActionIndexCache.Create("act_pickup_boulder_end");

        private readonly Dictionary<int, Action> _onCarriedProjectileDroppedCache = new();
        private readonly AmmoLimit _ammoComponent;

        public AmmoPickupHandler(AmmoLimit ammoComponent)
        {
            _ammoComponent = ammoComponent ?? throw new ArgumentNullException(nameof(ammoComponent));
        }

        public void Update(
            StandingPointWithWeaponRequirement? activePickupPoint,
            StandingPoint loadAmmoPoint,
            StandingPoint? reloaderOriginalPoint,
            ref Agent? reloaderAgent,
            ItemObject originalMissileItem,
            ItemObject loadedMissileItem,
            ActionIndexCache loadAmmoEndAction,
            UsableMachine machine)
        {
            if (activePickupPoint == null || !activePickupPoint.HasUser)
                return;

            var user = activePickupPoint.UserAgent;
            var action = user.GetCurrentAction(1);

            if (action == act_pickup_boulder_begin)
                return;

            if (action == act_pickup_boulder_end)
            {
                if (!_ammoComponent.TryConsumeAmmo())
                {
                    user.StopUsingGameObject(true, Agent.StopUsingGameObjectFlags.None);
                    return;
                }

                MissionWeapon missionWeapon = new MissionWeapon(loadedMissileItem, null, null, 1);
                user.EquipWeaponToExtraSlotAndWield(ref missionWeapon);
                user.StopUsingGameObject(true, Agent.StopUsingGameObjectFlags.None);

                if (CanIssueScriptedMove(user, machine))
                {
                    if (!loadAmmoPoint.HasUser && !loadAmmoPoint.IsDeactivated)
                    {
                        user.AIMoveToGameObjectEnable(loadAmmoPoint, machine, Agent.AIScriptedFrameFlags.NoAttack);
                    }
                    else if (reloaderOriginalPoint != null
                             && !reloaderOriginalPoint.HasUser
                             && !reloaderOriginalPoint.HasAIMovingTo)
                    {
                        user.AIMoveToGameObjectEnable(reloaderOriginalPoint, machine, Agent.AIScriptedFrameFlags.NoAttack);
                    }
                    else
                    {
                        Formation? formation = reloaderAgent?.Formation;
                        formation?.AttachUnit(reloaderAgent);
                        reloaderAgent = null;
                    }
                }
            }
            else if (!user.SetActionChannel(1, act_pickup_boulder_begin))
            {
                user.StopUsingGameObject(true);
            }

            if (!_onCarriedProjectileDroppedCache.ContainsKey(user.Index)
                && user.WieldedWeapon.Item == originalMissileItem)
            {
                _onCarriedProjectileDroppedCache[user.Index] = () => OnCarriedProjectileDropped(user, loadAmmoEndAction);
                user.OnAgentWieldedItemChange += _onCarriedProjectileDroppedCache[user.Index];
            }
        }

        private static bool CanIssueScriptedMove(Agent candidate, UsableMachine usableMachine)
            => candidate != null
               && candidate.IsAIControlled
               && candidate.IsActive()
               && candidate.Team != null
               && candidate.Detachment == usableMachine;

        private void OnCarriedProjectileDropped(Agent agent, ActionIndexCache loadAmmoEndAction)
        {
            agent.OnAgentWieldedItemChange -= _onCarriedProjectileDroppedCache[agent.Index];
            _onCarriedProjectileDroppedCache.Remove(agent.Index);

            if (agent.GetCurrentAction(1)?.Index != loadAmmoEndAction.Index)
                agent.StopUsingGameObject();
        }
    }
}
