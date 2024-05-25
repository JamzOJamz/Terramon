using System.IO;
using Terramon.Core.Networking;
using Terraria.ID;

namespace Terramon.Content.Packets;

public readonly struct ActiveSlotUpdatedPacket(byte player, int activeSlot)
    : IEasyPacket<ActiveSlotUpdatedPacket>, IEasyPacketHandler<ActiveSlotUpdatedPacket>
{
    private readonly byte _player = player;
    private readonly int _activeSlot = activeSlot;

    public void Serialize(BinaryWriter writer)
    {
        writer.Write(_player);
        writer.Write((byte)(_activeSlot + 1));
    }

    public ActiveSlotUpdatedPacket Deserialize(BinaryReader reader, in SenderInfo sender)
    {
        return new ActiveSlotUpdatedPacket(reader.ReadByte(), reader.ReadByte() - 1);
    }

    public void Receive(in ActiveSlotUpdatedPacket packet, in SenderInfo sender, ref bool handled)
    {
        /*sender.Mod.Logger.Debug(
            $"Received ActiveSlotPacket on {(Main.netMode == NetmodeID.Server ? "server" : "client")} for player {packet._player}");*/
        var player = Main.player[packet._player].GetModPlayer<TerramonPlayer>();
        player.ActiveSlot = packet._activeSlot;
        if (Main.netMode == NetmodeID.Server && sender.Forwarded)
            // Forward the changes to the other clients
            sender.Mod.SendPacket(packet, ignoreClient: sender.WhoAmI);
        handled = true;
    }
}