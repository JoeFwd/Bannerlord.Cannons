using System;
using System.Collections.Generic;
using System.Linq;
using Bannerlord.Cannons;
using Bannerlord.Cannons.BattleMechanics.AI.CommonAIFunctions;
using Bannerlord.Cannons.BattleMechanics.AI.TeamAI.FormationBehavior;
using Bannerlord.Cannons.BattleMechanics.Artillery;
using HarmonyLib;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using MathF = TaleWorlds.Library.MathF;

namespace TOR_Core.BattleMechanics.AI.TeamAI.FormationBehavior
{
    public class ArtilleryPositionalTactic : ArtilleryTactic
    {
        private List<Axis> _positionScoring; //Do not access this directly. Use the generator function public method below.
        public List<Axis> PositionScoring => _positionScoring ?? (_positionScoring = CreateArtilleryPositionAssessment());
        private List<Target> _latestScoredPositions;

        private Target _chosenArtilleryPosition;
        private TacticalPosition _mainDefensiveLinePosition;
        private TacticalPosition _linkedRangedDefensivePosition;

        private bool _usingMachines = true;
        private bool _hasBattleBeenJoined;


        public ArtilleryPositionalTactic(Team Team, IArtilleryCrewProvider artilleryCrewProvider) : base(Team, artilleryCrewProvider)
        {
        }


        protected override float GetTacticWeight()
        {
            if (Team.GeneralAgent == null ||
                Team.ActiveAgents.Select(agent => _artilleryCrewProvider.IsArtilleryCrew(agent)).Count() < 2)
            {
                return 0.0f;
            }
                

            // if (!Team.TeamAI.IsDefenseApplicable || !CheckAndDetermineFormation(ref _mainInfantry, f => f.QuerySystem.IsInfantryFormation))
            //     return 0.0f;

            if (!Team.TeamAI.IsCurrentTactic(this) || _mainDefensiveLinePosition == null)
                DeterminePositions();

            if (_chosenArtilleryPosition != null && !float.IsNaN(_chosenArtilleryPosition.UtilityValue))
            {
                var utility = (Team.QuerySystem.InfantryRatio + Team.QuerySystem.RangedRatio * 10) * 1.2f * _chosenArtilleryPosition.UtilityValue * 2.5f // * CalculateNotEngagingTacticalAdvantage(Team.QuerySystem) 
                              / MathF.Sqrt(Team.QuerySystem.RemainingPowerRatio);
                if (IsArtilleryAtPosition(_chosenArtilleryPosition.TacticalPosition))
                    utility += 5;

                return utility;
            }

            return 0.0f;
        }

        protected override void TickOccasionally()
        {
            if (!AreFormationsCreated)
                return;
         
            bool battleJoinedNew = HasBattleBeenJoined();
            var checkAndSetAvailableFormationsChanged = CheckAndSetAvailableFormationsChanged();
            DeterminePositions();
            if (_chosenArtilleryPosition == null || checkAndSetAvailableFormationsChanged || battleJoinedNew != _hasBattleBeenJoined || IsTacticReapplyNeeded)
            {
                if (checkAndSetAvailableFormationsChanged) ManageFormationCounts();

                _hasBattleBeenJoined = battleJoinedNew;
                if (_hasBattleBeenJoined)
                {
                    Engage();
                }
                else
                {
                    Defend();
                    if (_chosenArtilleryPosition != null)
                    {
                        if (!_usingMachines)
                            ResumeUsingMachines();
                    }
                }

                IsTacticReapplyNeeded = false;
            }
        }

        public bool IsArtilleryAtPosition(TacticalPosition position)
        {
            return Mission.Current.GetActiveEntitiesWithScriptComponentOfType<BaseFieldSiegeWeapon>()
                .Any(entity => entity.GlobalPosition.Distance(position.Position.GetGroundVec3MT()) < 30);
        }

