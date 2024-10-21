using System.IO;
using EasyPacketsLib;
using Terramon.Content.Items.PokeBalls;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;

namespace Terramon.Content.Packets;

/// <summary>
///     A packet to be sent whenever a player places a Poké Ball tile in the world by using
///     <see cref="Player.altFunctionUse" /> with a <see cref="BasePkballItem" />.
///     When received by other multiplayer clients, the sending player's use animation and tile placement sound are played.
/// </summary>
public readonly struct PlacedPkballTileRpc(byte player, Point16 tileCoords)
    : IEasyPacket<PlacedPkballTileRpc>, IEasyPacketHandler<PlacedPkballTileRpc>
{
    private readonly byte _player = player;
    private readonly Point16 _tileCoords = tileCoords;

    public void Serialise(BinaryWriter writer)
    {
        writer.Write(_player);
        writer.Write(_tileCoords.X);
        writer.Write(_tileCoords.Y);
    }

    public PlacedPkballTileRpc Deserialise(BinaryReader reader, in SenderInfo sender)
    {
        return new PlacedPkballTileRpc(reader.ReadByte(), new Point16(reader.ReadInt16(), reader.ReadInt16()));
    }

    public void Receive(in PlacedPkballTileRpc packet, in SenderInfo sender, ref bool handled)
    {
        sender.Mod.Logger.Debug(
            $"Received PlacedPkballTileRpc on {(Main.netMode == NetmodeID.Server ? "server" : "client")} for player {packet._player}");
        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            var player = Main.player[packet._player];
            player.itemRotation = 0;
            player.SetItemAnimation(15);
            SoundEngine.PlaySound(SoundID.Dig, packet._tileCoords.ToWorldCoordinates());
        }

        handled = true;
    }
}