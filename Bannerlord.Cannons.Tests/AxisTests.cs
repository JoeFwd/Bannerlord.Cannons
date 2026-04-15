using Bannerlord.Cannons.BattleMechanics.AI.CommonAIFunctions;
using FluentAssertions;
using Xunit;

namespace Bannerlord.Cannons.Tests;

/// <summary>
/// Tests for <see cref="Axis{T}.Evaluate"/> and <see cref="Axis{T}.IsActive"/>.
///
/// Uses <c>Axis&lt;object&gt;</c> with simple constant-returning lambdas so these tests
/// have zero engine dependencies — they verify only the normalisation and clamping
/// mathematics inside the generic Axis class.
/// </summary>
public class AxisTests
{
    // Helper: Axis<object> [min, max] with identity output (returns normalised value as-is)
    // and a parameter function that returns a fixed constant, ignoring the target.
    private static Axis<object> MakeLinearAxis(float paramValue, float min = 0f, float max = 100f)
        => new Axis<object>(min, max, x => x, _ => paramValue);

    // ── Evaluate: normalisation ──────────────────────────────────────────────

    [Fact]
    public void Evaluate_InputAtMin_ReturnsZero()
    {
        var axis = MakeLinearAxis(paramValue: 0f, min: 0f, max: 100f);
        axis.Evaluate(null!).Should().BeApproximately(0f, 0.001f);
    }

    [Fact]
    public void Evaluate_InputAtMax_ReturnsOne()
    {
        var axis = MakeLinearAxis(paramValue: 100f, min: 0f, max: 100f);
        axis.Evaluate(null!).Should().BeApproximately(1f, 0.001f);
    }

    [Fact]
    public void Evaluate_InputAtMidpoint_Returns0_5()
    {
        var axis = MakeLinearAxis(paramValue: 50f, min: 0f, max: 100f);
        axis.Evaluate(null!).Should().BeApproximately(0.5f, 0.001f);
    }

    // ── Evaluate: clamping ───────────────────────────────────────────────────

    [Fact]
    public void Evaluate_InputBelowMin_ClampsToZero()
    {
        // Parameter returns -50, below the axis minimum of 0.
        var axis = MakeLinearAxis(paramValue: -50f, min: 0f, max: 100f);
        axis.Evaluate(null!).Should().BeApproximately(0f, 0.001f);
    }

    [Fact]
    public void Evaluate_InputAboveMax_ClampsToOne()
    {
        // Parameter returns 200, above the axis maximum of 100.
        var axis = MakeLinearAxis(paramValue: 200f, min: 0f, max: 100f);
        axis.Evaluate(null!).Should().BeApproximately(1f, 0.001f);
    }

    [Fact]
    public void Evaluate_OutputFunctionAbove1_ClampsToOne()
    {
        // Output function returns 5.0 — the Axis must clamp it to 1.
        var axis = new Axis<object>(0f, 1f, _ => 5f, _ => 0.5f);
        axis.Evaluate(null!).Should().BeApproximately(1f, 0.001f);
    }

    [Fact]
    public void Evaluate_OutputFunctionBelow0_ClampsToZero()
    {
        // Output function returns -2.0 — the Axis must clamp it to 0.
        var axis = new Axis<object>(0f, 1f, _ => -2f, _ => 0.5f);
        axis.Evaluate(null!).Should().BeApproximately(0f, 0.001f);
    }

    // ── IsActive ─────────────────────────────────────────────────────────────

    [Fact]
    public void IsActive_NoActivationFunction_ReturnsTrue()
    {
        var axis = MakeLinearAxis(paramValue: 50f);
        axis.IsActive(null!).Should().BeTrue();
    }

    [Fact]
    public void IsActive_ActivationFunctionReturnsTrue_ReturnsTrue()
    {
        var axis = new Axis<object>(0f, 100f, x => x, _ => 50f, activationFunction: _ => true);
        axis.IsActive(null!).Should().BeTrue();
    }

    [Fact]
    public void IsActive_ActivationFunctionReturnsFalse_ReturnsFalse()
    {
        var axis = new Axis<object>(0f, 100f, x => x, _ => 50f, activationFunction: _ => false);
        axis.IsActive(null!).Should().BeFalse();
    }
}
