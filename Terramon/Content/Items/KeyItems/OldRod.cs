namespace Terramon.Content.Items.KeyItems;

public sealed class OldRod : FishingRod
{
    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.fishingPole = 30;
        Item.shootSpeed = 10f;
    }
    public override void ModifyFishingLine(Projectile bobber, ref Vector2 lineOriginOffset, ref Color lineColor)
    {
        base.ModifyFishingLine(bobber, ref lineOriginOffset, ref lineColor);
        lineOriginOffset.X += 30f;
        lineOriginOffset.Y -= 28f;
    }
}
