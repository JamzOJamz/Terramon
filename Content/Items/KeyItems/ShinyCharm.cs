using Microsoft.Xna.Framework;
using Terramon.Content.Rarities;

namespace Terramon.Content.Items.KeyItems;

public class ShinyCharm : KeyItem
{
    protected override int UseRarity => ModContent.RarityType<ShinyCharmRarity>();

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.width = 24;
        Item.height = 30;
    }
}

public class ShinyCharmRarity : DiscoRarity
{
    protected override Color[] Colors { get; } = {
        new(122, 255, 255),
        new(255, 86, 232),
        new(255, 192, 35)
    };

    protected override float Time => 2f;
}