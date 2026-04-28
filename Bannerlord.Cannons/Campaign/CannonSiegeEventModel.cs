using System.Collections.Generic;
using System.Linq;
using Bannerlord.Cannons.Infrastructure.Registry;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;

namespace Bannerlord.Cannons.Integration.Campaign
{
    public class CannonSiegeEventModel : SiegeEventModel
    {
        private readonly SiegeEventModel _previous;
        private readonly CannonAvailabilityProvider _availability;
        private readonly CannonPrefabProvider _prefab;

        public CannonSiegeEventModel(SiegeEventModel previous, ICannonRegistry cannonRegistry)
        {
            _previous = previous;
            _availability = new CannonAvailabilityProvider(cannonRegistry);
            _prefab = new CannonPrefabProvider(cannonRegistry);
        }

        public override int GetSiegeEngineDestructionCasualties(SiegeEvent siegeEvent, BattleSideEnum side, SiegeEngineType destroyedSiegeEngine)
            => _previous.GetSiegeEngineDestructionCasualties(siegeEvent, side, destroyedSiegeEngine);

        public override float GetCasualtyChance(MobileParty siegeParty, SiegeEvent siegeEvent, BattleSideEnum side)
            => _previous.GetCasualtyChance(siegeParty, siegeEvent, side);

        public override int GetColleteralDamageCasualties(SiegeEngineType attackerSiegeEngine, MobileParty party)
            => _previous.GetColleteralDamageCasualties(attackerSiegeEngine, party);

        public override float GetSiegeEngineHitChance(SiegeEngineType siegeEngineType, BattleSideEnum battleSide, SiegeBombardTargets target, Town town)
            => _previous.GetSiegeEngineHitChance(siegeEngineType, battleSide, target, town);

        public override string GetSiegeEngineMapPrefabName(SiegeEngineType siegeEngineType, int wallLevel, BattleSideEnum side)
        {
            var prefabName = _prefab.GetCampaignMapPrefabName(siegeEngineType.StringId, wallLevel, side);
            return prefabName ?? _previous.GetSiegeEngineMapPrefabName(siegeEngineType, wallLevel, side);
        }

        public override string GetSiegeEngineMapProjectilePrefabName(SiegeEngineType siegeEngineType)
        {
            var projectilePrefab = _prefab.GetCampaignMapProjectilePrefabName(siegeEngineType.StringId);
            return projectilePrefab ?? _previous.GetSiegeEngineMapProjectilePrefabName(siegeEngineType);
        }

        public override string GetSiegeEngineMapReloadAnimationName(SiegeEngineType siegeEngineType, BattleSideEnum side)
        {
            var reloadName = _prefab.GetCampaignMapReloadAnimationName(siegeEngineType.StringId);
            return reloadName ?? _previous.GetSiegeEngineMapReloadAnimationName(siegeEngineType, side);
        }

        public override string GetSiegeEngineMapFireAnimationName(SiegeEngineType siegeEngineType, BattleSideEnum side)
        {
            var fireName = _prefab.GetCampaignMapFireAnimationName(siegeEngineType.StringId);
            return fireName ?? _previous.GetSiegeEngineMapFireAnimationName(siegeEngineType, side);
        }

        public override sbyte GetSiegeEngineMapProjectileBoneIndex(SiegeEngineType siegeEngineType, BattleSideEnum side)
        {
            var boneIndex = _prefab.GetCampaignMapProjectileBoneIndex(siegeEngineType.StringId);
            return boneIndex >= 0
                ? (sbyte)boneIndex
                : _previous.GetSiegeEngineMapProjectileBoneIndex(siegeEngineType, side);
        }

        public override float GetSiegeStrategyScore(SiegeEvent siege, BattleSideEnum side, SiegeStrategy strategy)
            => _previous.GetSiegeStrategyScore(siege, side, strategy);

        public override float GetConstructionProgressPerHour(SiegeEngineType type, SiegeEvent siegeEvent, ISiegeEventSide side)
            => _previous.GetConstructionProgressPerHour(type, siegeEvent, side);

        public override MobileParty GetEffectiveSiegePartyForSide(SiegeEvent siegeEvent, BattleSideEnum side)
            => _previous.GetEffectiveSiegePartyForSide(siegeEvent, side);

        public override float GetAvailableManDayPower(ISiegeEventSide side)
            => _previous.GetAvailableManDayPower(side);

        public override IEnumerable<SiegeEngineType> GetAvailableAttackerRangedSiegeEngines(PartyBase party)
        {
            var baseEngines = _previous.GetAvailableAttackerRangedSiegeEngines(party);
            var cannonEngines = _availability.GetAvailableCannons(party, BattleSideEnum.Attacker);
            return baseEngines.Concat(cannonEngines);
        }

        public override IEnumerable<SiegeEngineType> GetAvailableDefenderSiegeEngines(PartyBase party)
        {
            var baseEngines = _previous.GetAvailableDefenderSiegeEngines(party);
            var cannonEngines = _availability.GetAvailableCannons(party, BattleSideEnum.Defender);
            return baseEngines.Concat(cannonEngines);
        }

        public override IEnumerable<SiegeEngineType> GetAvailableAttackerRamSiegeEngines(PartyBase party)
            => _previous.GetAvailableAttackerRamSiegeEngines(party);

        public override IEnumerable<SiegeEngineType> GetAvailableAttackerTowerSiegeEngines(PartyBase party)
            => _previous.GetAvailableAttackerTowerSiegeEngines(party);

        public override IEnumerable<SiegeEngineType> GetPrebuiltSiegeEnginesOfSettlement(Settlement settlement)
            => _previous.GetPrebuiltSiegeEnginesOfSettlement(settlement);

        public override IEnumerable<SiegeEngineType> GetPrebuiltSiegeEnginesOfSiegeCamp(BesiegerCamp camp)
            => _previous.GetPrebuiltSiegeEnginesOfSiegeCamp(camp);

        public override float GetSiegeEngineHitPoints(SiegeEvent siegeEvent, SiegeEngineType siegeEngine, BattleSideEnum battleSide)
            => _previous.GetSiegeEngineHitPoints(siegeEvent, siegeEngine, battleSide);

        public override int GetRangedSiegeEngineReloadTime(SiegeEvent siegeEvent, BattleSideEnum side, SiegeEngineType siegeEngine)
            => _previous.GetRangedSiegeEngineReloadTime(siegeEvent, side, siegeEngine);

        public override float GetSiegeEngineDamage(SiegeEvent siegeEvent, BattleSideEnum battleSide, SiegeEngineType siegeEngine, SiegeBombardTargets target)
            => _previous.GetSiegeEngineDamage(siegeEvent, battleSide, siegeEngine, target);

        public override FlattenedTroopRoster GetPriorityTroopsForSallyOutAmbush()
            => _previous.GetPriorityTroopsForSallyOutAmbush();
    }
}
