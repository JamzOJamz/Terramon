using Terraria.Localization;

namespace Terramon.Content.Items.Valuables;

public abstract class ValuableItem(int pokeDollars) : TerramonItem
{
    public override string Texture => "Terramon/Assets/Items/Valuables/" + GetType().Name;

    public override void SetStaticDefaults()
    {
        Item.ResearchUnlockCount = 50;
        TerramonItemAPI.Sets.HeldItem.Add(Type);
    }

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.maxStack = 999;
        Item.value = pokeDollars * 2;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        base.ModifyTooltips(tooltips);
        tooltips.Insert(tooltips.FindIndex(t => t.Name == "Tooltip0"),
            new TooltipLine(Mod, "ValuableItem", Language.GetTextValue("Mods.Terramon.CommonTooltips.ValuableItem")));
    }
}
