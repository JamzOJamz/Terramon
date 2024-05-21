using System.Collections.Generic;
using Terraria.GameContent.Creative;
using Terraria.Localization;

namespace Terramon.Content.Items.PokeBalls;

public abstract class BasePkballMiniItem : TerramonItem
{
    public override ItemLoadPriority LoadPriority => ItemLoadPriority.PokeBallMinis;

    protected override bool Obtainable => false;

    public override string Texture => "Terramon/Assets/Items/PokeBalls/" + GetType().Name;

    public override void SetStaticDefaults()
    {
        CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
    }

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.width = 16;
        Item.height = 16;
        Item.value = 0;
    }
    
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        base.ModifyTooltips(tooltips);
        tooltips.Insert(tooltips.FindIndex(t => t.Name == "Tooltip0"),
            new TooltipLine(Mod, "Vitamin", Language.GetTextValue("Mods.Terramon.CommonTooltips.PokeBallMinis")));
    }
    
}