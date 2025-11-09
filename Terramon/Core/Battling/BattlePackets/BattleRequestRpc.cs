using EasyPacketsLib;

namespace Terramon.Core.Battling.BattlePackets;

/// <summary>
///     Packet for general battle request actions. May be sent from clients or the server (DO NOT FORWARD)
/// </summary>
/// <param name="request">The type of request that was made</param>
/// <param name="sender">The <see cref="Entity.whoAmI"/> of the sender.</param>
/// <param name="receiver">The <see cref="Entity.whoAmI"/> of the receiver.</param>
public readonly struct BattleRequestRpc(BattleRequestType request, BattleParticipant sender, BattleParticipant receiver)
    : IEasyPacket<BattleRequestRpc>, IEasyPacketHandler<BattleRequestRpc>
{
    public static Terramon Mod => Terramon.Instance;

    private readonly BattleRequestType _request = request;
    private readonly BattleParticipant _sender = sender;
    private readonly BattleParticipant _receiver = receiver;
    public void Serialise(BinaryWriter writer)
    {
        writer.Write((byte)_request);
        writer.Write(_sender);
        writer.Write(_receiver);
    }

    public BattleRequestRpc Deserialise(BinaryReader reader, in SenderInfo sender)
    {
        var request = (BattleRequestType)reader.ReadByte();
        var receiver = reader.ReadParticipant();
        var senderr = reader.ReadParticipant();
        return new BattleRequestRpc(request, senderr, receiver);
    }

    public void Receive(in BattleRequestRpc packet, in SenderInfo sender, ref bool handled)
    {
        var Sender = packet._sender.Client;
        var Receiver = packet._receiver.Client;
        handled = true;
        this.DebugLog();
        if (Main.dedServ) // Sent from client to server
        {
            var mgr = BattleManager.Instance;
            switch (packet._request)
            {
                case BattleRequestType.Request: // Sent by requester
                    mgr.SubmitRequest(packet._sender, packet._receiver);
                    break;
                case BattleRequestType.Accept: // Sent by requestee
                    mgr.AcceptRequest(packet._receiver, packet._sender);
                    break;
                case BattleRequestType.Decline: // Sent by requestee
                    mgr.DeclineRequest(packet._receiver, packet._sender, error: false);
                    break;
                case BattleRequestType.Error: // Sent by requestee
                    mgr.DeclineRequest(packet._receiver, packet._sender, error: true);
                    break;
            }
        }
        else // Sent from server to this client
        {
            switch (packet._request)
            {
                case BattleRequestType.Request: // Sender is the requester
                    // DEBUG: Instantly accept request if receiver
                    if (packet._receiver.Client.IsLocal)
                    {
                        var reply = new BattleRequestRpc(BattleRequestType.Accept, packet._receiver, packet._sender);
                        Mod.SendPacket(in reply);
                    }
                    Sender.Foe = Receiver.Provider;
                    Sender.State = ClientBattleState.Requested;
                    break;
                case BattleRequestType.Accept: // Sender is the requestee
                    // Open the choosing menu for both sides
                    var local = TerramonPlayer.LocalPlayer;
                    if (packet._receiver.Client.IsLocal ||
                        packet._sender.Client.IsLocal)
                    {
                        // DEBUG: Just choose the currently active slot if any, or zero otherwise
                        var slot = (byte)Math.Max(local.ActiveSlot, 0);
                        var pick = new BattlePickRpc(++slot);
                        Mod.SendPacket(in pick);
                    }
                    Sender.Foe = Receiver.Provider;

                    Sender.State = ClientBattleState.PollingSlot;
                    Receiver.State = ClientBattleState.PollingSlot;
                    break;
                case BattleRequestType.Decline: // Sender is the requestee
                case BattleRequestType.Error: // Sent by either the server or the requestee
                    Receiver.Foe = null;
                    if (Sender.Foe == Receiver.Provider)
                        Sender.Foe = null;
                    break;
            }
        }
    }
}

public enum BattleRequestType : byte
{
    Request,
    Cancel,
    Accept,
    Decline,
    Error,
}
