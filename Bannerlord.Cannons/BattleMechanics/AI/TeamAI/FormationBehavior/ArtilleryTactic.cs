using System.Collections.Generic;
using System.Linq;
using Bannerlord.Cannons.Extensions;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.Cannons.BattleMechanics.AI.TeamAI.FormationBehavior;

public class ArtilleryTactic : TacticDefensiveLine
{
    protected readonly IArtilleryCrewProvider  _artilleryCrewProvider; 
        
    protected readonly Formation _artilleryFormation;
    protected readonly Formation _guardFormation;
    private bool _usingMachines;

    public ArtilleryTactic(Team team, IArtilleryCrewProvider artilleryCrewProvider) : base(team)
    {
        _artilleryCrewProvider = artilleryCrewProvider;
        _artilleryFormation = new Formation(this.Team, (int) TORFormationClass.Artillery);
        this.Team.FormationsIncludingSpecialAndEmpty.Add(_artilleryFormation);
        _guardFormation = new Formation(this.Team, (int) TORFormationClass.ArtilleryGuard);
        this.Team.FormationsIncludingSpecialAndEmpty.Add(_guardFormation);

        //TODO: Reminder, might need this if certain updates dont work.
        // var method = Traverse.Create(this.Team).Method("FormationAI_OnActiveBehaviorChanged").GetValue();
        // _artilleryFormation.AI.OnActiveBehaviorChanged += new Action<Formation>(this.Team.FormationAI_OnActiveBehaviorChanged);
        // _guardFormation.AI.OnActiveBehaviorChanged += new Action<Formation>(this.Team.FormationAI_OnActiveBehaviorChanged);
    }
    
    
    protected override void ManageFormationCounts()
        {
            AssignTacticFormations1121();

            var allFormations = Team.FormationsIncludingSpecialAndEmpty.ToList();
            var infantryFormations = Team.GetFormationsIncludingSpecial().ToList().FindAll(formation => formation.QuerySystem.IsInfantryFormation);
            var updatedFormations = new List<Formation>();

            allFormations.SelectMany(form => form.Arrangement.GetAllUnits()).ToList().Select(unit => (Agent) unit).ToList().ForEach(agent =>
            {
                if (_artilleryCrewProvider.IsArtilleryCrew(agent))
                {
                    if (!updatedFormations.Contains(agent.Formation))
                        updatedFormations.Add(agent.Formation);
                    if (!updatedFormations.Contains(_artilleryFormation))
                        updatedFormations.Add(_artilleryFormation);
                    agent.Formation = _artilleryFormation;
                }
            });

            if (infantryFormations.Count > 0)
            {
                var count = infantryFormations.Sum(form => form.Arrangement.UnitCount) * 0.1;
                {
                    count += count < _artilleryFormation.Arrangement.UnitCount ? 10 : 0;
                }
                count -= _guardFormation.Arrangement.UnitCount;


                infantryFormations.SelectMany(form => form.Arrangement.GetAllUnits()).ToList().Select(unit => (Agent) unit).ToList().ForEach(agent =>
                {
                    count += -1;
                    if (count >= 0)
                    {
                        if (!updatedFormations.Contains(agent.Formation))
                            updatedFormations.Add(agent.Formation);
                        if (!updatedFormations.Contains(_artilleryFormation))
                            updatedFormations.Add(_guardFormation);
                        agent.Formation = _guardFormation;
                    }
                });
            }

            updatedFormations.ForEach(formation => Team.TriggerOnFormationsChanged(formation));
            if (_artilleryFormation.CountOfUnits > 0) Team.TeamAI.OnUnitAddedToFormationForTheFirstTime(_artilleryFormation);
            if (_guardFormation.CountOfUnits > 0) Team.TeamAI.OnUnitAddedToFormationForTheFirstTime(_guardFormation);
        }

        protected override void OnCancel()
        {
            _usingMachines = false;
            StopUsingAllMachines();
            _artilleryFormation.Arrangement.GetAllUnits()
                .Select(unit => (Agent) unit)
                .ToList()
                .ForEach(agent => agent.Formation = _archers);

            _guardFormation.Arrangement.GetAllUnits()
                .Select(unit => (Agent) unit)
                .ToList()
                .ForEach(agent => agent.Formation = _mainInfantry);
        }

        protected override void StopUsingAllMachines()
        {
            if (_usingMachines) return; // A way to cancel out a call in the tick() method that we dont otherwise want to modify.
            base.StopUsingAllMachines();
        }

        protected void ResumeUsingMachines()
        {
            foreach (UsableMachine usable in _artilleryFormation.GetUsedMachines().ToList())
            {
                _artilleryFormation.StartUsingMachine(usable);
            }

            _usingMachines = true;
        }

        protected override bool CheckAndSetAvailableFormationsChanged()
        {
            var aiControlledFormationCount = FormationsIncludingSpecialAndEmpty.ToList().FindAll(form => form.CountOfUnits > 0).Count(f => f.IsAIControlled);
            if (aiControlledFormationCount != _AIControlledFormationCount)
            {
                _AIControlledFormationCount = aiControlledFormationCount;
                IsTacticReapplyNeeded = true;
                return true;
            }

            if (_mainInfantry != null && (_mainInfantry.CountOfUnits == 0 || !_mainInfantry.QuerySystem.IsInfantryFormation) ||
                _archers != null && (_archers.CountOfUnits == 0 || !_archers.QuerySystem.IsRangedFormation) ||
                _leftCavalry != null && (_leftCavalry.CountOfUnits == 0 || !_leftCavalry.QuerySystem.IsCavalryFormation) ||
                _rightCavalry != null && (_rightCavalry.CountOfUnits == 0 || !_rightCavalry.QuerySystem.IsCavalryFormation) ||
                _artilleryFormation != null && _artilleryFormation.CountOfUnits == 0 ||
                _guardFormation != null && _guardFormation.CountOfUnits == 0)
                return true;

            return _rangedCavalry != null && (_rangedCavalry.CountOfUnits == 0 || !_rangedCavalry.QuerySystem.IsRangedCavalryFormation);
        }
}