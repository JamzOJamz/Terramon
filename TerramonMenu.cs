using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;

namespace Terramon;

public class TerramonMenu : ModMenu
{
    public override string DisplayName => "Terramon";

    public override Asset<Texture2D> Logo =>
        ModContent.Request<Texture2D>("Terramon/logo", AssetRequestMode.ImmediateLoad);
}