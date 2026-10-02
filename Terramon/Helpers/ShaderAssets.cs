using ReLogic.Content;
using Terraria.Graphics.Shaders;

namespace Terramon.Helpers;
public static class ShaderAssets
{
    public const string Effects = "Assets/Effects/";

    public static MiscShaderData FadeToColor { get; private set; }
    public static MiscShaderData Outline { get; private set; }
    public static MiscShaderData StatBoost { get; private set; }
    public static Asset<Effect> Palette { get; private set; }

    private static AssetRepository _repo;
    internal static void Load(AssetRepository repo)
    {
        _repo = repo;

        FadeToColor = Register("FadeToColor");
        Outline = Register("Outline");
        StatBoost = Register("StatBoost");
        Palette = _repo.Request<Effect>(Effects + "Palette");
    }

    private static MiscShaderData Register(string name)
    {
        var newShader = new MiscShaderData(_repo.Request<Effect>(Effects + name), "ShaderPass");
        GameShaders.Misc[nameof(Terramon) + name] = newShader;
        return newShader;
    }
}
