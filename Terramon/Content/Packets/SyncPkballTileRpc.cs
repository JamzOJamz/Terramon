using Terramon.Content.Items.PokeBalls;
using Terramon.Helpers;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ModLoader.IO;

namespace Terramon.Content.Packets;

/// <summary>
///     A packet to be sent whenever the contents of a Poké Ball tile are modified.
/// </summary>
public struct SyncPkballTileRpc(Item item, bool isOpen, bool isDisposable, byte player, Point16 tileCoords)
    : IEasyPacket
{
    private Item _item = item;
    private bool _isOpen = isOpen;
    private bool _isDisposable = isDisposable;

    private byte _player = player;
    private Point16 _tileCoords = tileCoords;

    public readonly void Serialise(BinaryWriter writer)
    {
        _item.Serialize(writer, ItemSerializationContext.Syncing);
        writer.WriteFlags(_isOpen, _isDisposable);
        writer.Write(_player);
        writer.Write(_tileCoords.X);
        writer.Write(_tileCoords.Y);
    }

    public void Deserialise(BinaryReader reader, in SenderInfo sender)
    {
        _item = new Item();
        _item.DeserializeFrom(reader, ItemSerializationContext.Syncing);
        reader.ReadFlags(out _isOpen, out _isDisposable);
        _player = reader.ReadByte();
        _tileCoords = new Point16(reader.ReadInt16(), reader.ReadInt16());
    }

    public readonly void Receive(in SenderInfo sender, ref bool handled)
    {
        sender.Mod.Logger.Debug(
            $"Received SyncPkballTileRpc on {(Main.netMode == NetmodeID.Server ? "server" : "client")}"); // for player {packet._player}");
        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            TileUtils.TryGetTileEntityAs<BasePkballEntity>(_tileCoords.X, _tileCoords.Y, out var e);
            if (e == null)
                return;

            e.Item = _item;
            e.Disposable = _isDisposable;

            if (_isOpen)
                e.TryOpen();
            else
                e.Open = true;

            var player = Main.player[_player];
            player.itemRotation = 0;
            player.SetItemAnimation(18);

            SoundEngine.PlaySound(SoundID.Mech, _tileCoords.ToWorldCoordinates());
        }

        handled = true;
    }
}