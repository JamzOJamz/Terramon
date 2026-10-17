using ReLogic.Content;
using Terraria.GameContent;

namespace Terramon.Core.Systems.MoveAnimation;

public sealed class MegaPunch : BaseAnimation
{
    public static readonly Asset<Texture2D> fist = ModContent.Request<Texture2D>("Terramon/Assets/Animations/MegaPunch");
    public override void PlayAt(float currentTime, float previousTime, IAnimator animator)
    {
        var ampl = 4f;
        var pos = new EffectPosition
        {
            Anchor = 1f,
            XOffsetAbsolute = currentTime < 0.5f ? 0f : Main.rand.NextFloat(-ampl, ampl),
            YOffsetAbsolute = currentTime < 0.5f ? 0f : Main.rand.NextFloat(-ampl, ampl),
        };
        var scale = new EffectScale
        { 
            AbsoluteScale = currentTime >= 0.5f ? 1f : 5f - currentTime * 8f,
        };
        var factor = 47f;
        if (currentTime >= 0.5f)
        {
            var sin = MathF.Sin((currentTime - 0.5f) * MathF.Tau);
            for (int i = 0; i < 32; i++)
            {
                factor = ((1244f + i + (float)Main.timeForVisualEffects) * factor + 9999f) % 100f;
                animator.ShowTexture(TextureAssets.Extra[ExtrasID.SharpTears].Value, new EffectPosition { Anchor = 1f }, Color.Lerp(Color.Red, Color.Yellow, i / 16f) with { A = 0 }, new EffectRotation
                {
                    AbsoluteRotation = factor,
                }, 0,
                new EffectScale { AbsoluteScale = sin * 4f },
                new EffectScale { AbsoluteScale = 0.3f});
            }
        }
        animator.ShowTexture(fist.Value, pos, Color.White * (currentTime * 4f), default, 0, scale);
    }
}
