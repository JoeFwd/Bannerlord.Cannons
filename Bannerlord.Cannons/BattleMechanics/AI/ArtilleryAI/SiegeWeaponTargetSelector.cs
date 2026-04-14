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
    /// Selects the most threatening enemy siege weapon (ram, tower, ballista, mangonel,
    /// trebuchet, etc.) as a cannon target. Siege weapons always outscore infantry
    /// formations (utility 0.9–1.0 vs formation cap of 0.85).
    /// </summary>
    public class SiegeWeaponTargetSelector : ITargetSelector
    {
        private readonly BaseFieldSiegeWeapon _weapon;

        public SiegeWeaponTargetSelector(BaseFieldSiegeWeapon weapon)
        {
            _weapon = weapon;
        }

        public Target FindBestTarget()
        {
            Target best = null;
            float bestScore = float.MinValue;

            foreach (SiegeWeapon sw in GetEnemySiegeWeapons())
            {
                GameEntity entity = sw.GetTargetEntity();
                if (entity == null) continue;

                Vec3 position = (entity.GlobalBoxMax + entity.GlobalBoxMin) * 0.5f;
                if (!_weapon.IsTargetInRange(position)) continue;
                if (!_weapon.IsTargetWithinDirectionRestriction(position)) continue;
                if (!_weapon.HasLineOfSightToTarget(position)) continue;

                float distance = _weapon.GameEntity.GlobalPosition.Distance(position);
                // Score 0.9 (max range) → 1.0 (close). Always above FormationUtilityCap (0.85).
                float score = 1f - Math.Min(distance / 300f, 1f) * 0.1f;

                if (score > bestScore)
                {
                    bestScore = score;
                    var t = new Target { WeaponEntity = sw, SelectedWorldPosition = position };
                    t.UtilityValue = score;
                    best = t;
                }
            }

            return best;
        }

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
