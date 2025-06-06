using ReLogic.Content;
using Terramon.Content.Items;
using Terramon.Core.Systems;
using Terramon.Helpers;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent;
using Terraria.GameContent.Drawing;
using Terraria.Graphics.Capture;
using Terraria.Localization;
using Terraria.ModLoader.IO;
using Terraria.ObjectData;

namespace Terramon.Content.Tiles.Banners;

public enum BannerTier : byte
{
    None,
    Tier1,
    Tier2,
    Tier3,
    Tier4
}

public class PokeBannerItem(ushort id, DatabaseV2.PokemonSchema schema, int shimmerItem = 0) : TerramonItem
{
    private static Asset<Texture2D> _tierOverlay;

    private readonly LocalizedText _pokeName = Terramon.DatabaseV2.GetLocalizedPokemonName(id);

    // The real tier data. Needs to be synced in network and saved in IO.
    public BannerTier Tier = BannerTier.None;

    // As this is purely visual and freely cyclable, this does not need the same treatment as the above.
    public BannerTier VisualTier = BannerTier.None;

    private bool IsShiny => shimmerItem > 0;

    protected override bool CloneNewInstances => true;

    public override LocalizedText DisplayName =>
        Language.GetText(IsShiny
            ? "Mods.Terramon.Items.PokeBannerItem.ShinyDisplayName"
            : "Mods.Terramon.Items.PokeBannerItem.DisplayName").WithFormatArgs(_pokeName);

    public override LocalizedText Tooltip =>
        Language.GetText("Mods.Terramon.Items.PokeBannerItem.Tooltip").WithFormatArgs(_pokeName);

    public override string Name => $"{(IsShiny ? "Shiny" : string.Empty)}{schema.Identifier}Banner";

    public override string Texture =>
        $"Terramon/Assets/Tiles/Banners/{schema.Identifier}Banner";

    protected override int UseRarity => IsShiny ? ModContent.RarityType<KeyItemRarity>() : ItemRarityID.Blue;

    public override void SetStaticDefaults()
    {
        if (!Main.dedServ)
        {
            _tierOverlay ??= ModContent.Request<Texture2D>("Terramon/Assets/Tiles/Banners/BannerTierOverlay");

            // For correct item sprite drawing as cursor item icon
            Main.RegisterItemAnimation(Item.type, new DrawAnimationStaticFrame
            {
                Frame = IsShiny ? 1 : 0,
                FrameCount = 2,
                Vertical = false,
                SizeOffset = -2
            });
            
            // Fix for Wikithis
            CrossModSystem.Wikithis?.Call(1, Type,
                $"https://terrariamods.wiki.gg/wiki/Terramon_Mod/{DisplayName.Value.Replace(' ', '_')}");
        }

        ItemID.Sets.IsLavaImmuneRegardlessOfRarity[Type] = true;

        // Banner is able to shimmer into its shiny version and vice versa
        if (!IsShiny) return;
        ItemID.Sets.ShimmerTransformToItem[Type] = shimmerItem;
        ItemID.Sets.ShimmerTransformToItem[shimmerItem] = Type;
    }

    public override void SetDefaults()
    {
        Item.DefaultToPlaceableTile(
            IsShiny ? ModContent.TileType<ShinyPokeBannerTile>() : ModContent.TileType<PokeBannerTile>(), id - 1);
        base.SetDefaults();
        Item.width = 12;
        Item.height = 28;
    }

    public override void SaveData(TagCompound tag)
    {
        if (Tier != BannerTier.None)
            tag["tier"] = (byte)Tier;
    }

    public override void LoadData(TagCompound tag)
    {
        if (tag.ContainsKey("tier"))
            Tier = (BannerTier)tag.GetByte("tier");
    }

    public override void NetSend(BinaryWriter writer)
    {
        writer.Write((byte)Tier);
    }

    public override void NetReceive(BinaryReader reader)
    {
        Tier = (BannerTier)reader.ReadByte();
    }

    public override ModItem Clone(Item newEntity)
    {
        var bannerItem = base.Clone(newEntity) as PokeBannerItem;
        bannerItem!.Tier = Tier;
        return bannerItem;
    }

    public override void HoldItem(Player player)
    {
        if (player.whoAmI != Main.myPlayer)
            return;
        if (Main.mouseRight && Main.mouseRightRelease) CycleVisualTier();
    }

    private void CycleVisualTier()
    {
        var upperTier = Tier == BannerTier.None ? 4 : (byte)Tier;
        if (++VisualTier > (BannerTier)upperTier)
            VisualTier = BannerTier.None;
        SoundEngine.PlaySound(SoundID.MenuTick);
    }

