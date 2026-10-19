using Terramon.Content.Projectiles;
using Terramon.Helpers;
using Terramon.ID;
using Terraria.Audio;

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
    public void QueueContextfulAction(Action<WorldContext> action, EffectContext context, EffectTime delay = default, EffectOrder order = EffectOrder.WorldUpdate, ScaleParams scaleParams = default);
    public void ModifyDraw(EffectPosition? position = null, EffectRotation? rotation = null, EffectScale? scaleX = null, EffectScale? scaleY = null, EffectDirection direction = 0);
}

public enum EffectOrder
{
    WorldUpdate,
    PreDrawDust,
    PostDrawDust,
    PreDrawProjectiles,
    PostDrawProjectiles,
    MAX_GENERIC = PostDrawProjectiles + 1,
    PreDrawAnimated,
    PostDrawAnimated,
    PreDrawTarget,
    PostDrawTarget,
}
public readonly record struct EffectContext
{
    public readonly EffectPosition[] Positions;
    public readonly EffectRotation[] Rotations;
    public readonly EffectDirection[] Directions;
    public readonly EffectScale[] Scales;
    public EffectContext(EffectPosition[] positions = null, EffectRotation[] rotations = null, EffectDirection[] directions = null, EffectScale[] scales = null)
    {
        Positions = positions;
        Rotations = rotations;
        Directions = directions;
        Scales = scales;
    }
    public EffectContext(params Span<object> any)
    {
        var pos = new EffectPosition[any.Length];
        var rot = new EffectRotation[any.Length];
        var dir = new EffectDirection[any.Length];
        var sca = new EffectScale[any.Length];
        int posC, rotC, dirC, scaC;
        posC = rotC = dirC = scaC = 0;
        foreach (ref var obj in any)
        {
            switch (obj)
            {
                case EffectPosition p:
                    pos[posC++] = p;
                    break;
                case EffectRotation r:
                    rot[rotC++] = r;
                    break;
                case EffectDirection d:
                    dir[dirC++] = d;
                    break;
                case EffectScale s:
                    sca[scaC++] = s;
                    break;
                default:
                    throw new InvalidDataException();
            }
        }
        if (posC == 0)
            pos = null;
        else
            Array.Resize(ref pos, posC);
        if (rotC == 0)
            rot = null;
        else
            Array.Resize(ref rot, rotC);
        if (dirC == 0)
            dir = null;
        else
            Array.Resize(ref dir, dirC);
        if (scaC == 0)
            sca = null;
        else
            Array.Resize(ref sca, scaC);
        Positions = pos;
        Rotations = rot;
        Directions = dir;
        Scales = sca;
    }
    public readonly WorldContext ToWorld(Rectangle animated, Rectangle target, ScaleParams parameters = default)
    {
        var pos = default(Vector2[]);
        var rot = default(float[]);
        var dir = Directions == null ? null : new SpriteEffects[Directions.Length];
        var sca = Scales == null ? null : new float[Scales.Length];
        if (Positions != null)
        {
            pos = new Vector2[Positions.Length];
            for (var i = 0; i < Positions.Length; i++)
                pos[i] = Positions[i].ToVector2(animated, target);
        }
        if (Rotations != null)
        {
            rot = new float[Rotations.Length];
            for (var i = 0; i < Rotations.Length; i++)
                rot[i] = Rotations[i].ToRotation(animated, target);
        }
        if (Directions != null)
        {
            dir = new SpriteEffects[Directions.Length];
            for (var i = 0; i < Directions.Length; i++)
                dir[i] = Directions[i].ToSpriteEffects(animated, target);
        }
        if (Scales != null)
        {
            sca = new float[Scales.Length];
            for (var i = 0; i < Scales.Length; i++)
                sca[i] = Scales[i].ToScale(animated, target, ((uint)parameters.Axes >> i) & 1, parameters.PreSelf, parameters.PreOther, parameters.EffectSize);
        }
        return new WorldContext(pos, rot, dir, sca);
    }
}

public readonly record struct ScaleParams(ScaleAxes Axes, float PreSelf = 0f, float PreOther = 0f, Vector2 EffectSize = default)
{
    public static readonly ScaleParams XY = new(ScaleAxes.Y1);
    public static readonly ScaleParams YX = new(ScaleAxes.Y0);
    public static readonly ScaleParams XX = default;
    public static readonly ScaleParams YY = new(ScaleAxes.Y0 | ScaleAxes.Y1);
}

[Flags]
public enum ScaleAxes : uint
{
    X = 0,
    Y0 = 1,
    Y1 = 2,
    Y2 = 4,
    Y3 = 8,
    Y4 = 16,
    Y5 = 32,
    Y6 = 64,
    Y7 = 128,
}

public readonly record struct WorldContext(Vector2[] Positions = null, float[] Rotations = null, SpriteEffects[] Directions = null, float[] Scales = null);

