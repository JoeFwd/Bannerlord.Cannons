using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.Cannons.Integration.Mission.Spawn
{
    /// <summary>
    /// Resolves a cannon prefab before handing spawning back to Bannerlord's mission helper.
    /// Bannerlord's SpawnerEntityMissionHelper.GetPrefabName is private and non-virtual,
    /// so this class composes the native helper instead of inheriting from it.
    /// </summary>
    internal static class CannonSpawnerEntityMissionHelper
    {
        public static SpawnerEntityMissionHelper Create(
            GenericCannonSpawnerBase spawner,
            ILogger logger)
        {
            if (spawner == null) throw new ArgumentNullException(nameof(spawner));
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            if (spawner.GameEntity == null)
                throw new InvalidOperationException("Cannot resolve a cannon prefab without a spawner entity.");

            var spawnerName = spawner.GameEntity.Name;
            foreach (var candidate in GetPrefabNameCandidates(
                         spawnerName,
                         spawner.ToBeSpawnedOverrideName))
            {
                logger.LogWarning(
                    "Cannon spawner '{SpawnerName}' is trying prefab '{PrefabName}'.",
                    spawnerName,
                    candidate);

                if (!GameEntity.PrefabExists(candidate)) continue;

                spawner.ToBeSpawnedOverrideName = candidate;
                return new SpawnerEntityMissionHelper(spawner);
            }

            throw new InvalidOperationException(
                $"Cannon spawner '{spawnerName}' could not resolve an existing prefab.");
        }

        internal static IEnumerable<string> GetPrefabNameCandidates(
            string spawnerName,
            string? overrideName)
        {
            var yielded = new HashSet<string>(StringComparer.Ordinal);

            if (!string.IsNullOrWhiteSpace(overrideName))
            {
                var explicitOverride = overrideName!;
                if (yielded.Add(explicitOverride))
                    yield return explicitOverride;
            }

            const string spawnerSuffix = "_spawner";
            var candidate = spawnerName.EndsWith(spawnerSuffix, StringComparison.Ordinal)
                ? spawnerName.Substring(0, spawnerName.Length - spawnerSuffix.Length)
                : spawnerName;

            while (!string.IsNullOrWhiteSpace(candidate))
            {
                if (yielded.Add(candidate))
                    yield return candidate;

                var separatorIndex = candidate.LastIndexOf('_');
                if (separatorIndex <= 0) yield break;
                candidate = candidate.Substring(0, separatorIndex);
            }
        }
    }
}
