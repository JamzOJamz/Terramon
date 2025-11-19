using EasyPacketsLib;
using Terramon.Content.Items.PokeBalls;
using Terraria.Audio;
using Terraria.DataStructures;

namespace Terramon.Content.Packets;

/// <summary>
///     A packet to be sent whenever a player places a Poké Ball tile in the world by using
///     <see cref="Player.altFunctionUse" /> with a <see cref="BasePkballItem" />.
///     When received by other multiplayer clients, the sending player's use animation and tile placement sound are played.
/// </summary>
public struct PlacedPkballTileRpc(Point16 tileCoords) : IEasyPacket
{
    private Point16 _tileCoords = tileCoords;

    public readonly void Serialise(BinaryWriter writer)
    {
        writer.Write(_tileCoords.X);
        writer.Write(_tileCoords.Y);
    }

    public void Deserialise(BinaryReader reader, in SenderInfo sender)
    {
        _tileCoords = new Point16(reader.ReadUInt16(), reader.ReadUInt16());
    }

    public readonly void Receive(in SenderInfo sender, ref bool handled)
    {
        sender.Mod.Logger.Debug(
            $"Received PlacedPkballTileRpc on {(Main.netMode == NetmodeID.Server ? "server" : "client")} for player {sender.WhoAmI}");
        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            var player = Main.player[sender.WhoAmI];
            player.itemRotation = 0;
            player.SetItemAnimation(15);
            SoundEngine.PlaySound(SoundID.Dig, _tileCoords.ToWorldCoordinates());
        }

        handled = true;
    }
}