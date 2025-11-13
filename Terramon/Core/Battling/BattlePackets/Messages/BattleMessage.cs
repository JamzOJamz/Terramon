using EasyPacketsLib;

namespace Terramon.Core.Battling.BattlePackets.Messages;
public abstract class BattleMessage : ILoadable
{
    private static readonly Dictionary<Type, BattleMessage> _messagesByType = [];
    private static readonly List<Type> _messageTypes = [];

    public byte ID { get; private set; }
    public IBattleProvider Sender { get; set; }
    public IBattleProvider Recipient { get; private set; }

    protected BattleMessage()
    {
        if (_messagesByType.TryGetValue(GetType(), out var dummy))
            ID = dummy.ID;
    }

    public void Load(Mod mod)
    {
        ID = (byte)_messageTypes.Count;
        var t = GetType();
        _messageTypes.Add(t);
        _messagesByType.Add(t, this);
    }

    public virtual void Write(BinaryWriter w) { }
    public virtual void Read(BinaryReader r) { }

    /// <summary>
    ///     <para>
    ///         Send this message to another battle provider or the battle manager
    ///     </para>
    ///     <para>
    ///         <b>If sent from anywhere to server:</b>
    ///         <list type="bullet">
    ///             <item>All clients will run <see cref="IBattleProvider.Witness(BattleMessage)"/></item>
    ///             <item>The server will run <see cref="BattleManager.Reply(BattleMessage)"/></item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         <b>If sent from server to client:</b>
    ///         <list type="bullet">
    ///             <item>All clients will run <see cref="IBattleProvider.Witness(BattleMessage)"/></item>
    ///             <item>The target client will run <see cref="IBattleProvider.Reply(BattleMessage)"/></item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         <b>If sent from (client/server-owned provider) to client:</b>
    ///         <list type="bullet">
    ///             <item>The server will run <see cref="BattleManager.Witness(BattleMessage)"/></item>
    ///             <item>All clients will run <see cref="IBattleProvider.Witness(BattleMessage)"/></item>
    ///             <item>The target client will run <see cref="IBattleProvider.Reply(BattleMessage)"/></item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         <b>If sent from client to server-owned provider:</b>
    ///         <list type="bullet">
    ///             <item>The server will run <see cref="BattleManager.Witness(BattleMessage)"/></item>
    ///             <item>All clients will run <see cref="IBattleProvider.Witness(BattleMessage)"/></item>
    ///             <item>The server will run <see cref="IBattleProvider.Reply(BattleMessage)"/></item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         <b>If sent from (server/server-owned provider) to server-owned provider:</b>
    ///         <list type="bullet">
    ///             <item>All clients will run <see cref="IBattleProvider.Witness(BattleMessage)"/></item>
    ///             <item>The server will run <see cref="IBattleProvider.Reply(BattleMessage)"/></item>
    ///         </list>
    ///     </para>
    /// </summary>
    /// <param name="other">If null, the battle manager, else, the target provider</param>
    public void Send(IBattleProvider other = null)
    {
        Recipient = other;
        if (Main.netMode == NetmodeID.SinglePlayer)
        {
            // Only client always witnesses the message
            TerramonPlayer.LocalPlayer.Witness(this);

            if (other is null) // Sending to "server"
            {
                // "Client" or "server-owned" provider is sending to "server"
                // Simulate a packet being received by the server
                BattleManager.Instance.Reply(this);
            }
            else // Sending from "client" to "server-owned" provider or vice versa
            {
                // Simulate a packet going through the server
                if (BattleManager.Instance.Witness(this))
                // And to the recipient (if not intercepted)
                    other.Reply(this);
            }
            return;
        }

        if (Main.dedServ)
        {
            if (Sender is null && other is null) // Server is sending to server
            {
                // The behavior for this is that everyone witnesses the message and only the server replies
                var msgPacket = new BattleMessageRpc(this);
                Terramon.Instance.SendPacket(in msgPacket);

                BattleManager.Instance.Reply(this);
                return;
            }

            switch (other.OwningSide) // Server is sending to client or to server-owned provider (Wild Pokemon and Trainer NPCs)
            {
                case ModSide.Client:

                    // Original sender will also witness the packet being sent
                    var msgPacket = new BattleMessageRpc(this);
                    Terramon.Instance.SendPacket(msgPacket);
                    break;
                case ModSide.Server:
                    other.Reply(this);
                    break;
                default: // Shouldn't happen but just for completeness
                    Terramon.Instance.Logger.Warn($"Server tried to send a {GetType().Name} to an invalid {nameof(IBattleProvider)}");
                    break;
            }
        }
        else // Client is sending to server or another client
        {
            // Everything is already handled by the server
            var msgPacket = new BattleMessageRpc(this);
            Terramon.Instance.SendPacket(in msgPacket);
        }
    }

