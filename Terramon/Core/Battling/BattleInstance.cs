using Microsoft.Build.Logging;
using Showdown.NET.Definitions;
using Showdown.NET.Protocol;
using Showdown.NET.Simulator;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using Terramon.Content.Commands;
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

            // TODO: change team order for p1 to accurately reflect the active pokemon
            s.Write(ProtocolCodec.EncodePlayerChoiceCommand("p1", "team 123456"));
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
                    Console.WriteLine(element);
                    if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(SplitElement<>))
                        HandleSingleElement((ProtocolElement)t.GetProperty("Secret").GetValue(element));
                    else
                        HandleSingleElement(element);
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
    private void HandleSingleElement(ProtocolElement element)
    {
        var p2 = Player2;
        switch (element)
        {
            case RequestElement request:
                JsonObject o = JsonSerializer.Deserialize<JsonObject>(request.Request);
                if (o.ContainsKey("teamPreview"))
                    break;
                var side = o["side"];
                if (side is null)
                    break;
                int plr = side["id"].ToString()[1] - '0';
                bool forceSwitch = o.ContainsKey("forceSwitch");
                ConsoleWriteColor($"Request was made for player {plr} to {(forceSwitch ? "switch Pokémon" : "make a move")}", ConsoleColor.Green);
                if (plr == 1)
                {
                    CanChoose = true;
                    HasToSwitch = forceSwitch;
                    ConsoleWriteColor("Player one can make a move now", ConsoleColor.Green);
                }
                break;
            case DamageElement damageMessage:
                int playerOwningPokemon = damageMessage.Pokemon[1] - '0';
                ushort newHP = ushort.Parse(damageMessage.HP.Split('/', 2)[0]);
                PokemonData targetMon = playerOwningPokemon switch
                {
                    1 => Player1.GetActivePokemon(),
                    2 => WildNPC?.Data ?? Player2.GetActivePokemon(),
                    _ => null,
                };
                ConsoleWriteColor($"{targetMon.DisplayName} got hit and lost {targetMon.HP - newHP} HP!", ConsoleColor.Magenta);
                targetMon.HP = newHP;
                // this might not be needed for anything actually
                // p2.GetPokemonFromShowdown(damageMessage.Pokemon.Split(' ', 2)[1], damageMessage.Pokemon[2]).HP = newHP;
                break;
            case WinElement winMessage:
                if (Player1.Player.name == winMessage.Username)
                    ConsoleWriteColor("You winned :)", ConsoleColor.Green);
                else if (p2 != null && p2.Player.name == winMessage.Username)
                    ConsoleWriteColor("The other guy winned :/", ConsoleColor.DarkGreen);
                else
                    ConsoleWriteColor("That wild mon done did wonned...", ConsoleColor.DarkGreen);
                Player1.Battle = null;
                WildNPC?.EndBattle();
                TestBattleUI.Close();
                break;
        }
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
        Console.WriteLine($"Player {playerIndex} plays move '{moveIndex}'");
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
        Console.WriteLine($"Player {playerIndex} switches to Pokémon '{pokemon}'");
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

    #endregion

    public void Stop()
    {
        ShouldStop = true;
    }
}