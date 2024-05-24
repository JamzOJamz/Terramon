/*
 *  IEasyPacketHandler.cs
 *  DavidFDev
*/

namespace Terramon.Core.Networking.Internals;

/// <summary>
///     Implemented by <see cref="EasyPacketHandler{T1,T2}" /> as a non-generic wrapper for handling a packet.
/// </summary>
internal interface IEasyPacketHandler
{
    #region Methods

    void Register(Mod mod);

    #endregion
}