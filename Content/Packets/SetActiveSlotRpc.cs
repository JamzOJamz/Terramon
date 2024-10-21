using System;
using System.IO;
using EasyPacketsLib;
using Terraria.ID;

namespace Terramon.Content.Packets;

/// <summary>
///     A packet for synchronizing a player's active slot with all clients.
/// </summary>
[Obsolete("This packet is no longer used. Use SetActivePokemonRpc instead.")]
public readonly struct SetActiveSlotRpc(byte player, int activeSlot)
    : IEasyPacket<SetActiveSlotRpc>, IEasyPacketHandler<SetActiveSlotRpc>
{
    private readonly byte _player = player;
    private readonly int _activeSlot = activeSlot;

    public void Serialise(BinaryWriter writer)
    {
        writer.Write(_player);
        writer.Write((byte)(_activeSlot + 1));
    }

    public SetActiveSlotRpc Deserialise(BinaryReader reader, in SenderInfo sender)
    {
        return new SetActiveSlotRpc(reader.ReadByte(), reader.ReadByte() - 1);
    }

    public void Receive(in SetActiveSlotRpc packet, in SenderInfo sender, ref bool handled)
    {
        sender.Mod.Logger.Debug(
            $"Received SetActiveSlotRpc on {(Main.netMode == NetmodeID.Server ? "server" : "client")} for player {packet._player} {packet._activeSlot}");
        var player = Main.player[packet._player].GetModPlayer<TerramonPlayer>();
        player.ActiveSlot = packet._activeSlot;
        handled = true;
    }
}