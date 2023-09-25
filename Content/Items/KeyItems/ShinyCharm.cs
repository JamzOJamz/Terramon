using Microsoft.Xna.Framework;
using Terraria;

namespace Terramon.Content.Items.KeyItems;

public class ShinyCharm : BaseKeyItem
{
    protected override int UseRarity => ModContent.RarityType<ShinyCharmRarity>();

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.width = 24;
        Item.height = 30;
        Item.value = 1;
    }
}

public class ShinyCharmRarity : ModRarity
{
    private static readonly Color[] Colors =
    {
        new(122, 255, 255),
        new(255, 86, 232),
        new(255, 192, 35)
    };

    public override Color RarityColor => CalculateRarityColor();

    private static Color CalculateRarityColor()
    {
        var progress = (float)Main.timeForVisualEffects / 120f;
        return Color.Lerp(Colors[(int)progress % Colors.Length], Colors[((int)progress + 1) % Colors.Length],
            progress % 1f);
    }
}