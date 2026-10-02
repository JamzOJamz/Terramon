using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.GameContent;

namespace Terramon.Core.Systems.MoveAnimation.Animations;

public sealed class WaterGun : BaseAnimation
{
    public override void PlayAt(float currentTime, float previousTime, IAnimator animator)
    {
        var lookUpPokemonInflationToFindOutMore = currentTime switch
        {
            <= 0.2f => currentTime * 2.5f,
            <= 0.5f => 0.5f,
            _ => 0.5f - (currentTime - 0.5f),
        };
        animator.ModifyDraw(scaleX: new EffectScale { PreSelfRelative = 1f + lookUpPokemonInflationToFindOutMore });
        if (currentTime >= 0.5f)
        {
            var absWidth = 8f;
            if (currentTime > 0.6f && currentTime < 0.9f)
                absWidth += MathF.Sin((float)Main.timeForVisualEffects) * 2f;
            if (currentTime <= 0.6f)
                absWidth *= (currentTime - 0.5f) * 10f;
            if (currentTime >= 0.9f)
                absWidth *= 1f - (currentTime - 0.9f) * 10f;
            var beamWidth = new EffectScale
            {
                AbsoluteScale = absWidth
            };
            animator.DrawTrail(TextureAssets.MagicPixel.Value, new EffectPosition { XOffsetSelfRelativeDirectional = 0.5f }, new EffectPosition { Anchor = 1f }, beamWidth, Color.Blue, 7, 1);
            animator.QueuePositionalAction((p) =>
            {
                for (int i = 0; i < 2; i++)
                {
                    var d = Dust.NewDustPerfect(p + Main.rand.NextVector2Circular(4f, 32f), DustID.Water + i, Scale: 2f);
                    d.noGravity = true;
                }
            }, new EffectPosition { Anchor = 1f });
        }
    }
}
