using ReLogic.Content;

namespace Terramon.Content.Menus;

[Autoload(Side = ModSide.Client)]
public class TerramonMenu : ModMenu
{
    private static readonly Asset<Texture2D> LogoPurity;
    private static readonly Asset<Texture2D> LogoCorruption;

    static TerramonMenu()
    {
        if (Main.dedServ) return;

        LogoPurity = ModContent.Request<Texture2D>("Terramon/Assets/Misc/MenuLogo_0");
        LogoCorruption = ModContent.Request<Texture2D>("Terramon/Assets/Misc/MenuLogo_1");
    }

    public override string DisplayName => "Terramon Mod";

    public override int Music => MusicLoader.GetMusicSlot(Mod, "Sounds/Music/TitleTheme");

    public override bool PreDrawLogo(SpriteBatch spriteBatch, ref Vector2 logoDrawCenter, ref float logoRotation,
        ref float logoScale,
        ref Color drawColor)
    {
        // Offsets the logo draw position a bit for alignment purposes
        logoDrawCenter.Y += 16;

        // Get the logo textures to draw
        var logoPurity = LogoPurity.Value;
        var logoCorruption = LogoCorruption.Value;

        // Calculate rectangle and origin for the logo drawing for reuse
        var logoRect = new Rectangle(0, 0, logoPurity.Width, logoPurity.Height);
        var logoOrigin = new Vector2(logoPurity.Width * 0.5f, logoPurity.Height * 0.5f);

        // Draws the logo textures depending on the current logo alpha values
        spriteBatch.Draw(logoPurity, logoDrawCenter, logoRect, drawColor * (Main.LogoA / 255f), logoRotation,
            logoOrigin, logoScale, SpriteEffects.None, 0f);
        spriteBatch.Draw(logoCorruption, logoDrawCenter, logoRect, drawColor * (Main.LogoB / 255f), logoRotation,
            logoOrigin, logoScale, SpriteEffects.None, 0f);

        return false;
    }

    /// <summary>
    ///     Spoofs the last selected mod menu and sets the current one to Terramon's mod menu.
    /// </summary>
    public void ForceSwitchToThis()
    {
        MenuLoader.switchToMenu = this;
        MenuLoader.LastSelectedModMenu = FullName;
    }
}