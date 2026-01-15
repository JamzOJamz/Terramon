using ReLogic.Content;
using Terraria.Audio;
using Terraria.UI;

namespace Terramon.Content.GUI.Common;

public class TransformableUIButton : UIElement
{
    private Asset<Texture2D> _borderTexture;
    private bool _borderTextureIsOverlay = true;
    private Asset<Texture2D> _texture;
    protected bool RemoveFloatingPointsFromDrawPosition = true;
    protected float Scale = 1f;

    protected TransformableUIButton(Asset<Texture2D> texture)
    {
        _texture = texture;
        Width.Set(_texture.Width(), 0f);
        Height.Set(_texture.Height(), 0f);
    }

    public float VisibilityActive { get; private set; } = 1f;
    public float VisibilityInactive { get; private set; } = 0.4f;

    protected bool JustHovered { get; private set; }

    /// <summary>
    ///     Optional visibility override. When set to a value >= 0, it will override the normal visibility logic.
    ///     Set to -1 to disable the override and use normal visibility behavior.
    /// </summary>
    public float VisibilityOverride { get; set; } = -1f;

    public float Rotation { get; set; }

    public void SetHoverImage(Asset<Texture2D> texture, bool isOverlay = true)
    {
        _borderTexture = texture;
        if (texture == null) return;
        _borderTextureIsOverlay = isOverlay;
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
        if (RemoveFloatingPointsFromDrawPosition)
        {
            dimensions.X = (int)dimensions.X;
            dimensions.Y = (int)dimensions.Y;
        }

        var drawPos = dimensions.Center();

        // Use visibility override if set, otherwise use normal visibility logic
        var visibility = VisibilityOverride >= 0f
            ? VisibilityOverride
            : (IsMouseHovering ? VisibilityActive : VisibilityInactive);

        // TODO: Less code duplication here, I can't think of a good proper way to do this right now
        if (_borderTextureIsOverlay)
        {
            spriteBatch.Draw(_texture.Value, drawPos, null,
                Color.White * visibility, Rotation,
                _texture.Frame().Size() / 2f, Scale, SpriteEffects.None, 0f);
            if (_borderTexture != null && ContainsPoint(Main.MouseScreen) && !IgnoresMouseInteraction)
                spriteBatch.Draw(_borderTexture.Value, drawPos, null, Color.White, Rotation,
                    _borderTexture.Frame().Size() / 2f, Scale, SpriteEffects.None, 0f);
        }
        else
        {
            if (_borderTexture == null || !ContainsPoint(Main.MouseScreen) || IgnoresMouseInteraction)
                spriteBatch.Draw(_texture.Value, drawPos, null,
                    Color.White * visibility, Rotation,
                    _texture.Frame().Size() / 2f, Scale, SpriteEffects.None, 0f);
            else
                spriteBatch.Draw(_borderTexture.Value, drawPos, null, Color.White, Rotation,
                    _borderTexture.Frame().Size() / 2f, Scale, SpriteEffects.None, 0f);
        }
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (ContainsPoint(Main.MouseScreen) && !JustHovered)
        {
            SoundEngine.PlaySound(in SoundID.MenuTick);
            JustHovered = true;
        }
        else if (!ContainsPoint(Main.MouseScreen))
        {
            JustHovered = false;
        }
    }

    public void SetVisibility(float whenActive, float whenInactive)
    {
        VisibilityActive = MathHelper.Clamp(whenActive, 0f, 1f);
        VisibilityInactive = MathHelper.Clamp(whenInactive, 0f, 1f);
    }
}