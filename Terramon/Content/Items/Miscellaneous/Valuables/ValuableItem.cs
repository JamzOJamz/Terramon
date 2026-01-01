using Terramon.Helpers;
using Terraria.Localization;

namespace Terramon.Content.Items;

public abstract class ValuableItem(ushort pokeDollars) : TerramonItem
{
    private const int HighestValue = 30000;
    public static AliasRandom Pool { get; } = new();
    public override string Texture => "Terramon/Assets/Items/Miscellaneous/Valuables/" + GetType().Name;

    public override void SetStaticDefaults()
    {
        Item.ResearchUnlockCount = 3;
        TerramonItemAPI.Sets.HeldItem.Add(Type);
        // this will make relic gold's probability 0 which is bad
        var factor = 1d - pokeDollars / (double)HighestValue;
        // so remap it
        factor = factor * 0.95d + 0.05d;
        Pool.Add(Type, factor);
    }

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.value = pokeDollars * 2;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        base.ModifyTooltips(tooltips);
        tooltips.Insert(tooltips.FindIndex(t => t.Name == "Tooltip0"),
            new TooltipLine(Mod, "ValuableItem", Language.GetTextValue("Mods.Terramon.CommonTooltips.ValuableItem")));
    }
}