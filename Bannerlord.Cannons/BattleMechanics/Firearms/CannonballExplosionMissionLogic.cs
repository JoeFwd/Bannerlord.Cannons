using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.BattleMechanics.Artillery;

namespace TOR_Core.BattleMechanics.Firearms
{
    public class CannonballExplosionMissionLogic : MissionLogic
    {
        public override void OnMissileCollisionReaction(Mission.MissileCollisionReaction collisionReaction,
            Agent attackerAgent, Agent attachedAgent,
            sbyte attachedBoneIndex)
        {
            base.OnMissileCollisionReaction(collisionReaction, attackerAgent, attachedAgent, attachedBoneIndex);

            if (collisionReaction != Mission.MissileCollisionReaction.BecomeInvisible) return;
            var missileObj = Mission.Missiles.FirstOrDefault(missile => missile.ShooterAgent == attackerAgent);

            if(missileObj==null)return;
            
            if (FindArtilleryByMissile(missileObj) is not ArtilleryRangedSiegeWeapon artilleryRangedSiegeWeapon) return;
            
            var pos = missileObj.Entity.GlobalPosition;
            
            RunExplosionSoundEffects(pos, artilleryRangedSiegeWeapon.ProjectileGroundExplosionSoundId);
            RunExplosionVisualEffects(pos,artilleryRangedSiegeWeapon.ProjectileGroundExplosionEffect);
        }

        private static void RunExplosionVisualEffects(Vec3 position, string particleEffectID)
        {
            var effect = GameEntity.CreateEmpty(Mission.Current.Scene);
            effect.AddParticleSystemComponent(particleEffectID);
            
            MatrixFrame frame = MatrixFrame.Identity;
            // ParticleSystem.CreateParticleSystemAttachedToEntity(particleEffectID, effect, ref frame);
            var globalFrame = new MatrixFrame(Mat3.CreateMat3WithForward(in Vec3.Zero), position);
            effect.SetGlobalFrame(globalFrame);
        }
        
        private static void RunExplosionSoundEffects(Vec3 position, string soundID, string farAwaySoundID=null)
        {
            if (farAwaySoundID == null)
            {
                farAwaySoundID = soundID;
            }
            
            var distanceFromPlayer = position.Distance(Mission.Current.GetCameraFrame().origin);
            int soundIndex = distanceFromPlayer < 30 ? SoundEvent.GetEventIdFromString(soundID) : SoundEvent.GetEventIdFromString(farAwaySoundID);
            var sound = SoundEvent.CreateEvent(soundIndex, Mission.Current.Scene);
            if (sound != null)
            {
                sound.PlayInPosition(position);
            }
        }

        private ArtilleryRangedSiegeWeapon? FindArtilleryByMissile(Mission.Missile missile)
        {
            return GetArtilleryRangedSiegeWeapons().FirstOrDefault(artilleryRangedSiegeWeapon =>
                artilleryRangedSiegeWeapon.GameEntity.Equals(missile.MissionObjectToIgnore?.GameEntity));
        }

        private IList<ArtilleryRangedSiegeWeapon> GetArtilleryRangedSiegeWeapons()
        {
            List<GameEntity> cannonGameEntities = new List<GameEntity>();
            Mission.Scene.GetAllEntitiesWithScriptComponent<ArtilleryRangedSiegeWeapon>(ref cannonGameEntities);
            return cannonGameEntities
                .Select(cannonGameEntity => cannonGameEntity.GetFirstScriptOfType<ArtilleryRangedSiegeWeapon>())
                .ToList();
        } 
    }
}