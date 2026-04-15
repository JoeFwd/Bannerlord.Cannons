using System;
using Bannerlord.Cannons.BattleMechanics.AI.CommonAIFunctions;
using FluentAssertions;
using TaleWorlds.Library;
using Xunit;

namespace Bannerlord.Cannons.Tests;

/// <summary>
/// Tests for <see cref="CommonAIDecisionFunctions.ComputeCosAlpha"/> — a pure math
/// helper with no engine dependencies beyond <c>Vec2</c> (a plain struct from
/// TaleWorlds.Library).
///
/// ComputeCosAlpha computes the absolute cosine of the angle between the shot
/// direction and the formation's forward axis. It returns a negative sentinel
/// (-1) for degenerate (near-zero) input vectors.
/// </summary>
public class CommonAIDecisionFunctionsTests
{
    // ── ComputeCosAlpha ──────────────────────────────────────────────────────
    //
    // Key identities:
    //   Parallel vectors (0°)     → cosAlpha = 1  — shot fires straight into the depth
    //   Perpendicular vectors (90°) → cosAlpha = 0  — shot sweeps across the width
    //   Anti-parallel vectors (180°) → cosAlpha = 1  — |cos| is always non-negative
    //   45° offset                → cosAlpha = 1/√2 ≈ 0.707

    [Fact]
    public void ComputeCosAlpha_ParallelVectors_ReturnsOne()
    {
        // Shot fired exactly along the formation's forward axis.
        var forward     = new Vec2(1f, 0f);
        var toFormation = new Vec2(1f, 0f);

        float cosAlpha = CommonAIDecisionFunctions.ComputeCosAlpha(forward, toFormation);

        cosAlpha.Should().BeApproximately(1f, precision: 0.001f);
    }

    [Fact]
    public void ComputeCosAlpha_PerpendicularVectors_ReturnsZero()
    {
        // Shot fired at 90° to the formation's forward axis (pure broadside).
        var forward     = new Vec2(1f, 0f);
        var toFormation = new Vec2(0f, 1f);

        float cosAlpha = CommonAIDecisionFunctions.ComputeCosAlpha(forward, toFormation);

        cosAlpha.Should().BeApproximately(0f, precision: 0.001f);
    }

    [Fact]
    public void ComputeCosAlpha_AntiParallelVectors_ReturnsOne()
    {
        // Opposite directions still represent the same alignment angle because the
        // formula uses absolute value (|cos α|), so a 180° offset equals a 0° offset.
        var forward     = new Vec2(1f, 0f);
        var toFormation = new Vec2(-1f, 0f);

        float cosAlpha = CommonAIDecisionFunctions.ComputeCosAlpha(forward, toFormation);

        cosAlpha.Should().BeApproximately(1f, precision: 0.001f);
    }

    [Fact]
    public void ComputeCosAlpha_At45Degrees_ReturnsOneOverSqrtTwo()
    {
        var forward     = new Vec2(1f, 0f);
        var toFormation = new Vec2(1f, 1f); // 45° from forward

        float cosAlpha   = CommonAIDecisionFunctions.ComputeCosAlpha(forward, toFormation);
        float expected   = (float)(1.0 / Math.Sqrt(2));

        cosAlpha.Should().BeApproximately(expected, precision: 0.001f);
    }

    [Fact]
    public void ComputeCosAlpha_ZeroForwardVector_ReturnsNegativeSentinel()
    {
        // A zero-length forward vector has no direction — return -1 so the caller
        // can fall back to direction-independent density scoring.
        var forward     = new Vec2(0f, 0f);
        var toFormation = new Vec2(1f, 0f);

        float cosAlpha = CommonAIDecisionFunctions.ComputeCosAlpha(forward, toFormation);

        cosAlpha.Should().BeLessThan(0f,
            "degenerate forward vector should return the negative sentinel");
    }

    [Fact]
    public void ComputeCosAlpha_ZeroToFormationVector_ReturnsNegativeSentinel()
    {
        // A zero-length toFormation vector means the cannon is standing on top of
        // the formation — no meaningful direction, return -1 sentinel.
        var forward     = new Vec2(1f, 0f);
        var toFormation = new Vec2(0f, 0f);

        float cosAlpha = CommonAIDecisionFunctions.ComputeCosAlpha(forward, toFormation);

        cosAlpha.Should().BeLessThan(0f,
            "degenerate toFormation vector should return the negative sentinel");
    }

    [Theory]
    [InlineData(1f, 0f, 0f, 1f)]   // 90°
    [InlineData(1f, 0f, 1f, 1f)]   // 45°
    [InlineData(0f, 1f, -1f, 0f)]  // 90°
    [InlineData(1f, 1f, 1f, -1f)]  // 90°
    public void ComputeCosAlpha_NonDegenerateInput_ReturnsBetweenZeroAndOne(
        float fx, float fy, float tx, float ty)
    {
        var forward     = new Vec2(fx, fy);
        var toFormation = new Vec2(tx, ty);

        float cosAlpha = CommonAIDecisionFunctions.ComputeCosAlpha(forward, toFormation);

        cosAlpha.Should().BeGreaterOrEqualTo(0f)
            .And.BeLessOrEqualTo(1f,
                "cosAlpha must stay in [0, 1] for valid (non-degenerate) inputs");
    }
}
