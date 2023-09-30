using ReLogic.Content;
using Terraria.GameContent.UI.Elements;

namespace Terramon.Content.GUI.Common;

public class UIBlendedImage : UIImage
{
    public UIBlendedImage(Asset<Texture2D> texture) : base(texture)
    {
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, Main.DefaultSamplerState,
            DepthStencilState.None, Main.Rasterizer, null, Main.UIScaleMatrix);
        base.Draw(spriteBatch);
        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.AnisotropicClamp,
            DepthStencilState.None, Main.Rasterizer, null, Main.UIScaleMatrix);
    }
}