using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using Showdown.NET.Definitions;
using Showdown.NET.Protocol;
using Showdown.NET.Simulator;
using Terramon.Content.GUI.TurnBased;
using Terramon.Content.NPCs;

namespace Terramon.Core.Battling;

public sealed partial class BattleInstance
{
    /// <summary>
    ///     In wild battles, holds the index of the <see cref="PokemonNPC" /> currently being battled.
    ///     For trainer or other battle types, this value is null.
    /// </summary>
    public int? WildNPCIndex { get; init; }

    public PokemonNPC WildNPC => WildNPCIndex.HasValue ? (PokemonNPC)Main.npc[WildNPCIndex.Value].ModNPC : null;

    /// <summary>
    ///     The index of the player who initiated the battle or the host.
    ///     In wild battles, this is always the local player.
    ///     In trainer battles, this is the host player, and the other player is Player2.
    /// </summary>
    public int Player1Index { get; init; }

    public TerramonPlayer Player1 => Main.player[Player1Index].Terramon();

    /// <summary>
    ///     The index of other player in the battle (only applicable in trainer battles).
    ///     Null in wild battles.
    /// </summary>
    public int? Player2Index { get; init; }

    public TerramonPlayer Player2 => Player2Index.HasValue ? Main.player[Player2Index.Value].Terramon() : null;

    public int TickCount { get; set; }

    public BattleStream BattleStream { get; set; }

    public bool CanChoose { get; private set; }

    public bool HasToSwitch { get; private set; }

    public bool Player2HasToWait { get; private set; }

    public void Update()
    {
        TickCount++;
    }

    public void Stop()
    {
        ShouldStop = true;
    }

    public void EndEverywhere()
    {
        var p1 = Player1;
        var p2 = Player2;
        var w = WildNPC;

        if (w != null)
            w.EndBattle();
        else
            BattleStream?.Dispose();

        p1.Battle = null;
        if (p2 != null)
            p2.Battle = null;

        TestBattleUI.Close();
    }

    #region Battle Stream

    public void Start()
    {
        if (Player1Index != Main.myPlayer)
            return;
        Task.Run(RunAsync);
        Main.NewText("Battle started and running in background!");
    }

    private async Task RunAsync()
    {
        try
        {
            var s = BattleStream = new();

            var player = Main.player[Player1Index];
            var modPlayer = player.Terramon();
            var packedTeam = modPlayer.GetPackedTeam();

            var wild = WildNPC;
            var p2 = Player2;

            string otherName = wild != null ? wild.DisplayName.Value : p2 != null ? p2.Player.name : "Green";
            string otherTeam = wild != null ? wild.Data.GetPacked() : p2?.GetPackedTeam();

            ConsoleWrite($"Battle started by {player.name} against {otherName}", ConsoleColor.Blue);

            string start = JsonSerializer.Serialize(new
            {
                formatid = FormatID.Gen9CustomGame,
                p1 = new
                {
                    player.name,
                    team = packedTeam,
                },
                p2 = new
                {
                    name = otherName,
                    team = otherTeam,
                },
            });

            s.Write($">start {start}");

            const string defaultSpec = "123456";
            string p1activeSlot = (modPlayer.ActiveSlot + 1).ToString();
            string p1spec = modPlayer.ActiveSlot == 0 ? defaultSpec : p1activeSlot + defaultSpec.Replace(p1activeSlot, string.Empty);
            s.Write(ProtocolCodec.EncodePlayerChoiceCommand(1, "team", p1spec));
            s.Write(ProtocolCodec.EncodePlayerChoiceCommand(2, "team", defaultSpec));

            await foreach (var output in s.ReadOutputsAsync())
            {
                var frame = ProtocolCodec.Parse(output);
                if (frame is null || frame.Elements == null) continue;
                // Console.WriteLine($"Received message of type {frame.GetType().Name} from simulator");
                foreach (var element in CollectionsMarshal.AsSpan(frame.Elements))
                {
                    var finalElement = element is ISplitElement split ? split.Secret : element;
                    HandleSingleElement(finalElement, modPlayer, p2, wild);
                }
            }
        }
        catch (Exception ex)
        {
            ConsoleWrite($"Battle encountered an error: {ex.GetType()}: {ex.Message}", ConsoleColor.Red);
            ConsoleWrite(ex.StackTrace ?? "No stack trace.", ConsoleColor.Red);
            Stop();
        }
        finally
        {
            if (BattleStream != null && BattleStream.IsDisposed)
            {
                ConsoleWrite($"Disposing battle stream", ConsoleColor.Yellow);
                BattleStream.Dispose();
            }
        }
    }

