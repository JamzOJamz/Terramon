using Terraria.UI;

namespace Terramon.Content.GUI.Common;

public class UIContainer : UIElement
{
    private readonly Vector2 Size;

    public UIContainer(Vector2 size)
    {
        Size = size;
        InternalUpdateSize();
    }

    private void InternalUpdateSize()
    {
        MinWidth.Set(Size.X + PaddingLeft + PaddingRight, 0.0f);
        MinHeight.Set(Size.Y + PaddingTop + PaddingBottom, 0.0f);
    }

    public override void Recalculate()
    {
        InternalUpdateSize();
        base.Recalculate();
    }
}