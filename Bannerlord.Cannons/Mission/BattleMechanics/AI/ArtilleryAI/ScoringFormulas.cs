using System;

namespace Bannerlord.Cannons.BattleMechanics.AI.ArtilleryAI
{
    /// <summary>
    /// Pure-math scoring helpers used by target selectors.
    /// Contains no TaleWorlds engine dependencies and is fully unit-testable.
    /// </summary>
    public static class ScoringFormulas
    {
        /// <summary>
        /// Scores a target by distance using a cubic curve that rewards mid-range
        /// shots while slightly penalising both very close and maximum-range targets.
        ///
        /// <paramref name="normalised"/> is the raw distance divided by the axis
        /// maximum (e.g. 300 m), so it is in [0, 1].
        ///
        /// Key reference points (raw formula output, before Axis clamping to [0,1]):
        ///   x=0.00  (  0 m) → 0.781  — close range, mild penalty
        ///   x=0.13  ( 39 m) → ~0.731 — local minimum, strongest "too close" penalty
        ///   x=0.50  (150 m) → 0.926  — solid mid-range score
        ///   x=0.70  (210 m) → ~0.998 — sweet-spot peak
        ///   x=1.00  (300 m) → 0.671  — maximum range, dropped from peak
        ///
        /// Formula: 0.7 − 3(x−0.3)³ + x²
        /// The <see cref="Axis"/> wrapper clamps the final result to [0, 1].
        /// </summary>
        public static float DistanceScore(float normalised)
            => 0.7f - 3f * (float)Math.Pow(normalised - 0.3f, 3) + (float)Math.Pow(normalised, 2);

        /// <summary>
        /// Estimates how many units a single cannonball would pass through given
        /// the formation's size and the angle of attack.
        ///
        /// Rewards two things simultaneously:
        /// <list type="bullet">
        ///   <item><description><b>Dense formations</b> — more units = higher score.</description></item>
        ///   <item><description><b>Optimal angle</b> — firing along the formation's
        ///   <i>narrower</i> dimension maximises the number of soldiers the ball passes
        ///   through per metre. For a wide, shallow battle line (W &gt; D) this means
        ///   firing perpendicular to the forward axis (cosα ≈ 0). For a column (D &gt; W)
        ///   it means firing along the forward axis (cosα ≈ 1).</description></item>
        /// </list>
        ///
        /// Formula: N × (|cos α| / W + |sin α| / D)
        /// <list type="bullet">
        ///   <item><description>N = unit count</description></item>
        ///   <item><description>W = formation width (metres)</description></item>
        ///   <item><description>D = formation depth (metres)</description></item>
        ///   <item><description>α = angle between shot direction and formation's forward axis</description></item>
        /// </list>
        ///
        /// Practical output range: 0–20. The caller's <see cref="Axis"/> clamps at 20.
        /// </summary>
        /// <param name="unitCount">Number of active units in the formation.</param>
        /// <param name="width">Formation width in metres (clamped to ≥ 1 by caller).</param>
        /// <param name="depth">Formation depth in metres (clamped to ≥ 1 by caller).</param>
        /// <param name="cosAlpha">
        /// |cos α| — dot product of the normalised shot direction with the formation's
        /// normalised forward axis. Pass 1.0 for pure enfilade, 0.0 for pure frontal fire.
        /// </param>
        public static float EnfiladeScore(float unitCount, float width, float depth, float cosAlpha)
        {
            float sinAlpha = (float)Math.Sqrt(Math.Max(0f, 1f - cosAlpha * cosAlpha));
            return unitCount * (cosAlpha / width + sinAlpha / depth);
        }

        /// <summary>
        /// Scores a siege weapon target purely by proximity. Returns 1.0 at distance
        /// zero, declining linearly to 0.9 at <paramref name="maxScoringRange"/> and
        /// staying flat beyond that. This range [0.9, 1.0] is always above the
        /// formation utility cap (0.85), ensuring siege weapons are always preferred.
        /// </summary>
        /// <param name="distance">Distance in metres from cannon to target.</param>
        /// <param name="maxScoringRange">
        /// Distance at which the maximum penalty (10%) is reached.
        /// Beyond this range the score stays at 0.9.
        /// </param>
        public static float SiegeWeaponDistanceScore(float distance, float maxScoringRange)
            => 1f - Math.Min(distance / maxScoringRange, 1f) * 0.1f;
    }
}