    /// <summary>
    ///     Gets the corresponding Pokémon data for a Pokémon given its ID as output by Showdown.
    /// </summary>
    /// <param name="showdownMon"></param>
    private void GetPokemonFromShowdown(string showdownMon, out ShowdownPokemonData data)
    {
        data = new ShowdownPokemonData();

        if (showdownMon is null)
            return;

        var finalID = PokemonID.Parse(showdownMon);
        int plr = finalID.Player;
        data.Player = plr == 1 ? Player1 : Player2;
        if (data.Player is null)
            data.Wild = WildNPC;
        else
            data.Data = data.Player.GetPokemonFromShowdown(finalID.Name);
    }
    private struct ShowdownPokemonData()
    {
        private TerramonPlayer _owner;
        private PokemonNPC _wild;
        private PokemonData _data;

        public string Name { get; set; } = string.Empty;
        public bool Active { get; set; }

        public PokemonData Data
        {
            readonly get => _data;
            set
            {
                _data = value;
                if (_data != null)
                {
                    Name += _data.DisplayName;
                    Active = true;
                }
            }
        }
        public TerramonPlayer Player
        {
            readonly get => _owner;
            set
            {
                _owner = value;
                if (_owner != null)
                    Name += PlayerName + "'s ";
            }
        }
        public PokemonNPC Wild
        {
            readonly get => _wild;
            set
            {
                _wild = value;
                if (_wild != null)
                {
                    Name += "Wild ";
                    Data = _wild.Data;
                }
            }
        }

        public readonly ref ushort HP => ref _data.HP;

        public readonly ref Showdown.NET.Definitions.StatusID Status => ref _data.Status;

        public readonly ref StatStages StatStages => ref _data.StatStages;

        public readonly string PlayerName => _owner.Player.name;

        public readonly string PokeName => _data.DisplayName;

        public readonly override string ToString() => Name;
    }
    private void HandleSingleElement(ProtocolElement element, TerramonPlayer p1, TerramonPlayer p2, PokemonNPC wild)
    {
        PokemonData[] foeTeam = wild is null ? p2.Party : [wild.Data];

        ShowdownPokemonData source = new();
        ShowdownPokemonData target = new();

        if (element is IPokemonArgs args)
        {
            string src = args.Source ?? args.Attacker;
            string tgt = args.Target ?? args.Defender;

            if (src is null)
            {
                if (tgt is null)
                    tgt = args.Pokemon;
                else
                    src = args.Pokemon;
            }

            GetPokemonFromShowdown(src, out source);
            GetPokemonFromShowdown(tgt, out target);
        }

        HandleSingleElement_Inner(element, in source, in target, p1, p2, foeTeam);
    }
    private static void ConsoleWrite(string str, ConsoleColor color)
    {
        lock (str)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(str);
            Console.ResetColor();
        }
    }

    public bool MakeMove(int moveIndex) => MakeMove(1, moveIndex);

    public bool MakeMove(int playerIndex, int moveIndex)
    {
        if (BattleStream.IsDisposed)
            return false;
        if (playerIndex == 2 && Player2HasToWait)
        {
            Console.WriteLine("Player 2 waits");
            Player2HasToWait = false;
            return true;
        }

        if (playerIndex == 1)
        {
            if (!CanChoose || HasToSwitch)
                return false;
            CanChoose = false;
        }

        string choice = moveIndex == -1 ? GetBestAction(playerIndex) : $"move {moveIndex}";
        BattleStream.Write(ProtocolCodec.EncodePlayerChoiceCommand(playerIndex, choice));
        return true;
    }

    public bool MakeSwitch(string pokemon) => MakeSwitch(1, pokemon);

    public bool MakeSwitch(int playerIndex, string pokemon)
    {
        if (BattleStream.IsDisposed)
            return false;
        if (playerIndex == 1)
        {
            if (!CanChoose)
                return false;
            CanChoose = false;
        }

        BattleStream.Write(ProtocolCodec.EncodePlayerChoiceCommand(playerIndex, $"switch {pokemon}"));
        return true;
    }

    public string GetBestAction(int playerIndex)
    {
        _ = WildNPCIndex;
        return "default";
    }

    public bool AutoRespond(int playerIndex)
    {
        if (playerIndex == 2)
        {
            if (WildNPCIndex.HasValue)
                return MakeMove(2, -1);
            return false;
        }

        return MakeMove(1, -1);
    }

    #endregion

    public void Stop()
    {
        var p1 = Player1;
        var p2 = Player2;
        var w = WildNPC;

        if (w != null)
            w.EndBattle();
        else
            BattleStream?.Dispose();

        p1.Battle = null;
        if (p2 != null)
            p2.Battle = null;

        TestBattleUI.Close();
    }
}