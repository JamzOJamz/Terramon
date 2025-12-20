using ReLogic.Content;

namespace Terramon.Content.Items.KeyItems;

public abstract class FishingRod : KeyItem
{
    public Asset<Texture2D> InventorySprite;
    public override void SetStaticDefaults()
    {
        base.SetStaticDefaults();
        InventorySprite = ModContent.Request<Texture2D>(Texture + "_Inventory");
    }
    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.CloneDefaults(ItemID.WoodFishingPole);
        Item.maxStack = 1;
        Item.shoot = PokeBallBobber.ProjectileType;
        InventorySprite = ((FishingRod)ItemLoader.GetItem(Type)).InventorySprite;
    }
    public override void ModifyFishingLine(Projectile bobber, ref Vector2 lineOriginOffset, ref Color lineColor)
    {
        // vanilla fix: subtract player's gfxOffY
        lineOriginOffset.Y -= Main.player[bobber.owner].gfxOffY;
    }
    public sealed override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
    {
        spriteBatch.Draw(InventorySprite.Value, position, null, drawColor, 0f, InventorySprite.Size() * 0.5f, scale, 0, 0f);
        return false;
    }
}

public sealed class PokeBallBobber : ModProjectile
{
    public static int ProjectileType { get; private set; }
    public override string Texture => "Terramon/Assets/Items/KeyItems/PokeBallBobber";
    public override void SetStaticDefaults() => ProjectileType = Type;
    public override void SetDefaults() => Projectile.CloneDefaults(ProjectileID.BobberWooden);
}
