using ReLogic.Content;
using Terraria.Audio;

namespace Terramon.Core.Systems.MoveAnimation;

public sealed class Guillotine : BaseAnimation
{
    public static readonly SoundStyle Clank = new("Terramon/Sounds/AnimSFX/clank");
    public static readonly Asset<Texture2D> guillotines = ModContent.Request<Texture2D>("Terramon/Assets/Animations/Guillotine");
    public override void PlayAt(float currentTime, float previousTime, IAnimator animator)
    {
        ReadOnlySpan<Vector2> rightCurve =
        [
            new Vector2(0f, 0f),
            new Vector2(0.15f, 0.78f),
            new Vector2(1.06f, 2.03f),
            new Vector2(2.34f, 2.04f),
            new Vector2(1.63f, 0.69f),
            new Vector2(1f, 0f)
        ];
        ReadOnlySpan<Vector2> leftCurve =
        [
            new Vector2(0f, 0f),
            new Vector2(-0.52f, -0.28f),
            new Vector2(-0.61f, -0.92f),
            new Vector2(-0.03f, -1.34f),
            new Vector2(0.42f, -0.62f),
            new Vector2(1f, 0f)
        ];
        var right = currentTime >= 0.75f ? Vector2.UnitX : Tween.CheckBSpline(currentTime * (4f / 3f), rightCurve);
        var left = currentTime >= 0.75f ? Vector2.UnitX : Tween.CheckBSpline(currentTime * (4f / 3f), leftCurve);
        var rightPos = new EffectPosition { Anchor = right.X, YOffsetOtherRelative = right.Y, XOffsetDirectional = 24f, YOffsetAbsolute = 16f };
        var leftPos = new EffectPosition { Anchor = left.X, YOffsetOtherRelative = left.Y, XOffsetDirectional = -24f, YOffsetAbsolute = 8f };
        if (currentTime >= 0.75f)
        {
            var ampl = 4f - (currentTime - 0.75f) * 16f;
            var randR = Main.rand.NextVector2Circular(ampl, ampl);
            var randL = Main.rand.NextVector2Circular(ampl, ampl);
            rightPos.XOffsetAbsolute += randR.X;
            rightPos.YOffsetAbsolute += randR.Y;
            leftPos.XOffsetAbsolute += randL.X;
            leftPos.YOffsetAbsolute += randL.Y;
        }
        var rAngle = 0.2f;
        var lAngle = 0.2f;
        if (currentTime > 0.1f && currentTime < 0.8f)
        {
            var smooth = Tween.ApplyEasing(Ease.OutBack, (currentTime - 0.1f) * 1.42857143f);

            rAngle += smooth * MathF.Tau;
        }
        var tex = guillotines.Value;
        var color = Color.White * (currentTime * 8f);
        var xScale = new EffectScale { AbsoluteScale = currentTime <= 0.2f ? currentTime * 5f : 1f };
        animator.ShowTexture(tex, rightPos, color, new EffectRotation { RotationToTargetHorizontal = rAngle }, EffectDirection.ToTarget, xScale, new EffectScale { AbsoluteScale = 1f }, frame: tex.Frame(2, frameX: 1), origin: new Vector2(29f, 89f));
        animator.ShowTexture(tex, leftPos, color, new EffectRotation { RotationToTargetHorizontal = lAngle }, EffectDirection.ToTarget, xScale, new EffectScale { AbsoluteScale = 1f }, frame: tex.Frame(2, frameX: 0), origin: new Vector2(15f, 89f));

    }
    public override void Oneshots(bool start, IAnimator animator)
    {
        if (start)
            animator.QueuePositionalAction(p =>
            {
                SoundEngine.PlaySound(Clank, p);
            }, new EffectPosition { Anchor = 1f }, 0.7f);
    }
}