    public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor,
        Color itemColor,
        Vector2 origin, float scale)
    {
        var textureAsset = TextureAssets.Item[Type];
        var newFrame = textureAsset.Frame(2, frameX: IsShiny ? 1 : 0, sizeOffsetX: -2);
        spriteBatch.Draw(textureAsset.Value, position, newFrame, drawColor, 0, newFrame.Size() / 2f, scale,
            SpriteEffects.None, 0);
        return false;
    }

    public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation,
        ref float scale,
        int whoAmI)
    {
        var textureAsset = TextureAssets.Item[Type];
        var newFrame = textureAsset.Frame(2, frameX: IsShiny ? 1 : 0, sizeOffsetX: -2);
        var drawPos = Item.Center - Main.screenPosition;
        spriteBatch.Draw(textureAsset.Value, drawPos, newFrame, lightColor, rotation, newFrame.Size() / 2f, scale,
            SpriteEffects.None, 0f);
        return false;
    }

    public override void PostDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame,
        Color drawColor, Color itemColor, Vector2 origin, float scale)
    {
        if (Tier == BannerTier.None)
            return;

        var overlay = _tierOverlay.Value;

        var tierFrame = (int)Tier - 1;
        var width = overlay.Width / 4;

        position += new Vector2(0f, -16f);

        spriteBatch.Draw(_tierOverlay.Value, position, new Rectangle(tierFrame * width, 0, width, overlay.Height),
            Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }

    public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, float rotation,
        float scale, int whoAmI)
    {
        if (Tier == BannerTier.None)
            return;

        var overlay = _tierOverlay.Value;

        var tierFrame = (int)Tier - 1;
        var width = overlay.Width / 4;

        Vector2 offsetFromCenter = new(0f, 16f);

        var drawPos = Item.Center - Main.screenPosition;

        spriteBatch.Draw(_tierOverlay.Value, drawPos, new Rectangle(tierFrame * width, 0, width, overlay.Height),
            lightColor, rotation, offsetFromCenter, scale, SpriteEffects.None, 0f);
    }
}

public class PokeBannerTile : CustomPreviewTile
{
    // max is 227 for reference. after there's 227 banners don't do anything else cuz tiles be smart
    private const int SupportedPokeyMenHorizontal = 9;
    private static Asset<Texture2D> _tierUnderlay; // hmm yes real words
    private static RenderTarget2D _placementPreviewRt;
    private static bool _placementPreviewRtCreationIsQueued;
    private static bool _isRenderingToPlacementPreviewRt;

    static PokeBannerTile()
    {
        On_Main.CheckMonoliths += orig =>
        {
            orig();
            if (_placementPreviewRt == null || _placementPreviewRt.IsDisposed) return;
            var isDrawingPlacementPreviewThisFrame =
                TileObject.objectPreview.Active && Main.LocalPlayer.cursorItemIconEnabled && Main.placementPreview &&
                !CaptureManager.Instance.Active && Main.LocalPlayer.HeldItem.ModItem is PokeBannerItem bannerItem &&
                bannerItem.VisualTier != BannerTier.None;
            if (!isDrawingPlacementPreviewThisFrame) return;
            RenderPlacementPreviewToTarget();
        };
    }

    public override string Texture => "Terramon/Assets/Tiles/Banners/PokeBannerTile";

