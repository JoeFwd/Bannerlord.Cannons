using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Selects the most threatening enemy siege weapon (battering ram, siege tower,
    /// ballista, mangonel, trebuchet, etc.) as a cannon target.
    ///
    /// Siege weapons are always preferred over infantry formations: the utility score
    /// returned here is in [<see cref="ArtilleryAIConstants.SiegeWeaponScoreFloor"/>, 1.0],
    /// which is guaranteed to exceed the formation selector's cap of
    /// <see cref="ArtilleryAIConstants.FormationUtilityCap"/> (<see cref="FormationTargetSelector"/>).
    ///
    /// Within the siege weapon tier, closer targets score higher (up to 10% bonus),
    /// but any shootable siege weapon beats any formation regardless of distance.
    /// </summary>
    public class SiegeWeaponTargetSelector : ITargetSelector
    {
        private readonly BaseFieldSiegeWeapon _weapon;
        private readonly ILogger _logger;

        public SiegeWeaponTargetSelector(
            BaseFieldSiegeWeapon weapon,
            ILoggerFactory loggerFactory)
        {
            _weapon = weapon ?? throw new ArgumentNullException(nameof(weapon));
            _logger = (loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory)))
                .CreateLogger<SiegeWeaponTargetSelector>();
        }

        /// <summary>
        /// Evaluates all shootable enemy siege weapons and returns the highest-scoring
        /// one, or <c>null</c> if none are reachable.
        /// </summary>
        public Target FindBestTarget()
        {
            Target? best = null;
            float bestScore = float.MinValue;

            foreach (SiegeWeapon siegeWeapon in GetEnemySiegeWeapons())
            {
                WeakGameEntity entity = siegeWeapon.GetTargetEntity();
                if (!entity.IsValid) continue;

                Vec3 position = GetTargetPosition(entity);
                if (!IsShootable(position))
                    continue;

                float distance = _weapon.GameEntity.GlobalPosition.Distance(position);
                float score    = ScoringFormulas.SiegeWeaponDistanceScore(distance, ArtilleryAIConstants.MaxTargetRangeMetres);
                LogSiegeWeaponScore(siegeWeapon, entity, distance, score);

                if (score > bestScore)
                {
                    bestScore = score;
                    Target candidate = new Target { TargetableObject = siegeWeapon };
                    candidate.SelectedWorldPosition = position;
                    candidate.UtilityValue = score;
                    best = candidate;
                }
            }

            return best;
        }

        private void LogSiegeWeaponScore(SiegeWeapon siegeWeapon, WeakGameEntity entity, float distance, float score)
        {
            if (score <= 0f)
                return;

            _logger.LogInformation(
                "Cannon siege-weapon target score: Cannon={CannonName}, CannonEntity={CannonEntity}, CannonSide={CannonSide}, SiegeWeaponType={SiegeWeaponType}, SiegeTargetEntity={SiegeTargetEntity}, SiegeTargetSide={SiegeTargetSide}, Distance={Distance}, Score={Score}.",
                GetCannonName(),
                (_weapon.GameEntity.IsValid ? _weapon.GameEntity.Name : null) ?? string.Empty,
                _weapon.Side,
                siegeWeapon.GetType().Name,
                entity.Name,
                siegeWeapon.Side,
                distance,
                score);
        }

        private string GetCannonName()
            => _weapon is ArtilleryRangedSiegeWeapon artillery
                ? artillery.DisplayName
                : _weapon.GetType().Name;

        private bool IsShootable(Vec3 position)
            => _weapon.IsTargetInRange(position)
               && _weapon.IsTargetWithinDirectionRestriction(position)
               && _weapon.HasLineOfSightToTarget(position);

        private static Vec3 GetTargetPosition(WeakGameEntity entity)
            => (entity.GlobalBoxMax + entity.GlobalBoxMin) * 0.5f;

        /// <summary>
        /// Enumerates active enemy siege weapons.
        ///
        /// <see cref="SiegeWeapon.IsDeactivated"/> subsumes destroyed, disabled and
        /// invalid-entity weapons, and each weapon type overrides it to also mean
        /// "this engine has finished its job": a battering ram reports deactivated
        /// once it has arrived at its gate and that gate is open or destroyed, so a
        /// spent ram stops being a target. Mirrors the filter vanilla artillery uses
        /// in RangedSiegeWeaponAi.ThreatSeeker.GetAllThreats.
        /// </summary>
        private IEnumerable<SiegeWeapon> GetEnemySiegeWeapons()
        {
            return Mission.Current.ActiveMissionObjects
                .FindAllWithType<SiegeWeapon>()
                .Where(sw => sw.Side != BattleSideEnum.None
                    && sw.Side != _weapon.Side
                    && sw is not SiegeLadder
                    && !sw.IsDeactivated);
        }
    }
}
