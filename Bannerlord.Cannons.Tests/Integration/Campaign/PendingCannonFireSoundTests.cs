using System.Threading;
using Bannerlord.Cannons.Integration.Campaign.Patches;
using Xunit;

namespace Bannerlord.Cannons.Tests.Integration.Campaign;

public class PendingCannonFireSoundTests
{
    [Fact]
    public void TryTake_AfterSet_ReturnsSoundOnlyOnce()
    {
        var pending = new PendingCannonFireSound();
        pending.Set(42);

        Assert.True(pending.TryTake(out var soundEventId));
        Assert.Equal(42, soundEventId);
        Assert.False(pending.TryTake(out _));
    }

    [Fact]
    public void TryTake_AfterSetNull_ClearsEarlierSound()
    {
        var pending = new PendingCannonFireSound();
        pending.Set(42);
        pending.Set(null);

        Assert.False(pending.TryTake(out _));
    }

    [Fact]
    public void TryTake_OnAnotherThread_DoesNotSeeSound()
    {
        var pending = new PendingCannonFireSound();
        pending.Set(42);

        var takenOnOtherThread = true;
        var otherThread = new Thread(() => takenOnOtherThread = pending.TryTake(out _));
        otherThread.Start();
        otherThread.Join();

        Assert.False(takenOnOtherThread);
        Assert.True(pending.TryTake(out _));
    }
}
