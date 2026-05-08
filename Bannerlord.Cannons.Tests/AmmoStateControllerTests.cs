using Bannerlord.Cannons.Domain.Ammo;
using FluentAssertions;
using Xunit;

namespace Bannerlord.Cannons.Tests;

public class AmmoLimitTests
{
    [Fact]
    public void TrySetAmmo_WithPositiveCount_UpdatesAmmoAndHasAmmo()
    {
        var component = new AmmoLimit(() => { });

        var changed = component.TrySetAmmo(3);

        changed.Should().BeTrue();
        component.AmmoCount.Should().Be(3);
        component.HasAmmo.Should().BeTrue();
    }

    [Fact]
    public void TrySetAmmo_WithNegativeCount_ClampsToZeroAndMarksOutOfAmmo()
    {
        var component = new AmmoLimit(() => { });

        component.TrySetAmmo(-5);

        component.AmmoCount.Should().Be(0);
        component.HasAmmo.Should().BeFalse();
    }

    [Fact]
    public void TrySetAmmo_WithSameValue_ReturnsFalse()
    {
        var component = new AmmoLimit(() => { });
        component.TrySetAmmo(5);

        var changed = component.TrySetAmmo(5);

        changed.Should().BeFalse();
        component.AmmoCount.Should().Be(5);
    }

    [Fact]
    public void TryConsumeAmmo_WithAvailableAmmo_DecrementsAmmo()
    {
        var onConsumedCalls = 0;
        var component = new AmmoLimit(() => onConsumedCalls++);
        component.TrySetAmmo(2);

        var consumed = component.TryConsumeAmmo();

        consumed.Should().BeTrue();
        component.AmmoCount.Should().Be(1);
        component.HasAmmo.Should().BeTrue();
        onConsumedCalls.Should().Be(1);
    }

    [Fact]
    public void TryConsumeAmmo_WithNoAmmo_ReturnsFalse()
    {
        var component = new AmmoLimit(() => { });
        component.TrySetAmmo(0);

        var consumed = component.TryConsumeAmmo();

        consumed.Should().BeFalse();
        component.AmmoCount.Should().Be(0);
        component.HasAmmo.Should().BeFalse();
    }

    [Fact]
    public void SyncFromWeapon_UpdatesAmmoCount()
    {
        var component = new AmmoLimit(() => { });

        component.SyncFromWeapon(7);

        component.AmmoCount.Should().Be(7);
        component.HasAmmo.Should().BeTrue();
    }

    [Fact]
    public void CheckAmmo_WhenAmmoExhausted_SetsHasAmmoFalse()
    {
        var component = new AmmoLimit(() => { });
        component.SyncFromWeapon(0);

        var outOfAmmo = component.CheckAmmo();

        outOfAmmo.Should().BeTrue();
        component.HasAmmo.Should().BeFalse();
    }
}
