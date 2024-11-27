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
            var player = Main.player[sender.WhoAmI];
            pc.PoweredOn = packet._poweredOn;
            pc.User = packet._poweredOn ? sender.WhoAmI : -1;
            // Play sound if the PC is being turned off by the server and this player is using it
            if (Main.netMode == NetmodeID.MultiplayerClient && !packet._poweredOn &&
                TerramonPlayer.LocalPlayer.ActivePCTileEntityID == packet._id)
                SoundEngine.PlaySound(SoundID.MenuClose);
            player.GetModPlayer<TerramonPlayer>().ActivePCTileEntityID = packet._poweredOn ? packet._id : -1;
        }

        handled = true;
    }
}