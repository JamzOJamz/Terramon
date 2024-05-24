/*
 *  IEasyPacket.cs
 *  DavidFDev
 */

using System.IO;

namespace Terramon.Core.Networking.Internals;

/// <summary>
///     Implemented by <see cref="EasyPacket{T}" /> as a non-generic wrapper for receiving an easy packet and detouring it
///     to the struct.
/// </summary>
internal interface IEasyPacket
{
    #region Methods

    void ReceivePacket(BinaryReader reader, in SenderInfo sender);

    #endregion
}