        public void DeterminePositions()
        {
            if (_chosenArtilleryPosition == null || !IsArtilleryAtPosition(_chosenArtilleryPosition.TacticalPosition))
            {
                _latestScoredPositions = GatherCandidatePositions()
                    .Select(pos => new Target {TacticalPosition = pos})
                    .Select(target =>
                    {
                        target.UtilityValue = PositionScoring.GeometricMean(target);
                        return target;
                    }).ToList();
                if (_latestScoredPositions.Count > 0)
                {
                    var candidate = _latestScoredPositions.OrderByDescending(t => t.UtilityValue).FirstOrDefault();
                    if (float.IsNaN(candidate.UtilityValue)) _positionScoring = null;
                    if (candidate != null && candidate.UtilityValue != 0.0 && !float.IsNaN(candidate.UtilityValue)) _chosenArtilleryPosition = candidate;
                }
                else _chosenArtilleryPosition = null;
            }

            if (_chosenArtilleryPosition != null)
            {
                var tp = _chosenArtilleryPosition.TacticalPosition;
                var direction = (Team.QuerySystem.AverageEnemyPosition - tp.Position.AsVec2).Normalized();
                TacticalPosition primaryDefensivePosition = new TacticalPosition(
                    new WorldPosition(Mission.Current.Scene, tp.Position.GetGroundVec3MT() + direction.ToVec3() * 50),
                    direction, tp.Width, tp.Slope, tp.IsInsurmountable, tp.TacticalPositionType, tp.TacticalRegionMembership);

                if (primaryDefensivePosition != _mainDefensiveLinePosition)
                {
                    _mainDefensiveLinePosition = primaryDefensivePosition;
                    IsTacticReapplyNeeded = true;
                }

                if (_mainDefensiveLinePosition.LinkedTacticalPositions.Count > 0)
                {
                    TacticalPosition tacticalPosition2 = _mainDefensiveLinePosition.LinkedTacticalPositions.FirstOrDefault();
                    if (tacticalPosition2 == _linkedRangedDefensivePosition)
                        return;
                    _linkedRangedDefensivePosition = tacticalPosition2;
                    IsTacticReapplyNeeded = true;
                }
                else
                    _linkedRangedDefensivePosition = null;
            }
            else
            {
                _mainDefensiveLinePosition =null;
                _linkedRangedDefensivePosition = null;
            }
        }

        private List<TacticalPosition> GatherCandidatePositions()
        {
            var TeamAiAPositions = Team.TeamAI.TacticalPositions;

            var extractedPositions = Team.TeamAI.TacticalRegions
                .SelectMany(region => ExtractPossibleTacticalPositionsFromTacticalRegion(region));

            TacticalPosition tacticalPosition1 = new TacticalPosition(Team.QuerySystem.MedianPosition, (Team.QuerySystem.AverageEnemyPosition - Team.QuerySystem.MedianPosition.AsVec2).Normalized(), 50);
            var averageEnemyPosition = Team.QuerySystem.AverageEnemyPosition;

            float height = 0.0f;
            Mission.Current.Scene.GetHeightAtPoint(averageEnemyPosition, BodyFlags.CommonCollisionExcludeFlagsForCombat, ref height);
            var enemyPosition = averageEnemyPosition.ToVec3(height);
            var gatherCandidatePositions = TeamAiAPositions
                .Concat(extractedPositions)
                .AddItem(tacticalPosition1)
                .Where(position => LineOfSightAllowsArtillery(position, enemyPosition)).ToList();
            return gatherCandidatePositions;
        }

        private bool LineOfSightAllowsArtillery(TacticalPosition position, Vec3 enemyPosition)
        {
            return true; //TODO:Temp
            var posCorrected = position.Position.GetGroundVec3MT();
            posCorrected.z += 1.5f;
            var enemyCorrected = enemyPosition;
            enemyCorrected.z += 2.5f;
            if (position.TacticalRegionMembership == TacticalRegion.TacticalRegionTypeEnum.Forest || position.TacticalRegionMembership == TacticalRegion.TacticalRegionTypeEnum.DifficultTerrain)
            {
                return (CommonAIFunctions.HasLineOfSight(posCorrected, enemyCorrected, Team.TeamAI.IsDefenseApplicable ? 10 : 70) ||
                        CommonAIFunctions.HasLineOfSight(enemyCorrected, posCorrected, Team.TeamAI.IsDefenseApplicable ? 10 : 70));
                // && CommonAIFunctions.HasLineOfSight(posCorrected, posCorrected + position.Direction.Normalized().ToVec3()*15, 20);
            }

            return CommonAIFunctions.HasLineOfSight(posCorrected, enemyCorrected, Team.TeamAI.IsDefenseApplicable ? 70.0f : position.Position.GetGroundVec3MT().Distance(enemyCorrected) * 0.5f) ||
                   CommonAIFunctions.HasLineOfSight(enemyCorrected, posCorrected, Team.TeamAI.IsDefenseApplicable ? 70.0f : position.Position.GetGroundVec3MT().Distance(enemyCorrected) * 0.5f);
            //  && CommonAIFunctions.HasLineOfSight(posCorrected, posCorrected + position.Direction.Normalized().ToVec3()*15, 20);
        }

