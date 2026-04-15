using Bannerlord.Cannons.BattleMechanics.AI.ArtilleryAI;
using FluentAssertions;
using Xunit;

namespace Bannerlord.Cannons.Tests;

/// <summary>
/// Tests for <see cref="ScoringFormulas"/> — pure math with no engine dependencies.
///
/// The DistanceScore curve: f(x) = 0.7 − 3(x−0.3)³ + x²
/// Reference values (computed analytically):
///   x=0.00 → 0.781   (close range, mild penalty)
///   x=0.13 → ~0.731  (local minimum — the strongest "too close" point)
///   x=0.50 → 0.926   (good mid-range)
///   x=0.70 → ~0.998  (peak sweet-spot ≈ 210 m)
///   x=1.00 → 0.671   (maximum range — still scores but below the peak)
/// The Axis wrapper clamps the final value to [0, 1].
/// </summary>
public class ScoringFormulasTests
{
    // ── DistanceScore ────────────────────────────────────────────────────────

    [Fact]
    public void DistanceScore_AtZero_ReturnsAbove0_7()
    {
        // Point-blank shot: formula gives 0.781 (not lowest — the real dip is ~39 m).
        float score = ScoringFormulas.DistanceScore(0f);
        score.Should().BeApproximately(0.781f, precision: 0.002f);
    }

    [Fact]
    public void DistanceScore_AtLocalMinimum_LowerThanPeak()
    {
        // The curve dips around x≈0.13 (≈39 m) before rising to the peak at x≈0.7.
        float atMin  = ScoringFormulas.DistanceScore(0.13f);
        float atPeak = ScoringFormulas.DistanceScore(0.70f);
        atMin.Should().BeLessThan(atPeak, "the close-range dip scores below the sweet-spot peak");
    }

    [Fact]
    public void DistanceScore_AtPeak_IsNearOne()
    {
        // Peak is around x≈0.7 (≈210 m); score should be very close to 1.0 before clamping.
        float score = ScoringFormulas.DistanceScore(0.70f);
        score.Should().BeGreaterThan(0.99f);
    }

    [Fact]
    public void DistanceScore_At0_5_IsAbove0_9()
    {
        // Good mid-range (150 m): should score above 0.9.
        float score = ScoringFormulas.DistanceScore(0.5f);
        score.Should().BeGreaterThan(0.9f);
    }

    [Fact]
    public void DistanceScore_AtOne_IsApproximately0_67()
    {
        // At maximum axis range (300 m) the formula gives ~0.671 — the score drops
        // noticeably from the peak but the Axis ensures the cannon still considers it.
        float score = ScoringFormulas.DistanceScore(1.0f);
        score.Should().BeApproximately(0.671f, precision: 0.002f);
    }

    [Fact]
    public void DistanceScore_MidRange_ScoresHigherThanMaxRange()
    {
        float mid = ScoringFormulas.DistanceScore(0.5f);
        float max = ScoringFormulas.DistanceScore(1.0f);
        mid.Should().BeGreaterThan(max, "mid-range is the sweet-spot of the cubic curve");
    }

    [Theory]
    [InlineData(0.0f)]
    [InlineData(0.1f)]
    [InlineData(0.25f)]
    [InlineData(0.5f)]
    [InlineData(0.75f)]
    [InlineData(1.0f)]
    public void DistanceScore_AnyNormalisedInput_ReturnsFiniteValue(float x)
    {
        // Formula produces finite, non-NaN values across the full normalised range.
        // Values slightly outside [0,1] are expected and intentional; the Axis clamps them.
        float score = ScoringFormulas.DistanceScore(x);
        score.Should().NotBe(float.NaN);
        score.Should().NotBe(float.PositiveInfinity);
        score.Should().NotBe(float.NegativeInfinity);
    }

    // ── EnfiladeScore ────────────────────────────────────────────────────────
    // Formula: N × (|cosα|/W + |sinα|/D)
    // α = angle between shot direction and formation's forward axis.
    //
    // Insight: the formula rewards whichever axis is the NARROWER dimension.
    //   • If depth < width: firing perpendicular to forward (sinα=1) gives N/D > N/W.
    //   • If depth > width: firing along forward (cosα=1) gives N/W > N/D.
    // This correctly maximises penetration along the formation's thinnest direction.

