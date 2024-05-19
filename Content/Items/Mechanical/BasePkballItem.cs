using System.Collections.Generic;
using Terramon.Core.Helpers;
using Terraria.Audio;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.Localization;

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
        Item.UseSound = new SoundStyle("Terramon/Sounds/pkball_throw");
        Item.width = 32;
        Item.height = 32;
        Item.maxStack = 9999;
        Item.damage = 0;
        Item.autoReuse = false;
        Item.useStyle = ItemUseStyleID.Rapier;
        Item.value = igPrice * 3;
        Item.useTime = 15;
        Item.consumable = true;
    }

    public override bool CanUseItem(Player player)
    {
        if (player.altFunctionUse == 2)
        {
            Item.shoot = ProjectileID.None;
            Item.createTile = pokeballTile;
            Item.UseSound = null;
        }
        else
        {
            Item.shoot = pokeballThrow;
            Item.createTile = -1;
            Item.UseSound = new SoundStyle("Terramon/Sounds/pkball_throw");
            if (player.GetModPlayer<TerramonPlayer>().HasChosenStarter) return true;
            Main.NewText(Language.GetTextValue("Mods.Terramon.Misc.RequireStarter"), new Color(173, 173, 198));
            return false;
        }

        return true;
    }

    public override bool AltFunctionUse(Player player)
    {
        return true;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.Insert(tooltips.FindIndex(t => t.Name == "Tooltip0"),
            new TooltipLine(Mod, "RightClickPlace",
                Language.GetTextValue("Mods.Terramon.CommonTooltips.RightClickPlace")));
        if (Main.npcShop == 0)
        {
            var catchRate = $"[c/ADADC6:{Language.GetTextValue($"Mods.Terramon.Items.{GetType().Name}.CatchRate")}]";
            tooltips.Add(new TooltipLine(Mod, "CatchRate", catchRate));
        }
        base.ModifyTooltips(tooltips);
    }
}