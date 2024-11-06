using Terramon.Content.Rarities;
using Terramon.Core.Loaders;
using Terraria.GameContent.Creative;
using Terraria.Localization;

namespace Terramon.Content.Items;

[LoadGroup("KeyItems")]
public abstract class KeyItem : TerramonItem
{
    public override string Texture => "Terramon/Assets/Items/KeyItems/" + GetType().Name;

    protected override int UseRarity => ModContent.RarityType<KeyItemRarity>();

    public override void SetStaticDefaults()
    {
        CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
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
            new TooltipLine(Mod, "KeyItem", Language.GetTextValue("Mods.Terramon.CommonTooltips.KeyItem")));
    }
}

public class KeyItemRarity : DiscoRarity
{
    protected override Color[] Colors { get; } =
    [
        new Color(122, 255, 255),
        new Color(255, 86, 232),
        new Color(255, 192, 35)
    ];

    protected override float Time => 2f;
}