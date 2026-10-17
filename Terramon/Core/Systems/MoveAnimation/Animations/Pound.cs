namespace Terramon.Core.Systems.MoveAnimation.Animations;

public sealed class Pound : BaseAnimation
{
    public override void PlayAt(float currentTime, float previousTime, IAnimator animator)
    {
        var pos = default(EffectPosition);
        if (currentTime <= 0.5f)
        {
            pos.XOffsetDirectional = -32f * currentTime;
        }
        else
        {
            pos.XOffsetDirectional = float.Lerp(-16f, 48f, (currentTime - 0.5f) * 2f);
        }
        animator.ModifyDraw(pos);
    }
}
