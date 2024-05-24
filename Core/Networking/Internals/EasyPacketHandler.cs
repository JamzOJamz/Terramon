/*
 *  EasyPacketHandler.cs
 *  DavidFDev
*/

namespace Terramon.Core.Networking.Internals;

/// <summary>
///     Generic wrapper for an <see cref="IEasyPacketHandler{T}" /> type.
/// </summary>
internal readonly struct EasyPacketHandler<THandler, TPacket> : IEasyPacketHandler where THandler : struct, IEasyPacketHandler<TPacket> where TPacket : struct, IEasyPacket<TPacket>
{
    #region Static Methods

    private static void OnReceived(in TPacket packet, in SenderInfo sender, ref bool handled)
    {
        new THandler().Receive(in packet, in sender, ref handled);
    }

    #endregion

    #region Methods

    void IEasyPacketHandler.Register(Mod mod)
    {
        mod.AddPacketHandler<TPacket>(OnReceived);
    }

    #endregion
}