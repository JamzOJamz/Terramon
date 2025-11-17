using Terramon.Content.Items;
using Terraria.Localization;

namespace Terramon.Content.Tiles.Interactive;

public abstract class PCItem : TerramonItem
{
    public override string Texture => "Terramon/Assets/Tiles/Interactive/" + GetType().Name;

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.width = 22;
        Item.height = 32;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        base.ModifyTooltips(tooltips);
        tooltips.Insert(tooltips.FindIndex(t => t.Name == "Tooltip0"),
            new TooltipLine(Mod, "Vitamin", Language.GetTextValue("Mods.Terramon.CommonTooltips.PCItems")));
    }
}