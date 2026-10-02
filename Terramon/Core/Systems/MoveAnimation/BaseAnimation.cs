using Terramon.Helpers;
using Terramon.ID;
using Terraria.Audio;
using Terramon.Content.Projectiles;

namespace Terramon.Core.Systems.MoveAnimation;

public abstract class BaseAnimation : ILoadable
{
    public virtual void Load(Mod mod)
    {
        var t = GetType();
        if (!Enum.TryParse(t.Name, out MoveID move))
            throw new Exception($"Move animation {t.Name} wasn't recognized as a valid MoveID entry");
        MoveAnimations.Animations.Add((ushort)move, (BaseAnimation)Activator.CreateInstance(t));
    }

    public abstract void PlayAt(float currentTime, float previousTime, IAnimator animator);
    public virtual void Oneshots(bool start, IAnimator animator) { }

    public void Unload()
    {
        if (MoveAnimations.Animations.Count != 0)
            MoveAnimations.Animations.Clear();
    }
}

public interface IAnimator
{
    public void ShowTexture(Texture2D tex, EffectPosition position, Color color, EffectRotation rotation, EffectDirection direction, EffectScale? scaleX = null, EffectScale? scaleY = null, Rectangle? frame = null, Vector2? origin = null);
    // instead of a bunch of different methods, maybe we want to provide a single "EffectContext" struct and then receive back a "WorldContext" struct with the data after calculation
    // the point of delay is to use Task.Run() to guarantee certain actions are executed when we want to
    public void QueueDoublePositionalAction(Action<Vector2, Vector2> action, EffectPosition start, EffectPosition end, float delay = 0f);
    public void QueuePositionalAction(Action<Vector2> action, EffectPosition position, float delay = 0f);
    public void ModifyDraw(EffectPosition? position = null, EffectRotation? rotation = null, EffectScale? scaleX = null, EffectScale? scaleY = null, EffectDirection direction = 0);
}

public static class AnimationExtensions
{
    public static void PlaySound(this IAnimator animator, SoundStyle sound, EffectPosition? pos = null)
    {
        if (!pos.HasValue)
        {
            SoundEngine.PlaySound(sound);
            return;
        }
        animator.QueuePositionalAction(p => SoundEngine.PlaySound(sound, p), pos.Value);
    }
    public static void DrawTrail(this IAnimator animator, Texture2D tex, EffectPosition start, EffectPosition end, EffectScale thickness, Color color, int sections = 1, int growIn = 0, int shrinkOut = 0)
    {
        animator.QueueDoublePositionalAction((a, b) =>
        {
            DrawUtils.DrawDirectTrail(new TrailStyle { Color = color, Texture = tex }, a - Main.screenPosition, b - Main.screenPosition, thickness.ToScale(0f, 0f, Vector2.Distance(a, b)), sections, growIn, shrinkOut);
        }, start, end);
    }
}

public struct EffectPosition
{
    public static readonly EffectPosition Self;
    public static readonly EffectPosition Other = new() { Anchor = 1f };
    /// <summary>
    ///     Defines the positional anchor for the effect. 0f indicates the base position is on the center of the animated object, and 1f on the center of the targeted object.
    /// </summary>
    public float Anchor;
    /// <summary>
    ///     Technically the same as <see cref="Anchor"/>, except it's absolute instead of relative, meaning it works in world units. Positive means towards the target, negative means away from the target.
    /// </summary>
    public float XOffsetDirectional;
    /// <summary>
    ///     The same as <see cref="XOffsetDirectional"/>, except the direction of the target relative to the animated object isn't taken into account (i. e. positive value will always be right, negative will always be left)
    /// </summary>
    public float XOffsetAbsolute;
    /// <summary>
    ///     Absolute Y offset. Positive value will always be down, negative will always be up.
    /// </summary>
    public float YOffsetAbsolute;
    /// <summary>
    ///     X offset relative to the animated object's own width. Directional.
    /// </summary>
    public float XOffsetSelfRelativeDirectional;
    /// <summary>
    ///     Y offset relative to the animated object's own height.
    /// </summary>
    public float YOffsetSelfRelative;
    /// <summary>
    ///     X offset relative to the target object's width. Directional.
    /// </summary>
    public float XOffsetOtherRelativeDirectional;
    /// <summary>
    ///     Y offset relative to the target object's height.
    /// </summary>
    public float YOffsetOtherRelative;
    /// <summary>
    ///     Calculates the real position that this <see cref="EffectPosition"/> represents
    /// </summary>
    /// <param name="animated">The animated object.</param>
    /// <param name="target">The target object.</param>
    /// <returns>The real world position.</returns>
    public readonly Vector2 ToVector2(Rectangle animated, Rectangle target)
    {
        var animatedCenterX = animated.X + animated.Width * 0.5f;
        var targetCenterX = target.X + target.Width * 0.5f;
        var direction = Math.Sign(targetCenterX - animatedCenterX);

        var xAnchorCalc = float.Lerp(animatedCenterX, targetCenterX, Anchor);
        var xDirOffCalc = XOffsetDirectional * direction;
        var xRelDirOffCalc = XOffsetSelfRelativeDirectional * animated.Width * direction;
        var xOtherRelDirOffCalc = XOffsetOtherRelativeDirectional * target.Width * direction;
        var finalX = xAnchorCalc + xDirOffCalc + xRelDirOffCalc + xOtherRelDirOffCalc + XOffsetAbsolute;

        var animatedCenterY = animated.Y + animated.Height * 0.5f;
        var targetCenterY = target.Y + target.Height * 0.5f;
        var yAnchorCalc = float.Lerp(animatedCenterY, targetCenterY, Anchor);
        var yRelOffCalc = YOffsetSelfRelative * animated.Height;
        var yOtherRelDirOffCalc = YOffsetOtherRelative * target.Height;
        var finalY = yAnchorCalc + yRelOffCalc + yOtherRelDirOffCalc + YOffsetAbsolute;

        return new(finalX, finalY);
    }
}

