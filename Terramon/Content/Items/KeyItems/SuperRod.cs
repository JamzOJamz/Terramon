namespace Terramon.Content.Items.KeyItems;

public sealed class SuperRod : FishingRod
{
    public override void SetDefaults()
    {
        base.SetDefaults();
        CatchTerramonChance = 1f;
        Item.fishingPole = 45;
        Item.shootSpeed = 16f;
    }
    public override void ModifyFishingLine(Projectile bobber, ref Vector2 lineOriginOffset, ref Color lineColor)
    {
        base.ModifyFishingLine(bobber, ref lineOriginOffset, ref lineColor);
        lineOriginOffset.X += 40f;
        lineOriginOffset.Y -= 34f;
    }
}
