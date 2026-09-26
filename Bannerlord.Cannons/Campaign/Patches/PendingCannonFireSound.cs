using System.Threading;

namespace Bannerlord.Cannons.Integration.Campaign.Patches
{
    /// <summary>
    /// Carries the firing cannon's sound from <see cref="SettlementVisualCalculateDataAndDurationsForSiegeMachinePatch"/>
    /// to <see cref="MBSoundEventPlaySoundPatch"/>: SettlementVisual.Tick computes each missile's data and then plays
    /// the fire sound, but the sound call does not say which siege engine is firing.
    /// Settlement visuals tick in parallel, so the value is kept per thread.
    /// </summary>
    public sealed class PendingCannonFireSound
    {
        private readonly ThreadLocal<int?> _soundEventId = new ThreadLocal<int?>();

        public void Set(int? soundEventId) => _soundEventId.Value = soundEventId;

        public bool TryTake(out int soundEventId)
        {
            var pending = _soundEventId.Value;
            soundEventId = pending ?? -1;

            if (!pending.HasValue)
                return false;

            _soundEventId.Value = null;
            return true;
        }
    }
}
