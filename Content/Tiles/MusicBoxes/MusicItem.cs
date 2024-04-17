using Terramon.Content.Items;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.Utilities;

namespace Terramon.Content.Tiles.MusicBoxes;

public abstract class MusicItem : TerramonItem
{
    public override string Texture => "Terramon/Assets/Tiles/MusicBoxes/" + GetType().Name;
    
    protected override int UseRarity => ItemRarityID.LightRed;

    public override void SetStaticDefaults()
    {
        CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
    }
    
    public override bool? PrefixChance(int pre, UnifiedRandom rand)
    {
        return false;
    }
}