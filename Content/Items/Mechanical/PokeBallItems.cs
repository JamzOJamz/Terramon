using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent;

namespace Terramon.Content.Items.Mechanical;

public abstract class PokeBallItem : TerramonItem
{
    public override string Texture => "Terramon/Assets/Items/PokeBalls/" + GetType().Name;

    protected virtual float CatchRate => 1f;

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.width = 32;
        Item.height = 32;
        Item.value = 1;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        var catchRateText = CatchRate < float.MaxValue ? $"{CatchRate}x catch rate" : "100% catch rate";
        tooltips.Add(new TooltipLine(Mod, nameof(CatchRate), catchRateText)
        {
            OverrideColor = new Color(173, 173, 198)
        });
    }

    public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor,
        Color itemColor,
        Vector2 origin, float scale)
    {
        spriteBatch.Draw(TextureAssets.Item[Item.type].Value, position, null, drawColor, 0f, origin, scale * 0.9f,
            SpriteEffects.None, 0f);
        return false;
    }
}

public class RegularBallItem : PokeBallItem
{
    protected override int UseRarity => ModContent.RarityType<RegularBallRarity>();
}

public class RegularBallRarity : ModRarity
{
    public override Color RarityColor => new(214, 74, 86);
}

public class GreatBallItem : PokeBallItem
{
    protected override int UseRarity => ModContent.RarityType<GreatBallRarity>();
    protected override float CatchRate => 1.5f;
}

public class GreatBallRarity : ModRarity
{
    public override Color RarityColor => new(47, 155, 224);
}

public class UltraBallItem : PokeBallItem
{
    protected override int UseRarity => ModContent.RarityType<UltraBallRarity>();
    protected override float CatchRate => 2f;
}

public class UltraBallRarity : ModRarity
{
    public override Color RarityColor => new(249, 163, 27);
}

public class MasterBallItem : PokeBallItem
{
    protected override int UseRarity => ModContent.RarityType<MasterBallRarity>();
    protected override float CatchRate => float.MaxValue;
}

public class MasterBallRarity : ModRarity
{
    public override Color RarityColor => new(164, 96, 178);
}