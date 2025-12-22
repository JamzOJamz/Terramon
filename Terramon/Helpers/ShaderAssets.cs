using ReLogic.Content;
using Terraria.Graphics.Shaders;

namespace Terramon.Helpers;

public static class ShaderAssets
{
    private const string ShaderPath = "Assets/Shaders/";

    private static AssetRepository _repo;

    public static MiscShaderData FadeToColor { get; private set; }
    public static MiscShaderData Outline { get; private set; }
    public static Asset<Effect> Palette { get; private set; }

    internal static void Load(AssetRepository repo)
    {
        _repo = repo;

        FadeToColor = Register("FadeToColor", "FadePass");
        Outline = Register("Outline", "ShaderPass");
        Palette = _repo.Request<Effect>(ShaderPath + "Palette");
    }

    private static MiscShaderData Register(string name, string passName = null)
    {
        passName ??= name + "Pass";
        var newShader = new MiscShaderData(_repo.Request<Effect>(ShaderPath + name), passName);
        GameShaders.Misc[nameof(Terramon) + name] = newShader;
        return newShader;
    }
}