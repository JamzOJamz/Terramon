using ReLogic.Content;

namespace Terramon;

public class TerramonMenu : ModMenu
{
    public override string DisplayName => "Terramon";

    public override int Music => MusicLoader.GetMusicSlot(Mod, "Assets/Audio/Music/menu_theme");

    public override Asset<Texture2D> Logo =>
        ModContent.Request<Texture2D>("Terramon/logo", AssetRequestMode.ImmediateLoad);

    public override bool PreDrawLogo(SpriteBatch spriteBatch, ref Vector2 logoDrawCenter, ref float logoRotation, ref float logoScale,
        ref Color drawColor)
    {
        logoDrawCenter.Y += 10;
        return base.PreDrawLogo(spriteBatch, ref logoDrawCenter, ref logoRotation, ref logoScale, ref drawColor);
    }
}