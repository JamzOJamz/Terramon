using EasyPacketsLib;

namespace Terramon.Utilities.Terraria;

/// <summary>
///     Extension methods for <see cref="IEasyPacket" /> to provide debug logging functionality.
/// </summary>
public static class PacketExtensions
{
    private static void DebugLog(this IEasyPacket packet, string pre, string post = null)
    {
        var msg = (Main.dedServ ? "Server: " : "Client: ") + pre + $" {packet.GetType().Name} " + post;
        ModContent.GetInstance<Terramon>().Logger.Debug(msg);
        if (Main.dedServ)
            Console.WriteLine(msg);
    }

    public static void ReceiveLog(this IEasyPacket packet, string post = null)
    {
        DebugLog(packet, "Received", post);
    }

    public static void SendLog(this IEasyPacket packet, string post = null)
    {
        DebugLog(packet, "Sent", post);
    }
}