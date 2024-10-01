using ReLogic.Content;

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
        if (!ContainsPoint(Main.MouseScreen)) return;
        Main.LocalPlayer.mouseInterface = true;
        Main.hoverItemName = text;
    }
}