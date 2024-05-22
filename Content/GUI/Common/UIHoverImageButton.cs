using ReLogic.Content;
using Terraria.GameContent.UI.Elements;

namespace Terramon.Content.GUI.Common;

public class UIHoverImageButton : UIImageButton
{
    private readonly string _hoverText;
    private bool _isActivated;

    public UIHoverImageButton(Asset<Texture2D> texture, string hoverText) : base(texture)
    {
        _hoverText = hoverText;
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
        if (IsMouseHovering) Main.hoverItemName = _hoverText;
    }
}