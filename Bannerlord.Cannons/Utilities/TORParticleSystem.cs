using Bannerlord.Cannons.Logging;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.Utilities
{
    public class TORParticleSystem
    {
        private static readonly ILogger Logger = new ConsoleLoggerFactory().CreateLogger<TORParticleSystem>();
        
        /// <summary>
        /// Attach a particle to an agent's bone at the given index.
        /// </summary>
        /// <param name="agent">The agent receiving the particle system.</param>
        /// <param name="particleId">The ID of the particle system.</param>
        /// <param name="boneIndex">The index of the bone on the agent's skeleton that the particle should be attached to.</param>
        /// <param name="childEntity">The child entity that the particle is attached to.</param>
        /// <returns>The ParticleSystem that was attached to the agent's bone.</returns>
        public static ParticleSystem ApplyParticleToAgentBone(Agent agent, string particleId, sbyte boneIndex, out GameEntity childEntity, float elevationOffset = 0)
        {
            Skeleton skeleton = agent.AgentVisuals.GetSkeleton();
            Scene scene = Mission.Current.Scene;
            childEntity = GameEntity.CreateEmpty(scene);
            MatrixFrame localFrame = new MatrixFrame(Mat3.Identity, new Vec3(0, 0, 0));
            localFrame.Elevate(elevationOffset);
            ParticleSystem particle = ParticleSystem.CreateParticleSystemAttachedToEntity(particleId, childEntity, ref localFrame);
            if (particle != null)
            {
                agent.AgentVisuals.AddChildEntity(childEntity);
                skeleton.AddComponentToBone(boneIndex, particle);
            }
            else
            {
                Logger.Warn("Attempted to apply a null particle to agent bone. Particle ID: " + particleId + ". Agent name: " + agent.Name);
            }

            return particle;
        }
    }
}
