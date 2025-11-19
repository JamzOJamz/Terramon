/*
 *  IEasyPacket.cs
 *  DavidFDev
 */

using System.Diagnostics.Contracts;

namespace EasyPacketsLib;

/// <summary>
///     An easy solution for handled received easy packets and sending/receiving ModPackets with custom data.
///     Implement on a struct.
/// </summary>
public interface IEasyPacket
{
    #region Methods

    /// <summary>
    ///     Serialise the packet data using the provided writer.
    /// </summary>
    /// <example>
    ///     <code>
    ///         void IEasyPacket&lt;ExamplePacket&gt;.Serialise(BinaryWriter writer)
    ///         {
    ///             writer.Write(X);
    ///             writer.Write(Y);
    ///         }
    ///     </code>
    /// </example>
    // ReSharper disable once PureAttributeOnVoidMethod
    [Pure]
    void Serialise(BinaryWriter writer);

    /// <summary>
    ///     Deserialise the packet data using the provided reader.
    ///     An error will be raised if not all data is read from the reader.
    /// </summary>
    /// <example>
    ///     <code>
    ///         ExamplePacket IEasyPacket&lt;ExamplePacket&gt;.Deserialise(BinaryReader reader, in SenderInfo sender)
    ///         {
    ///             return new ExamplePacket(reader.ReadInt32(), reader.ReadInt32());
    ///         }
    ///     </code>
    /// </example>
    void Deserialise(BinaryReader reader, in SenderInfo sender);

    /// <summary>
    ///     Handle a received easy mod packet.
    ///     If a packet is received but is unhandled, an error is raised.
    /// </summary>
    /// <param name="sender">Information regarding the sender of the packet.</param>
    /// <param name="handled">An unhandled packet will raise an error.</param>
    /// <example>
    ///     <code>
    ///         void IEasyPacketHandler&lt;ExamplePacket&gt;.Receive(in ExamplePacket packet, in SenderInfo sender, ref bool handled)
    ///         {
    ///             sender.Mod.Logger.Debug($"X: {packet.X}, Y: {packet.Y}");
    ///             handled = true;
    ///         }
    ///     </code>
    /// </example>
    void Receive(in SenderInfo sender, ref bool handled);

    #endregion
}