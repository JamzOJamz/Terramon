using Showdown.NET.Definitions;
using Showdown.NET.Protocol;
using Showdown.NET.Simulator;
using System.Runtime.InteropServices;
using System.Text;
using Terramon.Core.Battling.BattlePackets;
using Terramon.Core.Battling.BattlePackets.Messages;

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

    public static BattleInstance Create(BattleClient a, BattleClient b)
    {
        var pa = a.Provider;
        var pb = b.Provider;

        var bf = new BattleField()
        {
            A = new(pa),
            B = new(pb),
        };

        a.Battle = b.Battle = bf;
        a.Foe = pb;
        b.Foe = pa;

        return new BattleInstance
        {
            ClientA = a,
            ClientB = b,
            Omniscient = bf,
        };
    }
    public static void Destroy(BattleInstance i)
    {
        i.Stop();
        i.ClientA.Battle = i.ClientB.Battle = null;
        i.ClientA.Foe = i.ClientB.Foe = null;
        i.ClientA.State = i.ClientB.State = ClientBattleState.None;

    }

    public BattleState State;
    public BattleField Omniscient; // omniscient viewer
    public BattleClient ClientA; // requester
    public BattleClient ClientB; // requestee
    // 1-indices
    public BattleStream Stream; // battle stream

    public bool ShouldStart =>
        State == BattleState.Picking &&
        ClientA.Pick != 0 &&
        ClientB.Pick != 0;

    public BattleClient SubmitTeam(BattleParticipant participant, SimplePackedPokemon[] packedTeam)
    {
        var c = participant.Client;
        if (ClientA == c || ClientB == c)
        {
            SubmitTeam_Internal(c, packedTeam);
            return c;
        }
        throw new Exception(
            $"Participant {participant} ({participant.Client.Name}) wasn't found in battle. Instead, {ClientA.Name} and {ClientB.Name} were found.");
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

        // Name is written as its side for simplicity when parsing, similar to how team names are written
        var setPlayer = ProtocolCodec.EncodeSetPlayerCommand(plr, plr.ToString(), sb.ToString());
        client.CachedTeamSpec = ProtocolCodec.EncodePlayerChoiceCommand(plr, "team", spec);

        Console.WriteLine(setPlayer);

        Stream.Write(setPlayer);
    }

    public void SubmitChoice(int plr, BattleChoice choice, int operand)
    {
        if (Stream is null || Stream.IsDisposed)
            return;

        string main;
        string secondary = null;
        if ((choice & BattleChoice.Move) != 0)
        {
            main = "move";
            if ((choice & BattleChoice.Mega) != 0)
                secondary = "mega";
            else if ((choice & BattleChoice.ZMove) != 0)
                secondary = "zmove";
            else if ((choice & BattleChoice.Max) != 0)
                secondary = "max";
        }
        else
        {
            main = choice switch
            {
                BattleChoice.Default => "default",
                BattleChoice.Pass => "pass",
                BattleChoice.Switch => "switch",
                _ => throw new Exception()
            };
        }

        string final = choice is BattleChoice.Default ?
            ProtocolCodec.EncodePlayerChoiceCommand(plr, main) : secondary == null ?
            ProtocolCodec.EncodePlayerChoiceCommand(plr, main, operand.ToString()) :
            ProtocolCodec.EncodePlayerChoiceCommand(plr, main, secondary, operand.ToString());

        Console.WriteLine(final);
        Stream.Write(final);
    }

    public void EnsureStreamStarted()
    {
        if (Stream != null)
            return;

        Stream = new BattleStream();
        Task.Run(RunAsync);
    }

    public void StartEffects()
    {
        State = BattleState.Ongoing;

        ClientA.State = ClientB.State = ClientBattleState.Ongoing;

        // No this isn't a mistake, but because Terraria isn't a quantum program,
        // we can't make it so that both sides run after the other one at the same time
        // unless we do this
        ClientA.Provider.StartBattleEffects();
        ClientB.Provider.StartBattleEffects();
        ClientA.Provider.StartBattleEffects();
        ClientB.Provider.StartBattleEffects();
    }

    public void Stop()
    {
        ClientA.BattleStopped();
        ClientB.BattleStopped();

        Stream?.Dispose();
    }

    public BattleClient this[int side]
        => side == 1 ? ClientA : side == 2 ? ClientB : null;

    private async Task RunAsync()
    {
        try
        {
            var start = ProtocolCodec.EncodeStartCommand(FormatID.Gen9CustomGame);
            Console.WriteLine(start);
            Stream.Write(start);
            var mgr = BattleManager.Instance;

            // I would love to write directly to a packet on multiplayer
            // But IDK if that's possible with EasyPacketsLib
            // I think that we can probably do some hack for it anyway
            // Or come up with a better solution than EasyPacketsLib but I digress

            bool inMainFrame = false;

            // Kept per-round
            BinaryWriter pWriter = null;
            BinaryWriter sWriter = null;

            await foreach (var output in Stream.ReadOutputsAsync())
            {
                var frame = ProtocolCodec.Parse(output);
                if (frame is null || frame.Elements == null) continue;

                if (frame is UpdateFrame)
                {
                    pWriter?.Dispose();
                    sWriter?.Dispose();

                    pWriter = new(new MemoryStream());
                    sWriter = new(new MemoryStream());

                    inMainFrame = true;
                }
                else if (frame is SideUpdateFrame)
                {
                    if (inMainFrame)
                    {
                        Observe(ref pWriter, ref sWriter);
                        inMainFrame = false;
                    }
                }

                // Console.WriteLine($"Received message of type {frame.GetType().Name} from simulator");
                foreach (var element in CollectionsMarshal.AsSpan(frame.Elements))
                {
                    Log(element.ToString());
                    if (element is ISplitElement split)
                    {
                        int tgt = (split.PlayerID is 1 or 2) ? split.PlayerID : -1;
                        mgr.HandleSingleElement(this, sWriter, split.Secret, toSide: -1);
                        mgr.HandleSingleElement(this, pWriter, split.Public, toSide: tgt);
                        continue;
                    }
                    mgr.HandleSingleElement(this, pWriter, element, toSide: -1);
                }
            }
        }
        catch (Exception ex)
        {
            Log($"Battle encountered an error: {ex.GetType()}: {ex.Message}", ConsoleColor.Red);
            Log(ex.StackTrace ?? "No stack trace.", ConsoleColor.Red);
            var battleEnd = new TieStatement(eitherParticipant: ClientA.Provider, type: TieStatement.TieType.Forced);
            battleEnd.Send();
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

    /// <summary>
    ///     Reads and handles all <see cref="BattleAction"/>s currently written to the buffers
    ///     Also resets those buffers
    /// </summary>
    public void Observe(ref BinaryWriter p, ref BinaryWriter s)
    {
        var owner = ClientA.Provider;

        if (p.BaseStream.Length != 0)
        {
            p.BaseStream.WriteByte(0); // footer

            if (Main.dedServ) // mp
            {
                Console.WriteLine($"Sent payload with {p.BaseStream.Length} bytes of content.");

                var payload = new BattlePayloadRpc(owner, (MemoryStream)p.BaseStream);
                Terramon.Instance.SendPacket(payload);
                p.Dispose();
                p = null;
            }
            else
            {
                BattleManager.Instance.Observe(owner.ID, (MemoryStream)p.BaseStream, onlyToSelf: false);
                p = null;
            }
        }

        if (s.BaseStream.Length != 0)
        {
            s.BaseStream.WriteByte(0); // footer
            BattleManager.Instance.Observe(owner.ID, (MemoryStream)s.BaseStream, onlyToSelf: true);
            p = null;
        }
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
