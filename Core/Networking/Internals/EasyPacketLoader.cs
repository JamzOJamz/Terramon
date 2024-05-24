/*
 *  EasyPacketLoader.cs
 *  DavidFDev
*/

using System;
using System.Collections.Generic;
using System.Linq;
using Terraria.ModLoader.Core;

namespace Terramon.Core.Networking.Internals;

// ReSharper disable once ClassNeverInstantiated.Global
internal sealed class EasyPacketLoader : ModSystem
{
    #region Static Fields and Constants

    private static readonly Dictionary<ushort, IEasyPacket> PacketByNetId = new();
    private static readonly Dictionary<IntPtr, ushort> NetIdByPtr = new();
    private static readonly Dictionary<IntPtr, MulticastDelegate> HandlerByPtr = new();
    private static readonly string EasyPacketFullName;
    private static readonly string EasyPacketHandlerFullName;

    #endregion

    #region Static Methods

    /// <summary>
    ///     Check if an easy packet is registered.
    /// </summary>
    internal static bool IsRegistered<T>() where T : struct, IEasyPacket<T>
    {
        return NetIdByPtr.ContainsKey(typeof(T).TypeHandle.Value);
    }

    /// <summary>
    ///     Get an easy packet type by its registered net ID.
    /// </summary>
    internal static IEasyPacket GetPacket(ushort netId)
    {
        return PacketByNetId.GetValueOrDefault(netId);
    }

    /// <summary>
    ///     Get the registered net ID of an easy packet.
    /// </summary>
    internal static ushort GetNetId<T>() where T : struct, IEasyPacket<T>
    {
        return NetIdByPtr.GetValueOrDefault(typeof(T).TypeHandle.Value);
    }

    /// <summary>
    ///     Add an easy packet handler.
    /// </summary>
    internal static void AddHandler<T>(HandleEasyPacketDelegate<T> handler) where T : struct, IEasyPacket<T>
    {
        var ptr = typeof(T).TypeHandle.Value;
        if (!HandlerByPtr.ContainsKey(ptr))
        {
            HandlerByPtr.Add(ptr, null);
        }

        HandlerByPtr[ptr] = (MulticastDelegate)Delegate.Combine(HandlerByPtr[ptr], handler);
    }

    /// <summary>
    ///     Remove an easy packet handler.
    /// </summary>
    internal static void RemoveHandler<T>(HandleEasyPacketDelegate<T> handler) where T : struct, IEasyPacket<T>
    {
        var ptr = typeof(T).TypeHandle.Value;
        if (!HandlerByPtr.ContainsKey(ptr))
        {
            return;
        }

        HandlerByPtr[ptr] = (MulticastDelegate)Delegate.Remove(HandlerByPtr[ptr], handler);
    }

    /// <summary>
    ///     Get the handler for an easy packet.
    /// </summary>
    internal static HandleEasyPacketDelegate<T> GetHandler<T>() where T : struct, IEasyPacket<T>
    {
        return HandlerByPtr.GetValueOrDefault(typeof(T).TypeHandle.Value) as HandleEasyPacketDelegate<T>;
    }

    #endregion

    #region Constructors

    static EasyPacketLoader()
    {
        // Cache the full interface type definitions to be used during loading
        EasyPacketFullName = typeof(IEasyPacket<>).GetGenericTypeDefinition().FullName;
        EasyPacketHandlerFullName = typeof(IEasyPacketHandler<>).GetGenericTypeDefinition().FullName;
    }

    #endregion

    #region Properties

    /// <summary>
    ///     Total number of easy packets registered across all mods.
    /// </summary>
    public static ushort NetEasyPacketCount { get; private set; }

    #endregion

    #region Methods

