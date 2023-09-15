using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent.UI.Elements;

namespace Terramon.Content.GUI.Common;

public class UIHoverImage : UIImage
{
    private readonly string HoverText;

    protected UIHoverImage(Asset<Texture2D> texture, string hoverText) : base(texture)
    {
        HoverText = hoverText;
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        base.DrawSelf(spriteBatch);
        if (ContainsPoint(Main.MouseScreen)) Main.LocalPlayer.mouseInterface = true;
        if (IsMouseHovering) Main.hoverItemName = HoverText;
    }
}