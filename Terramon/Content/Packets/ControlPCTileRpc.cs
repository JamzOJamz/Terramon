using EasyPacketsLib;
using Terramon.Content.Tiles.Interactive;
using Terraria.Audio;
using Terraria.DataStructures;

namespace Terramon.Content.Packets;

public struct ControlPCTileRpc(int id, bool poweredOn) : IEasyPacket
{
    private int _id = id;
    private bool _poweredOn = poweredOn;

    public readonly void Serialise(BinaryWriter writer)
    {
        writer.Write7BitEncodedInt(_id);
        writer.Write(_poweredOn);
    }

    public void Deserialise(BinaryReader reader, in SenderInfo sender)
    {
        _id = reader.Read7BitEncodedInt();
        _poweredOn = reader.ReadBoolean();
    }

    public readonly void Receive(in SenderInfo sender, ref bool handled)
    {
        sender.Mod.Logger.Debug(
            $"Received ControlPCTileRpc on {(Main.netMode == NetmodeID.Server ? "server" : "client")} for player {sender.WhoAmI}");
        if (TileEntity.ByID.TryGetValue(_id, out var entity) && entity is PCTileEntity pc)
        {
            pc.PoweredOn = _poweredOn;

            // The server has determined that the PC should be turned off (player out of range)
            if (sender.WhoAmI == 255 && !_poweredOn)
            {
                pc.PoweredOn = false;
                pc.User = -1;
                var modPlayer = TerramonPlayer.LocalPlayer;
                if (modPlayer.ActivePCTileEntityID != _id) return;
                SoundEngine.PlaySound(SoundID.MenuClose);
                modPlayer.ActivePCTileEntityID = -1;
                return;
            }

            // Multiplayer client manually toggling the PC
            var player = Main.player[sender.WhoAmI];
            pc.User = _poweredOn ? sender.WhoAmI : -1;
            player.Terramon().ActivePCTileEntityID = _poweredOn ? _id : -1;
        }

        handled = true;
    }
}