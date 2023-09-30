using ReLogic.Content;
using Terraria.GameContent.UI.Elements;

namespace Terramon.Content.GUI.Common;

public class UIHoverImageButton : UIImageButton
{
    private readonly string HoverText;
    private bool IsActivated;

    public UIHoverImageButton(Asset<Texture2D> texture, string hoverText) : base(texture)
    {
        HoverText = hoverText;
    }

    public void SetIsActive(bool active)
    {
        IsActivated = active;
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        if (!IsActivated) return;
        base.DrawSelf(spriteBatch);
        if (ContainsPoint(Main.MouseScreen)) Main.LocalPlayer.mouseInterface = true;
        if (IsMouseHovering) Main.hoverItemName = HoverText;
    }
}