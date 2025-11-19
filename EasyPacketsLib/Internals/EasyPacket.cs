/*
 *  EasyPacket.cs
 *  DavidFDev
 */

using System.Reflection;
using MonoMod.Cil;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace EasyPacketsLib.Internals;

/// <summary>
///     Used to receive an incoming packet and detour it to the struct.
/// </summary>
public static class EasyPacket
{
// #if DEBUG
    public static Type lastProcessedPacket;

    static EasyPacket()
    {
        var handlePacketMethod =
            typeof(ModNet).GetMethod("HandleModPacket", BindingFlags.Static | BindingFlags.NonPublic);
        MonoModHooks.Modify(handlePacketMethod, static il =>
        {
            var c = new ILCursor(il);
            c.GotoNext(MoveType.AfterLabel, i => i.MatchLdcI4(52));
            c.EmitDelegate(() =>
            {
                if (lastProcessedPacket != null)
                    EasyPacketLoader.RegisteredMod.Logger.Error(
                        $"Read underflow for {(lastProcessedPacket.IsValueType ? "packet" : "message")} of type {lastProcessedPacket.Name}");
            });
        });
    }
// #endif

    #region Methods

    public static void ReceivePacket(in IEasyPacket packet, BinaryReader reader, in SenderInfo sender)
    {
        packet.Deserialise(reader, in sender);

        // Check if the packet should be automatically forwarded to clients
        if (Main.netMode == NetmodeID.Server && sender.Forwarded)
        {
            EasyPacketExtensions.SendPacket_Internal(sender.Mod, in packet, sender.WhoAmI, sender.ToClient,
                sender.IgnoreClient, true);
        }

        // Handle the received packet
        var handled = false;
        packet.Receive(in sender, ref handled);

        if (!handled)
        {
            sender.Mod.Logger.Error($"Unhandled packet: {packet.GetType().Name}.");
        }
    }

    #endregion
}