public struct EffectRotation
{
    /// <summary>
    ///     Positive is always clockwise, negative is always counterclockwise.
    /// </summary>
    public float AbsoluteRotation;
    /// <summary>
    ///     Positive is clockwise if the target is to the right, and counterclockwise if the target is to the left.
    /// </summary>
    public float RotationToTargetHorizontal;
    /// <summary>
    ///     Rotation relative to the rotation of the angle of the line of sight between the two objects. Leave <see langword="null"/> to not rotate relative to it.
    /// </summary>
    public float? RotationToTarget;
    public readonly float ToRotation(Rectangle animated, Rectangle target)
    {
        var animatedCenter = animated.Center();
        var targetCenter = target.Center();
        var direction = Math.Sign(targetCenter.X - animatedCenter.X);
        var rotation = AbsoluteRotation + RotationToTargetHorizontal * direction;
        if (RotationToTarget.HasValue)
            rotation += animatedCenter.AngleTo(targetCenter) + RotationToTarget.Value;
        return rotation;
    }
}

public struct EffectScale
{
    /// <summary>
    ///     Scale on this axis in world units.
    /// </summary>
    public float AbsoluteScale;
    /// <summary>
    ///     Scale on this axis relative to the animated object's own size.
    /// </summary>
    public float SelfRelative;
    /// <summary>
    ///     Scale on this axis relative to the target's size.
    /// </summary>
    public float OtherRelative;
    /// <summary>
    ///     Scale on this axis relative to the size of the bounding box that contains both objects completely.
    /// </summary>
    public float SpaceRelative;
    /// <summary>
    ///     Scale on this axis relative to a provided "default" size connected to the animated object. For example, <see cref="Projectile.scale"/> for <see cref="PokemonPet"/>. Leave <see langword="null"/> to not mess with this.
    /// </summary>
    public float? PreSelfRelative;
    /// <summary>
    ///     Scale on this axis relative to a provided "default" size connected to the target. For example, <see cref="Projectile.scale"/> for <see cref="PokemonPet"/>. Leave <see langword="null"/> to not mess with this.
    /// </summary>
    public float? PreOtherRelative;
    public readonly float ToScale(Rectangle animated, Rectangle target, int axis, float pre = default, float preOther = default)
    {
        var space = Rectangle.Union(animated, target);
        if (axis == 0)
            return ToScale(animated.Width, target.Width, space.Width, pre, preOther);
        return ToScale(animated.Height, target.Height, space.Height, pre, preOther);
    }
    public readonly float ToScale(float animatedSize, float targetSize, float spaceSize, float pre = default, float preOther = default)
    {
        var selfRel = SelfRelative * animatedSize;
        var otherRel = OtherRelative * targetSize;
        var spaceRel = SpaceRelative * spaceSize;
        var preRel = PreSelfRelative.HasValue ? pre * PreSelfRelative.Value : 0f;
        var preOtherRel = PreOtherRelative.HasValue ? preOther * PreOtherRelative.Value : 0f;
        return selfRel + otherRel + spaceRel + preRel + preOtherRel + AbsoluteScale;
    }
}

public enum EffectDirection
{
    Keep,
    Flip,
    ToTarget,
    ToSelf,
}

public static class EffectExtensions
{
    public static SpriteEffects ToSpriteEffects(this EffectDirection dir, Rectangle animated, Rectangle target)
    {
        if (dir is EffectDirection.Keep)
            return SpriteEffects.None;
        else if (dir is EffectDirection.Flip)
            return SpriteEffects.FlipHorizontally;

        var animatedCenterX = animated.X + animated.Width * 0.5f;
        var targetCenterX = target.X + target.Width * 0.5f;
        var direction = Math.Sign(targetCenterX - animatedCenterX);
        if (dir is EffectDirection.ToTarget)
            direction = -direction;
        if (direction == 1)
            return SpriteEffects.None;
        return SpriteEffects.FlipHorizontally;
    }
}
