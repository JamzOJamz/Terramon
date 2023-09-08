using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terramon.Content.Items.Decorative;
using Terraria;
using Terraria.ID;
using Terraria.ObjectData;

namespace Terramon.Content.Tiles.Decorative;

public abstract class PokeBallProp : ModTile
{
    public override string Texture => "Terramon/Assets/Items/PokeBalls/" + GetAssetName();

    public override void SetStaticDefaults()
    {
        Main.tileShine[Type] = 1100;
        Main.tileSolid[Type] = false;
        Main.tileSolidTop[Type] = false;
        Main.tileFrameImportant[Type] = true;
        TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
        TileObjectData.newTile.StyleHorizontal = true;
        TileObjectData.newTile.LavaDeath = false;
        TileObjectData.addTile(Type);
        HitSound = SoundID.Tink;
        DustType = DustID.Titanium;

        AddMapEntry(new Color(240, 34, 64));
    }

    private string GetAssetName()
    {
        return GetType().Name.Split("Prop")[0] + "Projectile";
    }
}

public class RegularBallProp : PokeBallProp
{
    public override IEnumerable<Item> GetItemDrops(int i, int j)
    {
        var herbItemType = ModContent.ItemType<RegularBallHeldProp>();
        yield return new Item(herbItemType);
    }
}