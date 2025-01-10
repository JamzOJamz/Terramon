using System.Collections.Generic;
using System.IO;
using EasyPacketsLib;
using Terramon.Content.Packets;
using Terramon.Helpers;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent.ObjectInteractions;
using Terraria.Localization;
using Terraria.ModLoader.IO;
using Terraria.ObjectData;

namespace Terramon.Content.Items.PokeBalls;

public abstract class BasePkballTile : ModTile
{
    private const int MaxInteractDistance = 80;
    public override string Texture => "Terramon/Assets/Items/PokeBalls/" + GetType().Name;
    public override string HighlightTexture => "Terramon/Assets/Items/PokeBalls/PokeBallTile_Highlight";

    protected virtual int DropItem => -1;

    public override void SetStaticDefaults()
    {
        Main.tileShine[Type] = 1100;
        Main.tileSolid[Type] = false;
        Main.tileSolidTop[Type] = false;
        Main.tileFrameImportant[Type] = true;

        TileID.Sets.HasOutlines[Type] = true;

        TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
        TileObjectData.newTile.StyleHorizontal = true;
        TileObjectData.newTile.HookPostPlaceMyPlayer =
            new PlacementHook(ModContent.GetInstance<BasePkballEntity>().Hook_AfterPlacement, -1, 0, false);
        TileObjectData.newTile.UsesCustomCanPlace = true;
        TileObjectData.newTile.LavaDeath = false;
        TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.Table, 1, 0);
        TileObjectData.addTile(Type);

        HitSound = SoundID.Tink;
        DustType = DustID.Marble;

        AddMapEntry(Color.LightGray,
            Language.GetText($"Mods.Terramon.Items.{GetType().Name.Replace("Tile", "Item")}.DisplayName"));
    }

    public override void PlaceInWorld(int i, int j, Item item)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient) return;
        Mod.SendPacket(new PlacedPkballTileRpc(new Point16(i, j)), -1, Main.myPlayer,
            true);
    }

    public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
    {
        if (!TileUtils.TryGetTileEntityAs<BasePkballEntity>(i, j, out var e)) return base.PreDraw(i, j, spriteBatch);
        Main.tile[i, j].TileFrameX = e.Open ? (short)18 : (short)0;
        
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
                MaxInteractDistance)) // Match condition in RightClick. Interaction should only show if clicking it does something
            return;

        player.noThrow = 2;
        player.cursorItemIconEnabled = true;
        if (TileUtils.TryGetTileEntityAs<BasePkballEntity>(i, j, out var e) && !e.Item.IsAir && !e.Disposable)
        {
            player.cursorItemIconID = e.Item.type;
            player.cursorItemIconText = "  x" + e.Item.stack;
        }
        else
            player.cursorItemIconID = DropItem;
        
    }

    public override bool RightClick(int i, int j)
    {
        var player = Main.LocalPlayer;

        if (!player.IsWithinSnappngRangeToTile(i, j,
                MaxInteractDistance)) return false; // Avoid being able to trigger it from long range
        if (!TileUtils.TryGetTileEntityAs<BasePkballEntity>(i, j, out var e)) return false;
        
        SoundEngine.PlaySound(SoundID.Mech);
        
        if (e.Open) //when pokeball is open, insert item + close
        {
            e.TryAddItem(player);
            e.Open = false;
        }
        else if (!e.TryAddItem(player)) //when closed, try adding items, otherwise open it
        {
            if (e.TryOpen() && !e.Item.IsAir)
            {
                player.QuickSpawnItem(Entity.GetSource_None(), e.Item, e.Item.stack);
                e.Item.TurnToAir();
            }
        }
        
        if (Main.netMode == NetmodeID.MultiplayerClient)
            Mod.SendPacket(new SyncPkballTileRpc(e.Item, e.Open, e.Disposable, (byte)player.whoAmI, new Point16(i, j)), -1, Main.myPlayer,
                true);

        return true;
    }

    public override IEnumerable<Item> GetItemDrops(int i, int j)
    {
        if (TileUtils.TryGetTileEntityAs<BasePkballEntity>(i, j, out var e))
        {
            if (!e.Item.IsAir) Main.LocalPlayer.QuickSpawnItem(Entity.GetSource_None(), e.Item, e.Item.stack);

            if (!e.Disposable)
                yield return new Item(DropItem);
        }
        else
        {
            yield return new Item(DropItem);
        }
    }
}

public class BasePkballEntity : ModTileEntity
{
    public bool Disposable;
    public Item Item = new();
    public bool Open;

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

    public bool TryAddItem(Player player)
    {
        if (player.HeldItem.IsAir) return false;

        if (Open || (!Disposable && player.HeldItem.type == Item.type))
        {
            if (player.HeldItem.maxStack <= Item.stack) return false;

            if (Item.IsAir)
            {
                Item = player.HeldItem.Clone();
                Item.stack = 1;
            }
            else
                Item.stack++;

            player.HeldItem.stack -= 1;
            return true;
        }

        return false;
    }

    public bool TryOpen()
    {
        if (Disposable && !Item.IsAir)
        {
            WorldGen.KillTile(Position.X, Position.Y);
            Kill(Position.X, Position.Y); //kill entity after tile so tile can correctly drop items
            return false;
        }

        Open = true;
        return true;
    }

    public override void OnNetPlace()
    {
        NetMessage.SendData(MessageID.TileEntitySharing, number: ID, number2: Position.X, number3: Position.Y);
    }

    public override void NetSend(BinaryWriter writer)
    {
        Item.Serialize(writer, ItemSerializationContext.Syncing);
        writer.Write(Open);
        writer.Write(Disposable);
    }

    public override void NetReceive(BinaryReader reader)
    {
        Item.DeserializeFrom(reader, ItemSerializationContext.Syncing);
        Open = reader.ReadBoolean();
        Disposable = reader.ReadBoolean();
    }

    public override void SaveData(TagCompound tag)
    {
        tag.Set("pkballItem" , Item.SerializeData());
        tag.Set("pkballOpen", Open);
        tag.Set("pkballDisposable", Disposable);
    }

    public override void LoadData(TagCompound tag)
    {
        Item = tag.Get<Item>("pkballItem");
        Open = tag.GetBool("pkballOpen");
        Disposable = tag.GetBool("pkballDisposable");
    }
}