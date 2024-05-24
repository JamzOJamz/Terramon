/*
 *  EasyPacketsLib.cs
 *  DavidFDev
 */

using System;
using System.IO;
using Terramon.Core.Networking.Internals;
using Terraria.ID;

namespace Terramon.Core.Networking;

public sealed class EasyPacketsLib
{
    /// <summary>Handles packets sent from your mod. Call this in <see cref="Mod.HandlePacket(BinaryReader, int)" />.</summary>
    public static void HandlePacket(BinaryReader reader, int whoAmI)
    {
        // BUG: Getting 256 for some reason; might be a 1.4.4 issue
        whoAmI = Math.Clamp(whoAmI, 0, 255);

        var modNetId = ModNet.NetModCount < 256 ? reader.ReadByte() : reader.ReadInt16();
        var packetNetId = EasyPacketLoader.NetEasyPacketCount < 256 ? reader.ReadByte() : reader.ReadUInt16();
        var flags = (BitsByte)reader.ReadByte();
        var forward = flags[0];
        var expected = flags[1];

        // Get the mod that sent the packet using its net id
        var sentByMod = ModNet.GetMod(modNetId);

        // Check if the mod exists and is synced
        if (sentByMod is not { IsNetSynced: true })
        {
            // Don't throw if it's okay that the mod doesn't exist
            // This means the mod on the server has Side=NoSync and this client doesn't have the mod
            if (Main.netMode == NetmodeID.MultiplayerClient && !expected) return;

            throw new Exception(
                $"HandlePacket received an invalid mod Net ID: {modNetId}. Could not find a mod with that Net ID.");
        }

        // Get the easy packet mod type using its net id
        var packet = EasyPacketLoader.GetPacket(packetNetId);
        if (packet == null)
            throw new Exception(
                $"HandlePacket received an invalid easy mod packet with Net ID: {packetNetId}. Could not find an easy mod packet with that Net ID.");

        // Special case if the packet was forwarded
        byte toClient = 255;
        byte ignoreClient = 255;
        if (forward)
        {
            if (Main.netMode == NetmodeID.Server)
            {
                // Server knows who to forward the packet to
                toClient = reader.ReadByte();
                ignoreClient = reader.ReadByte();
            }
            else
            {
                // Client knows who originally forwarded the packet
                whoAmI = reader.ReadByte();
            }
        }

        // Let the easy packet mod type receive the packet
        packet.ReceivePacket(reader, new SenderInfo(sentByMod, (byte)whoAmI, flags, toClient, ignoreClient));
    }
}