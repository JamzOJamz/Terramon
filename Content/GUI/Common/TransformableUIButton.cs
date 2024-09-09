using ReLogic.Content;
using Terraria.Audio;
using Terraria.ID;
using Terraria.UI;

namespace Terramon.Content.GUI.Common;

public class TransformableUIButton : UIElement
{
    public float Rotation { get; set; }
    private Asset<Texture2D> _texture;
    private float _visibilityActive = 1f;
    private float _visibilityInactive = 0.4f;
    private Asset<Texture2D> _borderTexture;

    protected TransformableUIButton(Asset<Texture2D> texture)
    {
        _texture = texture;
        Width.Set(_texture.Width(), 0f);
        Height.Set(_texture.Height(), 0f);
    }

    public void SetHoverImage(Asset<Texture2D> texture)
    {
        _borderTexture = texture;
    }

    public void SetImage(Asset<Texture2D> texture)
    {
        _texture = texture;
        Width.Set(_texture.Width(), 0f);
        Height.Set(_texture.Height(), 0f);
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        var dimensions = GetDimensions();
        spriteBatch.Draw(_texture.Value, dimensions.Center(), null, Color.White * (IsMouseHovering ? _visibilityActive : _visibilityInactive), Rotation, _texture.Frame().Size() / 2f, 1f, SpriteEffects.None, 0f);
        if (_borderTexture != null && IsMouseHovering)
            spriteBatch.Draw(_borderTexture.Value, dimensions.Center(), null, Color.White, Rotation, _borderTexture.Frame().Size() / 2f, 1f, SpriteEffects.None, 0f);
    }

    public override void MouseOver(UIMouseEvent evt)
    {
        base.MouseOver(evt);
        SoundEngine.PlaySound(SoundID.MenuTick);
    }

    public void SetVisibility(float whenActive, float whenInactive)
    {
        _visibilityActive = MathHelper.Clamp(whenActive, 0f, 1f);
        _visibilityInactive = MathHelper.Clamp(whenInactive, 0f, 1f);
    }
}