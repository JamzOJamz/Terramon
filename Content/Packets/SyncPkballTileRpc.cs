using System.IO;
using EasyPacketsLib;
using Terramon.Content.Items.PokeBalls;
using Terramon.Helpers;
using Terraria.Audio;
using Terraria.DataStructures;


namespace Terramon.Content.Packets;

/// <summary>
///     A packet to be sent whenever the contents of a Poké Ball tile are modified.
/// </summary>
public readonly struct SyncPkballTileRpc(Item item, bool isOpen, bool isDisposable, byte player, Point16 tileCoords)
    : IEasyPacket<SyncPkballTileRpc>, IEasyPacketHandler<SyncPkballTileRpc>
{
    private readonly Item _item = item;
    private readonly bool _isOpen = isOpen;
    private readonly bool _isDisposable = isDisposable;

    private readonly byte _player = player;
    private readonly Point16 _tileCoords = tileCoords;

    public void Serialise(BinaryWriter writer)
    {
        _item.Serialize(writer, ItemSerializationContext.Syncing);
        writer.Write(_isOpen);
        writer.Write(_tileCoords.X);
        writer.Write(_tileCoords.Y);
    }

    public SyncPkballTileRpc Deserialise(BinaryReader reader, in SenderInfo sender)
    {
        var i = new Item();
        i.DeserializeFrom(reader, ItemSerializationContext.Syncing);
        return new SyncPkballTileRpc(i, reader.ReadBoolean(), reader.ReadBoolean(), reader.ReadByte(),
            new Point16(reader.ReadInt16(), reader.ReadInt16()));
    }

    public void Receive(in SyncPkballTileRpc packet, in SenderInfo sender, ref bool handled)
    {
        sender.Mod.Logger.Debug(
            $"Received SyncPkballTileRpc on {(Main.netMode == NetmodeID.Server ? "server" : "client")}"); // for player {packet._player}");
        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            TileUtils.TryGetTileEntityAs<BasePkballEntity>(packet._tileCoords.X, packet._tileCoords.Y, out var e);
            if (e == null)
                return;

            e.Item = packet._item;
            e.Disposable = packet._isDisposable;

            if (packet._isOpen)
                e.TryOpen();
            else
                e.Open = true;

            var player = Main.player[packet._player];
            player.itemRotation = 0;
            player.SetItemAnimation(18);

            SoundEngine.PlaySound(SoundID.Mech, packet._tileCoords.ToWorldCoordinates());
        }

        handled = true;
    }
}