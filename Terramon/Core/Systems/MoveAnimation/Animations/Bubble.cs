using Terraria.GameContent;

namespace Terramon.Core.Systems.MoveAnimation.Animations;

public sealed class Bubble : BaseAnimation
{
    public override void PlayAt(float currentTime, float previousTime, IAnimator animator)
    {
        var bubble = TextureAssets.Bubble.Value;
        var baseScale = new Vector2(1f);
        var v = MathF.Sin(currentTime * MathF.PI + MathHelper.PiOver2) + 1f;
        var n = MathF.Sin(currentTime * MathF.Tau * 4f) * v * 0.15f;
        animator.QueueDraw(bubble, Color.White, new EffectPosition
        {
            Anchor = currentTime
        },
        new EffectScale
        {
            AbsoluteScale = 1f + n
        }, new EffectScale
        {
            AbsoluteScale = 1f - n
        });
    }
    public override void Oneshots(bool start, IAnimator animator)
    {
        if (start)
        {
            animator.QueueSound(SoundID.Drown with { Pitch = 0.5f, PitchVariance = 0.1f }, new EffectPosition { Anchor = 1f });
            return;
        }
        animator.QueueSound(SoundID.Item54, default);
        animator.QueueContextfulAction(wCtx =>
        {
            for (var i = 0; i < 8; i++)
            {
                var d = Dust.NewDustPerfect(wCtx.Positions[0], DustID.BubbleBurst_Blue, (MathF.Tau / 8f * i).ToRotationVector2() * 2f);
                d.noGravity = true;
            }
        }, new EffectContext(positions: [new EffectPosition { Anchor = 1f }]));
    }
}
