namespace Bannerlord.Cannons.Integration.UI
{
    public class CampaignMapSiegeEngineDeploymentIconEnricher
    {
        private const string SiegeEngineDeploymentIconBrushName = "CustomBattle.Siege.MachineIcon";

        private readonly BrushStyleExtender _brushStyleExtender;

        public CampaignMapSiegeEngineDeploymentIconEnricher(BrushStyleExtender brushStyleExtender)
        {
            _brushStyleExtender = brushStyleExtender;
        }

        public void AddCampaignMapSiegeEngineDeploymentIcon(string siegeEngineName, string siegeDeploymentIconSpritePath)
        {
            _brushStyleExtender.AddBrushStyle(siegeEngineName, siegeDeploymentIconSpritePath,
                SiegeEngineDeploymentIconBrushName);
        }
    }
}
