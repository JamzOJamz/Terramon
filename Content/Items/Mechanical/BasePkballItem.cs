using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Creative;
using Terraria.ID;

namespace Terramon.Content.Items.Mechanical;

public abstract class BasePkballItem : TerramonItem
{
    protected virtual int pokeballThrow => ModContent.ProjectileType<BasePkballProjectile>();
    protected virtual int pokeballTile => ModContent.TileType<BasePkballTile>();
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
        Item.UseSound = new SoundStyle("Terramon/Assets/Audio/Sounds/pkball_throw");
        Item.width = 32;
        Item.height = 32;
        Item.maxStack = 99;
        Item.damage = 0;
        Item.autoReuse = false;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.value = igPrice * 3;
        Item.useTime = 15;
        Item.consumable = true;
    }

    public override bool CanUseItem(Player player) => (player.altFunctionUse != 2); //Don't execute this code if the alternate function is being executed

    public override bool AltFunctionUse(Player player)
    {
        base.AltFunctionUse(player);
        int mouseTileX = (int)(Main.mouseX + Main.screenPosition.X) / 16;
        int mouseTileY = (int)(Main.mouseY + Main.screenPosition.Y) / 16;
        mouseTileX = Math.Clamp(mouseTileX, 0, Main.maxTilesX);
        mouseTileY = Math.Clamp(mouseTileY, 0, Main.maxTilesY);

        if (!Main.tile[mouseTileX, mouseTileY].HasTile && Vector2.Distance(player.position, new Vector2(mouseTileX, mouseTileY) * 16) < 96)
        {
            WorldGen.PlaceTile(mouseTileX, mouseTileY, pokeballTile);
            TileEntity.PlaceEntityNet(mouseTileX, mouseTileY, ModContent.TileEntityType<BasePkballEntity>());
            player.ConsumeItem(Type);
        }
        //Item.shoot = 0;
        //Item.createTile = pokeballTile;

        return false;
    }

    public override bool? UseItem(Player player)
    {
        Main.NewText($"{player.altFunctionUse}, {Item.createTile}");
        return base.UseItem(player);
    }

    /*public override bool? UseItem(Player player) //Manage what happens when the player uses the item
    {
        Item.consumable = true;
        SoundEngine.PlaySound(new SoundStyle("TerramonMod/Sounds/pkball_throw"), player.position);
        return true;
    }*/
}