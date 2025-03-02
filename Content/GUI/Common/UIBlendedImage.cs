using ReLogic.Content;
using Terraria.GameContent.UI.Elements;

namespace Terramon.Content.GUI.Common;

public class UIBlendedImage(Asset<Texture2D> texture) : UIImage(texture)
{
    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, null, null, null, null, Main.UIScaleMatrix);
        base.DrawSelf(spriteBatch);
        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, null, null, Main.UIScaleMatrix);
    }
}