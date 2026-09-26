using System;
using System.Collections.Concurrent;
using Bannerlord.Cannons.Infrastructure.Registry;
using TaleWorlds.Engine;

namespace Bannerlord.Cannons.Integration.Campaign
{
    public class CannonFireSoundProvider
    {
        private readonly ICannonRegistry _cannonRegistry;

        // Queried per missile per frame from parallel settlement ticks, so the native name lookup is cached.
        private readonly ConcurrentDictionary<string, int> _soundEventIdsByName =
            new ConcurrentDictionary<string, int>(StringComparer.Ordinal);

        public CannonFireSoundProvider(ICannonRegistry cannonRegistry)
        {
            _cannonRegistry = cannonRegistry;
        }

        /// <returns>The sound event id the cannon plays when firing on the campaign map, or null if
        /// <paramref name="siegeEngineId"/> is not a cannon or its sound event does not exist.</returns>
        public int? GetCampaignMapFireSoundEventId(string siegeEngineId)
        {
            var soundEventName = _cannonRegistry.GetCannon(siegeEngineId)?.CampaignMapFireSoundId;
            if (soundEventName is null)
                return null;

            var soundEventId = _soundEventIdsByName.GetOrAdd(soundEventName, SoundEvent.GetEventIdFromString);
            return soundEventId >= 0 ? soundEventId : null;
        }
    }
}
