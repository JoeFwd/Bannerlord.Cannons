using Bannerlord.Cannons.BattleMechanics.Artillery;
using Xunit;

namespace Bannerlord.Cannons.Tests;

public class BaseFieldSiegeWeaponTests
{
    [Theory]
    [InlineData(-1.5707964f, -0.5f, 0.2f, false)]
    [InlineData(-0.5f, -0.5f, 0.2f, true)]
    [InlineData(0f, -0.5f, 0.2f, true)]
    [InlineData(0.2f, -0.5f, 0.2f, true)]
    [InlineData(0.21f, -0.5f, 0.2f, false)]
    public void IsReleaseAngleWithinRestrictions_ReturnsExpectedResult(
        float releaseAngle,
        float bottomReleaseAngleRestriction,
        float topReleaseAngleRestriction,
        bool expected)
    {
        bool result = BaseFieldSiegeWeapon.IsReleaseAngleWithinRestrictions(
            releaseAngle,
            bottomReleaseAngleRestriction,
            topReleaseAngleRestriction);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsReleaseAngleWithinRestrictions_WhenReleaseAngleIsInvalid_ReturnsFalse()
    {
        Assert.False(BaseFieldSiegeWeapon.IsReleaseAngleWithinRestrictions(float.NaN, -0.5f, 0.2f));
        Assert.False(BaseFieldSiegeWeapon.IsReleaseAngleWithinRestrictions(float.PositiveInfinity, -0.5f, 0.2f));
    }
}
