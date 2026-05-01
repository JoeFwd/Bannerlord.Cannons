using Bannerlord.Cannons.Infrastructure.Icons;
using Bannerlord.Cannons.Infrastructure.Registry;
using Bannerlord.Cannons.Integration.UI;
using Microsoft.Extensions.Logging;
using TaleWorlds.Engine.GauntletUI;

namespace Bannerlord.Cannons.Initialisation
{
    public class CannonIconRegistrar
    {
        private readonly ICannonRegistry _cannonRegistry;
        private readonly ILoggerFactory _loggerFactory;

        public CannonIconRegistrar(ICannonRegistry cannonRegistry, ILoggerFactory loggerFactory)
        {
            _cannonRegistry = cannonRegistry;
            _loggerFactory = loggerFactory;
        }

        public void Register()
        {
            var brushExtender = new BrushStyleExtender(
                UIResourceManager.BrushFactory,
                UIResourceManager.SpriteData,
                _loggerFactory);
            var deploymentIconEnricher = new SiegeEngineDeploymentIconEnricher(brushExtender);
            var campaignMapIconEnricher = new CampaignMapSiegeEngineDeploymentIconEnricher(brushExtender);
            var iconProvider = new CannonIconProvider(_cannonRegistry);

            foreach (var icon in iconProvider.GetSiegeEngineIcons())
            {
                deploymentIconEnricher.AddSiegeEngineDeploymentIcon(
                    icon.Name,
                    icon.SiegeDeploymentSelectionIconSpriteId);
                campaignMapIconEnricher.AddCampaignMapSiegeEngineDeploymentIcon(
                    icon.Name,
                    icon.CampaignMapSelectionIconSpriteId);
            }
        }
    }
}
