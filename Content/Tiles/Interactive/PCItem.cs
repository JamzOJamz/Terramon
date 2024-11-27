using Terramon.Content.Items;
using Terramon.Core.Loaders;
using Terraria.Localization;

namespace Terramon.Content.Tiles.Interactive;

[LoadGroup("Interactive")]
public abstract class PCItem : TerramonItem
{
    public override string Texture => "Terramon/Assets/Tiles/Interactive/" + GetType().Name;

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.width = 20;
        Item.height = 30;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        base.ModifyTooltips(tooltips);
        tooltips.Insert(tooltips.FindIndex(t => t.Name == "Tooltip0"),
            new TooltipLine(Mod, "Vitamin", Language.GetTextValue("Mods.Terramon.CommonTooltips.PCItems")));
    }
}