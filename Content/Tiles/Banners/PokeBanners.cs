using ReLogic.Content;
using Terramon.Content.Items;
using Terraria.DataStructures;
using Terraria.GameContent.Drawing;
using Terraria.Localization;
using Terraria.ObjectData;
using Terraria.Enums;
using Terraria.ModLoader.IO;
using MonoMod.RuntimeDetour;
using System.Reflection;

namespace Terramon.Content.Tiles.Banners;

public enum BannerTier : byte
{
    None,
    Tier1,
    Tier2,
    Tier3,
    Tier4
}
public class PokeBannerItem(ushort id, DatabaseV2.PokemonSchema schema) : TerramonItem
{
    private static Asset<Texture2D> _tierOverlay;

    private readonly LocalizedText _pokeName = Terramon.DatabaseV2.GetLocalizedPokemonName(id);
    protected override bool CloneNewInstances => true;

    public override LocalizedText DisplayName =>
        Language.GetText("Mods.Terramon.Items.PokeBannerItem.DisplayName").WithFormatArgs(_pokeName);

    public override LocalizedText Tooltip => Language.GetText("Mods.Terramon.Items.PokeBannerItem.Tooltip").WithFormatArgs(_pokeName);

    public override string Name { get; } = $"{schema.Identifier}Banner";

    public override string Texture => $"Terramon/Assets/Tiles/Banners/{schema.Identifier}Banner";

    protected override int UseRarity => ItemRarityID.Blue;

    // The real tier data. Needs to be synced in network and saved in IO.
    public BannerTier tier = BannerTier.None;
    // As this is purely visual and freely cyclable, this does not need the same treatment as the above.
    public BannerTier visualTier = BannerTier.None;

