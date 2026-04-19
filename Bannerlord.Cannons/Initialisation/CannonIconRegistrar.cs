using Bannerlord.Cannons.Infrastructure.Icons;
using Bannerlord.Cannons.Infrastructure.Registry;
using Bannerlord.Cannons.Integration.UI;
using TaleWorlds.Engine.GauntletUI;

namespace Bannerlord.Cannons.Initialisation
{
    public class CannonIconRegistrar
    {
        public void Register()
        {
            var brushExtender = new BrushStyleExtender(
                UIResourceManager.BrushFactory,
                UIResourceManager.SpriteData);
            var deploymentIconEnricher = new SiegeEngineDeploymentIconEnricher(brushExtender);
            var campaignMapIconEnricher = new CampaignMapSiegeEngineDeploymentIconEnricher(brushExtender);
            var iconProvider = new CannonIconProvider(CannonRegistry.Instance);

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
