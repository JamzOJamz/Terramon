using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent;

namespace Terramon.Content.Items.Mechanical;

public abstract class PokeBallItem : TerramonItem
{
    public override string Texture => "Terramon/Assets/Items/PokeBalls/" + GetType().Name;

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.width = 32;
        Item.height = 32;
        Item.value = 1;
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
    public override Color RarityColor => new(209, 55, 77);
}

public class GreatBallItem : PokeBallItem
{
    protected override int UseRarity => ModContent.RarityType<GreatBallRarity>();
}

public class GreatBallRarity : ModRarity
{
    public override Color RarityColor => new(47, 155, 224);
}

public class UltraBallItem : PokeBallItem
{
    protected override int UseRarity => ModContent.RarityType<UltraBallRarity>();
}

public class UltraBallRarity : ModRarity
{
    public override Color RarityColor => new(249, 163, 27);
}

public class MasterBallItem : PokeBallItem
{
    protected override int UseRarity => ModContent.RarityType<MasterBallRarity>();
}

public class MasterBallRarity : ModRarity
{
    public override Color RarityColor => new(164, 96, 178);
}