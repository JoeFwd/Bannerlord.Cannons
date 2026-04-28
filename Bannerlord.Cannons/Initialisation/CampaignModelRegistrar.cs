using System.Linq;
using Bannerlord.Cannons.Integration.Campaign;
using Bannerlord.Cannons.Infrastructure.Registry;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.Core;

namespace Bannerlord.Cannons.Initialisation
{
    public class CampaignModelRegistrar
    {
        private readonly ICannonRegistry _cannonRegistry;

        public CampaignModelRegistrar(ICannonRegistry cannonRegistry)
        {
            _cannonRegistry = cannonRegistry;
        }

        public void Register(Game game, IGameStarter starterObject)
        {
            if (game.GameType is not Campaign ||
                starterObject is not CampaignGameStarter campaignGameStarter)
            {
                return;
            }

            campaignGameStarter.AddModel(
                new CannonSiegeEventModel(
                    campaignGameStarter.Models.OfType<SiegeEventModel>().Last(),
                    _cannonRegistry));
        }
    }
}
