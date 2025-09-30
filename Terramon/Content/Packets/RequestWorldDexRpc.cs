using EasyPacketsLib;

namespace Terramon.Content.Packets;

/// <summary>
///     A packet for requesting a full sync of the World Dex.
/// </summary>
public readonly struct RequestWorldDexRpc
    : IEasyPacket<RequestWorldDexRpc>, IEasyPacketHandler<RequestWorldDexRpc>
{
    public void Serialise(BinaryWriter writer) // This is empty because the packet has no data
    {
    }

    public RequestWorldDexRpc Deserialise(BinaryReader reader, in SenderInfo sender)
    {
        return new RequestWorldDexRpc();
    }

    public void Receive(in RequestWorldDexRpc packet, in SenderInfo sender, ref bool handled)
    {
        if (Main.netMode != NetmodeID.Server) return;
        sender.Mod.Logger.Debug(
            $"Received RequestWorldDexRpc on server from player {sender.WhoAmI}");
        var worldDex = TerramonWorld.GetWorldDex();
        var entries = worldDex.Entries
            .Select(entry => ((ushort)entry.Key, entry.Value))
            .ToArray();
        sender.Mod.SendPacket(new UpdateWorldDexRpc(entries), sender.WhoAmI);
        handled = true;
    }
}