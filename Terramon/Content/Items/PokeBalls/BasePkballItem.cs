using Terramon.Content.Commands;
using Terraria.Audio;
using Terraria.Localization;

namespace Terramon.Content.Items.PokeBalls;

public abstract class BasePkballItem : TerramonItem
{
    protected abstract int PokeballThrow { get; }
    protected abstract int PokeballTile { get; }

    /// <summary>
    ///     The in-game price of the Poké Ball in the core series games measured in Pokémon Dollars. Used for price scaling in
    ///     town NPC shops.
    /// </summary>
    protected virtual int InGamePrice => 0;

    public override string Texture => "Terramon/Assets/Items/PokeBalls/" + GetType().Name;

    public override void SetStaticDefaults()
    {
        Item.ResearchUnlockCount = 50;
    }

    public override void SetDefaults()
    {
        Item.CloneDefaults(ItemID.Shuriken);
        base.SetDefaults();
        Item.shoot = PokeballThrow;
        Item.shootSpeed = 6.5f;
        Item.UseSound = new SoundStyle("Terramon/Sounds/pkball_throw") { Volume = 0.8f };
        Item.width = 32;
        Item.height = 32;
        Item.maxStack = 9999;
        Item.damage = 0;
        Item.autoReuse = false;
        Item.useStyle = ItemUseStyleID.Rapier;
        Item.value = InGamePrice * 6;
        Item.useTime = 15;
        Item.consumable = true;
    }

    public override bool CanUseItem(Player player)
    {
        if (player.altFunctionUse == 2)
        {
            Item.shoot = ProjectileID.None;
            Item.createTile = PokeballTile;
            Item.UseSound = null;
        }
        else
        {
            Item.shoot = PokeballThrow;
            Item.createTile = -1;
            Item.UseSound = new SoundStyle("Terramon/Sounds/pkball_throw");
            if (player.GetModPlayer<TerramonPlayer>().HasChosenStarter) return true;
            player.NewText(Language.GetTextValue("Mods.Terramon.Misc.RequireStarter"), TerramonCommand.ChatColorYellow);
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
            var catchRateTooltipLine = new TooltipLine(Mod, "CatchRate", catchRate);
            var journeyResearchIndex = tooltips.FindIndex(t => t.Name == "JourneyResearch");
            if (journeyResearchIndex != -1)
                tooltips.Insert(journeyResearchIndex, catchRateTooltipLine);
            else
                tooltips.Add(catchRateTooltipLine);
        }

        base.ModifyTooltips(tooltips);
    }
}