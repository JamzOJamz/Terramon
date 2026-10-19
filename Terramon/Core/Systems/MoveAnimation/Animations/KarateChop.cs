using ReLogic.Content;
using System.Diagnostics;
using Terraria.Audio;

namespace Terramon.Core.Systems.MoveAnimation;

public sealed class KarateChop : BaseAnimation
{
    public static readonly SoundStyle FightHit = new("Terramon/Sounds/AnimSFX/fight_hit");
    public static readonly Asset<Texture2D> karateHand = ModContent.Request<Texture2D>("Terramon/Assets/Animations/KarateHand");
    public override void PlayAt(float currentTime, float previousTime, IAnimator animator)
    {
        var t = karateHand.Value;
        for (var i = MathF.Max(0f, previousTime - 0.05f); i < currentTime; i += 0.005f)
            animator.QueueDraw(t, Color.White with { A = 0 } * (i * 0.5f), PositionAt(i), null, ScaleAt(i), RotationAt(i), EffectDirection.ToTarget);
        animator.QueueDraw(t, Color.White, PositionAt(currentTime), null, ScaleAt(currentTime), RotationAt(currentTime), EffectDirection.ToTarget);
    }
    public override void Oneshots(bool start, IAnimator animator)
    {
        if (start)
            animator.QueueSound(FightHit, EffectPosition.Other, new EffectTime { AnimRelative = 0.65f });
    }
    private static EffectPosition PositionAt(float t)
    {
        var pos = Tween.CheckBSpline(t * t * t,
            new Vector2(0f, 0f),
            new Vector2(0.28f, -4.38f),
            new Vector2(-1.16f, -3.66f),
            new Vector2(1.04f, -2.5f),
            new Vector2(1f, 0f),
            new Vector2(1f, 0.42f),
            new Vector2(1f, -0.29f),
            new Vector2(1f, 0.08f),
            new Vector2(1f, -0.03f),
            new Vector2(1f, 0.01f),
            new Vector2(1f, 0f));

        return new EffectPosition
        {
            Anchor = pos.X,
            YOffsetSelfRelative = pos.Y,
            YOffsetOtherRelative = pos.Y,
        };
    }
    private static EffectRotation RotationAt(float t)
    {
        return new EffectRotation
        {
            RotationToTarget = Tween.ApplyEasing(Ease.InOutBack, t, new EaseParams { BackConstant = 2 }) - 1f
        };
    }
    private static EffectScale ScaleAt(float t)
    {
        return t switch
        {
            <= 0.25f => new EffectScale { EffectSelfRelative = t * 4f },
            <= 0.5f => new EffectScale { EffectSelfRelative = 1f - ((t - 0.25f) * 4f), EffectOtherRelative = (t - 0.25f) * 4f },
            <= 0.75f => new EffectScale { EffectOtherRelative = 1f },
            _ => new EffectScale { EffectOtherRelative = 1f - ((t - 0.75f) * 4f) },
        };
    }
}
