using System.Collections.Generic;
using System.IO;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.ObjectInteractions;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader.IO;
using Terraria.ObjectData;

namespace Terramon.Content.Items.Mechanical;

public abstract class BasePkballTile : ModTile
{
    private const int maxInteractDistance = 80;
    public override string Texture => "Terramon/Assets/Items/PokeBalls/" + GetType().Name;
    public override string HighlightTexture => "Terramon/Assets/Items/PokeBalls/PokeBallTile_Highlight";

    protected virtual int dropItem => -1;

    public override void SetStaticDefaults()
    {
        Main.tileShine[Type] = 1100;
        Main.tileSolid[Type] = false;
        Main.tileSolidTop[Type] = false;
        Main.tileFrameImportant[Type] = true;

        TileID.Sets.HasOutlines[Type] = true;
        TileObjectData.newTile.StyleHorizontal = true;

        TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
        TileObjectData.newTile.HookPostPlaceMyPlayer =
            new PlacementHook(ModContent.GetInstance<BasePkballEntity>().Hook_AfterPlacement, -1, 0, false);
        TileObjectData.newTile.UsesCustomCanPlace = true;
        TileObjectData.newTile.LavaDeath = false;
        TileObjectData.addTile(Type);

        HitSound = SoundID.Tink;
        DustType = DustID.Marble;

        AddMapEntry(Color.LightGray,
            Language.GetText($"Mods.Terramon.Items.{GetType().Name.Replace("Tile", "Item")}.DisplayName"));
    }

    public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
    {
        if (!TileUtils.TryGetTileEntityAs<BasePkballEntity>(i, j, out var e)) return base.PreDraw(i, j, spriteBatch);
        Main.tile[i, j].TileFrameX = e.open ? (short)18 : (short)0;

        return base.PreDraw(i, j, spriteBatch);
    }

    public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings)
    {
        return true;
    }

    public override void MouseOver(int i, int j)
    {
        var player = Main.LocalPlayer;

        if (!player.IsWithinSnappngRangeToTile(i, j,
                maxInteractDistance)) // Match condition in RightClick. Interaction should only show if clicking it does something
            return;

        player.noThrow = 2;
        player.cursorItemIconEnabled = true;
        if (TileUtils.TryGetTileEntityAs<BasePkballEntity>(i, j, out var e) && !e.item.IsAir)
            player.cursorItemIconID = e.item.type;
        else
            player.cursorItemIconID = dropItem;
    }

    public override bool RightClick(int i, int j)
    {
        var player = Main.LocalPlayer;

        if (!player.IsWithinSnappngRangeToTile(i, j,
                maxInteractDistance)) return false; // Avoid being able to trigger it from long range
        if (!TileUtils.TryGetTileEntityAs<BasePkballEntity>(i, j, out var e)) return false;
        SoundEngine.PlaySound(SoundID.Mech);
        if (e.open) //when closing
        {
            if (!player.HeldItem.IsAir && player.HeldItem.ModItem is not BasePkballItem)
            {
                e.item = player.HeldItem.Clone();
                e.item.stack = player.HeldItem.stack;
                player.HeldItem.stack -= player.HeldItem.stack;
            }

            e.open = false;
        }
        else //when opening
        {
            if (!e.item.IsAir)
            {
                player.QuickSpawnItem(Entity.GetSource_None(), e.item, e.item.stack);
                e.item = new Item();
            }

            e.open = true;

            if (e.disposable)
                WorldGen.KillTile(i, j);
        }

        return true;
    }

    public override IEnumerable<Item> GetItemDrops(int i, int j)
    {
        if (TileUtils.TryGetTileEntityAs<BasePkballEntity>(i, j, out var e))
        {
            if (!e.item.IsAir) Main.LocalPlayer.QuickSpawnItem(Entity.GetSource_None(), e.item, e.item.stack);

            if (!e.disposable)
                yield return new Item(dropItem);
        }
        else
        {
            yield return new Item(dropItem);
        }


        ModContent.GetInstance<BasePkballEntity>().Kill(i, j);
    }
}

public class BasePkballEntity : ModTileEntity
{
    public bool disposable;
    public Item item = new();
    public bool open;

    public override bool IsTileValidForEntity(int x, int y)
    {
        var tile = Main.tile[x, y];
        return tile.HasTile && ModContent.GetModTile(tile.TileType) is BasePkballTile;
    }

    public override int Hook_AfterPlacement(int i, int j, int t, int style, int direction, int alternate)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            // Sync the tile
            NetMessage.SendTileSquare(Main.myPlayer, i, j);

            // Sync the placement of the tile entity with other clients
            // The "type" parameter refers to the tile type which placed the tile entity, so "Type" (the type of the tile entity) needs to be used here instead
            NetMessage.SendData(MessageID.TileEntityPlacement, number: i, number2: j, number3: Type);
        }

        // ModTileEntity.Place() handles checking if the entity can be placed, then places it for you
        // Set "tileOrigin" to the same value you set TileObjectData.newTile.Origin to in the ModTile
        Point16 tileOrigin = new(0, 0);
        var placedEntity = Place(i - tileOrigin.X, j - tileOrigin.Y);
        return placedEntity;
    }

    public override void OnNetPlace()
    {
        if (Main.netMode == NetmodeID.Server)
            NetMessage.SendData(MessageID.TileEntitySharing, number: ID, number2: Position.X, number3: Position.Y);
    }

    public override void NetSend(BinaryWriter writer)
    {
        item.Serialize(writer, ItemSerializationContext.Syncing);
        writer.Write(open);
        writer.Write(disposable);
    }

    public override void NetReceive(BinaryReader reader)
    {
        item.DeserializeFrom(reader, ItemSerializationContext.Syncing);
        open = reader.ReadBoolean();
        disposable = reader.ReadBoolean();
    }

    public override void SaveData(TagCompound tag)
    {
        tag.Set("pkballTile", item.SerializeData());
        tag.Set("pkballOpen", open);
        tag.Set("pkballDisposable", disposable);
    }

    public override void LoadData(TagCompound tag)
    {
        item = tag.Get<Item>("pkballTile");
        open = tag.GetBool("pkballOpen");
        disposable = tag.GetBool("pkballDisposable");
    }
}