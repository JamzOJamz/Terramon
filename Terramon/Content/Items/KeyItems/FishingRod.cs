using ReLogic.Content;
using Terramon.Content.Items.PokeBalls;
using Terraria.Localization;

namespace Terramon.Content.Items;

public abstract class FishingRod : KeyItem
{
    /// <summary>
    ///     The default chance for any non-Terramon rod to catch a Terramon item or NPC.
    /// </summary>
    public const float DefaultCatchTerramonChance = 0.0625f;

    private Asset<Texture2D> _inventorySprite;

    public float CatchTerramonChance;

    public override void SetStaticDefaults()
    {
        base.SetStaticDefaults();
        _inventorySprite = ModContent.Request<Texture2D>(Texture + "_Inventory");
    }

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.useStyle = ItemUseStyleID.Swing;
        Item.useAnimation = 8;
        Item.useTime = 8;
        Item.UseSound = SoundID.Item1;
        Item.shoot = PokeBallBobber.ProjectileType;
        _inventorySprite = ((FishingRod)ItemLoader.GetItem(Type))._inventorySprite;
    }

    public override void ModifyFishingLine(Projectile bobber, ref Vector2 lineOriginOffset, ref Color lineColor)
    {
        // Vanilla fix: subtract player's gfxOffY
        lineOriginOffset.Y -= Main.player[bobber.owner].gfxOffY;
    }

    public sealed override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame,
        Color drawColor, Color itemColor, Vector2 origin, float scale)
    {
        spriteBatch.Draw(_inventorySprite.Value, position, null, drawColor, 0f, _inventorySprite.Size() * 0.5f, scale,
            0,
            0f);
        return false;
    }
}

public sealed class FishingRodGlobal : GlobalItem
{
    private static LocalizedText TerramonChanceTooltip { get; set; }

    public override void SetStaticDefaults()
    {
        TerramonChanceTooltip = Mod.GetLocalization("CommonTooltips.TerramonFishChance");
    }

    public override bool AppliesToEntity(Item entity, bool lateInstantiation)
        => lateInstantiation && entity.fishingPole > 0;

    public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
    {
        var index = tooltips.FindIndex(l => l.Name is "FishingPower");
        if (index == -1)
            return;
        float fishingChance;
        fishingChance = item.shoot == PokeBallBobber.ProjectileType
            ? ((FishingRod)item.ModItem).CatchTerramonChance
            : FishingRod.DefaultCatchTerramonChance;
        var line = new TooltipLine
        (
            Mod,
            "TerramonFishChance",
            $"[i:{ModContent.ItemType<PokeBallItem>()}] {TerramonChanceTooltip.Format((int)(fishingChance * 100f))}"
        )
        {
            OverrideColor = ModContent.GetInstance<PokeBallRarity>().RarityColor,
        };
        tooltips.Insert(index + 1, line);
    }
}

public sealed class PokeBallBobber : ModProjectile
{
    public static int ProjectileType { get; private set; }
    public override string Texture => "Terramon/Assets/Items/KeyItems/PokeBallBobber";
    public override void SetStaticDefaults() => ProjectileType = Type;
    public override void SetDefaults() => Projectile.CloneDefaults(ProjectileID.BobberWooden);
}