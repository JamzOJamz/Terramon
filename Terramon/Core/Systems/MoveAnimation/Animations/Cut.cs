using Terraria.GameContent;

namespace Terramon.Core.Systems.MoveAnimation;

public sealed class Cut : BaseAnimation
{
    public override void PlayAt(float currentTime, float previousTime, IAnimator animator)
    {
        var sin = MathF.Sin(currentTime * MathF.PI);
        var dirTime = currentTime - 0.5f;
        var backDirTime = -dirTime + 0.5f;
        var cutMiddle = new EffectPosition { Anchor = 1f, XOffsetOtherRelativeDirectional = dirTime, YOffsetOtherRelative = dirTime, XOffsetDirectional = dirTime * 32f, YOffsetAbsolute = dirTime * 32f };
        var cutStart = cutMiddle;
        var cutEnd = cutMiddle;
        cutStart.XOffsetOtherRelativeDirectional -= sin;
        cutStart.YOffsetOtherRelative -= sin;
        cutEnd.XOffsetOtherRelativeDirectional += sin;
        cutEnd.YOffsetOtherRelative += sin;
        animator.DrawTrail(TextureAssets.MagicPixel.Value, cutStart, cutEnd, new EffectScale { AbsoluteScale = 4f }, Color.Yellow, 3, 2, 1);
        animator.DrawTrail(TextureAssets.MagicPixel.Value, cutStart, cutEnd, new EffectScale { AbsoluteScale = 2f }, Color.White, 3, 2, 1);
    }
}
