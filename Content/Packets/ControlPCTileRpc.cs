using EasyPacketsLib;
using Terramon.Content.Tiles.Interactive;
using Terraria.Audio;
using Terraria.DataStructures;

namespace Terramon.Content.Packets;

public readonly struct ControlPCTileRpc(int id, bool poweredOn)
    : IEasyPacket<ControlPCTileRpc>, IEasyPacketHandler<ControlPCTileRpc>
{
    private readonly int _id = id;
    private readonly bool _poweredOn = poweredOn;

    public void Serialise(BinaryWriter writer)
    {
        writer.Write7BitEncodedInt(_id);
        writer.Write(_poweredOn);
    }

    public ControlPCTileRpc Deserialise(BinaryReader reader, in SenderInfo sender)
    {
        return new ControlPCTileRpc(reader.Read7BitEncodedInt(), reader.ReadBoolean());
    }

    public void Receive(in ControlPCTileRpc packet, in SenderInfo sender, ref bool handled)
    {
        sender.Mod.Logger.Debug(
            $"Received ControlPCTileRpc on {(Main.netMode == NetmodeID.Server ? "server" : "client")} for player {sender.WhoAmI}");
        if (TileEntity.ByID.TryGetValue(packet._id, out var entity) && entity is PCTileEntity pc)
        {
            pc.PoweredOn = packet._poweredOn;

            // The server has determined that the PC should be turned off (player out of range)
            if (sender.WhoAmI == 255 && !packet._poweredOn)
            {
                pc.PoweredOn = false;
                pc.User = -1;
                var modPlayer = TerramonPlayer.LocalPlayer;
                if (modPlayer.ActivePCTileEntityID != packet._id) return;
                SoundEngine.PlaySound(SoundID.MenuClose);
                modPlayer.ActivePCTileEntityID = -1;
                return;
            }

            // Multiplayer client manually toggling the PC
            var player = Main.player[sender.WhoAmI];
            pc.User = packet._poweredOn ? sender.WhoAmI : -1;
            player.GetModPlayer<TerramonPlayer>().ActivePCTileEntityID = packet._poweredOn ? packet._id : -1;
        }

        handled = true;
    }
}