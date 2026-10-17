using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.Audio;
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
        animator.ShowTexture(bubble, new EffectPosition
        {
            Anchor = currentTime
        },
        Color.White, default, 0, new EffectScale
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
            animator.PlaySound(SoundID.Drown with { Pitch = 0.5f, PitchVariance = 0.1f }, new EffectPosition { Anchor = 1f });
            return;
        }
        animator.PlaySound(SoundID.Item54, default);
        animator.QueuePositionalAction(static p =>
        {
            for (int i = 0; i < 8; i++)
            {
                var d = Dust.NewDustPerfect(p, DustID.BubbleBurst_Blue, (MathF.Tau / 8f * i).ToRotationVector2() * 2f);
                d.noGravity = true;
            }
        }, new EffectPosition { Anchor = 1f });
    }
}
