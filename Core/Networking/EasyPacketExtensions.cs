/*
 *  EasyPacketExtensions.cs
 *  DavidFDev
 */

using System;
using System.IO;
using Terramon.Core.Networking.Internals;
using Terraria.ID;

namespace Terramon.Core.Networking;

/// <summary>
///     Extension methods for sending easy packets and handling how they are received.
/// </summary>
public static class EasyPacketExtensions
{
    #region Static Methods

    /// <summary>
    ///     Send an easy packet.
    ///     If a packet is received but is unhandled, an error is raised.
    /// </summary>
    /// <example>
    ///     <code>Mod.SendPacket(new ExamplePacket(10, 20));</code>
    /// </example>
    /// <param name="mod">Mod sending the packet.</param>
    /// <param name="packet">Packet instance that implements <see cref="IEasyPacket{T}" />.</param>
    /// <param name="toClient">If non-negative, this packet will only be sent to the specified client.</param>
    /// <param name="ignoreClient">If non-negative, this packet will not be sent to the specified client.</param>
    /// <param name="forward">If sending from a client, this packet will be forwarded to other clients through the server.</param>
    /// <typeparam name="T">Type that implements <see cref="IEasyPacket{T}" />.</typeparam>
    public static void SendPacket<T>(this Mod mod, in T packet, int toClient = -1, int ignoreClient = -1,
        bool forward = false) where T : struct, IEasyPacket<T>
    {
        forward = forward && Main.netMode == NetmodeID.MultiplayerClient;
        SendPacket_Internal(mod, in packet, (byte)Main.myPlayer, toClient, ignoreClient, forward);
    }

    /// <summary>
    ///     An easy packet handler is invoked when the packet is received.
    ///     If a packet is received but is unhandled, an error is raised.
    /// </summary>
    /// <example>
    ///     <code>
    ///         public class ExamplePacketHandler : ModSystem
    ///         {
    ///             public override void Load()
    ///             {
    ///                 Mod.AddPacketHandler&lt;ExamplePacket&gt;(OnExamplePacketReceived);
    ///             }
    /// 
    ///             public override void Unload()
    ///             {
    ///                 Mod.RemovePacketHandler&lt;ExamplePacket&gt;(OnExamplePacketReceived);
    ///             }
    /// 
    ///             private void OnExamplePacketReceived(in ExamplePacket packet, in SenderInfo sender, ref bool handled)
    ///             {
    ///                 Mod.Logger.Debug($"X: {packet.X}, Y: {packet.Y}");
    ///                 handled = true;
    ///             }
    ///         }
    ///     </code>
    /// </example>
    /// <param name="mod">Mod handling the packet.</param>
    /// <param name="handler">Method handling the packet.</param>
    /// <typeparam name="T">Type that implements <see cref="IEasyPacket{T}" />.</typeparam>
    public static void AddPacketHandler<T>(this Mod mod, HandleEasyPacketDelegate<T> handler)
        where T : struct, IEasyPacket<T>
    {
        EasyPacketLoader.AddHandler(handler);
    }

    /// <summary>
    ///     An easy packet handler is invoked when the packet is received.
    ///     If a packet is received but is unhandled, an error is raised.
    /// </summary>
    /// <example>
    ///     <code>
    ///         public class ExamplePacketHandler : ModSystem
    ///         {
    ///             public override void Load()
    ///             {
    ///                 Mod.AddPacketHandler&lt;ExamplePacket&gt;(OnExamplePacketReceived);
    ///             }
    /// 
    ///             public override void Unload()
    ///             {
    ///                 Mod.RemovePacketHandler&lt;ExamplePacket&gt;(OnExamplePacketReceived);
    ///             }
    /// 
    ///             private void OnExamplePacketReceived(in ExamplePacket packet, in SenderInfo sender, ref bool handled)
    ///             {
    ///                 Mod.Logger.Debug($"X: {packet.X}, Y: {packet.Y}");
    ///                 handled = true;
    ///             }
    ///         }
    ///     </code>
    /// </example>
    /// <param name="mod">Mod handling the packet.</param>
    /// <param name="handler">Method handling the packet.</param>
    /// <typeparam name="T">Type that implements <see cref="IEasyPacket{T}" />.</typeparam>
    public static void RemovePacketHandler<T>(this Mod mod, HandleEasyPacketDelegate<T> handler)
        where T : struct, IEasyPacket<T>
    {
        EasyPacketLoader.RemoveHandler(handler);
    }

    /// <summary>
    ///     Serialise an easy packet.
    /// </summary>
    /// <param name="writer"></param>
    /// <param name="packet">Packet instance that implements <see cref="IEasyPacket{T}" />.</param>
    /// <typeparam name="T">Type that implements <see cref="IEasyPacket{T}" />.</typeparam>
    public static void Write<T>(this BinaryWriter writer, in T packet) where T : struct, IEasyPacket<T>
    {
        packet.Serialize(writer);
    }

    /// <summary>
    ///     Deserialise an easy packet.
    /// </summary>
    /// <param name="reader"></param>
    /// <param name="sender">Information regarding the sender of the packet.</param>
    /// <typeparam name="T">Type that implements <see cref="IEasyPacket{T}" />.</typeparam>
    /// <returns>Packet instance that implements <see cref="IEasyPacket{T}" />.</returns>
    public static T Read<T>(this BinaryReader reader, in SenderInfo sender) where T : struct, IEasyPacket<T>
    {
        return new T().Deserialize(reader, in sender);
    }

