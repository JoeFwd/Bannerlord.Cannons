using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.Cannons.BattleMechanics.AI.CommonAIFunctions
{
    /// <summary>
    /// Wraps TaleWorlds' <see cref="Threat"/> class to represent an artillery target.
    ///
    /// Why extend <c>Threat</c>?
    /// <list type="bullet">
    ///   <item><description>
    ///     It decouples our code from TaleWorlds internals. If the engine's <c>Threat</c>
    ///     class changes, we adapt here without touching every targeting class.
    ///   </description></item>
    ///   <item><description>
    ///     It avoids calling formations "threats" — the term is wrong when we're
    ///     targeting our own army's rally points or friendly positions.
    ///   </description></item>
    /// </list>
    ///
    /// Position resolution priority (see <see cref="GetPosition"/>):
    /// siege weapon entity → agent → formation median → selected world position →
    /// tactical position → base Threat position.
    /// </summary>
    public class Target : Threat
    {
        /// <summary>
        /// The world position the cannon should aim at. Updated each tick by the AI
        /// controller, which may apply lead prediction (for moving formations) or
        /// simply refresh to the entity centre (for siege weapons).
        /// Set to <see cref="Vec3.Zero"/> to signal "no valid aim point" and block firing.
        /// </summary>
        public Vec3 SelectedWorldPosition = Vec3.Zero;

        /// <summary>A tactical map position associated with this target, if any.</summary>
        public TacticalPosition? TacticalPosition;

        /// <summary>
        /// The computed utility score (0–1) for this target. Stored via the base class's
        /// <c>ThreatValue</c> field so that TaleWorlds APIs that accept <c>Threat</c> can
        /// sort / filter targets without additional conversion.
        /// </summary>
        public float UtilityValue
        {
            get => ThreatValue;
            set => ThreatValue = value;
        }

        /// <summary>
        /// Resolves the best current world position for this target, tried in priority order:
        /// <list type="number">
        ///   <item><description>Siege weapon entity bounding-box centre.</description></item>
        ///   <item><description>Agent's collision capsule centre.</description></item>
        ///   <item><description>Formation median-agent position.</description></item>
        ///   <item><description><see cref="SelectedWorldPosition"/> (cached aim point).</description></item>
        ///   <item><description>Tactical position ground point.</description></item>
        ///   <item><description>Base <see cref="Threat.Position"/> fallback.</description></item>
        /// </list>
        /// Returns <see cref="Vec3.Invalid"/> if all options are unavailable.
        /// </summary>
        public Vec3 GetPosition()
        {
            if (WeaponEntity != null)
            {
                var entity = WeaponEntity.GetTargetEntity();
                if (entity != null)
                    return (entity.GlobalBoxMax + entity.GlobalBoxMin) * 0.5f;
            }

            if (base.Agent != null)
                return base.Agent.CollisionCapsuleCenter;

            if (Formation != null)
            {
                var medianAgent = Formation.GetMedianAgent(
                    false, false,
                    Formation.GetAveragePositionOfUnits(false, false));
                // GetMedianAgent can return null when the formation is being wiped out.
                if (medianAgent != null)
                    return medianAgent.Position;
            }

            if (SelectedWorldPosition != Vec3.Zero)
                return SelectedWorldPosition;

            if (TacticalPosition != null)
                return TacticalPosition.Position.GetGroundVec3MT();

            var basePos = base.Position;
            return basePos;
        }

        /// <summary>
        /// Like <see cref="GetPosition"/>, but prefers the cached/calculated position
        /// (<see cref="SelectedWorldPosition"/>) over live entity/agent queries. Use this
        /// when the caller has already set an aim point and wants to reuse it (e.g. to
        /// compute distance to the nearest enemy formation without paying the entity
        /// lookup cost every tick).
        /// </summary>
        public Vec3 GetPositionPrioritizeCalculated()
        {
            if (SelectedWorldPosition != Vec3.Zero)
                return SelectedWorldPosition;

            if (TacticalPosition != null)
                return TacticalPosition.Position.GetGroundVec3MT();

            var basePos = base.Position;
            return basePos;
        }

        /// <summary>
        /// Returns the velocity of this target. For formation targets, returns the
        /// formation's current movement velocity (used for lead calculation). Falls back
        /// to the base <see cref="Threat.GetVelocity"/> for agent/weapon targets.
        /// </summary>
        public new Vec3 GetVelocity()
        {
            if (Formation != null)
                return Formation.QuerySystem.CurrentVelocity.ToVec3();

            return base.GetVelocity();
        }

        /// <summary>
        /// The resolved agent for this target. If no agent has been explicitly set but
        /// a <see cref="Formation"/> is present, returns the formation's median agent
        /// nearest to <see cref="SelectedWorldPosition"/> (or the formation centre if
        /// no aim point has been calculated yet). May return <c>null</c> if the
        /// formation has no living units.
        /// </summary>
        public new Agent? Agent
        {
            get
            {
                if (base.Agent == null && Formation != null)
                {
                    Vec2 referencePos = SelectedWorldPosition == Vec3.Zero
                        ? Formation.CurrentPosition
                        : SelectedWorldPosition.AsVec2;
                    return Formation.GetMedianAgent(false, false, referencePos);
                }
                return base.Agent;
            }
            set => base.Agent = value;
        }

        /// <summary>Resolves the target position via <see cref="GetPosition"/>.</summary>
        public new Vec3 Position => GetPosition();
    }
}
