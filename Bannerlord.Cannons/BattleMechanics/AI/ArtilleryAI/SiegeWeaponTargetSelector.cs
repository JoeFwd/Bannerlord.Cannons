using System;
using System.Collections.Generic;
using System.Linq;
using Bannerlord.Cannons.BattleMechanics.AI.CommonAIFunctions;
using Bannerlord.Cannons.BattleMechanics.Artillery;
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
    /// returned here is in [0.9, 1.0], which is guaranteed to exceed the formation
    /// selector's cap of 0.85 (<see cref="FormationTargetSelector"/>).
    ///
    /// Within the siege weapon tier, closer targets score higher (up to 10% bonus),
    /// but any shootable siege weapon beats any formation regardless of distance.
    /// </summary>
    public class SiegeWeaponTargetSelector : ITargetSelector
    {
        // Score range for siege weapon targets. The minimum (0.9) must exceed the
        // formation utility cap (0.85) so siege weapons always win the priority race.
        private const float MaxScoringRangeMetres = 300f; // flat penalty beyond this distance
        private const float MaxDistancePenalty    = 0.1f; // score floor = 1.0 - 0.1 = 0.9

        private readonly BaseFieldSiegeWeapon _weapon;

        public SiegeWeaponTargetSelector(BaseFieldSiegeWeapon weapon)
        {
            _weapon = weapon;
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
                GameEntity? entity = siegeWeapon.GetTargetEntity();
                if (entity == null) continue;

                Vec3 position = (entity.GlobalBoxMax + entity.GlobalBoxMin) * 0.5f;

                if (!_weapon.IsTargetInRange(position))                  continue;
                if (!_weapon.IsTargetWithinDirectionRestriction(position)) continue;
                if (!_weapon.HasLineOfSightToTarget(position))            continue;

                float distance = _weapon.GameEntity.GlobalPosition.Distance(position);
                float score    = ScoringFormulas.SiegeWeaponDistanceScore(distance, MaxScoringRangeMetres);

                if (score > bestScore)
                {
                    bestScore = score;
                    best = new Target { WeaponEntity = siegeWeapon, SelectedWorldPosition = position };
                    best.UtilityValue = score;
                }
            }

            return best;
        }

        /// <summary>
        /// Enumerates active enemy siege weapons. Destroyed weapons (checked via
        /// <see cref="DestructableComponent"/>) are excluded.
        /// </summary>
        private IEnumerable<SiegeWeapon> GetEnemySiegeWeapons()
        {
            return Mission.Current.ActiveMissionObjects
                .FindAllWithType<SiegeWeapon>()
                .Where(sw => sw.Side != BattleSideEnum.None
                    && sw.Side != _weapon.Side
                    && (sw.DestructionComponent == null || !sw.DestructionComponent.IsDestroyed));
        }
    }
}
