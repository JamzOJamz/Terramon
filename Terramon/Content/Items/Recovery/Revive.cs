using Terramon.Helpers;

namespace Terramon.Content.Items;

public class Revive : BaseReviveItem
{
    protected override float RestorationPercentage => 0.5f;

    protected override int UseRarity => ModContent.RarityType<ReviveRarity>();

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.width = 20;
        Item.height = 30;
    }
}

public class ReviveRarity : ModRarity
{
    public override Color RarityColor { get; } = ColorUtils.FromHexRGB(0xFFCA51);
}