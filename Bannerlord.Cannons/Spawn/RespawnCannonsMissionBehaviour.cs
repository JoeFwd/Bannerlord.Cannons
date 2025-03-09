using System;
using System.Collections.Generic;
using System.Linq;
using Bannerlord.Cannons.Queries.Models;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.BattleMechanics.Artillery;

namespace Bannerlord.Cannons.Spawn;

public class RespawnCannonsMissionBehaviour() : MissionLogic
{
    private const string CannonPrefabName = "tor_greatcannon";
    
    public override void AfterStart()
    {
        base.AfterStart();
        RespawnCannons();
    }

    private void RespawnCannons()
    {
        GetCanonIds().ToList().ForEach(cannonId =>
        {
            try
            {
                
                // var position = GetCannonPosition(cannonId);
                // var direction = GetCannonDirection(cannonId);
                // ClearCannon(cannonId);
                // SpawnCannon(new Cannon(new CannonId(cannonId), direction, position));
                // DisableCannonPhysics(cannonId);

                // DisableCannonPhysics(cannonId);
            }
            catch (ArgumentException e)
            {
                System.Diagnostics.Debug.WriteLine(e);
            }
        });
    }

    private IList<string> GetCanonIds()
    {
        List<GameEntity> cannonGameEntities = new List<GameEntity>();
        Mission.Scene.GetAllEntitiesWithScriptComponent<ArtilleryRangedSiegeWeapon>(ref cannonGameEntities);
        return cannonGameEntities.Select(entity => entity.GetGuid()).ToList();
    }

    private Position GetCannonPosition(string cannonId)
    {
        GameEntity cannonGameEntity = GetCannonEntity(cannonId);
        return new Position(cannonGameEntity.GlobalPosition.X, cannonGameEntity.GlobalPosition.Y);
    }

    private Direction GetCannonDirection(string cannonId)
    {
        GameEntity cannonGameEntity = GetCannonEntity(cannonId);
        return new Direction(cannonGameEntity.GlobalPosition.RotationX);
    }

    private void ClearCannon(string cannonId)
    {
        GameEntity cannonGameEntity = GetCannonEntity(cannonId);
        var parentCannonEntity = cannonGameEntity.Root;
        if (parentCannonEntity is not null)
        {
            RemoveEntity(parentCannonEntity);
        }
    }

    private void SpawnCannon(Cannon cannon)
    {
        Vec3 direction = new Vec3(x: cannon.Direction.Degrees, y: 0, z: 0);
        
        var x = cannon.Position.X;
        var y = cannon.Position.Y;

        var z = Mission.Scene.GetGroundHeightAtPosition(new Vec3(x, y));
        
        Vec3 position = new Vec3(x, y, z);
        
        
        var rotation = Mat3.CreateMat3WithForward(-direction);
        var entity = GameEntity.Instantiate(Mission.Current.Scene, CannonPrefabName, true);
        entity.SetMobility(GameEntity.Mobility.dynamic);
        entity.EntityFlags = (entity.EntityFlags | EntityFlags.DontSaveToScene);
        var frame = new MatrixFrame(rotation, position);
        entity.SetGlobalFrameMT(frame);
        // var artillery = entity.GetFirstScriptInFamilyDescending<BaseFieldSiegeWeapon>();
        // if (artillery != null)
        // {
        //     artillery.SetSide(triggeredByAgent.Team.Side);
        //     artillery.Team = triggeredByAgent.Team;
        //     artillery.SetForcedUse(!triggeredByAgent.Team.IsPlayerTeam);
        // }
    }

    private void DisableCannonPhysics(string cannonId)
    {
        GameEntity entity = GetCannonEntity(cannonId);
        
        List<GameEntity> gameEntitiesToRemove = new List<GameEntity>();
        entity.GetChildrenRecursive(ref gameEntitiesToRemove);
        
        gameEntitiesToRemove.GetRange(0, 1).ForEach(gameEntityToRemove =>
        {
            gameEntityToRemove.SetPhysicsState(false, false);
            // gameEntityToRemove.RemoveEnginePhysics();
            // gameEntityToRemove.RemovePhysics();
            // gameEntityToRemove.RemovePhysicsMT();
            // gameEntityToRemove.BodyFlag = BodyFlags.Disabled;
        });
    }

    private GameEntity GetCannonEntity(string cannonId)
    {
        var entities = new List<GameEntity>();
        Mission.Scene.GetEntities(ref entities);
        return entities.FirstOrDefault(entity => entity.GetGuid().Contains(cannonId)) ?? throw new ArgumentException($"Could not find cannon entity with guid {cannonId}");
    }

    private static void RemoveEntity(GameEntity entity)
    {
        entity.RemoveEnginePhysics();
        entity.RemoveAllChildren();
        entity.Remove(74); // need to check which id to use.   
    }
}