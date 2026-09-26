using Bannerlord.Cannons.Api;
using Xunit;

namespace Bannerlord.Cannons.Tests.Api;

public class CannonTests
{
    [Fact]
    public void CampaignMapFireSoundId_PreservesExistingPositionalConstructor()
    {
        var cannon = new Cannon("test_cannon", true, false)
        {
            CampaignMapFireSoundId = "test_campaign_fire"
        };

        Assert.Equal("test_cannon", cannon.Id);
        Assert.True(cannon.IsDefensiveSiegeWeapon);
        Assert.False(cannon.IsAttackerSiegeWeapon);
        Assert.Equal("test_campaign_fire", cannon.CampaignMapFireSoundId);
    }
}
