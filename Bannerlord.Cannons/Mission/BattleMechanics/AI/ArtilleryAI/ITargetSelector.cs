using Bannerlord.Cannons.BattleMechanics.AI.CommonAIFunctions;

namespace Bannerlord.Cannons.BattleMechanics.AI.ArtilleryAI
{
    public interface ITargetSelector
    {
        Target FindBestTarget();
    }
}
