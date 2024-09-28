using System.Collections.Generic;
using Terraria.Localization;

namespace Terramon.Content.Items.Vitamins;

public abstract class Vitamin : TerramonItem
{
    public override ItemLoadPriority LoadPriority => ItemLoadPriority.Vitamins;

    public override string Texture => "Terramon/Assets/Items/Vitamins/" + GetType().Name;

    protected override bool HasPokemonDirectUse => true;

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        base.ModifyTooltips(tooltips);
        tooltips.Insert(tooltips.FindIndex(t => t.Name == "Tooltip0"),
            new TooltipLine(Mod, "Vitamin", Language.GetTextValue("Mods.Terramon.CommonTooltips.Vitamin")));
    }
}