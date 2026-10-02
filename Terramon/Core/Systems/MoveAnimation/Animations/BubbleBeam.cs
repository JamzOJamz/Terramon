using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.GameContent;

namespace Terramon.Core.Systems.MoveAnimation.Animations;

public sealed class BubbleBeam : BaseAnimation
{
    public override void PlayAt(float currentTime, float previousTime, IAnimator animator)
    {
        var sinFactor = (1f - currentTime) * MathF.Tau * 8f;
        var (sin, cos) = Math.SinCos(sinFactor);
        Main.instance.LoadProjectile(ProjectileID.Bubble);
        var bub = TextureAssets.Projectile[ProjectileID.Bubble].Value;

        var test = currentTime <= 0.25f ? currentTime * 4f : 1f;
        var bubel = (int)(16f * test);
        var gogogo = currentTime >= 0.75f ? (currentTime - 0.75f) * 4f : 0f;
        var bubelGo = (int)(16f * gogogo);
        var prevGo = (int)(16f * (previousTime >= 0.75f ? (previousTime - 0.75f) * 4f : 0f));
        Bubbles(animator, bub, sinFactor, prevGo, bubelGo, bubel, true);
        Main.spriteBatch.FlushBatch();

        animator.DrawTrail(TextureAssets.MagicPixel.Value, new EffectPosition { Anchor = gogogo }, new EffectPosition { Anchor = test }, new EffectScale { AbsoluteScale = 4f }, Color.LightCyan, 2, 1);
        Bubbles(animator, bub, sinFactor, prevGo, bubelGo, bubel, false);

        animator.QueuePositionalAction(static p =>
        {
            for (int i = 0; i < 2; i++)
            {
                var d = Dust.NewDustPerfect(p, DustID.BubbleBlock, Main.rand.NextVector2Circular(2f, 8f));
                d.noGravity = true;
            }
        }, new EffectPosition { Anchor = test });
    }
    private static void Bubbles(IAnimator animator, Texture2D tex, float sinFactor, int prevStart, int start, int count, bool leftCos)
    {
        for (int i = prevStart; i < count; i++)
        {
            var prog = i / 16f;
            var (sin, cos) = Math.SinCos(sinFactor + i * MathHelper.PiOver2);
            if (leftCos && cos > 0f)
                continue;
            if (!leftCos && cos < 0f)
                continue;
            var intens = 0.25f + prog;
            var pos = new EffectPosition { Anchor = prog, XOffsetDirectional = (float)cos * 10f * intens, YOffsetAbsolute = (float)sin * 10f * intens };
            if (i < start)
            {
                animator.PlaySound(SoundID.Item54, pos);
                animator.QueuePositionalAction(static p =>
                {
                    for (int j = 0; j < 8; j++)
                    {
                        var d = Dust.NewDustPerfect(p, DustID.BubbleBlock, (MathF.Tau / 8f * j).ToRotationVector2());
                        d.noGravity = true;
                    }
                }, pos);
            }
            else
                animator.ShowTexture(tex, pos, Color.White, default, 0, new EffectScale { AbsoluteScale = intens });
        }
    }
}
