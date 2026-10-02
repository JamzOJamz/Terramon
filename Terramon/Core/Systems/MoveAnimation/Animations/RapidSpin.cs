namespace Terramon.Core.Systems.MoveAnimation.Animations;

public sealed class RapidSpin : BaseAnimation
{
    public override void PlayAt(float currentTime, float previousTime, IAnimator animator)
    {
        var rot = default(EffectRotation);
        var pos = default(EffectPosition);
        if (currentTime <= 0.75f)
        {
            var a = Tween.ApplyEasing(Ease.InBack, currentTime * (4f / 3f));
            rot.RotationToTargetHorizontal = MathF.Min(a, 0.2f);
            pos.Anchor = a;
        }
        else
        {
            var t = (currentTime - 0.75f) * 4f;
            pos.Anchor = Tween.ApplyEasing(Ease.OutSine, 1f - t);
            pos.YOffsetSelfRelative = -(1f - (t <= 0.25f ? (1f - Tween.ApplyEasing(Ease.OutQuad, t * 4f)) : Tween.ApplyEasing(Ease.OutBounce, (t - 0.25f) * (4 / 3f))));
        }
        var sinFactor = currentTime * MathF.Tau * 8f * currentTime;
        var cos = MathF.Cos(sinFactor);
        var dir = cos < 0f ? EffectDirection.ToSelf : EffectDirection.ToTarget;
        animator.ModifyDraw(pos, rot, new EffectScale { AbsoluteScale = MathF.Abs(cos) }, new EffectScale { PreSelfRelative = 1f }, dir);
    }
}
