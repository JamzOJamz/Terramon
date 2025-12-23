namespace Terramon.Content.Items.KeyItems;

public sealed class GoodRod : FishingRod
{
    public override void SetDefaults()
    {
        base.SetDefaults();
        CatchTerramonChance = 0.75f;
        Item.fishingPole = 30;
        Item.shootSpeed = 13f;
    }
    public override void ModifyFishingLine(Projectile bobber, ref Vector2 lineOriginOffset, ref Color lineColor)
    {
        base.ModifyFishingLine(bobber, ref lineOriginOffset, ref lineColor);
        lineOriginOffset.X += 38f;
        lineOriginOffset.Y -= 32f;
    }
}