    internal static void SendPacket_Internal<T>(this Mod mod, in T packet, byte whoAmI, int toClient, int ignoreClient,
        bool forward) where T : struct, IEasyPacket<T>
    {
        if (Main.netMode == NetmodeID.SinglePlayer) throw new Exception("SendPacket called in single-player.");

        if (!EasyPacketLoader.IsRegistered<T>())
            throw new Exception($"SendPacket called on an unregistered type: {typeof(T).Name}.");

        // Check if the mod is synced
        if (!mod.IsNetSynced)
        {
            // Client's IsNetSynced is true if Side=Both, but if Side=NoSync, true if the server has the mod
            // Server's IsNetSynced is true if Side=Both or Side=NoSync
            if (Main.netMode == NetmodeID.MultiplayerClient && mod.Side == ModSide.NoSync) return;

            throw new Exception("SendPacket called on an un-synced mod.");
        }

        // Important that the packet is sent by this mod, so that it is received correctly
        var modPacket = Terramon.Instance.GetPacket();

        // Mod's net id is synced across server and clients
        var modNetId = mod.NetID;
        if (ModNet.NetModCount < 256)
            modPacket.Write((byte)modNetId);
        else
            modPacket.Write(modNetId);

        // Easy packet type's net id is synced across server and clients
        var packetNetId = EasyPacketLoader.GetNetId<T>();
        if (EasyPacketLoader.NetEasyPacketCount < 256)
            modPacket.Write((byte)packetNetId);
        else
            modPacket.Write(packetNetId);

        // Write any additional flags
        var expected = mod.Side == ModSide.Both;
        var flags = new BitsByte { [0] = forward, [1] = expected };
        modPacket.Write(flags);

        // Special case if the packet is to be forwarded
        if (forward)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                // Send this so that the server knows who to forward the packet to
                modPacket.Write(toClient < 0 ? (byte)255 : (byte)toClient);
                modPacket.Write(ignoreClient < 0 ? (byte)255 : (byte)ignoreClient);
            }
            else
            {
                // Send this so that the receiving client knows who originally forwarded the packet
                modPacket.Write(whoAmI);
            }
        }

        // Let the easy packet serialise itself
        packet.Serialize(modPacket);

        // Finally, send the packet
        modPacket.Send(toClient, ignoreClient);
    }

    /// <summary>
    ///     Work in progress. Do not use.
    /// </summary>
    internal static void SendPacket_SplitSupport<T>(this Mod mod, in T packet, byte whoAmI, int toClient,
        int ignoreClient, bool forward) where T : struct, IEasyPacket<T>
    {
        if (Main.netMode == NetmodeID.SinglePlayer) throw new Exception("SendPacket called in single-player.");

        if (!EasyPacketLoader.IsRegistered<T>())
            throw new Exception($"SendPacket called on an unregistered type: {typeof(T).Name}.");

        // Let the easy packet serialise itself, and retrieve the bytes to be sent in the packet body
        byte[] bodyBytes;
        using (var stream = new MemoryStream())
        {
            using (var writer = new BinaryWriter(stream))
            {
                packet.Serialize(writer);
            }

            stream.Flush();
            bodyBytes = stream.GetBuffer();
        }

        if (bodyBytes.Length == 0) throw new Exception($"SendPacket called on an empty packet: {typeof(T).Name}.");

        // Determine the total size of the packet, in case it's over the max
        var totalSize = bodyBytes.Length;

        // Mod's net ID
        if (ModNet.NetModCount < 256)
            totalSize += sizeof(byte);
        else
            totalSize += sizeof(short);

        // Easy packet's net ID
        if (EasyPacketLoader.NetEasyPacketCount < 256)
            totalSize += sizeof(byte);
        else
            totalSize += sizeof(ushort);

        totalSize += sizeof(byte); // flags

        // Special case if the packet is to be forwarded
        if (forward)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                totalSize += sizeof(byte); // toClient
                totalSize += sizeof(byte); // ignoreClient
            }
            else
            {
                totalSize += sizeof(byte); // whoAmI
            }
        }

        // TODO: Take into account bytes added by ModPacket
        totalSize += sizeof(short); // mod.NetID

        // Check if the packet data needs to be separated into multiple packets
        const int maxSize = int.MaxValue; // TODO
        var split = totalSize >= maxSize;
        if (split) totalSize += sizeof(int); // length

        for (var i = 0; i < bodyBytes.Length;)
        {
            // Important that the packet is sent by this mod, so that it is received correctly
            var modPacket = Terramon.Instance.GetPacket();

            // Mod's net id is synced across server and clients
            var modNetId = mod.NetID;
            if (ModNet.NetModCount < 256)
                modPacket.Write((byte)modNetId);
            else
                modPacket.Write(modNetId);

            // Easy packet type's net id is synced across server and clients
            var packetNetId = EasyPacketLoader.GetNetId<T>();
            if (EasyPacketLoader.NetEasyPacketCount < 256)
                modPacket.Write((byte)packetNetId);
            else
                modPacket.Write(packetNetId);

            // Write any additional flags
            var flags = new BitsByte { [0] = forward, [1] = split };
            modPacket.Write(flags);

            // Special case if the packet is to be forwarded
            if (forward)
            {
                if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    // Send this so that the server knows who to forward the packet to
                    modPacket.Write(toClient < 0 ? (byte)255 : (byte)toClient);
                    modPacket.Write(ignoreClient < 0 ? (byte)255 : (byte)ignoreClient);
                }
                else
                {
                    // Send this so that the receiving client knows who originally forwarded the packet
                    modPacket.Write(whoAmI);
                }
            }

            // TODO: Determine length properly. This logic is wrong.
            var length = Math.Min(bodyBytes.Length - i, maxSize - (totalSize - bodyBytes.Length));
            if (split) modPacket.Write(length);

            // Write the packet data
            modPacket.Write(new ReadOnlySpan<byte>(bodyBytes, i, i + length));
            i += length;

            // Finally, send the packet
            modPacket.Send(toClient, ignoreClient);
        }
    }

    #endregion
}