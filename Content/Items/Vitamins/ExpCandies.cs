using Terramon.Core.Systems.PokemonDirectUseSystem;
using Terramon.Helpers;

namespace Terramon.Content.Items;

public abstract class ExpCandy : Vitamin, IPokemonDirectUse
{
    // To be made obtainable in a future update post-0.1 beta
    public override bool Obtainable => false;

    protected override int UseRarity { get; } = ModContent.RarityType<ExpCandyRarity>();
    
    /// <summary>
    ///     The amount of experience points granted by this Exp. Candy.
    /// </summary>
    protected abstract int Points { get; }

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.UseSound = SoundID.Item4;
    }
}

public class ExpCandyXS : ExpCandy
{
    protected override int Points => 100;
    
    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.width = 22;
        Item.height = 18;
    }
}

public class ExpCandyS : ExpCandy
{
    protected override int Points => 800;
    
    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.width = 26;
        Item.height = 22;
    }
}

public class ExpCandyM : ExpCandy
{
    protected override int Points => 3000;
    
    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.width = 24;
        Item.height = 22;
    }
}

public class ExpCandyL : ExpCandy
{
    protected override int Points => 10000;
    
    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.width = 22;
        Item.height = 28;
    }
}

public class ExpCandyXL : ExpCandy
{
    protected override int Points => 30000;
    
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
