using System.IO;
using EasyPacketsLib;
using Terramon.Content.GUI;
using Terramon.Core.Loaders.UILoading;
using Terraria.ID;

namespace Terramon.Content.Packets;

/// <summary>
///     A packet for synchronizing any updates to the World Dex with all clients.
/// </summary>
public readonly struct UpdateWorldDexRpc(byte player, ushort id, PokedexEntryStatus status)
    : IEasyPacket<UpdateWorldDexRpc>, IEasyPacketHandler<UpdateWorldDexRpc>
{
    private readonly byte _player = player;
    private readonly ushort _id = id;
    private readonly PokedexEntryStatus _status = status;

    public void Serialise(BinaryWriter writer)
    {
        writer.Write(_player);
        writer.Write(_id);
        writer.Write((byte)_status);
    }

    public UpdateWorldDexRpc Deserialise(BinaryReader reader, in SenderInfo sender)
    {
        return new UpdateWorldDexRpc(reader.ReadByte(), reader.ReadUInt16(), (PokedexEntryStatus)reader.ReadByte());
    }

    public void Receive(in UpdateWorldDexRpc packet, in SenderInfo sender, ref bool handled)
    {
        sender.Mod.Logger.Debug(
            $"Received UpdateWorldDexRpc on {(Main.netMode == NetmodeID.Server ? "server" : "client")} for player {packet._player}");
        TerramonWorld.UpdateWorldDex(packet._id, packet._status, Main.player[packet._player].name, netSend: false);
        if (HubUI.Active) UILoader.GetUIState<HubUI>().RefreshPokedex(_id);
        handled = true;
    }
}