    public override void SetStaticDefaults()
    {
        _tierOverlay ??= ModContent.Request<Texture2D>("Terramon/Assets/Tiles/Banners/BannerTierOverlay");
        ItemID.Sets.IsLavaImmuneRegardlessOfRarity[Type] = true;
    }
    public override void SetDefaults()
    {
        Item.DefaultToPlaceableTile(ModContent.TileType<PokeBannerTile>(), id - 1);
        base.SetDefaults();
    }
    public override void SaveData(TagCompound tag)
    {
        if (tier != BannerTier.None)
            tag["tier"] = (byte)tier;
    }
    public override void LoadData(TagCompound tag)
    {
        if (tag.ContainsKey("tier"))
            tier = (BannerTier)tag.GetByte("tier");
    }
    public override void NetSend(BinaryWriter writer)
    {
        writer.Write((byte)tier);
    }
    public override void NetReceive(BinaryReader reader)
    {
        tier = (BannerTier)reader.ReadByte();
    }
    public override ModItem Clone(Item newEntity)
    {
        var bannerItem = base.Clone(newEntity) as PokeBannerItem;
        bannerItem.tier = tier;
        return bannerItem;
    }
    public override void PostDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
    {
        if (tier == BannerTier.None)
            return;

        Texture2D overlay = _tierOverlay.Value;

        int tierFrame = ((int)tier) - 1;
        int width = overlay.Width / 4;

        position += new Vector2(0f, -16f);
        
        spriteBatch.Draw(_tierOverlay.Value, position, new(tierFrame * width, 0, width, overlay.Height), Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
    }
}

public class PokeBannerTile : CustomPreviewTile
{
    // max is 226 for reference. after there's 226 banners don't do anything else cuz tiles be smart
    private const int SupportedPokeyMenHorizontal = 6;
    public override string Texture => $"Terramon/Assets/Tiles/Banners/PokeBannerTile";
    public override void SetStaticDefaults()
    {
        Main.tileFrameImportant[Type] = true;
        Main.tileNoAttach[Type] = true;
        Main.tileLavaDeath[Type] = false;
        TileID.Sets.DisableSmartCursor[Type] = true;
        TileID.Sets.MultiTileSway[Type] = true;
        TileObjectData.newTile.CopyFrom(TileObjectData.Style1x2Top);
        TileObjectData.newTile.Height = 3;
        TileObjectData.newTile.CoordinateHeights = [16, 16, 16];

        TileObjectData.newTile.StyleHorizontal = true;
        TileObjectData.newTile.StyleWrapLimit = SupportedPokeyMenHorizontal;
        TileObjectData.newTile.StyleMultiplier = 1;
        TileObjectData.newTile.StyleLineSkip = 2;

        TileObjectData.newTile.AnchorTop = new AnchorData(AnchorType.SolidTile | AnchorType.SolidSide | AnchorType.SolidBottom | AnchorType.PlanterBox, TileObjectData.newTile.Width, 0);
        TileObjectData.newTile.DrawYOffset = -2; // Draw this tile 2 pixels up, allowing the banner pole to align visually with the bottom of the tile it is anchored to.
        TileObjectData.newTile.LavaDeath = false;

        // This alternate placement supports placing on un-hammered platform tiles. Note how the DrawYOffset accounts for the height adjustment needed for the tile to look correctly attached.
        TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
        TileObjectData.newAlternate.AnchorTop = new AnchorData(AnchorType.Platform, TileObjectData.newTile.Width, 0);
        TileObjectData.newAlternate.DrawYOffset = -10;
        TileObjectData.addAlternate(0);

        TileObjectData.addTile(Type);

        DustType = -1; // No dust when mined
        AddMapEntry(new Color(13, 88, 130), Language.GetText("MapObject.Banner"));
    }
    /// <summary>
    /// This method will only output the correct values if the provided i, j coordinates are the top-left tile of a PokeBannerTile.
    /// </summary>
    /// <param name="i">The X coordinate of the top-left tile.</param>
    /// <param name="j">The Y coordinate of the top-left tile.</param>
    /// <param name="realTier">The functional tier of the banner tile.</param>
    /// <param name="visualTier">The visual tier of the banner tile.</param>
    public static void GetTierData(int i, int j, out BannerTier realTier, out BannerTier visualTier)
    {
        Tile t = Framing.GetTileSafely(i, j);
        realTier = (BannerTier)(t.TileFrameY % 54);
        visualTier = (BannerTier)(t.TileFrameX % 18);
    }
    public override void PlaceInWorld(int i, int j, Item item)
    {
        if (item.ModItem is PokeBannerItem bannerItem)
        {
            Tile t = Framing.GetTileSafely(i, j);
            // store real tier info for item dropping
            t.TileFrameY += (byte)bannerItem.tier;

            if (bannerItem.visualTier != BannerTier.None)
            {

                // reframe it to the alt appearance if the visual tier is any of the ball tiers
                for (int k = 0; k < 3; k++)
                {
                    Tile reframe = Framing.GetTileSafely(i, j + k);
                    reframe.TileFrameY += 54;
                }

                // store visual tier info for ball drawing
                t.TileFrameX += (byte)bannerItem.visualTier;
            }
        }
    }
    public bool TopLeftPokeBanner(int i, int j)
    {
        // if top left, the tile below will match this (cuz tileframey will be 18, 72, just any multiple of 54 on top of 18)
        // i'm sorry that this is so horrid
        Tile below = Framing.GetTileSafely(i, j + 1);
        return below.TileType == Type && below.TileFrameY % 54 == 18;
    }
    public override bool PreDrawPlacementPreview(TileObjectPreviewData data, Texture2D texture, Vector2 position, Rectangle? sourceRect, Color color)
    {
        Main.spriteBatch.Draw(texture, position + Vector2.UnitX * 16f, sourceRect, color);
        return true;
    }
    public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
    {
        Tile tile = Main.tile[i, j];

        bool topLeft = TopLeftPokeBanner(i, j);

        short oldFrameX = 0;
        short oldFrameY = 0;

        if (topLeft)
        {
            oldFrameX = tile.TileFrameX;
            oldFrameY = tile.TileFrameY;

            GetTierData(i, j, out BannerTier realTier, out BannerTier visualTier);

            if (visualTier != BannerTier.None)
                tile.TileFrameX -= (byte)visualTier;

            if (realTier != BannerTier.None)
                tile.TileFrameY -= (byte)realTier;
        }

        if (TileObjectData.IsTopLeft(tile))
        {
            // Makes this tile sway in the wind and with player interaction when used with TileID.Sets.MultiTileSway
            Main.instance.TilesRenderer.AddSpecialPoint(i, j, TileDrawing.TileCounterType.MultiTileVine);
        }

        if (topLeft)
        {
            tile.TileFrameX = oldFrameX;
            tile.TileFrameY = oldFrameY;
        }
        // We must return false here to prevent the normal tile drawing code from drawing the default static tile. Without this a duplicate tile will be drawn.
        return false;
    }

    public override void SetDrawPositions(int i, int j, ref int width, ref int offsetY, ref int height, ref short tileFrameX, ref short tileFrameY)
    {
        // readjust the appearance of the top left tile
        if (TopLeftPokeBanner(i, j))
        {
            GetTierData(i, j, out BannerTier realTier, out BannerTier visualTier);

            tileFrameX -= (byte)visualTier;
            tileFrameY -= (byte)realTier;
        }

        // Due to MultiTileVine rendering the tile 2 pixels higher than expected for modded tiles using TileObjectData.DrawYOffset, we need to add 2 to fix the math for correct drawing
        offsetY += 2;
        return;
    }

    public override void NearbyEffects(int i, int j, bool closer)
    {
        if (closer)
        {
            return;
        }
    }
}