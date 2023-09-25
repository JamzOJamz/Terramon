using System.Collections.Generic;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.Localization;

namespace Terramon.Content.Items.KeyItems;

public abstract class BaseKeyItem : TerramonItem
{
    public override string Texture => "Terramon/Assets/Items/KeyItems/" + GetType().Name;
    
    public override void SetStaticDefaults()
    {
        ItemID.Sets.IsLavaImmuneRegardlessOfRarity[Type] = true;
        CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
    }
    
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.Insert(tooltips.FindIndex(t => t.Name == "Tooltip0"),
            new TooltipLine(Mod, "KeyItem", Language.GetTextValue("Mods.Terramon.CommonTooltips.KeyItem")));
    }
}