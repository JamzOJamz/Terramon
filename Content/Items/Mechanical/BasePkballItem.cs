using Terraria.Audio;
using Terraria.GameContent.Creative;
using Terraria.ID;

namespace Terramon.Content.Items.Mechanical;

public abstract class BasePkballItem : TerramonItem
{
    protected virtual int pokeballThrow => ModContent.ProjectileType<BasePkballProjectile>();
    protected virtual int igPrice => -1; //ingame price (from pokemon games) so price scaling matches

    public override string Texture => "Terramon/Assets/Items/PokeBalls/" + GetType().Name;

    public override void SetStaticDefaults()
    {
        CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 99;
    }

    public override void SetDefaults()
    {
        Item.CloneDefaults(ItemID.Shuriken);
        base.SetDefaults();
        Item.shoot = pokeballThrow;
        Item.shootSpeed = 6.5f;
        Item.UseSound = new SoundStyle("Terramon/Content/Audio/Sounds/pkball_throw");
        Item.width = 32;
        Item.height = 32;
        Item.maxStack = 99;
        Item.damage = 0;
        Item.autoReuse = false;
        Item.useStyle = ItemUseStyleID.Thrust;
        Item.value = igPrice * 3;
        Item.useTime = 15;
        Item.consumable = true;
    }

    /*public override bool? UseItem(Player player) //Manage what happens when the player uses the item
    {
        Item.consumable = true;
        SoundEngine.PlaySound(new SoundStyle("TerramonMod/Sounds/pkball_throw"), player.position);
        return true;
    }*/
}