    public void Intersend(BattleMessage original)
        => Intersend(original.Sender, original.Recipient);

    public void Intersend(IBattleProvider firstSender, IBattleProvider firstRecipient)
    {
        Sender = firstSender;
        Send(firstRecipient);
        Sender = firstRecipient;
        Send(firstSender);
    }

    /// <summary>
    ///     Returns a parameterless message of type <typeparamref name="T"/> to the sender.
    /// </summary>
    /// <returns>False, for interception purposes.</returns>
    public bool Return<T>() where T : BattleMessage, new()
    {
        var reply = new T
        {
            Sender = Recipient
        };
        reply.Send(Sender);
        return false;
    }
    /// <summary>
    /// Returns a message to the sender.
    /// </summary>
    /// <returns>False, for interception purposes.</returns>
    public bool Return(BattleMessage reply)
    {
        reply.Sender = Recipient;
        reply.Send(Sender);
        return false;
    }

    /// <summary>
    ///     Sets this <see cref="BattleMessage"/>'s <see cref="Sender"/> and <see cref="Recipient"/> to those of the provided message's.
    /// </summary>
    public BattleMessage Set(BattleMessage original)
    {
        Sender = original.Sender;
        Recipient = original.Recipient;
        return this;
    }

    public void Unload()
    {
        
    }

    public readonly struct BattleMessageRpc(BattleMessage underlying)
    : IEasyPacket<BattleMessageRpc>, IEasyPacketHandler<BattleMessageRpc>
    {
        private readonly BattleMessage _underlying = underlying;

        public void Serialise(BinaryWriter writer)
        {
            var sender = _underlying.Sender?.GetParticipantID() ?? BattleParticipant.None;
            var recipient = _underlying.Recipient?.GetParticipantID() ?? BattleParticipant.None;

            var cur = writer.BaseStream.Length;

            writer.Write(_underlying.ID);
            writer.Write(sender);
            writer.Write(recipient);
            _underlying.Write(writer);

            Console.WriteLine($"Wrote {writer.BaseStream.Length - cur} bytes for message of type {_underlying.GetType().Name}");
        }

        public BattleMessageRpc Deserialise(BinaryReader reader, in SenderInfo sender)
        {
            var cur = reader.BaseStream.Position;

            var id = reader.ReadByte();
            var newMsg = (BattleMessage)Activator.CreateInstance(_messageTypes[id]);
            newMsg.Sender = reader.ReadParticipant().Provider;
            newMsg.Recipient = reader.ReadParticipant().Provider;
            newMsg.Read(reader);

            Console.WriteLine($"Read {reader.BaseStream.Position - cur} bytes for message of type {newMsg.GetType().Name}");

            return new(newMsg);
        }

        public void Receive(in BattleMessageRpc packet, in SenderInfo sender, ref bool handled)
        {
            handled = true;

            var msg = packet._underlying;
            var target = msg.Recipient;

            if (target is null) // Sent to server
            {
                if (Main.dedServ)
                    BattleManager.Instance.Reply(msg);
                else
                    TerramonPlayer.LocalPlayer.Witness(msg);
            }
            else // Sent to client or server-owned provider
            {
                if (Main.dedServ)
                {
                    if (BattleManager.Instance.Witness(msg))
                        msg.Send(msg.Recipient);
                }
                else if (target.IsLocal)
                    target.Reply(msg);
                else
                    TerramonPlayer.LocalPlayer.Witness(msg);
            }
        }
    }
}
