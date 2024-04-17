using System.Collections.Generic;
using Terraria.ID;

namespace Terramon.Content.Tiles.MusicBoxes;

public class MusicBoxWildBattle : MusicTile
{
    public override IEnumerable<Item> GetItemDrops(int i, int j)
    {
        yield return new Item(ModContent.ItemType<MusicItemWildBattle>());
    }

    public override void MouseOver(int i, int j)
    {
        var player = Main.LocalPlayer;
        player.noThrow = 2;
        player.cursorItemIconEnabled = true;
        player.cursorItemIconID = ModContent.ItemType<MusicItemWildBattle>();
    }
}

public class MusicItemWildBattle : MusicItem
{
    public override void SetStaticDefaults()
    {
        base.SetStaticDefaults();
        MusicLoader.AddMusicBox(Mod, MusicLoader.GetMusicSlot(Mod, "Assets/Audio/Music/battle_wild"),
            ModContent.ItemType<MusicItemWildBattle>(), ModContent.TileType<MusicBoxWildBattle>());
    }

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.maxStack = 1;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.useTurn = true;
        Item.useAnimation = 15;
        Item.useTime = 10;
        Item.autoReuse = true;
        Item.consumable = true;
        Item.createTile = ModContent.TileType<MusicBoxWildBattle>();
        Item.width = 30;
        Item.height = 26;
        Item.value = 100000;
        Item.accessory = true;
    }
}