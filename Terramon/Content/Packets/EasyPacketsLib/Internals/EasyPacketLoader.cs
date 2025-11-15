/*
 *  EasyPacketLoader.cs
 *  DavidFDev
 */

using System.Reflection;
using Terraria.ModLoader.Core;

namespace Terramon.Content.Packets;

// ReSharper disable once ClassNeverInstantiated.Global
internal static class EasyPacketLoader
{
    #region Static Fields and Constants

    private static readonly Dictionary<ushort, IEasyPacket> PacketByNetId = [];
    private static readonly Dictionary<IntPtr, ushort> NetIdByPtr = [];

    #endregion

    #region Static Methods

    /// <summary>
    ///     Get an easy packet type by its registered net ID.
    /// </summary>
    public static IEasyPacket GetPacket(ushort netId)
    {
        return PacketByNetId.GetValueOrDefault(netId);
    }

    /// <summary>
    ///     Get the registered net ID of an easy packet.
    /// </summary>
    public static ushort GetNetId(in IEasyPacket packet)
    {
        return NetIdByPtr.GetValueOrDefault(packet.GetType().TypeHandle.Value);
    }

    public static void RegisterMod()
    {
        var interfaceName = typeof(IEasyPacket).FullName;
        // Register easy packets
        foreach (var type in Terramon.Instance.Code.GetTypes()
                     .Where(t => t.GetInterface(interfaceName) != null))
        {
            RegisterPacket(type);
        }
    }

    /// <summary>
    ///     Clear static references when the mod is unloaded.
    /// </summary>
    public static void ClearStatics()
    {
        PacketByNetId.Clear();
        NetIdByPtr.Clear();
        NetEasyPacketCount = 0;
    }

    /// <summary>
    ///     Register an easy packet.
    /// </summary>
    /// <param name="type">Type that implements <see cref="IEasyPacket" />.</param>
    private static void RegisterPacket(Type type)
    {
        // Create a new default instance of the easy packet type
        // https://stackoverflow.com/a/1151470/20943906
        var instance = (IEasyPacket)Activator.CreateInstance(type) ??
            throw new Exception($"Failed to register easy packet type: {type.Name}.");

        // Register the created instance, assigning a unique net id
        var netId = NetEasyPacketCount++;
        PacketByNetId.Add(netId, instance);
        NetIdByPtr.Add(type.TypeHandle.Value, netId);

        Terramon.Instance.Logger.Debug($"Registered {type.Name} (ID: {netId})");
    }

    #endregion

    #region Properties

    /// <summary>
    ///     Total number of easy packets registered across all registered mods.
    /// </summary>
    public static ushort NetEasyPacketCount { get; private set; }

    #endregion
}