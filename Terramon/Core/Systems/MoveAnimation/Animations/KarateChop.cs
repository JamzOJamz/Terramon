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
        for (float i = MathF.Max(0f, previousTime - 0.05f); i < currentTime; i += 0.005f)
            animator.ShowTexture(karateHand.Value, PositionAt(i), Color.White with { A = 0 } * (i * 0.5f), RotationAt(i), EffectDirection.ToTarget, scaleY: ScaleAt(i));
        animator.ShowTexture(karateHand.Value, PositionAt(currentTime), Color.White, RotationAt(currentTime), EffectDirection.ToTarget, scaleY: ScaleAt(currentTime));
        // this is not a good permanent solution, since for very fast animations we are not guaranteed that the condition will actually be met
        // might be better to play the sound asynchronously since then the time where the audio is played is under control
        // this applies to other one-shot animation events like spawning dust or gore
        var playSoundAt = 0.65f;
        if (playSoundAt >= previousTime && currentTime > playSoundAt)
            animator.PlaySound(FightHit, new EffectPosition { Anchor = 1f });
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
            <= 0.25f => new EffectScale { SelfRelative = t * 4f },
            <= 0.5f => new EffectScale { SelfRelative = 1f - ((t - 0.25f) * 4f), OtherRelative = (t - 0.25f) * 4f },
            <= 0.75f => new EffectScale { OtherRelative = 1f },
            _ => new EffectScale { OtherRelative = 1f - ((t - 0.75f) * 4f) },
        };
    }
}
