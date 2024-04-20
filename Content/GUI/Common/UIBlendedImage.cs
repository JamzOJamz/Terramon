using ReLogic.Content;
using Terraria.GameContent.UI.Elements;

namespace Terramon.Content.GUI.Common;

public class UIBlendedImage : UIImage
{
    public UIBlendedImage(Asset<Texture2D> texture) : base(texture)
    {
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, Main.DefaultSamplerState,
            DepthStencilState.None, Main.Rasterizer, null, Main.UIScaleMatrix);
        base.DrawSelf(spriteBatch);
        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.AnisotropicClamp,
            DepthStencilState.None, Main.Rasterizer, null, Main.UIScaleMatrix);
    }
}