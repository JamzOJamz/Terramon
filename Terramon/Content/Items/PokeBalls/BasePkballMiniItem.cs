using Terraria.Localization;

namespace Terramon.Content.Items.PokeBalls;

public abstract class BasePkballMiniItem : TerramonItem
{

    public override string Texture => "Terramon/Assets/Items/PokeBalls/" + GetType().Name;

    public override void SetStaticDefaults()
    {
        TerramonItemAPI.Sets.Unobtainable.Add(Type);
        Item.ResearchUnlockCount = 1;
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