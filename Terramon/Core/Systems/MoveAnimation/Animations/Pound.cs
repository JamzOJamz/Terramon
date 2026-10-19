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
    public override void Oneshots(bool start, IAnimator animator)
    {
        if (start)
            return;
        animator.QueueContextfulAction(wCtx =>
        {
            var hitPosition = wCtx.Positions[0];

            var particleCount = Main.rand.Next(12, 18);

            for (var i = 0; i < particleCount; i++)
            {
                var angle = (float)(i * 2 * Math.PI / particleCount);

                angle += Main.rand.NextFloat(-0.3f, 0.3f);

                var speed = Main.rand.NextFloat(1.5f, 2.5f);
                var velocity = new Vector2(
                    (float)Math.Cos(angle) * speed,
                    (float)Math.Sin(angle) * speed
                );

                var dustColor = Main.rand.Next(6) switch
                {
                    0 => Color.Red,
                    1 => Color.Orange,
                    2 => Color.Yellow,
                    3 => Color.OrangeRed,
                    4 => Color.Crimson,
                    _ => Color.Gold
                };

                var spawnPos = hitPosition + new Vector2(
                    Main.rand.NextFloat(-3, 3),
                    Main.rand.NextFloat(-3, 3)
                );

                var dust = Dust.NewDustDirect(
                    spawnPos,
                    0, 0,
                    DustID.PortalBolt,
                    velocity.X,
                    velocity.Y,
                    100,
                    dustColor,
                    Main.rand.NextFloat(0.8f, 1.3f) // Varied scale for depth
                );

                dust.noGravity = true;
                dust.fadeIn = Main.rand.NextFloat(0.2f, 0.5f);

                if (i % 3 == 0)
                {
                    dust.velocity *= 0.6f;
                    dust.scale *= 1.2f;
                }
            }

            for (var i = 0; i < Main.rand.Next(3, 6); i++)
            {
                var flashDust = Dust.NewDustDirect(
                    hitPosition + new Vector2(Main.rand.NextFloat(-2, 2), Main.rand.NextFloat(-2, 2)),
                    0, 0,
                    DustID.PortalBolt,
                    Main.rand.NextFloat(-0.5f, 0.5f),
                    Main.rand.NextFloat(-0.5f, 0.5f),
                    100,
                    Color.LightYellow,
                    Main.rand.NextFloat(1.0f, 1.5f)
                );

                flashDust.noGravity = true;
                flashDust.fadeIn = 0.1f;
            }

            for (var i = 0; i < Main.rand.Next(4, 8); i++)
            {
                var sparkAngle = Main.rand.NextFloat(0, (float)(2 * Math.PI));
                var sparkSpeed = Main.rand.NextFloat(1f, 2f);

                var sparkVelocity = new Vector2(
                    (float)Math.Cos(sparkAngle) * sparkSpeed,
                    (float)Math.Sin(sparkAngle) * sparkSpeed
                );

                var spark = Dust.NewDustDirect(
                    hitPosition,
                    0, 0,
                    DustID.PortalBolt,
                    sparkVelocity.X,
                    sparkVelocity.Y,
                    100,
                    Color.Orange,
                    Main.rand.NextFloat(0.4f, 0.8f)
                );

                spark.noGravity = false;
                spark.fadeIn = Main.rand.NextFloat(0.4f, 0.8f);
            }
        }, new EffectContext(positions: [EffectPosition.Other]));
    }
}
