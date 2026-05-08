using TaleWorlds.TwoDimension;

namespace Bannerlord.Cannons.Integration.UI
{
    public interface IBannerlordSpriteRepository
    {
        Sprite? GetSprite(string name);
    }
}
