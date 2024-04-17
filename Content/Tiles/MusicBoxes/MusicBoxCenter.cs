using System.Collections.Generic;
using Terramon.Content.Items.Mechanical;
using Terraria.ID;

namespace Terramon.Content.Tiles.MusicBoxes;

public class MusicBoxCenter : MusicTile
{
    public override IEnumerable<Item> GetItemDrops(int i, int j)
    {
        yield return new Item(ModContent.ItemType<MusicItemCenter>());
    }

    public override void MouseOver(int i, int j)
    {
        var player = Main.LocalPlayer;
        player.noThrow = 2;
        player.cursorItemIconEnabled = true;
        player.cursorItemIconID = ModContent.ItemType<MusicItemCenter>();
    }
}

public class MusicItemCenter : MusicItem
{
    public override void SetStaticDefaults()
    {
        base.SetStaticDefaults();
        MusicLoader.AddMusicBox(Mod, MusicLoader.GetMusicSlot(Mod, "Sounds/Music/PokeCenter"),
            ModContent.ItemType<MusicItemCenter>(), ModContent.TileType<MusicBoxCenter>());
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
        Item.createTile = ModContent.TileType<MusicBoxCenter>();
        Item.width = 30;
        Item.height = 26;
        Item.value = 100000;
        Item.accessory = true;
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ItemID.MusicBox)
            .AddIngredient(ModContent.ItemType<PokeBallItem>())
            .AddTile(TileID.TinkerersWorkbench)
            .Register();
    }
}