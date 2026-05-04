using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace Bannerlord.Cannons.Integration.Mission.Spawn
{
    public class CannonTrajectoryVisualiser : ScriptComponentBehavior
    {
        private readonly struct SourceSnapshot
        {
            public SourceSnapshot(Vec3 offset, float missileSpeed, float verticalAngleMinInDegrees, float verticalAngleMaxInDegrees, float horizontalAngleRangeInDegrees, float airFrictionConstant)
            {
                Offset = offset;
                MissileSpeed = missileSpeed;
                VerticalAngleMinInDegrees = verticalAngleMinInDegrees;
                VerticalAngleMaxInDegrees = verticalAngleMaxInDegrees;
                HorizontalAngleRangeInDegrees = horizontalAngleRangeInDegrees;
                AirFrictionConstant = airFrictionConstant;
            }

            public Vec3 Offset { get; }
            public float MissileSpeed { get; }
            public float VerticalAngleMinInDegrees { get; }
            public float VerticalAngleMaxInDegrees { get; }
            public float HorizontalAngleRangeInDegrees { get; }
            public float AirFrictionConstant { get; }
        }

        private struct TrajectoryParams
        {
            public Vec3 MissileShootingPositionOffset;
            public float MissileSpeed;
            public float VerticalAngleMinInDegrees;
            public float VerticalAngleMaxInDegrees;
            public float HorizontalAngleRangeInDegrees;
            public float AirFrictionConstant;
            public bool IsValid;
        }

        [EditorVisibleScriptComponentVariable(true)]
        public bool ShowTrajectory;

        private const float DefaultMissileSpeed = 40f;
        private const float DefaultVerticalMinInDegrees = -5f;
        private const float DefaultVerticalMaxInDegrees = 45f;
        private const float DefaultHorizontalRangeInDegrees = 100f;
        private const float AirFrictionConstant = 0f;
        private const float TrajectoryForwardOffset = 1.5f;
        private const float SnapshotEpsilon = 0.0001f;

        private GameEntity? _trajectoryMeshHolder;
        private TrajectoryParams _trajectoryParams;
        private SourceSnapshot? _lastSnapshot;

        public void SetTrajectoryParams(
            Vec3 missileShootingPositionOffset,
            float missileSpeed,
            float verticalAngleMinInDegrees,
            float verticalAngleMaxInDegrees,
            float horizontalAngleRangeInDegrees,
            float airFrictionConstant)
        {
            _trajectoryParams.MissileShootingPositionOffset = missileShootingPositionOffset;
            _trajectoryParams.MissileSpeed = missileSpeed;
            _trajectoryParams.VerticalAngleMinInDegrees = verticalAngleMinInDegrees;
            _trajectoryParams.VerticalAngleMaxInDegrees = verticalAngleMaxInDegrees;
            _trajectoryParams.HorizontalAngleRangeInDegrees = horizontalAngleRangeInDegrees;
            _trajectoryParams.AirFrictionConstant = airFrictionConstant;
            _trajectoryParams.IsValid = true;

            RebuildOrToggleTrajectoryMesh();
        }

        protected override void OnRemoved(int removeReason)
        {
            if (_trajectoryMeshHolder != null)
                _trajectoryMeshHolder.Remove(removeReason);
        }

        protected override void OnEditorInit()
        {
            base.OnEditorInit();
            RefreshFromSpawner();
            RebuildOrToggleTrajectoryMesh();
        }

        protected override void OnEditorTick(float dt)
        {
            base.OnEditorTick(dt);
            RefreshFromSpawner();
        }

        protected override void OnEditorVariableChanged(string variableName)
        {
            base.OnEditorVariableChanged(variableName);
            if (variableName == nameof(ShowTrajectory))
                RebuildOrToggleTrajectoryMesh();
        }

        private void RefreshFromSpawner()
        {
            if (!TryBuildSnapshotFromSpawner(out var snapshot)) return;
            if (_lastSnapshot.HasValue && SnapshotsEqual(_lastSnapshot.Value, snapshot)) return;

            _lastSnapshot = snapshot;
            SetTrajectoryParams(
                snapshot.Offset,
                snapshot.MissileSpeed,
                snapshot.VerticalAngleMinInDegrees,
                snapshot.VerticalAngleMaxInDegrees,
                snapshot.HorizontalAngleRangeInDegrees,
                snapshot.AirFrictionConstant);
        }

        private bool TryBuildSnapshotFromSpawner(out SourceSnapshot snapshot)
        {
            snapshot = default;

            var spawner = FindSpawnerInParents();
            if (spawner?.GameEntity == null || GameEntity == null) return false;
            if (spawner is not ICannonTrajectoryPreviewSource trajectorySource) return false;

            var projectileLeavingPosition = FindProjectileLeavingPosition(spawner.GameEntity);
            var defaultGlobalOrigin = projectileLeavingPosition?.GlobalPosition ?? spawner.GameEntity.GlobalPosition;
            var frame = GameEntity.GetGlobalFrame();

            var offset = ToLocalOffset(frame, defaultGlobalOrigin);
            var missileSpeed = trajectorySource.GetTrajectoryPreviewBaseMuzzleVelocity();
            if (missileSpeed <= 0f) missileSpeed = DefaultMissileSpeed;

            var verticalMinInDegrees = ToDegrees(trajectorySource.GetTrajectoryPreviewBottomReleaseAngleRestriction());
            var verticalMaxInDegrees = ToDegrees(trajectorySource.GetTrajectoryPreviewTopReleaseAngleRestriction());
            if (verticalMaxInDegrees <= verticalMinInDegrees)
            {
                verticalMinInDegrees = DefaultVerticalMinInDegrees;
                verticalMaxInDegrees = DefaultVerticalMaxInDegrees;
            }

            var horizontalRangeInDegrees = trajectorySource.GetTrajectoryPreviewDirectionRestrictionDegrees();
            if (horizontalRangeInDegrees <= 0f) horizontalRangeInDegrees = DefaultHorizontalRangeInDegrees;

            snapshot = new SourceSnapshot(offset, missileSpeed, verticalMinInDegrees, verticalMaxInDegrees, horizontalRangeInDegrees, AirFrictionConstant);
            return true;
        }

        private GenericCannonSpawnerBase? FindSpawnerInParents()
        {
            var current = GameEntity;
            while (current != null)
            {
                var spawner = current.GetFirstScriptOfType<GenericCannonSpawnerBase>();
                if (spawner != null) return spawner;
                current = current.Parent;
            }
            return null;
        }

        private static GameEntity? FindProjectileLeavingPosition(GameEntity root)
        {
            var children = new List<GameEntity>();
            root.GetChildrenRecursive(ref children);
            return children.FirstOrDefault(e => e != null && e.Name == "projectile_leaving_position");
        }

        private static Vec3 ToLocalOffset(MatrixFrame frame, Vec3 worldTarget)
        {
            var worldOffset = worldTarget - frame.origin;
            return new Vec3(
                Vec3.DotProduct(frame.rotation.s, worldOffset),
                Vec3.DotProduct(frame.rotation.f, worldOffset) + TrajectoryForwardOffset,
                Vec3.DotProduct(frame.rotation.u, worldOffset));
        }

        private static float ToDegrees(float valueInRadians) => valueInRadians * (180f / MathF.PI);

        private static bool SnapshotsEqual(SourceSnapshot a, SourceSnapshot b)
        {
            return NearlyEqual(a.Offset.x, b.Offset.x)
                   && NearlyEqual(a.Offset.y, b.Offset.y)
                   && NearlyEqual(a.Offset.z, b.Offset.z)
                   && NearlyEqual(a.MissileSpeed, b.MissileSpeed)
                   && NearlyEqual(a.VerticalAngleMinInDegrees, b.VerticalAngleMinInDegrees)
                   && NearlyEqual(a.VerticalAngleMaxInDegrees, b.VerticalAngleMaxInDegrees)
                   && NearlyEqual(a.HorizontalAngleRangeInDegrees, b.HorizontalAngleRangeInDegrees)
                   && NearlyEqual(a.AirFrictionConstant, b.AirFrictionConstant);
        }

        private static bool NearlyEqual(float a, float b) => MathF.Abs(a - b) < SnapshotEpsilon;

        private void RebuildOrToggleTrajectoryMesh()
        {
            if (!ShowTrajectory || !_trajectoryParams.IsValid || GameEntity == null || GameEntity.IsGhostObject())
            {
                _trajectoryMeshHolder?.SetVisibilityExcludeParents(ShowTrajectory);
                return;
            }

            if (_trajectoryMeshHolder == null)
            {
                _trajectoryMeshHolder = GameEntity.CreateEmpty(Scene, isModifiableFromEditor: false);
                if (_trajectoryMeshHolder == null) return;

                _trajectoryMeshHolder.EntityFlags |= EntityFlags.DontSaveToScene;
                GameEntity.AddChild(_trajectoryMeshHolder, autoLocalizeFrame: true);
            }

            var frame = GameEntity.GetGlobalFrame();
            frame.origin += frame.rotation.s * _trajectoryParams.MissileShootingPositionOffset.x
                          + frame.rotation.f * _trajectoryParams.MissileShootingPositionOffset.y
                          + frame.rotation.u * _trajectoryParams.MissileShootingPositionOffset.z;

            _trajectoryMeshHolder.SetGlobalFrame(in frame);
            _trajectoryMeshHolder.ComputeTrajectoryVolume(
                _trajectoryParams.MissileSpeed,
                _trajectoryParams.VerticalAngleMaxInDegrees,
                _trajectoryParams.VerticalAngleMinInDegrees,
                _trajectoryParams.HorizontalAngleRangeInDegrees,
                _trajectoryParams.AirFrictionConstant);
            _trajectoryMeshHolder.SetVisibilityExcludeParents(ShowTrajectory);
        }
    }
}
