using ReLogic.Content;
using Terraria.GameContent.UI.Elements;

namespace Terramon.Content.GUI.Common;

public class UIHoverImageButton(Asset<Texture2D> texture, string text) : TransformableUIButton(texture)
{
    private bool _isActivated = true;

    public void SetHoverText(string hoverText)
    {
        text = hoverText;
    }

    public void SetIsActive(bool active)
    {
        _isActivated = active;
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        if (!_isActivated) return;
        base.DrawSelf(spriteBatch);
        if (ContainsPoint(Main.MouseScreen)) Main.LocalPlayer.mouseInterface = true;
        if (IsMouseHovering) Main.hoverItemName = text;
    }
}