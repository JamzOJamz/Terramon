using ReLogic.Content;
using Terraria.Graphics.Shaders;

namespace Terramon.Helpers;
public static class ShaderAssets
{
    public const string Effects = "Assets/Effects/";

    public static MiscShaderData FadeToColor { get; private set; }
    public static MiscShaderData Outline { get; private set; }

    private static AssetRepository _repo;
    internal static void Load(AssetRepository repo)
    {
        _repo = repo;

        FadeToColor = Register("FadeToColor", "FadePass");
        Outline = Register("Outline", "ShaderPass");
    }

    private static MiscShaderData Register(string name, string passName = null)
    {
        passName ??= name + "Pass";
        var newShader = new MiscShaderData(_repo.Request<Effect>(Effects + name), passName);
        GameShaders.Misc[nameof(Terramon) + name] = newShader;
        return newShader;
    }
}