        private List<Axis> CreateArtilleryPositionAssessment()
        {
            var function = new List<Axis>();
            var distance = Team.QuerySystem.AveragePosition.Distance(Team.QuerySystem.AverageEnemyPosition);
            Mission.Current.Scene.GetTerrainMinMaxHeight(out float minHeight, out float maxHeight);
            function.Add(new Axis(0, distance, x => x, CommonAIDecisionFunctions.TargetDistanceToHostiles(Team)));
            function.Add(new Axis(0, distance, x => 1 - x, CommonAIDecisionFunctions.TargetDistanceToOwnArmy(Team)));
            function.Add(new Axis(0, 1, x => x, CommonAIDecisionFunctions.AssessPositionForArtillery()));
            function.Add(new Axis(minHeight, maxHeight, x => x, CommonAIDecisionFunctions.PositionHeight()));
            return function;
        }

        private List<TacticalPosition> ExtractPossibleTacticalPositionsFromTacticalRegion(
            TacticalRegion tacticalRegion)
        {
            List<TacticalPosition> fromTacticalRegion = new List<TacticalPosition>();
            fromTacticalRegion.AddRange(tacticalRegion.LinkedTacticalPositions); //.Where(ltp => ltp.TacticalPositionType == TacticalPosition.TacticalPositionTypeEnum.HighGround);
            if (tacticalRegion.tacticalRegionType == TacticalRegion.TacticalRegionTypeEnum.Forest)
            {
                Vec2 direction = (Team.QuerySystem.AverageEnemyPosition - tacticalRegion.Position.AsVec2).Normalized();
                TacticalPosition tacticalPosition1 = new TacticalPosition(tacticalRegion.Position, direction, tacticalRegion.radius, tacticalRegionMembership: TacticalRegion.TacticalRegionTypeEnum.Forest);
                fromTacticalRegion.Add(tacticalPosition1);
                float num = tacticalRegion.radius * 0.87f;
                TacticalPosition tacticalPosition2 = new TacticalPosition(new WorldPosition(Mission.Current.Scene, UIntPtr.Zero, tacticalRegion.Position.GetNavMeshVec3() + (num * direction).ToVec3(), false), direction, tacticalRegion.radius,
                    tacticalRegionMembership: TacticalRegion.TacticalRegionTypeEnum.Forest);
                fromTacticalRegion.Add(tacticalPosition2);
                TacticalPosition tacticalPosition3 = new TacticalPosition(new WorldPosition(Mission.Current.Scene, UIntPtr.Zero, tacticalRegion.Position.GetNavMeshVec3() - (num * direction).ToVec3(), false), direction, tacticalRegion.radius,
                    tacticalRegionMembership: TacticalRegion.TacticalRegionTypeEnum.Forest);
                fromTacticalRegion.Add(tacticalPosition3);
            }

            return fromTacticalRegion;
        }


        private bool HasBattleBeenJoined() => _mainInfantry?.QuerySystem.ClosestEnemyFormation == null || _mainInfantry.AI.ActiveBehavior is BehaviorCharge || _mainInfantry.AI.ActiveBehavior is BehaviorTacticalCharge ||
                                              _mainInfantry.QuerySystem.MedianPosition.AsVec2.Distance(_mainInfantry.QuerySystem.ClosestEnemyFormation.MedianPosition.AsVec2) / (double) _mainInfantry.QuerySystem.ClosestEnemyFormation.MovementSpeedMaximum <=
                                              5.0 + (_hasBattleBeenJoined ? 5.0 : 0.0); //TODO: Need to improve logic for detecting that battle has started.

        protected override bool ResetTacticalPositions()
        {
            DeterminePositions();
            return true;
        }

