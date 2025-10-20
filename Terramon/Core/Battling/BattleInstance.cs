using Showdown.NET.Definitions;
using Showdown.NET.Protocol;
using Showdown.NET.Simulator;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using Terramon.Content.GUI;
using Terramon.Content.NPCs;

namespace Terramon.Core.Battling;

public class BattleInstance
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

    public bool ShouldStop { get; private set; }

    public bool CanChoose { get; private set; }

    public bool HasToSwitch { get; private set; }

    public bool Player2HasToWait { get; private set; }

    public void Update()
    {
        TickCount++;
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

            ConsoleWriteColor($"Battle started by {player.name} against {otherName}", ConsoleColor.Blue);

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

            string p1activeSlot = (modPlayer.ActiveSlot + 1).ToString();
            string p1teamOrder = p1activeSlot + "123456".Replace(p1activeSlot, string.Empty);
            s.Write(ProtocolCodec.EncodePlayerChoiceCommand("p1", "team", p1teamOrder));
            s.Write(ProtocolCodec.EncodePlayerChoiceCommand("p2", "team 123456"));

            await foreach (var output in s.ReadOutputsAsync())
            {
                if (ShouldStop)
                    break;

                var frame = ProtocolCodec.Parse(output);
                if (frame == null || frame.Elements == null) continue;
                Console.WriteLine($"Received message of type {frame.GetType().Name} from simulator");
                foreach (var element in CollectionsMarshal.AsSpan(frame.Elements))
                {
                    Type t = element.GetType();
                    var finalElement = element;
                    if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(SplitElement<>))
                        finalElement = (ProtocolElement)t.GetProperty("Secret").GetValue(element);
                    if (HandleSingleElement(finalElement, modPlayer, p2, wild))
                        Console.WriteLine(finalElement);
                }
            }
        }
        catch (Exception ex)
        {
            ConsoleWriteColor($"Battle encountered an error: {ex.Message}", ConsoleColor.Red);
            Stop();
        }
        finally
        {
            ConsoleWriteColor($"Disposing battle stream", ConsoleColor.Yellow);
            BattleStream?.Dispose();
        }
    }
    /// <summary>
    /// Gets the corresponding Pokémon data for a Pokémon given its ID as output by Showdown. 
    /// </summary>
    /// <param name="showdownMon"></param>
    private void GetPokemonFromShowdown(string showdownMon, out TerramonPlayer player, out PokemonNPC wild, out PokemonData poke)
    {
        int plr = showdownMon[1] - '0';
        ReadOnlySpan<char> pokeName = showdownMon.AsSpan()[5..];
        player = plr == 1 ? Player1 : Player2;
        if (player is null)
        {
            wild = WildNPC;
            poke = wild.Data;
        }
        else
        {
            wild = null;
            poke = player.GetPokemonFromShowdown(pokeName);
        }
    }
    private bool HandleSingleElement(ProtocolElement element, TerramonPlayer p1, TerramonPlayer p2, PokemonNPC wild)
    {
        switch (element)
        {
            // nothings
            case TimestampElement:
            case GameTypeElement:
            case PlayerDetailsElement:
            case GenElement:
            case TierElement:
            case PokeElement:
            case TeamSizeElement:
            case UpkeepElement:
                return false;
            // spacers
            case ClearPokeElement:
            case TeamPreviewElement:
            case SpacerElement:
            case StartElement:
                Console.WriteLine();
                return false;
            case MoveElement moveMessage:
                GetPokemonFromShowdown(moveMessage.Pokemon, out var mplr, out var mw, out var mpkd);
                ConsoleWriteColor($"{(mw is null ? $"{mplr.Player.name}'s" : "Wild")} {mpkd.DisplayName} used {moveMessage.Move}!", ConsoleColor.Yellow);
                return false;
            case SwitchElement switchMessage:
                GetPokemonFromShowdown(switchMessage.Pokemon, out var splr, out _, out var spkd);
                if (splr != null)
                    ConsoleWriteColor($"Player {splr.Player.name} switches to {spkd.DisplayName}", ConsoleColor.Magenta);
                return false;
            case TurnElement turnMessage:
                Console.WriteLine($"It is turn {turnMessage.Number}");
                return false;
            case RequestElement request:
                JsonObject o = JsonSerializer.Deserialize<JsonObject>(request.Request);
                if (o.ContainsKey("teamPreview"))
                    return false;
                var side = o["side"];
                if (side is null)
                    return false;
                int plr = side["id"].ToString()[1] - '0';
                bool forceSwitch = o.ContainsKey("forceSwitch");
                bool wait = o.ContainsKey("wait");
                ConsoleWriteColor($"Request was made for player {plr} to {(forceSwitch ? "switch Pokémon" : wait ? "wait" : "make a move")}", ConsoleColor.Green);
                if (plr == 1)
                {
                    CanChoose = true;
                    HasToSwitch = forceSwitch;
                }
                else if (plr == 2)
                {
                    if (wait)
                        Player2HasToWait = true;
                }
                return false;
            case DamageElement damageMessage:
                GetPokemonFromShowdown(damageMessage.Pokemon, out var dplr, out var dw, out var targetMon);
                ushort newHP = ushort.Parse(damageMessage.HP.Split('/', 2)[0]);
                ConsoleWriteColor($"{(dw is null ? $"{dplr.Player.name}'s" : "Wild")} {targetMon.DisplayName} got hit and lost {targetMon.HP - newHP} HP!", ConsoleColor.Magenta);
                targetMon.HP = newHP;
                return false;
            case FaintElement faintMessage:
                GetPokemonFromShowdown(faintMessage.Pokemon, out var fplr, out var fw, out var fpkd);
                ConsoleWriteColor($"{(fw is null ? $"{fplr.Player.name}'s" : "Wild")} {fpkd.DisplayName} has fainted!", ConsoleColor.DarkRed);
                return false;
            case WinElement winMessage:
                if (p1.Player.name == winMessage.Username)
                    ConsoleWriteColor("You winned :)", ConsoleColor.Green);
                else if (p2 != null && p2.Player.name == winMessage.Username)
                    ConsoleWriteColor("The other guy winned :/", ConsoleColor.DarkGreen);
                else
                    ConsoleWriteColor("That wild mon done did wonned...", ConsoleColor.DarkGreen);
                p1.Battle = null;
                WildNPC?.EndBattle();
                TestBattleUI.Close();
                return false;
        }
        return true;
    }
    private static void ConsoleWriteColor(object obj, ConsoleColor color)
    {
        var prev = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine(obj);
        Console.ForegroundColor = prev;
    }
    public bool MakeMove(int moveIndex) => MakeMove(1, moveIndex);
    public bool MakeMove(int playerIndex, int moveIndex)
    {
        if (playerIndex == 2 && Player2HasToWait)
        {
            Console.WriteLine($"Player 2 waits");
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
        BattleStream.Write(ProtocolCodec.EncodePlayerChoiceCommand($"p{playerIndex}", choice));
        return true;
    }
    public bool MakeSwitch(string pokemon) => MakeSwitch(1, pokemon);
    public bool MakeSwitch(int playerIndex, string pokemon)
    {
        if (playerIndex == 1)
        {
            if (!CanChoose)
                return false;
            CanChoose = false;
        }
        BattleStream.Write(ProtocolCodec.EncodePlayerChoiceCommand($"p{playerIndex}", $"switch {pokemon}"));
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
            else
                return false;
        }
        return MakeMove(1, -1);
    }
    #endregion

    public void Stop()
    {
        ShouldStop = true;
    }
}