    public override void SetStaticDefaults()
    {
        if (!Main.dedServ)
            _tierUnderlay = ModContent.Request<Texture2D>("Terramon/Assets/Tiles/Banners/BannerTileTiers");

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

        TileObjectData.newTile.AnchorTop =
            new AnchorData(AnchorType.SolidTile | AnchorType.SolidSide | AnchorType.SolidBottom | AnchorType.PlanterBox,
                TileObjectData.newTile.Width, 0);
        TileObjectData.newTile.DrawYOffset =
            -2; // Draw this tile 2 pixels up, allowing the banner pole to align visually with the bottom of the tile it is anchored to.
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
    ///     This method will only output the correct values if the provided i, j coordinates are the top-left tile of a
    ///     PokeBannerTile.
    /// </summary>
    /// <param name="i">The X coordinate of the top-left tile.</param>
    /// <param name="j">The Y coordinate of the top-left tile.</param>
    /// <param name="realTier">The functional tier of the banner tile.</param>
    /// <param name="visualTier">The visual tier of the banner tile.</param>
    private static void GetTierData(int i, int j, out BannerTier realTier, out BannerTier visualTier)
    {
        GetTierData(Framing.GetTileSafely(i, j), out realTier, out visualTier);
    }

    /// <summary>
    ///     This method will only output the correct values if the provided Tile is the top-left tile of a PokeBannerTile.
    /// </summary>
    /// <param name="t">The top-left tile.</param>
    /// <param name="realTier">The functional tier of the banner tile.</param>
    /// <param name="visualTier">The visual tier of the banner tile.</param>
    private static void GetTierData(Tile t, out BannerTier realTier, out BannerTier visualTier)
    {
        realTier = (BannerTier)(t.TileFrameY % 54);
        visualTier = (BannerTier)(t.TileFrameX % 18);
    }

    public override void PlaceInWorld(int i, int j, Item item)
    {
        if (item.ModItem is not PokeBannerItem bannerItem) return;
        var t = Framing.GetTileSafely(i, j);

        // store real tier info for item dropping
        t.TileFrameY += (byte)bannerItem.Tier;

        if (bannerItem.VisualTier == BannerTier.None) return;
        // reframe it to the alt appearance if the visual tier is any of the ball tiers
        for (var k = 0; k < 3; k++)
        {
            var reframe = Framing.GetTileSafely(i, j + k);
            reframe.TileFrameY += 54;
        }

        // store visual tier info for ball drawing
        t.TileFrameX += (byte)bannerItem.VisualTier;
    }

    public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
    {
        if (fail || noItem)
            return;

        Point16 thisPoint = new(i, j);
        var tile = Framing.GetTileSafely(thisPoint);
        var thing = tile.TileFrameY % 54;
        Point16 origin = new(i, j - thing / 18); // thank you integer division

        if (thisPoint != origin)
            return;

        var topTile = Framing.GetTileSafely(origin);
        var below = Framing.GetTileSafely(origin + new Point16(0, 1));

        var style = Math.Max(TileObjectData.GetTileStyle(below), 0);

        Item drop = new(TileLoader.GetItemDropFromTypeAndStyle(Type, style));

        if (drop.ModItem is PokeBannerItem bannerItem)
        {
            GetTierData(topTile, out var realTier, out var visualTier);

            bannerItem.Tier = realTier;
            bannerItem.VisualTier = visualTier;
        }

        Item.NewItem(WorldGen.GetItemSource_FromTileBreak(origin.X, origin.Y + 1), origin.X * 16, (origin.Y + 1) * 16,
            16, 16, drop);
    }

    public override bool CanDrop(int i, int j)
    {
        return false;
    }

    private bool TopLeftPokeBanner(int i, int j)
    {
        // if top left, the tile below will match this (cuz tileframey will be 18, 72, just any multiple of 54 on top of 18)
        // i'm sorry that this is so horrid
        var below = Framing.GetTileSafely(i, j + 1);
        return below.TileType == Type && below.TileFrameY % 54 == 18;
    }

    private static void RenderPlacementPreviewToTarget()
    {
        _isRenderingToPlacementPreviewRt = true;

        var storedZoom = Main.GameViewMatrix.Zoom;
        Main.GameViewMatrix.Zoom = new Vector2(1, 1);
        var storedSpriteEffects = Main.GameViewMatrix.Effects;
        Main.GameViewMatrix.Effects = SpriteEffects.None;

        var gd = Main.graphics.GraphicsDevice;
        gd.SetRenderTarget(_placementPreviewRt);
        gd.Clear(Color.Transparent);

        var sb = Main.spriteBatch;
        sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None,
            Main.Rasterizer, null, Main.Transform);

        Main.instance.LoadTiles(TileObject.objectPreview.Type);
        TileObject.DrawPreview(Main.spriteBatch, TileObject.objectPreview, Vector2.Zero);

        sb.End();
        gd.SetRenderTarget(null);

        Main.GameViewMatrix.Zoom = storedZoom;
        Main.GameViewMatrix.Effects = storedSpriteEffects;

        _isRenderingToPlacementPreviewRt = false;
    }

