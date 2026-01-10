using System.Linq;
using Bannerlord.Cannons.BattleMechanics.AI.TeamAI.FormationBehavior;
using TaleWorlds.MountAndBlade;
using TOR_Core.BattleMechanics.AI.TeamAI.FormationBehavior;

namespace Bannerlord.Cannons.BattleMechanics.AI;

public class CannonTacticLogic : MissionLogic
{
    private readonly IArtilleryCrewProvider _artilleryCrewProvider;

    public CannonTacticLogic(IArtilleryCrewProvider artilleryCrewProvider)
    {
        _artilleryCrewProvider = artilleryCrewProvider;
    }

    public override void EarlyStart()
    {
        base.EarlyStart();
        
        Mission.Current.Teams.ToList().ForEach(team =>
        {
            switch (Mission.Current.MissionTeamAIType)
            {
                case Mission.MissionTeamAITypeEnum.NoTeamAI:
                case Mission.MissionTeamAITypeEnum.FieldBattle:
                    
                    team.AddTacticOption(new ArtilleryPositionalTactic(team, _artilleryCrewProvider));
                    team.FormationsIncludingSpecialAndEmpty.ForEach(formation => formation.AI.AddAiBehavior(new TORBehaviorProtectArtillery(formation)));
                    break;
                case Mission.MissionTeamAITypeEnum.Siege:
                    team.AddTacticOption(new ArtilleryTactic(team, _artilleryCrewProvider));
                    break;
            }
        });
    }
}