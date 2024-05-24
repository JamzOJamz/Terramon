/*
 *  IEasyPacket`1.cs
 *  DavidFDev
 */

using System.Diagnostics.Contracts;
using System.IO;

namespace Terramon.Core.Networking;

/// <summary>
///     An easy solution for sending/receiving ModPackets with custom data.
///     Implement on a struct, preferably a readonly struct.<br />
///     Send the packet using <see cref="EasyPacketExtensions.SendPacket{T}" />.<br />
///     Handle the packet using <see cref="EasyPacketExtensions.AddPacketHandler{T}" />.
/// </summary>
/// <example>
///     <code>
///         public readonly struct ExamplePacket : IEasyPacket&lt;ExamplePacket&gt;
///         {
///             public readonly int X;
///             public readonly int Y;
/// 
///             public ExamplePacket(int x, int y)
///             {
///                 X = x;
///                 Y = y;
///             }
/// 
///             void IEasyPacket&lt;ExamplePacket&gt;.Serialise(BinaryWriter writer)
///             {
///                 writer.Write(X);
///                 writer.Write(Y);
///             }
/// 
///             ExamplePacket IEasyPacket&lt;ExamplePacket&gt;.Deserialise(BinaryReader reader, in SenderInfo sender)
///             {
///                 return new ExamplePacket(reader.ReadInt32(), reader.ReadInt32());
///             }
///         }
///     </code>
/// </example>
public interface IEasyPacket<out T> where T : struct, IEasyPacket<T>
{
    #region Methods

    /// <summary>
    ///     Serialise the packet data using the provided writer.
    /// </summary>
    /// <example>
    ///     <code>
    ///         void IEasyPacket&lt;ExamplePacket&gt;.Serialize(BinaryWriter writer)
    ///         {
    ///             writer.Write(X);
    ///             writer.Write(Y);
    ///         }
    ///     </code>
    /// </example>
    // ReSharper disable once PureAttributeOnVoidMethod
    [Pure]
    void Serialize(BinaryWriter writer);

    /// <summary>
    ///     Deserialize the packet data using the provided reader.
    ///     An error will be raised if not all data is read from the reader.
    /// </summary>
    /// <example>
    ///     <code>
    ///         ExamplePacket IEasyPacket&lt;ExamplePacket&gt;.Deserialize(BinaryReader reader, in SenderInfo sender)
    ///         {
    ///             return new ExamplePacket(reader.ReadInt32(), reader.ReadInt32());
    ///         }
    ///     </code>
    /// </example>
    [Pure]
    T Deserialize(BinaryReader reader, in SenderInfo sender);

    #endregion
}