public static class AnimationExtensions
{
    public static void QueueDraw(this IAnimator animator, Texture2D tex, Color color, EffectPosition position, EffectScale? scaleX = null, EffectScale? scaleY = null, EffectRotation rotation = default, EffectDirection direction = EffectDirection.Keep, Rectangle? frame = null, Vector2? origin = null, EffectTime delay = default, EffectOrder order = EffectOrder.PostDrawAnimated)
    {
        if (!(scaleX.HasValue || scaleY.HasValue))
            return;
        var finalScales = (scaleX.HasValue && scaleY.HasValue) ? new EffectScale[] { scaleX.Value, scaleY.Value } : [scaleX ?? scaleY.Value];
        var sParams = ((scaleY.HasValue && !scaleX.HasValue) ? ScaleParams.YX : ScaleParams.XY) with { EffectSize = tex.Size() };
        animator.QueueContextfulAction(wCtx =>
        {
            var scale = new Vector2(wCtx.Scales[0]);
            if (wCtx.Scales.Length != 1)
                scale.Y = wCtx.Scales[1];
            Main.spriteBatch.Draw
            (
                tex,
                wCtx.Positions[0] - Main.screenPosition,
                frame,
                color,
                wCtx.Rotations[0],
                origin ?? (frame.HasValue ? frame.Value.Size() : tex.Size()) * 0.5f,
                scale,
                wCtx.Directions[0],
                0f
            );
        }, new EffectContext(positions: [position], rotations: [rotation], scales: finalScales, directions: [direction]), delay, order, sParams);
    }
    public static void QueueSound(this IAnimator animator, SoundStyle sound, EffectPosition? pos = null, EffectTime delay = default)
    {
        animator.QueueContextfulAction(wCtx => SoundEngine.PlaySound(
            sound,
            wCtx.Positions?[0]),
            new EffectContext(positions: pos.HasValue ? [pos.Value] : null), delay);
    }
    public static void DrawTrail(this IAnimator animator, Texture2D tex, EffectPosition start, EffectPosition end, EffectScale thickness, Color color, int sections = 1, int growIn = 0, int shrinkOut = 0, EffectTime delay = default, EffectOrder order = EffectOrder.PreDrawAnimated)
    {
        animator.QueueContextfulAction(wCtx => DrawUtils.DrawDirectTrail(
            new TrailStyle { Color = color, Texture = tex },
            wCtx.Positions[0] - Main.screenPosition, wCtx.Positions[1] - Main.screenPosition,
            wCtx.Scales[0],
            sections, growIn, shrinkOut), new EffectContext (positions: [start, end], scales: [thickness]), delay, order);
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
    ///     Scale relative to a provided effect size and the animated object's own size.<br/>
    ///     Example: When drawing a texture with a size of 16x16 px, we want its size to visually match the animated object's width.
    ///     For this we would use a value of 1f in <see cref="EffectSelfRelative"/>. Other options do not work as they will not take the effect's size into account.
    /// </summary>
    public float EffectSelfRelative;
    /// <summary>
    ///     Scale relative to a provided effect size and the target's size.<br/>
    ///     Example: When drawing a texture with a size of 16x16 px, we want its size to visually match the target's width.
    ///     For this we would use a value of 1f in <see cref="EffectOtherRelative"/>. Other options do not work as they will not take the effect's size into account.
    /// </summary>
    public float EffectOtherRelative;
    /// <summary>
    ///     Scale on this axis relative to a provided "default" size connected to the animated object. For example, <see cref="Projectile.scale"/> for <see cref="PokemonPet"/>. Leave <see langword="null"/> to not mess with this.
    /// </summary>
    public float? PreSelfRelative;
    /// <summary>
    ///     Scale on this axis relative to a provided "default" size connected to the target. For example, <see cref="Projectile.scale"/> for <see cref="PokemonPet"/>. Leave <see langword="null"/> to not mess with this.
    /// </summary>
    public float? PreOtherRelative;
    public readonly float ToScale(Rectangle animated, Rectangle target, uint axis, float pre = default, float preOther = default, Vector2 effectSize = default)
    {
        var space = Rectangle.Union(animated, target);
        if (axis == 0)
            return ToScale(animated.Width, target.Width, space.Width, pre, preOther, effectSize.X);
        return ToScale(animated.Height, target.Height, space.Height, pre, preOther, effectSize.Y);
    }
    public readonly float ToScale(float animatedSize, float targetSize, float spaceSize, float pre = default, float preOther = default, float effectSize = default)
    {
        var selfRel = SelfRelative * animatedSize;
        var otherRel = OtherRelative * targetSize;
        var spaceRel = SpaceRelative * spaceSize;
        var preRel = PreSelfRelative.HasValue ? pre * PreSelfRelative.Value : 0f;
        var preOtherRel = PreOtherRelative.HasValue ? preOther * PreOtherRelative.Value : 0f;
        float effectSelfRel;
        float effectOtherRel;
        if (effectSize != 0f)
        {
            effectSelfRel = animatedSize / effectSize * EffectSelfRelative;
            effectOtherRel = targetSize / effectSize * EffectOtherRelative;
        }
        else
            effectSelfRel = effectOtherRel = 0f;
        return selfRel + otherRel + spaceRel + preRel + preOtherRel + effectSelfRel + effectOtherRel + AbsoluteScale;
    }
}

public enum EffectDirection
{
    Keep,
    Flip,
    ToTarget,
    ToSelf,
}

public struct EffectTime
{
    /// <summary>
    ///     Percentage of the length of the animation
    /// </summary>
    public float AnimRelative;
    public int Frames;
    public int Milliseconds;
    public readonly int ToFrames(int animMaxFrames)
    {
        var animFactor = animMaxFrames * AnimRelative;
        var frames = (int)animFactor;
        var milliseconds = (animFactor - frames) * (1f / 60f * 1000f) + Milliseconds;
        return frames + Frames + (int)(60f * milliseconds / 1000f);
    }
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
        var direction = animatedCenterX > targetCenterX ? 1 : 0;
        if (dir is EffectDirection.ToTarget)
            direction ^= 1;
        return (SpriteEffects)direction;
    }
}
