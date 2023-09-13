using Microsoft.Xna.Framework;
using Terramon.Content.Items;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace Terramon.Content.Tiles.MusicBoxes;

public class MusicBoxCenter : ModTile
{
        public override string Texture => "Terramon/Assets/Tiles/MusicBoxes/" + GetType().Name;
        public override void SetStaticDefaults() {
		Main.tileFrameImportant[Type] = true;
		Main.tileObsidianKill[Type] = true;
		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
		TileObjectData.newTile.Origin = new Point16(0, 1);
		TileObjectData.newTile.LavaDeath = false;
		TileObjectData.newTile.DrawYOffset = 2;
		TileObjectData.addTile(Type);

		LocalizedText name = CreateMapEntryName();
		// name.SetDefault("Music Box");
		AddMapEntry(new Color(200, 200, 200), name);
	}

	public override void KillMultiTile(int i, int j, int frameX, int frameY) {
		Item.NewItem(new EntitySource_TileBreak(i, j), i * 16, j * 16, 16, 48, ModContent.ItemType<MusicItemCenter>());
	}

	public override void MouseOver(int i, int j) {
		Player player = Main.LocalPlayer;
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
		// DisplayName.SetDefault("Music Box (Pokemon Center)");

		CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;

		// The following code links the music box's item and tile with a music track:
		//   When music with the given ID is playing, equipped music boxes have a chance to change their id to the given item type.
		//   When an item with the given item type is equipped, it will play the music that has musicSlot as its ID.
		//   When a tile with the given type and Y-frame is nearby, if its X-frame is >= 36, it will play the music that has musicSlot as its ID.
		// When getting the music slot, you should not add the file extensions!
		MusicLoader.AddMusicBox(Mod, MusicLoader.GetMusicSlot(Mod, "Assets/Audio/Music/PokeCenter"), ModContent.ItemType<MusicItemCenter>(), ModContent.TileType<MusicBoxCenter>());
	}

	public override void SetDefaults()
	{
		base.SetDefaults();
		Item.useStyle = ItemUseStyleID.Swing;
		Item.useTurn = true;
		Item.useAnimation = 15;
		Item.useTime = 10;
		Item.autoReuse = true;
		Item.consumable = true;
		Item.createTile = ModContent.TileType<MusicBoxCenter>();
		Item.width = 24;
		Item.height = 24;
		Item.rare = ItemRarityID.LightRed;
		Item.value = 100000;
		Item.accessory = true;
	}

	public override void AddRecipes()
	{
		CreateRecipe()
			.AddIngredient(ItemID.MusicBox)
			.AddTile(TileID.TinkerersWorkbench)
			.Register();
	}
}