    [Fact]
    public void EnfiladeScore_ShallowFormation_PerpendicularShotScoresHigher()
    {
        // Formation is wide (W=20) and shallow (D=5).
        // Firing perpendicular to forward axis (sinAlpha=1) traverses the depth D=5 m,
        // penetrating more soldiers per metre than firing along the width.
        float perpendicular = ScoringFormulas.EnfiladeScore(unitCount: 30, width: 20, depth: 5, cosAlpha: 0f);
        float parallel      = ScoringFormulas.EnfiladeScore(unitCount: 30, width: 20, depth: 5, cosAlpha: 1f);

        perpendicular.Should().BeGreaterThan(parallel,
            "for a shallow formation (D<W), firing through the depth hits more per metre");
    }

    [Fact]
    public void EnfiladeScore_ColumnFormation_ParallelShotScoresHigher()
    {
        // Formation is narrow (W=5) and deep (D=20) — a column.
        // Firing along the forward axis (cosAlpha=1) traverses the narrow width W=5 m.
        float parallel      = ScoringFormulas.EnfiladeScore(unitCount: 30, width: 5, depth: 20, cosAlpha: 1f);
        float perpendicular = ScoringFormulas.EnfiladeScore(unitCount: 30, width: 5, depth: 20, cosAlpha: 0f);

        parallel.Should().BeGreaterThan(perpendicular,
            "for a column formation (W<D), firing through the width hits more per metre");
    }

    [Fact]
    public void EnfiladeScore_DenseFormation_HigherThanSparse()
    {
        // More units with the same footprint means more hits.
        float dense  = ScoringFormulas.EnfiladeScore(unitCount: 60, width: 10, depth: 5, cosAlpha: 0.5f);
        float sparse = ScoringFormulas.EnfiladeScore(unitCount: 20, width: 10, depth: 5, cosAlpha: 0.5f);

        dense.Should().BeGreaterThan(sparse);
    }

    [Fact]
    public void EnfiladeScore_IsNonNegative()
    {
        float score = ScoringFormulas.EnfiladeScore(unitCount: 10, width: 8, depth: 4, cosAlpha: 0.7f);
        score.Should().BeGreaterOrEqualTo(0f);
    }

    [Fact]
    public void EnfiladeScore_ZeroUnits_ReturnsZero()
    {
        float score = ScoringFormulas.EnfiladeScore(unitCount: 0, width: 10, depth: 5, cosAlpha: 0.5f);
        score.Should().Be(0f);
    }

    // ── SiegeWeaponDistanceScore ─────────────────────────────────────────────

    [Fact]
    public void SiegeWeaponDistanceScore_AtZero_ReturnsOne()
    {
        float score = ScoringFormulas.SiegeWeaponDistanceScore(distance: 0f, maxScoringRange: 300f);
        score.Should().Be(1.0f);
    }

    [Fact]
    public void SiegeWeaponDistanceScore_AtMaxRange_Returns0_9()
    {
        float score = ScoringFormulas.SiegeWeaponDistanceScore(distance: 300f, maxScoringRange: 300f);
        score.Should().BeApproximately(0.9f, precision: 0.001f);
    }

    [Fact]
    public void SiegeWeaponDistanceScore_BeyondMaxRange_StaysAt0_9()
    {
        // Score should not drop below 0.9 regardless of how far the target is.
        float score = ScoringFormulas.SiegeWeaponDistanceScore(distance: 9999f, maxScoringRange: 300f);
        score.Should().BeApproximately(0.9f, precision: 0.001f);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(100f)]
    [InlineData(300f)]
    [InlineData(1000f)]
    public void SiegeWeaponDistanceScore_AlwaysAboveFormationCap(float distance)
    {
        // FormationTargetSelector caps formation utility at 0.85.
        // Any siege weapon score must exceed that cap so siege weapons always win priority.
        const float formationUtilityCap = 0.85f;
        float score = ScoringFormulas.SiegeWeaponDistanceScore(distance, maxScoringRange: 300f);
        score.Should().BeGreaterThan(formationUtilityCap,
            "siege weapons must always outprioritise infantry formations");
    }
}
