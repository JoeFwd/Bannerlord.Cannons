using System;
using System.Collections.Generic;
using Bannerlord.Cannons.BattleMechanics.AI.CommonAIFunctions;
using FluentAssertions;
using Xunit;

namespace Bannerlord.Cannons.Tests;

/// <summary>
/// Tests for <see cref="AxisExtensions.GeometricMean{T}"/> and
/// <see cref="AxisExtensions.ArithmeticMean{T}"/>.
///
/// Uses <c>Axis&lt;object&gt;</c> so these tests have zero engine dependencies.
/// All lambdas are <c>_ => constant</c> and never dereference the target argument.
/// </summary>
public class AxisExtensionsTests
{
    // Helper: Axis<object> that always returns the given constant score (before clamping).
    private static Axis<object> ConstantAxis(float score)
        => new Axis<object>(0f, 1f, _ => score, _ => score);

    private static Axis<object> InactiveAxis(float score)
        => new Axis<object>(0f, 1f, _ => score, _ => score, activationFunction: _ => false);

    // ── GeometricMean ────────────────────────────────────────────────────────

    [Fact]
    public void GeometricMean_SingleAxis_EqualsAxisValue()
    {
        var axes   = new List<Axis<object>> { ConstantAxis(0.6f) };
        float mean = axes.GeometricMean(null!);
        mean.Should().BeApproximately(0.6f, 0.001f);
    }

    [Fact]
    public void GeometricMean_TwoAxes_IsGeometricMean()
    {
        // Geometric mean of 0.25 and 1.0 = sqrt(0.25 * 1.0) = 0.5
        var axes   = new List<Axis<object>> { ConstantAxis(0.25f), ConstantAxis(1.0f) };
        float mean = axes.GeometricMean(null!);
        mean.Should().BeApproximately(0.5f, 0.001f);
    }

    [Fact]
    public void GeometricMean_OneAxisZero_DropsResultToZero()
    {
        // The geometric mean is multiplicative — any axis that scores 0 wipes out the
        // total score. This is intentional: a formation that fails one critical axis
        // (e.g. is at maximum range) should not score well overall.
        var axes   = new List<Axis<object>> { ConstantAxis(0.8f), ConstantAxis(0.0f) };
        float mean = axes.GeometricMean(null!);
        mean.Should().BeApproximately(0f, 0.001f);
    }

    [Fact]
    public void GeometricMean_OneAxisInactive_ExcludedFromCalc()
    {
        // An inactive axis must be ignored; result should equal the single active axis.
        // If the inactive axis (score 0.0) were included, the result would be 0.
        var axes   = new List<Axis<object>> { ConstantAxis(0.6f), InactiveAxis(0.0f) };
        float mean = axes.GeometricMean(null!);
        mean.Should().BeApproximately(0.6f, 0.001f,
            "inactive axes must not participate in the geometric mean");
    }

    [Fact]
    public void GeometricMean_NoActiveAxes_ReturnsZero()
    {
        var axes   = new List<Axis<object>> { InactiveAxis(0.9f), InactiveAxis(0.7f) };
        float mean = axes.GeometricMean(null!);
        mean.Should().Be(0f);
    }

    [Fact]
    public void GeometricMean_EmptyList_ReturnsZero()
    {
        var axes   = new List<Axis<object>>();
        float mean = axes.GeometricMean(null!);
        mean.Should().Be(0f);
    }

    // ── ArithmeticMean ───────────────────────────────────────────────────────

    [Fact]
    public void ArithmeticMean_TwoAxes_IsArithmeticMean()
    {
        // Arithmetic mean of 0.4 and 0.8 = 0.6
        var axes   = new List<Axis<object>> { ConstantAxis(0.4f), ConstantAxis(0.8f) };
        float mean = (float)axes.ArithmeticMean(null!);
        mean.Should().BeApproximately(0.6f, 0.001f);
    }

    [Fact]
    public void ArithmeticMean_NoActiveAxes_ReturnsZero()
    {
        var axes   = new List<Axis<object>> { InactiveAxis(0.5f) };
        float mean = (float)axes.ArithmeticMean(null!);
        mean.Should().Be(0f);
    }
}
