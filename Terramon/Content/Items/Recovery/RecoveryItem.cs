using Terramon.Core.Systems.PokemonDirectUseSystem;
using Terraria.Localization;

namespace Terramon.Content.Items;

public abstract class RecoveryItem : TerramonItem, IPokemonDirectUse
{
    public virtual bool AffectedByPokemonDirectUse(PokemonData data)
    {
        return true;
    }
    
    public virtual int PokemonDirectUse(Player player, PokemonData data, int amount = 1)
    {
        return amount;
    }
    
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        base.ModifyTooltips(tooltips);
        tooltips.Insert(tooltips.FindIndex(t => t.Name == "Tooltip0"),
            new TooltipLine(Mod, "Recovery", Language.GetTextValue("Mods.Terramon.CommonTooltips.RecoveryItem")));
    }
}