    public override void Load()
    {
        // Register easy packets and handlers, including from other mods
        // Order must be the same for all users, so that net ids are synced
        // TODO: Including Side=NoSync mods will cause ids to not be the same for all users; can the mod and type name be hashed and used as the id?
        foreach (var mod in ModLoader.Mods
                     .Where(m => m.Side is ModSide.Both /*or ModSide.NoSync*/)
                     .OrderBy(m => m.Name, StringComparer.InvariantCulture))
        {
            var loadableTypes = AssemblyManager.GetLoadableTypes(mod.Code);
            foreach (var type in loadableTypes
                         .Where(t => t.IsValueType && !t.ContainsGenericParameters && t.GetInterface(EasyPacketFullName) != null)
                         .OrderBy(t => t.FullName, StringComparer.InvariantCulture))
            {
                RegisterPacket(mod, type);
            }

            foreach (var type in loadableTypes
                         .Where(t => t.IsValueType && !t.ContainsGenericParameters && t.GetInterface(EasyPacketHandlerFullName) != null)
                         .OrderBy(t => t.FullName, StringComparer.InvariantCulture))
            {
                RegisterHandler(mod, type);
            }
        }
    }

    public override void Unload()
    {
        // Ensure the static fields are cleared
        PacketByNetId.Clear();
        NetIdByPtr.Clear();
        HandlerByPtr.Clear();
        NetEasyPacketCount = 0;
    }

    /// <summary>
    ///     Register an easy packet.
    /// </summary>
    /// <param name="mod">Mod that defined the easy packet.</param>
    /// <param name="type">Type that implements <see cref="IEasyPacket{T}" />.</param>
    private void RegisterPacket(Mod mod, Type type)
    {
        // Ensure the interface generic argument matches the type implementing it
        // This is not enforced by the code, so we must check it here and explain why in detail
        var genericArg = type.GetInterface(EasyPacketFullName)!.GetGenericArguments()[0];
        if (genericArg != type)
        {
            throw new Exception($"Failed to register easy packet type: {type.Name}." +
                                $"\nActual:\n   struct {type.Name} : IEasyPacket<[c/{Color.Red.Hex3()}:{genericArg.Name}]>" +
                                $"\nExpected:\n   struct {type.Name} : IEasyPacket<[c/{Color.Green.Hex3()}:{type.Name}]>" +
                                "\nPlease fix the struct definition so that the interface generic argument matches the type implementing it." +
                                $"\nDefined in mod: [c/{Color.Yellow.Hex3()}:{mod.Name}].\n");
        }

        // Create a new default instance of the easy packet type
        // https://stackoverflow.com/a/1151470/20943906
        var instance = (IEasyPacket)Activator.CreateInstance(typeof(EasyPacket<>).MakeGenericType(genericArg), true);
        if (instance == null)
        {
            throw new Exception($"Failed to register easy packet type: {type.Name}.");
        }

        // Register the created instance, assigning a unique net id
        var netId = NetEasyPacketCount++;
        PacketByNetId.Add(netId, instance);
        NetIdByPtr.Add(type.TypeHandle.Value, netId);

        Mod.Logger.Debug($"Registered IEasyPacket<{type.Name}> (Mod: {mod.Name}, ID: {netId}).");
    }

    /// <summary>
    ///     Register an easy packet handler.
    /// </summary>
    /// <param name="mod">Mod that defined the easy packet handler.</param>
    /// <param name="type">Type that implements <see cref="IEasyPacketHandler{T}" />.</param>
    private void RegisterHandler(Mod mod, Type type)
    {
        // Create a new default instance of the easy packet handler type, and allow it to register it instead
        var instance = (IEasyPacketHandler)Activator.CreateInstance(typeof(EasyPacketHandler<,>).MakeGenericType(type, type.GetInterface(EasyPacketHandlerFullName)!.GetGenericArguments()[0]), true);
        if (instance == null)
        {
            throw new Exception($"Failed to register easy packet type: {type.Name}.");
        }

        // The instance is thrown away because its only purpose is to register itself as a handler
        instance.Register(mod);

        Mod.Logger.Debug($"Registered IEasyPacketHandler<{type.Name}> (Mod: {mod.Name}).");
    }

    #endregion
}