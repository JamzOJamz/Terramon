/*
 *  SenderInfo.cs
 *  DavidFDev
 */

using Terraria;
using Terraria.ModLoader;

namespace EasyPacketsLib;

/// <summary>
///     Information regarding the sender of an easy packet that has been received.
/// </summary>
public readonly ref struct SenderInfo
{
    #region Fields

    private readonly BitsByte _flags;
    private readonly byte _toClient;
    private readonly byte _ignoreClient;

    /// <summary>
    ///     Mod that sent the packet.
    /// </summary>
    public readonly Mod Mod;

    /// <summary>
    ///     Index of the player that sent the packet.
    ///     If forwarded or sent by a client, this is the index of that client.
    ///     If sent directly by the server, this is the index of the server (255).
    /// </summary>
    public readonly byte WhoAmI;

    #endregion

    #region Constructors

    internal SenderInfo(Mod mod, byte whoAmI, BitsByte flags, byte toClient, byte ignoreClient)
    {
        Mod = mod;
        WhoAmI = whoAmI;
        _flags = flags;
        _toClient = toClient;
        _ignoreClient = ignoreClient;
    }

    #endregion

    #region Properties

    /// <summary>
    ///     Packet is, or has been, forwarded by a client.
    /// </summary>
    public bool Forwarded => _flags[0];

    /// <summary>
    ///     If non-negative, this packet will only be received by the specified client.
    /// </summary>
    public int ToClient => _toClient == 255 ? -1 : _toClient;

    /// <summary>
    ///     If non-negative, this packet will not be received by the specified client.
    /// </summary>
    public int IgnoreClient => _ignoreClient == 255 ? -1 : _ignoreClient;

    #endregion
}