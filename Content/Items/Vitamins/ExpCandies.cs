using Terramon.Helpers;
using Terraria.ID;

namespace Terramon.Content.Items.Vitamins;

public abstract class ExpCandy : Vitamin
{
    // To be made obtainable in a future update post-0.1 beta
    public override bool Obtainable => false;

    protected override int UseRarity { get; } = ModContent.RarityType<ExpCandyRarity>();

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.UseSound = SoundID.Item4;
    }
}

public class ExpCandyXS : ExpCandy
{
    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.width = 22;
        Item.height = 18;
    }
}

public class ExpCandyS : ExpCandy
{
    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.width = 26;
        Item.height = 22;
    }
}

public class ExpCandyM : ExpCandy
{
    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.width = 24;
        Item.height = 22;
    }
}

public class ExpCandyL : ExpCandy
{
    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.width = 22;
        Item.height = 28;
    }
}

public class ExpCandyXL : ExpCandy
{
    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.width = 26;
        Item.height = 34;
    }
}

public class ExpCandyRarity : ModRarity
{
    public override Color RarityColor { get; } = ColorUtils.FromHex(0x76C2F2);
}