        private void Defend()
        {
            if (Team.IsPlayerTeam && !Team.IsPlayerGeneral && Team.IsPlayerSergeant)
                SoundTacticalHorn(MoveHornSoundIndex);


            if (_mainInfantry != null)
            {
                _mainInfantry.AI.ResetBehaviorWeights();
                SetDefaultBehaviorWeights(_mainInfantry);
                _mainInfantry.AI.SetBehaviorWeight<BehaviorDefend>(5f).TacticalDefendPosition = _mainDefensiveLinePosition;
                _mainInfantry.AI.SetBehaviorWeight<BehaviorTacticalCharge>(1f);
            }

            if (_artilleryFormation != null && _artilleryFormation.CountOfUnits > 0 && _chosenArtilleryPosition != null)
            {
                _artilleryFormation.AI.ResetBehaviorWeights();
                SetDefaultBehaviorWeights(_artilleryFormation);
                var enemyDirection = (_chosenArtilleryPosition.TacticalPosition.Position.AsVec2 - Team.QuerySystem.AverageEnemyPosition).Normalized();
                _artilleryFormation.AI.SetBehaviorWeight<BehaviorDefend>(15f).DefensePosition = new WorldPosition(Mission.Current.Scene, _chosenArtilleryPosition.TacticalPosition.Position.GetGroundVec3MT() + enemyDirection.ToVec3() * 12);
                _artilleryFormation.AI.SetBehaviorWeight<BehaviorSkirmishLine>(1f);
                _artilleryFormation.AI.SetBehaviorWeight<BehaviorScreenedSkirmish>(1f);
            }

            if (_guardFormation != null && _guardFormation.CountOfUnits > 0 && _chosenArtilleryPosition != null)
            {
                _guardFormation.AI.ResetBehaviorWeights();
                SetDefaultBehaviorWeights(_guardFormation);
                _guardFormation.AI.SetBehaviorWeight<BehaviorTacticalCharge>(1f);
                _guardFormation.AI.SetBehaviorWeight<TORBehaviorProtectArtillery>(15.0f);
                _guardFormation.AI.SetBehaviorWeight<BehaviorDefend>(10).TacticalDefendPosition = _chosenArtilleryPosition.TacticalPosition;
            }

            if (_archers != null)
            {
                _archers.AI.ResetBehaviorWeights();
                SetDefaultBehaviorWeights(_archers);
                _archers.AI.SetBehaviorWeight<BehaviorSkirmishLine>(1f);
                _archers.AI.SetBehaviorWeight<BehaviorScreenedSkirmish>(1f);
                if (_linkedRangedDefensivePosition != null)
                    _archers.AI.SetBehaviorWeight<BehaviorDefend>(10f).TacticalDefendPosition = _linkedRangedDefensivePosition;
            }

            if (_leftCavalry != null)
            {
                _leftCavalry.AI.ResetBehaviorWeights();
                SetDefaultBehaviorWeights(_leftCavalry);
                _leftCavalry.AI.SetBehaviorWeight<BehaviorProtectFlank>(1f).FlankSide = FormationAI.BehaviorSide.Left;
                _leftCavalry.AI.SetBehaviorWeight<BehaviorCavalryScreen>(1f);
            }

            if (_rightCavalry != null)
            {
                _rightCavalry.AI.ResetBehaviorWeights();
                SetDefaultBehaviorWeights(_rightCavalry);
                _rightCavalry.AI.SetBehaviorWeight<BehaviorProtectFlank>(1f).FlankSide = FormationAI.BehaviorSide.Right;
                _rightCavalry.AI.SetBehaviorWeight<BehaviorCavalryScreen>(1f);
            }

            if (_rangedCavalry == null)
                return;
            _rangedCavalry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_rangedCavalry);
            _rangedCavalry.AI.SetBehaviorWeight<BehaviorMountedSkirmish>(1f);
            _rangedCavalry.AI.SetBehaviorWeight<BehaviorHorseArcherSkirmish>(1f);
        }

        private void Engage()
        {
            if (Team.IsPlayerTeam && !Team.IsPlayerGeneral && Team.IsPlayerSergeant)
                SoundTacticalHorn(AttackHornSoundIndex);
            if (_mainInfantry != null)
            {
                _mainInfantry.AI.ResetBehaviorWeights();
                SetDefaultBehaviorWeights(_mainInfantry);
                _mainInfantry.AI.SetBehaviorWeight<BehaviorDefend>(1f).TacticalDefendPosition = _mainDefensiveLinePosition;
                _mainInfantry.AI.SetBehaviorWeight<BehaviorTacticalCharge>(1f);
            }


            if (_archers != null)
            {
                _archers.AI.ResetBehaviorWeights();
                SetDefaultBehaviorWeights(_archers);
                _archers.AI.SetBehaviorWeight<BehaviorSkirmish>(1f);
                _archers.AI.SetBehaviorWeight<BehaviorScreenedSkirmish>(1f);
                if (_linkedRangedDefensivePosition != null)
                    _archers.AI.SetBehaviorWeight<BehaviorDefend>(1f).TacticalDefendPosition = _linkedRangedDefensivePosition;
            }

            if (_leftCavalry != null)
            {
                _leftCavalry.AI.ResetBehaviorWeights();
                SetDefaultBehaviorWeights(_leftCavalry);
                _leftCavalry.AI.SetBehaviorWeight<BehaviorFlank>(1f);
                _leftCavalry.AI.SetBehaviorWeight<BehaviorTacticalCharge>(1f);
            }

            if (_rightCavalry != null)
            {
                _rightCavalry.AI.ResetBehaviorWeights();
                SetDefaultBehaviorWeights(_rightCavalry);
                _rightCavalry.AI.SetBehaviorWeight<BehaviorFlank>(1f);
                _rightCavalry.AI.SetBehaviorWeight<BehaviorTacticalCharge>(1f);
            }

            if (_rangedCavalry == null)
                return;
            _rangedCavalry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_rangedCavalry);
            _rangedCavalry.AI.SetBehaviorWeight<BehaviorMountedSkirmish>(1f);
            _rangedCavalry.AI.SetBehaviorWeight<BehaviorHorseArcherSkirmish>(1f);
        }
    }
}