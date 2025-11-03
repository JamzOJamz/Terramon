using Terramon.Core.Loaders;
using Terraria.Localization;

namespace Terramon.Content.Items;

[LoadGroup("HeldItems")]
public abstract class HeldItem : TerramonItem
{
    public override string Texture => "Terramon/Assets/Items/HeldItems/" + GetType().Name;

    public override void SetStaticDefaults()
    {
        Item.ResearchUnlockCount = 1;
    }

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.maxStack = 1;
        Item.value = 0;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        base.ModifyTooltips(tooltips);
        tooltips.Insert(tooltips.FindIndex(t => t.Name == "Tooltip0"),
            new TooltipLine(Mod, "HeldItem", Language.GetTextValue("Mods.Terramon.CommonTooltips.HeldItem")));
    }
}