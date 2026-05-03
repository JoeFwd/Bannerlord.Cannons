using System.Reflection;
using System.Runtime.Serialization;
using Bannerlord.Cannons.BattleMechanics.AI.ArtilleryAI;
using TaleWorlds.MountAndBlade;
using Xunit;

namespace Bannerlord.Cannons.Tests;

public class FormationTargetSelectorTests
{
    [Fact]
    public void ShouldFilterOutPlayerFormation_WhenPlayerTroopIsInFormation_ReturnsTrue()
    {
        Formation formation = CreateFormation(
            isPlayerTroopInFormation: true,
            hasPlayerControlledTroop: false);

        Assert.True(FormationTargetSelector.ShouldFilterOutPlayerFormation(formation));
    }

    [Fact]
    public void ShouldFilterOutPlayerFormation_WhenFormationHasPlayerControlledTroop_ReturnsTrue()
    {
        Formation formation = CreateFormation(
            isPlayerTroopInFormation: false,
            hasPlayerControlledTroop: true);

        Assert.True(FormationTargetSelector.ShouldFilterOutPlayerFormation(formation));
    }

    [Fact]
    public void ShouldFilterOutPlayerFormation_WhenFormationHasNoPlayer_ReturnsFalse()
    {
        Formation formation = CreateFormation(
            isPlayerTroopInFormation: false,
            hasPlayerControlledTroop: false);

        Assert.False(FormationTargetSelector.ShouldFilterOutPlayerFormation(formation));
    }

    private static Formation CreateFormation(bool isPlayerTroopInFormation, bool hasPlayerControlledTroop)
    {
#pragma warning disable SYSLIB0050
        var formation = (Formation)FormatterServices.GetUninitializedObject(typeof(Formation));
#pragma warning restore SYSLIB0050

        SetBackingField(formation, nameof(Formation.IsPlayerTroopInFormation), isPlayerTroopInFormation);
        SetBackingField(formation, nameof(Formation.HasPlayerControlledTroop), hasPlayerControlledTroop);
        return formation;
    }

    private static void SetBackingField(Formation formation, string propertyName, bool value)
    {
        FieldInfo? field = typeof(Formation).GetField(
            $"<{propertyName}>k__BackingField",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(field);
        field.SetValue(formation, value);
    }
}
