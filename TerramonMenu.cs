using ReLogic.Content;

namespace Terramon;

public class TerramonMenu : ModMenu
{
    public override string DisplayName => "Terramon Mod";

    public override int Music => MusicLoader.GetMusicSlot(Mod, "Sounds/Music/Menu");

    public override Asset<Texture2D> Logo =>
        ModContent.Request<Texture2D>("Terramon/Assets/Misc/MenuLogo");

    public override bool PreDrawLogo(SpriteBatch spriteBatch, ref Vector2 logoDrawCenter, ref float logoRotation,
        ref float logoScale,
        ref Color drawColor)
    {
        /*var logoTexture = Logo.Value;
        var b = (byte)((255 + Main.tileColor.R * 2) / 3);
        var color = new Color(b, b, b, 255);
        logoDrawCenter.Y += 16;
        spriteBatch.Draw(logoTexture, logoDrawCenter, new Rectangle(0, 0, logoTexture.Width, logoTexture.Height),
            color, logoRotation, new Vector2(logoTexture.Width * 0.5f, logoTexture.Height * 0.5f), logoScale,
            SpriteEffects.None, 0f);
        return false;*/

        logoDrawCenter.Y += 16;
        return true;
    }
}