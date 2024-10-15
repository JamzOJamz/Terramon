using ReLogic.Content;
using Terraria.Localization;

namespace Terramon.Content.GUI.Common;

public class UIHoverImageButton : TransformableUIButton
{
    private bool _isActivated = true;
    private object _text;

    public UIHoverImageButton(Asset<Texture2D> texture, string text) : base(texture)
    {
        _text = text;
    }

    public UIHoverImageButton(Asset<Texture2D> texture, LocalizedText text) : base(texture)
    {
        _text = text;
    }

    public void SetHoverText(string hoverText)
    {
        _text = hoverText;
    }

    public void SetHoverText(LocalizedText hoverText)
    {
        _text = hoverText;
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
        if (Main.inFancyUI)
            Main.instance.MouseText(_text.ToString());
        else
            Main.hoverItemName = _text.ToString();
    }
}