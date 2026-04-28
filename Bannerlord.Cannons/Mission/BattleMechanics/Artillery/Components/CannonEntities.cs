using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.Cannons.BattleMechanics.Artillery.Components
{
    /// <summary>
    /// Value-object that holds the scene-entity references collected from the cannon's root entity.
    /// Encapsulates all <see cref="GameEntity.CollectObjectsWithTag{T}"/> calls so the rest of the
    /// codebase never has to deal with raw tag strings.
    /// </summary>
    public class CannonEntities
    {
        public SynchedMissionObject Body   { get; }
        public SynchedMissionObject Barrel { get; }
        public SynchedMissionObject WheelL { get; }
        public SynchedMissionObject WheelR { get; }

        private CannonEntities(
            SynchedMissionObject body,
            SynchedMissionObject barrel,
            SynchedMissionObject wheelL,
            SynchedMissionObject wheelR)
        {
            Body   = body;
            Barrel = barrel;
            WheelL = wheelL;
            WheelR = wheelR;
        }

        /// <summary>
        /// Collects and returns a <see cref="CannonEntities"/> from the given <paramref name="root"/> entity using
        /// the supplied tag strings.
        /// </summary>
        public static CannonEntities Collect(
            GameEntity root,
            string baseTag,
            string barrelTag,
            string leftWheelTag,
            string rightWheelTag)
        {
            var body   = CollectFirst<SynchedMissionObject>(root, baseTag);
            var barrel = CollectFirst<SynchedMissionObject>(root, barrelTag);
            var wheelL = CollectFirst<SynchedMissionObject>(root, leftWheelTag);
            var wheelR = CollectFirst<SynchedMissionObject>(root, rightWheelTag);

            return new CannonEntities(body, barrel, wheelL, wheelR);
        }

        private static T CollectFirst<T>(GameEntity root, string tag) where T : ScriptComponentBehavior
        {
            return root.CollectObjectsWithTag<T>(tag)[0];
        }
    }
}
