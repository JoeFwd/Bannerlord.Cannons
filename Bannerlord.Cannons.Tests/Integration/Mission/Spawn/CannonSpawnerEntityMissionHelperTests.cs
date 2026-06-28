using System.Linq;
using Bannerlord.Cannons.Integration.Mission.Spawn;
using Xunit;

namespace Bannerlord.Cannons.Tests.Integration.Mission.Spawn;

public class CannonSpawnerEntityMissionHelperTests
{
    [Fact]
    public void GetPrefabNameCandidates_RemovesSpawnerAndTrailingSegments()
    {
        var candidates = CannonSpawnerEntityMissionHelper
            .GetPrefabNameCandidates("dadg_veuglaire_defender_spawner", "")
            .ToArray();

        Assert.Equal(
            new[]
            {
                "dadg_veuglaire_defender",
                "dadg_veuglaire",
                "dadg"
            },
            candidates);
    }

    [Fact]
    public void GetPrefabNameCandidates_TriesExplicitOverrideFirst()
    {
        var candidates = CannonSpawnerEntityMissionHelper
            .GetPrefabNameCandidates(
                "dadg_veuglaire_defender_spawner",
                "custom_veuglaire")
            .ToArray();

        Assert.Equal("custom_veuglaire", candidates[0]);
        Assert.Equal("dadg_veuglaire_defender", candidates[1]);
    }

    [Fact]
    public void GetPrefabNameCandidates_DoesNotReturnDuplicateOverride()
    {
        var candidates = CannonSpawnerEntityMissionHelper
            .GetPrefabNameCandidates(
                "dadg_veuglaire_spawner",
                "dadg_veuglaire")
            .ToArray();

        Assert.Equal(new[] { "dadg_veuglaire", "dadg" }, candidates);
    }
}
