using EasyPacketsLib;
using Showdown.NET.Definitions;
using Showdown.NET.Protocol;
using Showdown.NET.Simulator;
using System.Runtime.InteropServices;
using System.Text;
using Terramon.Core.Battling.BattlePackets;

namespace Terramon.Core.Battling;

public sealed class BattleInstance
{
    public const ConsoleColor BattleAction = ConsoleColor.Yellow;
    public const ConsoleColor BattleFollowup = ConsoleColor.DarkYellow;
    public const ConsoleColor BattleReceive = ConsoleColor.Magenta;
    public const ConsoleColor BattleReceiveFollowup = ConsoleColor.DarkMagenta;
    public const ConsoleColor Meta = ConsoleColor.Cyan;
    public const ConsoleColor MetaProgress = ConsoleColor.DarkCyan;
    public const ConsoleColor MetaFollowup = ConsoleColor.DarkGray;
    public const ConsoleColor ChronoAction = ConsoleColor.Blue;
    public const ConsoleColor FieldAction = ConsoleColor.DarkBlue;
    public const ConsoleColor Faint = ConsoleColor.Red;
    public const ConsoleColor Error = ConsoleColor.DarkRed;
    public const ConsoleColor Win = ConsoleColor.Green;
    public const ConsoleColor NotWin = ConsoleColor.DarkGreen;

    public BattleState State;
    public BattleObserver Omniscient; // omniscient viewer
    public BattleClient ClientA; // requester
    public BattleClient ClientB; // requestee
    // 1-indices
    public BattleStream Stream; // battle stream

    private MemoryStream _secret;
    private MemoryStream _public;


    public bool ShouldStart =>
        State == BattleState.Picking &&
        ClientA.Pick != 0 &&
        ClientA.Pick != 0;

    public BattleClient SubmitTeam(BattleParticipant participant, SimplePackedPokemon[] packedTeam)
    {
        BattleClient client;
        if (ClientA == participant.Client)
            client = ClientA;
        else if (ClientB == participant.Client)
            client = ClientB;
        else
            throw new Exception(
                $"Participant {participant} ({participant.Client.Name}) wasn't found in battle. Instead, {ClientA.Name} and {ClientB.Name} were found.");

        SubmitTeam_Internal(client, packedTeam);
        return client;
    }

    private void SubmitTeam_Internal(BattleClient client, SimplePackedPokemon[] packedTeam)
    {
        EnsureStreamStarted();

        bool a = client == ClientA;
        var plr = a ? 1 : 2;
        var sb = new StringBuilder();
        for (int i = 0; i < packedTeam.Length - 1; i++)
        {
            sb.Append(packedTeam[i]);
            sb.Append(']');
        }
        sb.Append(packedTeam[^1]);

        const string defaultSpec = "123456";
        string spec = client.Pick == 1 ? defaultSpec : $"{client.Pick}{defaultSpec.Replace(client.Pick.ToString(), string.Empty)}";

        Stream.Write(ProtocolCodec.EncodeSetPlayerCommand(plr, client.Name, sb.ToString()));
        Stream.Write(ProtocolCodec.EncodePlayerChoiceCommand(plr, "team", spec));
    }

    public void EnsureStreamStarted()
    {
        if (Stream != null)
            return;

        Stream = new BattleStream();
        Task.Run(RunAsync);
    }

    public void Start()
    {
        ClientA.State = ClientBattleState.Ongoing;
        ClientB.State = ClientBattleState.Ongoing;
        ClientA.Provider.StartBattleEffects();
        ClientB.Provider.StartBattleEffects();
    }

    public void Stop()
    {

    }

    private async Task RunAsync()
    {
        try
        {
            Stream.Write(ProtocolCodec.EncodeStartCommand(FormatID.Gen9CustomGame));
            var mgr = BattleManager.Instance;

            // I would love to write directly to a packet on multiplayer
            // But IDK if that's possible with EasyPacketsLib
            // I think that we can probably do some hack for it anyway
            // Or come up with a better solution than EasyPacketsLib but I digress

            _public = new();
            _secret = new();

            using var pWriter = new BinaryWriter(_public);
            using var sWriter = new BinaryWriter(_secret);
            using var w = new BinaryWriter(new DoubleStream(_public, _secret));

            bool inMainFrame = false;

            await foreach (var output in Stream.ReadOutputsAsync())
            {
                var frame = ProtocolCodec.Parse(output);
                if (frame is null || frame.Elements == null) continue;
                if (frame is UpdateFrame)
                    inMainFrame = true;
                else if (frame is SideUpdateFrame && inMainFrame)
                {
                    SendToObservers();
                }
                // Console.WriteLine($"Received message of type {frame.GetType().Name} from simulator");
                foreach (var element in CollectionsMarshal.AsSpan(frame.Elements))
                {
                    if (element is ISplitElement split)
                    {
                        int tgt = (split.PlayerID is 1 or 2) ? split.PlayerID : -1;
                        mgr.HandleSingleElement(this, sWriter, split.Secret, toClient: -1, exclusive: true);
                        mgr.HandleSingleElement(this, pWriter, split.Public, toClient: tgt, exclusive: true);
                        continue;
                    }
                    mgr.HandleSingleElement(this, w, element, toClient: -1, exclusive: false);
                }
            }
        }
        catch (Exception ex)
        {
            Log($"Battle encountered an error: {ex.GetType()}: {ex.Message}", ConsoleColor.Red);
            Log(ex.StackTrace ?? "No stack trace.", ConsoleColor.Red);
            Stop();
        }
        finally
        {
            if (Stream != null && !Stream.IsDisposed)
            {
                Log($"Disposing battle stream", ConsoleColor.Yellow);
                Stream.Dispose();
            }
        }
    }

    private void SendToObservers()
    {
        var participant = ClientA.Provider.GetParticipantID();
        _public.WriteByte(0);
        var payload = new BattlePayloadRpc(participant, _public);
        Terramon.Instance.SendPacket(in payload);

        BattleManager.Instance.Observe(participant, _secret);
    }

    public static void Log(string str, ConsoleColor col = ConsoleColor.Gray)
    {
        lock (str)
        {
            Console.ForegroundColor = col;
            Console.WriteLine(str);
            Console.ResetColor();
        }
    }
}

public enum BattleState : byte
{
    Request,
    Picking,
    Ongoing,
    Ended,
}