    public override bool PreDrawPlacementPreview(SpriteBatch sb, TileObjectPreviewData data, Texture2D texture,
        Vector2 position, Rectangle sourceRect, Color color)
    {
        if (Main.LocalPlayer.HeldItem.ModItem is not PokeBannerItem bannerItem ||
            bannerItem.VisualTier == BannerTier.None) return true;

        var isTopTile = sourceRect.Y == 0;
        Rectangle newFrame = new(sourceRect.X, sourceRect.Y + 54, sourceRect.Width, sourceRect.Height);

        if (_isRenderingToPlacementPreviewRt)
        {
            position = new Vector2(0, sourceRect.Y);
            color = Color.White;

            if (isTopTile)
            {
                var underlay = _tierUnderlay.Value;
                var width = underlay.Width / 4;
                Rectangle underlayFrame = new(((int)bannerItem.VisualTier - 1) * width, 0, width, underlay.Height);
                sb.Draw(_tierUnderlay.Value, position, underlayFrame, color);
            }

            sb.Draw(texture, position, newFrame, color);
        }
        else
        {
            sb.Draw(_placementPreviewRt, position, new Rectangle(0, sourceRect.Y, 16, 16), color);
        }

        return false;
    }

    public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
    {
        var tile = Main.tile[i, j];

        if (TopLeftPokeBanner(i, j))
            Main.instance.TilesRenderer.AddSpecialPoint(i, j, TileDrawing.TileCounterType.MultiTileVine);

        if (tile.TileFrameY % 54 == 36) return false;
        GetTierData(i, j, out _, out var visualTier);

        if (visualTier == BannerTier.None)
            return false;

        var underlay = _tierUnderlay.Value;
        var width = underlay.Width / 4;
        Rectangle underlayFrame = new(((int)visualTier - 1) * width, 0, width, underlay.Height);

        TileUtils.DrawTileCommon(spriteBatch, i, j, underlay,
            new Vector2(0f, WorldGen.IsBelowANonHammeredPlatform(i, j) ? -10f : -2f), underlayFrame);

        // We must return false here to prevent the normal tile drawing code from drawing the default static tile. Without this a duplicate tile will be drawn.
        return false;
    }

    public override void SetDrawPositions(int i, int j, ref int width, ref int offsetY, ref int height,
        ref short tileFrameX, ref short tileFrameY)
    {
        // readjust the appearance of the top left tile
        if (TopLeftPokeBanner(i, j))
        {
            GetTierData(i, j, out var realTier, out var visualTier);

            tileFrameX -= (byte)visualTier;
            tileFrameY -= (byte)realTier;
        }

        // Due to MultiTileVine rendering the tile 2 pixels higher than expected for modded tiles using TileObjectData.DrawYOffset, we need to add 2 to fix the math for correct drawing
        offsetY += 2;
    }

    public override void Load()
    {
        base.Load();
        if (Main.dedServ) return;
        if (_placementPreviewRt != null || _placementPreviewRtCreationIsQueued) return;
        Main.QueueMainThreadAction(() =>
        {
            _placementPreviewRt = new RenderTarget2D(Main.graphics.GraphicsDevice, 16, 54);
        });
        _placementPreviewRtCreationIsQueued = true;
    }

    public override void Unload()
    {
        if (Main.dedServ) return;
        Main.QueueMainThreadAction(() =>
        {
            _placementPreviewRt?.Dispose();
            _placementPreviewRt = null;
        });
        _placementPreviewRtCreationIsQueued = false;
    }
}

public class ShinyPokeBannerTile : PokeBannerTile
{
    public override string Texture => "Terramon/Assets/Tiles/Banners/PokeBannerTile_Shiny";

    public override void EmitParticles(int i, int j, Tile tileCache, short tileFrameX, short tileFrameY,
        Color tileLight, bool visible)
    {
        if (!visible || (int)Main.timeForVisualEffects % 7 != 0 || !Main.rand.NextBool(5)) return;

        var spawnPosition = new Vector2(i * 16 + 4, j * 16 + 4);
        var dust = Dust.NewDustDirect(
            spawnPosition + new Vector2(Main.rand.NextFloat(-1, 1), Main.rand.NextFloat(-1, 1)), 8, 8,
            DustID.TreasureSparkle);
        dust.velocity = new Vector2(Main.WindForVisuals * 2f, 0f);
        dust.noGravity = true;
        dust.scale *= 1f + Main.rand.NextFloat(-0.03f, 0.03f);
    }
}

internal class DrawAnimationStaticFrame : DrawAnimation
{
    public int SizeOffset;
    public bool Vertical;

    public override Rectangle GetFrame(Texture2D texture, int frameCounterOverride = -1)
    {
        return Vertical
            ? texture.Frame(verticalFrames: FrameCount, frameY: Frame, sizeOffsetY: SizeOffset)
            : texture.Frame(FrameCount, frameX: Frame, sizeOffsetX: SizeOffset);
    }
}