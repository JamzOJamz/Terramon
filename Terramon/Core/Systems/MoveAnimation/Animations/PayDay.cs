using Terraria.GameContent;

namespace Terramon.Core.Systems.MoveAnimation;

public sealed class PayDay : BaseAnimation
{
    public override void PlayAt(float currentTime, float previousTime, IAnimator animator)
    {
        var tex = TextureAssets.Coin[2].Value;
        var pos = new EffectPosition { Anchor = currentTime };
        animator.QueueDraw(
            tex, Color.White, pos, new EffectScale { AbsoluteScale = 1f }, null,
            new EffectRotation { RotationToTarget = MathHelper.PiOver2 }, frame: tex.Frame(verticalFrames: 8, frameY: (int)(currentTime * 4f % 1f * 7f)));
    }
    public override void Oneshots(bool start, IAnimator animator)
    {
        if (start)
            animator.QueueSound(SoundID.CoinPickup, EffectPosition.Self);
        else
        {
            animator.QueueContextfulAction(wCtx =>
            {
                for (var i = 0; i < 24; i++)
                    Gore.NewGorePerfect(wCtx.Positions[0], (-Vector2.UnitY * (Main.rand.NextFloat() + 1f) * 5f).RotatedByRandom(2.0), GoreID.ShadowMimicCoins);
            }, new EffectContext(positions: [EffectPosition.Other]));
        }
    }
}
