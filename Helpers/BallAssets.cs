using ReLogic.Content;
using Terramon.ID;

namespace Terramon.Helpers;

[Autoload(Side = ModSide.Client)]
public class BallAssets : ILoadable
{
    private const string PokeballIconPathFormat = "Terramon/Assets/Items/PokeBalls/{0}MiniItem";
    private static Asset<Texture2D>[] _ballIcons;

    public void Load(Mod mod)
    {
        _ballIcons = new Asset<Texture2D>[256];
        foreach (var ballId in Enum.GetValues(typeof(BallID)).Cast<BallID>())
        {
            var id = (int)ballId;
            var path = string.Format(PokeballIconPathFormat, ballId);
            if (!ModContent.HasAsset(path)) continue;
            _ballIcons[id] = ModContent.Request<Texture2D>(path);
        }
    }

    public void Unload()
    {
        _ballIcons = null;
    }

    public static Asset<Texture2D> GetBallIcon(BallID id)
    {
        return _ballIcons[(int)id];
    }
}