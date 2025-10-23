using Showdown.NET.Definitions;
using Showdown.NET.Protocol;
using Showdown.NET.Simulator;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using Terramon.Content.GUI.TurnBased;
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
            s.Write(ProtocolCodec.EncodePlayerChoiceCommand(1, "team", p1teamOrder));
            s.Write(ProtocolCodec.EncodePlayerChoiceCommand(2, "team 123456"));

            await foreach (var output in s.ReadOutputsAsync())
            {
                if (ShouldStop)
                    break;

                var frame = ProtocolCodec.Parse(output);
                if (frame == null || frame.Elements == null) continue;
                // Console.WriteLine($"Received message of type {frame.GetType().Name} from simulator");
                foreach (var element in CollectionsMarshal.AsSpan(frame.Elements))
                {
                    var finalElement = element is ISplitElement split ? split.Secret : element;
                    if (HandleSingleElement(finalElement, modPlayer, p2, wild))
                        Console.WriteLine(finalElement);
                }
            }
        }
        catch (Exception ex)
        {
            ConsoleWriteColor($"Battle encountered an error: {ex.GetType()}: {ex.Message}", ConsoleColor.Red);
            ConsoleWriteColor(ex.StackTrace ?? "No stack trace.", ConsoleColor.Red);
            Stop();
        }
        finally
        {
            if (BattleStream != null && BattleStream.IsDisposed)
            {
                ConsoleWriteColor($"Disposing battle stream", ConsoleColor.Yellow);
                BattleStream.Dispose();
            }
        }
    }
    /// <summary>
    /// Gets the corresponding Pokémon data for a Pokémon given its ID as output by Showdown. 
    /// </summary>
    /// <param name="showdownMon"></param>
    private void GetPokemonFromShowdown(string showdownMon, out TerramonPlayer player, out PokemonNPC wild, out PokemonData poke, out string monMessage)
    {
        var finalID = PokemonID.Parse(showdownMon);
        int plr = finalID.Player;
        player = plr == 1 ? Player1 : Player2;
        if (player is null)
        {
            wild = WildNPC;
            poke = wild.Data;
        }
        else
        {
            wild = null;
            poke = player.GetPokemonFromShowdown(finalID.Name);
        }
        monMessage = $"{(wild is null ? $"{player.Player.name}'s" : "Wild")} {poke.DisplayName}";
    }
    private void GetPokemonFromShowdown(string showdownMon, out TerramonPlayer player, out PokemonNPC wild, out PokemonData poke)
    {
        GetPokemonFromShowdown(showdownMon, out player, out wild, out poke, out _);
    }
    private bool HandleSingleElement(ProtocolElement element, TerramonPlayer p1, TerramonPlayer p2, PokemonNPC wild)
    {
        switch (element)
        {
            // nothings (or skip)
            case TimestampElement:
            case GameTypeElement:
            case PlayerDetailsElement:
            case GenElement:
            case TierElement:
            case RatedElement:
            case RuleElement:
            case PokeElement:
            case TeamSizeElement:
            case InactiveElement:
            case InactiveOffElement:
            case UpkeepElement:
            case DebugElement:
                return false;
            // spacers
            case ClearPokeElement:
            case TeamPreviewElement:
            case SpacerElement:
            case StartElement:
                Console.WriteLine();
                return false;
            case MoveElement moveMessage:
                GetPokemonFromShowdown(moveMessage.Pokemon, out _, out _, out _, out var monMessage);
                ConsoleWriteColor($"{monMessage} used {moveMessage.Move}!", ConsoleColor.Yellow);
                return false;
            case FailElement:
                ConsoleWriteColor("But it failed!", ConsoleColor.Yellow);
                return false;
            case BlockElement blockMessage:
                var name = PokemonID.Parse(blockMessage.Pokemon).Name;
                ConsoleWriteColor($"But {name} blocked it!", ConsoleColor.Yellow);
                return false;
            case MissElement:
                ConsoleWriteColor("But it missed!", ConsoleColor.Yellow);
                return false;
            case SwitchElement switchMessage:
                GetPokemonFromShowdown(switchMessage.Pokemon, out var plr, out _, out var pkd);
                if (plr != null)
                    ConsoleWriteColor($"Player {plr.Player.name} switches to {pkd.DisplayName}", ConsoleColor.Magenta);
                return false;
            case DragElement dragMessage:
                GetPokemonFromShowdown(dragMessage.Pokemon, out plr, out _, out pkd);
                if (plr != null)
                    ConsoleWriteColor($"Player {plr.Player.name} had their Pokémon forcefully switched to {pkd.DisplayName}!", ConsoleColor.DarkMagenta);
                return false;
            case DetailsChangeElement detailsChangeMessage:
                // THINGS
                ConsoleWriteColor(nameof(DetailsChangeElement), ConsoleColor.DarkRed);
                return false;
            case FormeChangeElement formeChangeMessage:
                // THINGS
                ConsoleWriteColor(nameof(FormeChangeElement), ConsoleColor.DarkRed);
                return false;
            case ReplaceElement replaceMessage:
                // Illusion does bring about an interesting question.
                // i think when starting a battle with someone,you should wait until both have sent their teams,
                // which is obvious, but then after that, a client should override what it thinks a remote client's
                // team looks like given what's given in a public PokeElement (or wherever the fake Pokémon imitated by Illusion is shown to a client)
                // otherwise, GetPokemonFromShowdown is gonna spazz out, 100%
                // THINGS
                ConsoleWriteColor(nameof(ReplaceElement), ConsoleColor.DarkRed);
                return false;
            case SwapElement swapMessage:
                // idk what this is tbh
                ConsoleWriteColor(nameof(SwapElement), ConsoleColor.DarkRed);
                return false;
            case CantElement cantMessage:
                GetPokemonFromShowdown(cantMessage.Pokemon, out _, out _, out _, out monMessage);
                ConsoleWriteColor($"{monMessage} couldn't use the move{(cantMessage.Move is null ? string.Empty : $" {cantMessage.Move}")}: {cantMessage.Reason}", ConsoleColor.DarkYellow);
                return false;
            case TurnElement turnMessage:
                Console.WriteLine($"It is turn {turnMessage.Number}.");
                return false;
            case RequestElement request:
                JsonObject o = JsonSerializer.Deserialize<JsonObject>(request.Request);
                if (o.ContainsKey("teamPreview"))
                    return false;
                var side = o["side"];
                if (side is null)
                    return false;
                int plrID = side["id"].ToString()[1] - '0';
                bool forceSwitch = o.ContainsKey("forceSwitch");
                bool wait = o.ContainsKey("wait");
                ConsoleWriteColor($"Request was made for player {plrID} to {(forceSwitch ? "switch Pokémon" : wait ? "wait" : "make a move")}", ConsoleColor.Green);
                if (plrID == 1)
                {
                    CanChoose = true;
                    HasToSwitch = forceSwitch;
                }
                else if (plrID == 2)
                {
                    if (wait)
                        Player2HasToWait = true;
                }
                return false;
            case ErrorElement error:
                if (error.Type == ErrorType.Other)
                    return true;
                CanChoose = true;
                ConsoleWriteColor(error, ConsoleColor.Red);
                return false;
            case DamageElement damageMessage:
                GetPokemonFromShowdown(damageMessage.Pokemon, out _, out _, out var targetMon, out monMessage);
                ushort newHP = ushort.Parse(damageMessage.HP.Split('/', 2)[0]);
                ConsoleWriteColor($"{monMessage} got hit and lost {targetMon.HP - newHP} HP!", ConsoleColor.Magenta);
                targetMon.HP = newHP;
                return false;
            case CritElement:
                ConsoleWriteColor("It was a critical hit!", ConsoleColor.DarkYellow);
                return false;
            case SuperEffectiveElement:
                ConsoleWriteColor("It was super effective!", ConsoleColor.DarkYellow);
                return false;
            case ResistedElement:
                ConsoleWriteColor("It wasn't very effective...", ConsoleColor.DarkYellow);
                return false;
            case ImmuneElement immuneMessage:
                GetPokemonFromShowdown(immuneMessage.Pokemon, out _, out _, out _, out monMessage);
                ConsoleWriteColor($"But {monMessage} was immune!", ConsoleColor.DarkYellow);
                return false;
            case HitCountElement hitCountMessage:
                GetPokemonFromShowdown(hitCountMessage.Pokemon, out _, out _, out _, out monMessage);
                ConsoleWriteColor($"{monMessage} was hit {hitCountMessage.Num} times!", ConsoleColor.DarkYellow);
                return false;
            case HealElement healMessage:
                GetPokemonFromShowdown(healMessage.Pokemon, out _, out _, out targetMon, out monMessage);
                newHP = ushort.Parse(healMessage.HP.Split('/', 2)[0]);
                ConsoleWriteColor($"{monMessage} was healed for {newHP - targetMon.HP}!", ConsoleColor.Green);
                targetMon.HP = newHP;
                return false;
            case SetHPElement setHPMessage:
                GetPokemonFromShowdown(setHPMessage.Pokemon, out _, out _, out targetMon, out monMessage);
                newHP = ushort.Parse(setHPMessage.HP.Split('/', 2)[0]);
                ConsoleWriteColor($"{monMessage} now has {newHP} HP!", ConsoleColor.Yellow);
                targetMon.HP = newHP;
                return false;
            case StatusElement statusMessage:
                GetPokemonFromShowdown(statusMessage.Pokemon, out _, out _, out targetMon, out monMessage);
                ConsoleWriteColor($"{monMessage} has been inflicted with the {statusMessage.Status} status!", ConsoleColor.DarkYellow);
                targetMon.Status = statusMessage.Status;
                return false;
            case CureStatusElement cureStatusMessage:
                GetPokemonFromShowdown(cureStatusMessage.Pokemon, out _, out _, out targetMon, out monMessage);
                ConsoleWriteColor($"{monMessage} has recovered from {cureStatusMessage.Status}!", ConsoleColor.DarkGreen);
                targetMon.CureStatus();
                return false;
            case CureTeamElement cureTeamMessage:
                int playerTeam = cureTeamMessage.Pokemon[1] - '0';

                ConsoleWriteColor($"Everyone in team {playerTeam} has been healed!", ConsoleColor.Green);

                PokemonData[] party = null;
                if (playerTeam == 2)
                {
                    if (p2 != null)
                        party = p2.Party;
                    else
                        wild.Data.CureStatus();
                }
                else
                    party = p1.Party;
                if (party != null)
                    foreach (var member in party)
                        member.CureStatus();
                return false;
            case BoostElement boostMessage:
                GetPokemonFromShowdown(boostMessage.Pokemon, out _, out _, out targetMon, out monMessage);
                var stat = boostMessage.Stat;
                ConsoleWriteColor($"{monMessage} has had their {stat} raised by {boostMessage.Amount}!", ConsoleColor.DarkYellow);
                int currentStage = targetMon.StatStages[stat];
                int newStage = Math.Clamp(currentStage + boostMessage.Amount, -7, 7);
                if (currentStage != newStage)
                    targetMon.StatStages[stat] = newStage;
                else
                    ConsoleWriteColor($"But the cap for {stat} was reached!", ConsoleColor.DarkGray);
                return false;
            case UnboostElement unboostMessage:
                GetPokemonFromShowdown(unboostMessage.Pokemon, out _, out _, out targetMon, out monMessage);
                stat = unboostMessage.Stat;
                ConsoleWriteColor($"{monMessage} has had their {stat} lowered by {unboostMessage.Amount}!", ConsoleColor.DarkYellow);
                currentStage = targetMon.StatStages[stat];
                newStage = Math.Clamp(currentStage - unboostMessage.Amount, -7, 7);
                if (currentStage != newStage)
                    targetMon.StatStages[stat] = newStage;
                else
                    ConsoleWriteColor($"But {stat} is already as low as possible!", ConsoleColor.DarkGray);
                return false;
            case SetBoostElement setBoostMessage:
                GetPokemonFromShowdown(setBoostMessage.Pokemon, out _, out _, out targetMon, out monMessage);
                stat = setBoostMessage.Stat;
                ConsoleWriteColor($"{monMessage} has had their {stat} set to {setBoostMessage.Amount}!", ConsoleColor.DarkYellow);
                newStage = Math.Clamp(setBoostMessage.Amount, -7, 7);
                if (setBoostMessage.Amount != newStage)
                    targetMon.StatStages[stat] = newStage;
                else
                    ConsoleWriteColor($"But the value was clamped!", ConsoleColor.DarkGray);
                return false;
            case StartVolatileElement startVolatileMessage:
                GetPokemonFromShowdown(startVolatileMessage.Pokemon, out _, out _, out _, out monMessage);
                ConsoleWriteColor($"Status effect {startVolatileMessage.Effect} was applied to {monMessage}.", ConsoleColor.DarkCyan);
                return false;
            case EndVolatileElement endVolatileMessage:
                GetPokemonFromShowdown(endVolatileMessage.Pokemon, out _, out _, out _, out monMessage);
                ConsoleWriteColor($"Status effect {endVolatileMessage.Effect} ended for {monMessage}.", ConsoleColor.DarkCyan);
                return false;
            case FaintElement faintMessage:
                GetPokemonFromShowdown(faintMessage.Pokemon, out _, out _, out pkd, out monMessage);
                ConsoleWriteColor($"{monMessage} has fainted!", ConsoleColor.DarkRed);
                pkd.Faint();
                return false;
            case WinElement winMessage:
                if (p1.Player.name == winMessage.Username)
                    ConsoleWriteColor("You winned :)", ConsoleColor.Green);
                else if (p2 != null && p2.Player.name == winMessage.Username)
                    ConsoleWriteColor("The other guy winned :/", ConsoleColor.DarkGreen);
                else
                    ConsoleWriteColor("That wild mon done did wonned...", ConsoleColor.DarkGreen);
                EndEverywhere();
                return false;
            case TieElement:
                ConsoleWriteColor("The battle has ended in a tie.", ConsoleColor.Yellow);
                EndEverywhere();
                return true;

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
        if (BattleStream.IsDisposed)
            return false;
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

    public void EndEverywhere()
    {
        var p1 = Player1;
        var p2 = Player2;

        WildNPC?.EndBattle();
        p1.Battle = null;
        if (p2 != null)
            p2.Battle = null;

        TestBattleUI.Close();
    }
}