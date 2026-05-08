using Microsoft.Extensions.Logging;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.TwoDimension;

namespace Bannerlord.Cannons.Integration.UI
{
    public class BannerlordSpriteRepository : IBannerlordSpriteRepository
    {
        private readonly ILogger _logger;

        public BannerlordSpriteRepository(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<BannerlordSpriteRepository>();
        }

        public Sprite? GetSprite(string name)
        {
            var sprite = UIResourceManager.SpriteData.GetSprite(name);
            if (sprite is null)
                _logger.LogWarning("Could not find sprite '{SpriteName}'. Icon will not show.", name);
            return sprite;
        }
    }
}
