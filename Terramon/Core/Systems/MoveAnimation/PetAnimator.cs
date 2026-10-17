using Terramon.Content.Projectiles;
using Terraria.Audio;

namespace Terramon.Core.Systems.MoveAnimation;

public sealed class PetAnimator : IAnimator
{
    public PokemonPet pet;
    public Entity other;
    public static readonly PetAnimator Instance = new();
    public void ShowTexture(Texture2D tex, EffectPosition position, Color color, EffectRotation rotation, EffectDirection direction, EffectScale? scaleX = null, EffectScale? scaleY = null, Rectangle? frame = null, Vector2? origin = null)
    {
        if (!(scaleX.HasValue || scaleY.HasValue))
            return;
        var selfBox = pet.Projectile.Hitbox;
        var otherBox = other.Hitbox;
        var finalPos = position.ToVector2(selfBox, otherBox);
        var space = Rectangle.Union(selfBox, otherBox);
        var xScale = scaleX.GetValueOrDefault().ToScale(selfBox.Width, otherBox.Width, space.Width, pet.Projectile.scale);
        var yScale = scaleY.GetValueOrDefault().ToScale(selfBox.Height, otherBox.Height, space.Height, pet.Projectile.scale);
        var finalScale = scaleX.HasValue && scaleY.HasValue ? new Vector2(xScale, yScale) : new Vector2(scaleX.HasValue ? xScale : yScale);
        Main.spriteBatch.Draw(tex, finalPos - Main.screenPosition, frame, color, rotation.ToRotation(selfBox, otherBox), origin.HasValue ? origin.Value : (frame.HasValue ? frame.Value.Size() : tex.Size()) * 0.5f, finalScale, direction.ToSpriteEffects(selfBox, otherBox), 0f);
    }
    public void PlaySound(in SoundStyle sound, EffectPosition? position)
    {
        SoundEngine.PlaySound(in sound, position.HasValue ? position.Value.ToVector2(pet.Projectile.Hitbox, other.Hitbox) : null);
    }
    public void QueuePositionalAction(Action<Vector2> action, EffectPosition position, float delay = 0f)
    {
        action(position.ToVector2(pet.Projectile.Hitbox, other.Hitbox));
    }
    public void ModifyDraw(EffectPosition? position = null, EffectRotation? rotation = null, EffectScale? scaleX = null, EffectScale? scaleY = null, EffectDirection direction = 0)
    {
        pet.AnimPosition = position;
        pet.AnimRot = rotation;
        pet.AnimScaleX = scaleX;
        pet.AnimScaleY = scaleY;
        pet.AnimDir = direction;
    }
    public void QueueDoublePositionalAction(Action<Vector2, Vector2> action, EffectPosition start, EffectPosition end, float delay = 0f)
    {
        var a = pet.Projectile.Hitbox;
        var b = other.Hitbox;
        action(start.ToVector2(a, b), end.ToVector2(a, b));
    }
}
