using System.IO;
using EasyPacketsLib;
using Terramon.Content.GUI;
using Terramon.Core.Loaders.UILoading;


namespace Terramon.Content.Packets;

/// <summary>
///     A packet for synchronizing any updates to the World Dex with all clients.
/// </summary>
public readonly struct UpdateWorldDexRpc((ushort, PokedexEntry)[] entries)
    : IEasyPacket<UpdateWorldDexRpc>, IEasyPacketHandler<UpdateWorldDexRpc>
{
    private readonly (ushort, PokedexEntry)[] _entries = entries;

    public void Serialise(BinaryWriter writer)
    {
        writer.Write7BitEncodedInt(_entries.Length);
        foreach (var (id, entry) in _entries)
        {
            writer.Write7BitEncodedInt(id);
            writer.Write((byte)entry.Status);
            writer.Write(entry.LastUpdatedBy ?? string.Empty);
        }
    }

    public UpdateWorldDexRpc Deserialise(BinaryReader reader, in SenderInfo sender)
    {
        var readEntries = new (ushort, PokedexEntry)[reader.Read7BitEncodedInt()];
        for (var i = 0; i < readEntries.Length; i++)
        {
            var id = (ushort)reader.Read7BitEncodedInt();
            var status = (PokedexEntryStatus)reader.ReadByte();
            var lastUpdatedBy = reader.ReadString();
            readEntries[i] = (id, new PokedexEntry(status, lastUpdatedBy));
        }

        return new UpdateWorldDexRpc(readEntries);
    }

    public void Receive(in UpdateWorldDexRpc packet, in SenderInfo sender, ref bool handled)
    {
        sender.Mod.Logger.Debug(
            $"Received UpdateWorldDexRpc on {(Main.netMode == NetmodeID.Server ? "server" : "client")} from player {sender.WhoAmI}");
        var fromServer = sender.WhoAmI == 255; // 255 is the server
        var hubActive = HubUI.Active;
        foreach (var (id, entry) in packet._entries)
        {
            TerramonWorld.UpdateWorldDex(id, entry.Status,
                fromServer ? entry.LastUpdatedBy : Main.player[sender.WhoAmI].name, netSend: false);
            if (hubActive && !fromServer)
                UILoader.GetUIState<HubUI>().RefreshPokedex(id);
        }

        if (hubActive && fromServer)
            UILoader.GetUIState<HubUI>().RefreshPokedex();

        handled = true;
    }
}