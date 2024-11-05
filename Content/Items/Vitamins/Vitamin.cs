using System.Collections.Generic;
using Terramon.Core.Loaders;
using Terraria.Localization;

namespace Terramon.Content.Items;

[LoadAfter(typeof(EvolutionaryItem))]
public abstract class Vitamin : TerramonItem
{
    public override string Texture => "Terramon/Assets/Items/Vitamins/" + GetType().Name;

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        base.ModifyTooltips(tooltips);
        tooltips.Insert(tooltips.FindIndex(t => t.Name == "Tooltip0"),
            new TooltipLine(Mod, "Vitamin", Language.GetTextValue("Mods.Terramon.CommonTooltips.Vitamin")));
    }
}