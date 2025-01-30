using Terraria.UI;

namespace Terramon.Content.GUI.Common;

public class UIContainer : UIElement
{
    private readonly Vector2 _size;

    public UIContainer(Vector2 size)
    {
        _size = size;
        InternalUpdateSize();
    }

    private void InternalUpdateSize()
    {
        MinWidth.Set(_size.X + PaddingLeft + PaddingRight, 0.0f);
        MinHeight.Set(_size.Y + PaddingTop + PaddingBottom, 0.0f);
    }

    public override void Recalculate()
    {
        InternalUpdateSize();
        base.Recalculate();
    }

    /*public override void Draw(SpriteBatch spriteBatch)
    {
        // Draw the container as a semi-transparent pink box for debugging purposes
        spriteBatch.Draw(TextureAssets.MagicPixel.Value, GetDimensions().ToRectangle(), Color.Pink * 0.5f);

        base.Draw(spriteBatch);
    }*/
}