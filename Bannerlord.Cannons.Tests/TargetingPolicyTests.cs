using Bannerlord.Cannons.BattleMechanics.Artillery.Components;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.Cannons.Tests;

public class TargetingPolicyTests
{
    [Theory]
    [InlineData(TargetFlags.NotAThreat, -1000f)]
    [InlineData(TargetFlags.IsSiegeEngine, 20f)]
    [InlineData(TargetFlags.IsStructure, 5f)]
    [InlineData(TargetFlags.IsSiegeEngine | TargetFlags.IsStructure, 1f)]
    [InlineData(TargetFlags.DebugThreat, 1000000f)]
    [InlineData(TargetFlags.IsSiegeEngine | TargetFlags.DebugThreat, 200000f)]
    public void process_target_value_applies_expected_rules(TargetFlags flags, float expected)
    {
        var policy = new TargetingPolicy();

        float value = policy.ProcessTargetValue(100f, flags);

        Assert.Equal(expected, value);
    }

    [Fact]
    public void compute_base_target_value_uses_original_formula()
    {
        var policy = new TargetingPolicy();

        float value = policy.ComputeBaseTargetValue(1.5f, 0.5f, 2f);

        Assert.Equal(60f, value);
    }

    [Fact]
    public void build_flags_marks_not_a_threat_for_destroyed_or_deactivated()
    {
        var policy = new TargetingPolicy();

        TargetFlags destroyed = policy.BuildFlags(true, false, BattleSideEnum.Attacker);
        TargetFlags deactivated = policy.BuildFlags(false, true, BattleSideEnum.Defender);

        Assert.True(destroyed.HasAnyFlag(TargetFlags.NotAThreat));
        Assert.True(deactivated.HasAnyFlag(TargetFlags.NotAThreat));
    }
}
