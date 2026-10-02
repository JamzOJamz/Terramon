using Terraria.GameContent;

namespace Terramon.Core.Systems.MoveAnimation;

public sealed class PayDay : BaseAnimation
{
    public override void PlayAt(float currentTime, float previousTime, IAnimator animator)
    {
        var tex = TextureAssets.Coin[2].Value;
        var pos = new EffectPosition { Anchor = currentTime };
        animator.ShowTexture(tex, pos, Color.White, new EffectRotation { RotationToTarget = MathHelper.PiOver2 }, 0, new EffectScale { AbsoluteScale = 1f }, frame: tex.Frame(verticalFrames: 8, frameY: (int)(currentTime * 4f % 1f * 7f)));
    }
    public override void Oneshots(bool start, IAnimator animator)
    {
        if (start)
            animator.PlaySound(SoundID.CoinPickup, default(EffectPosition));
        else
        {
            animator.QueuePositionalAction(p =>
            {
                for (int i = 0; i < 24; i++)
                {
                    var g = Gore.NewGorePerfect(p, (-Vector2.UnitY * (Main.rand.NextFloat() + 1f) * 5f).RotatedByRandom(2.0), GoreID.ShadowMimicCoins);
                }
            }, new EffectPosition { Anchor = 1f });
        }
    }
}
