using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using TaleWorlds.GauntletUI;

namespace Bannerlord.Cannons.Integration.UI
{
    public class BrushStyleExtender
    {
        private const string BrushLayerName = "Default";

        private readonly IBannerlordSpriteRepository _spriteRepository;
        private readonly ILogger _logger;
        private readonly BrushFactory _brushFactory;

        public BrushStyleExtender(
            BrushFactory brushFactory,
            IBannerlordSpriteRepository spriteRepository,
            ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<BrushStyleExtender>();
            _brushFactory = brushFactory;
            _spriteRepository = spriteRepository;
        }

        public void AddBrushStyle(string siegeEngineName, string fullSpriteName, string brushName)
        {
            Brush brush = _brushFactory.GetBrush(brushName);

            if (brush is null)
            {
                _logger.LogError($"Could not find any Brush with name {brushName}");
                return;
            }

            var sprite = _spriteRepository.GetSprite(fullSpriteName);

            if (sprite is null)
            {
                _logger.LogError($"Could not find any Sprite with name {fullSpriteName}. Icon will not show up.");
                return;
            }

            brush.AddStyle(new Style(new List<BrushLayer>
            {
                new() { Name = BrushLayerName, Sprite = sprite }
            })
            {
                Name = siegeEngineName,
                DefaultStyle = brush.DefaultStyle
            });
        }
    }
}
