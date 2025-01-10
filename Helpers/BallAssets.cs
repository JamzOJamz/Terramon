using ReLogic.Content;
using Terramon.ID;

namespace Terramon.Helpers;

public class BallAssets : ILoadable
{
    private const string PokeballIconPrefix = "Terramon/Assets/Items/PokeBalls/";
    private static Asset<Texture2D>[] _ballIcons;

    public static Asset<Texture2D> GetBallIcon(BallID id) => _ballIcons[(int)id];
    
    public void Load(Mod mod)
    {
        _ballIcons = new Asset<Texture2D>[byte.MaxValue + 1];
        for (var i = 0; i <= byte.MaxValue; i++)
        {
            var ballName = Enum.GetName(typeof(BallID), i);
            if (ballName == null) continue;
            var ballIconName = ballName + "MiniItem";
            _ballIcons[i] = ModContent.Request<Texture2D>(PokeballIconPrefix + ballIconName);
        }
    }

    public void Unload()
    {
        _ballIcons = null;
    }
}