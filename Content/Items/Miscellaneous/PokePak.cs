using ReLogic.Content;
using Terraria.Localization;

namespace Terramon.Content.Items;

public class PokePak : TerramonItem
{
    private const string OverlayPath = "Terramon/Assets/Items/Miscellaneous/PokePak_Overlay";

    private static Asset<Texture2D> _overlayTexture;
    public override string Texture => "Terramon/Assets/Items/Miscellaneous/PokePak";

    public override LocalizedText DisplayName =>
        Language.GetText("Mods.Terramon.Items.PokePak.DisplayName").WithFormatArgs("Untitled Box");

    public override void SetStaticDefaults()
    {
        if (Main.dedServ) return;
        _overlayTexture = ModContent.Request<Texture2D>(OverlayPath);
    }

    public override void PostDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame,
        Color drawColor, Color itemColor,
        Vector2 origin, float scale)
    {
        spriteBatch.Draw(_overlayTexture.Value, position, frame, Color.White, 0f, origin, scale, SpriteEffects.None,
            0f);
    }

    public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, float rotation,
        float scale,
        int whoAmI)
    {
        Main.GetItemDrawFrame(Item.type, out _, out var itemFrame);
        var origin = itemFrame.Size() / 2f;
        var drawPosition = Item.Bottom - Main.screenPosition - new Vector2(0, origin.Y);

        spriteBatch.Draw(_overlayTexture.Value, drawPosition, itemFrame, Color.White, rotation, origin, scale,
            SpriteEffects.None, 0f);
    }
}