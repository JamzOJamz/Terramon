using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terramon.Content.Items;
using Terramon.Content.Items.Mechanical;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ObjectData;
using Terraria.Utilities;

namespace Terramon.Content.Tiles.MusicBoxes;

public class MusicBoxCenter : ModTile
{
    public override string Texture => "Terramon/Assets/Tiles/MusicBoxes/" + GetType().Name;

    public override void SetStaticDefaults()
    {
        Main.tileFrameImportant[Type] = true;
        Main.tileObsidianKill[Type] = true;
        TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
        TileObjectData.newTile.Origin = new Point16(0, 1);
        TileObjectData.newTile.LavaDeath = false;
        TileObjectData.newTile.DrawYOffset = 2;
        TileObjectData.addTile(Type);
        DustType = DustID.Titanium;

        var name = CreateMapEntryName();
        AddMapEntry(new Color(200, 200, 200), name);
    }
    
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

public class MusicItemCenter : TerramonItem
{
    public override string Texture => "Terramon/Assets/Tiles/MusicBoxes/" + GetType().Name;

    public override void SetStaticDefaults()
    {
        CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
        MusicLoader.AddMusicBox(Mod, MusicLoader.GetMusicSlot(Mod, "Assets/Audio/Music/PokeCenter"),
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
        Item.rare = ItemRarityID.LightRed;
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
    
    public override bool? PrefixChance(int pre, UnifiedRandom rand) => false;
}
