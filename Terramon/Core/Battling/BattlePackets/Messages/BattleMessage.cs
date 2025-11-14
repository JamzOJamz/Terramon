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

        Terramon.Instance.Logger.Info($"Name: {t.Name}, ID: {ID}");

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
    public void Send(IBattleProvider other = null, bool selfWitness = true)
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
                BattleManager.Instance.Witness(this);
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
            if (other is null) // Server or server-owned provider is sending to server
            {
                // The behavior for this is that everyone witnesses the message and only the server replies

                // If has sender (server-owned provider), make them witness the message
                Sender?.Witness(this);
                BattleManager.Instance.Witness(this);

                var msgPacket = new BattleMessageRpc(this);
                Terramon.Instance.SendPacket(msgPacket);

                BattleManager.Instance.Reply(this);
                return;
            }

            if (selfWitness)
                BattleManager.Instance.Witness(this);

            switch (other.OwningSide) // Server is sending to client or to server-owned provider (Wild Pokemon and Trainer NPCs)
            {
                case ModSide.Client:

                    // Original sender will also witness the packet being sent
                    var msgPacket = new BattleMessageRpc(this);
                    Terramon.Instance.SendPacket(msgPacket);
                    break;
                case ModSide.Server:
                    other.Witness(this);
                    other.Reply(this);
                    break;
                default: // Shouldn't happen but just for completeness
                    Terramon.Instance.Logger.Warn($"Server tried to send a {GetType().Name} to an invalid {nameof(IBattleProvider)}");
                    break;
            }
        }
        else // Client is sending to server or another client
        {
            if (Sender is null)
            {
                Terramon.Instance.Logger.Warn($"Client {Main.myPlayer} tried to send a message as the server");
                return;
            }
            if (selfWitness)
                Sender.Witness(this);
            // Everything is already handled by the server
            var msgPacket = new BattleMessageRpc(this);
            Terramon.Instance.SendPacket(msgPacket);
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

    public struct BattleMessageRpc(BattleMessage underlying) : IEasyPacket
    {
        private BattleMessage _underlying = underlying;

        public readonly void Serialise(BinaryWriter writer)
        {
            var start = writer.BaseStream.Length;
            var cur = start;

            writer.Write(_underlying.ID);
            LogSize();
            writer.Write(_underlying.Sender);
            LogSize();
            writer.Write(_underlying.Recipient);
            LogSize();
            _underlying.Write(writer);
            LogSize();

            Console.WriteLine($"Wrote a total of {writer.BaseStream.Length - start} bytes for {_underlying.GetType().Name} from {_underlying.Sender?.BattleName ?? "server"} to {_underlying.Recipient?.BattleName ?? "server"}");

            void LogSize()
            {
                Console.WriteLine($"Wrote {writer.BaseStream.Length - cur} bytes");
                cur = writer.BaseStream.Length;
            }
        }

        public void Deserialise(BinaryReader reader, in SenderInfo sender)
        {
            var start = reader.BaseStream.Position;
            var cur = start;

            var id = reader.ReadByte();
            LogSize();
            _underlying = (BattleMessage)Activator.CreateInstance(_messageTypes[id]);
            _underlying.Sender = reader.ReadParticipant();
            LogSize();
            _underlying.Recipient = reader.ReadParticipant();
            LogSize();
            _underlying.Read(reader);
            LogSize();

            Console.WriteLine($"Read a total of {reader.BaseStream.Position - start} bytes for {_underlying.GetType().Name}");

            void LogSize()
            {
                Console.WriteLine($"Read {reader.BaseStream.Position - cur} bytes");
                cur = reader.BaseStream.Position;
            }
        }

        public readonly void Receive(in SenderInfo sender, ref bool handled)
        {
            EasyPacket.lastProcessedPacket = _underlying.GetType();

            handled = true;

            var msg = _underlying;
            var target = msg.Recipient;

            if (target is null) // Sent to server
            {
                if (Main.dedServ)
                {
                    BattleManager.Instance.Witness(msg);
                    BattleManager.Instance.Reply(msg);
                }
                else
                    TerramonPlayer.LocalPlayer.Witness(msg);
            }
            else // Sent to client or server-owned provider
            {
                if (Main.dedServ)
                {
                    if (BattleManager.Instance.Witness(msg))
                        msg.Send(msg.Recipient, selfWitness: false);
                }
                else if (target.IsLocal)
                    target.Reply(msg);
                else
                    TerramonPlayer.LocalPlayer.Witness(msg);
            }
        }
    }
}
