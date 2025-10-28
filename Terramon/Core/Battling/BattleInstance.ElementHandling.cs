using Showdown.NET.Definitions;
using Showdown.NET.Protocol;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Terramon.Core.Battling;
public sealed partial class BattleInstance
{
    private const ConsoleColor BattleAction = ConsoleColor.Yellow;
    private const ConsoleColor BattleFollowup = ConsoleColor.DarkYellow;
    private const ConsoleColor BattleReceive = ConsoleColor.Magenta;
    private const ConsoleColor BattleReceiveFollowup = ConsoleColor.DarkMagenta;
    private const ConsoleColor Meta = ConsoleColor.Cyan;
    private const ConsoleColor MetaProgress = ConsoleColor.DarkCyan;
    private const ConsoleColor MetaFollowup = ConsoleColor.DarkGray;
    private const ConsoleColor ChronoAction = ConsoleColor.Blue;
    private const ConsoleColor FieldAction = ConsoleColor.DarkBlue;
    private const ConsoleColor Faint = ConsoleColor.Red;
    private const ConsoleColor Error = ConsoleColor.DarkRed;
    private const ConsoleColor Win = ConsoleColor.Green;
    private const ConsoleColor NotWin = ConsoleColor.DarkGreen;
    private void HandleSingleElement_Inner(ProtocolElement element, in ShowdownPokemonData source, in ShowdownPokemonData target,
        TerramonPlayer p1, TerramonPlayer p2, PokemonData[] foeTeam)
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
            case TeamSizeElement:
            case InactiveElement:
            case InactiveOffElement:
            case UpkeepElement:
            case HintElement:
            case CenterElement:
            case MessageElement:
            case DebugElement:
                break;
            // spacers
            case ClearPokeElement:
            case TeamPreviewElement:
            case SpacerElement:
            case StartElement:
                Console.WriteLine();
                break;
            // basic info
            case PokeElement pokeMessage:
                Details d = Details.Parse(pokeMessage.Details);
                ConsoleWrite($"Player: {pokeMessage.Player}, Species: {d.Species}, Level: {d.Level}, Gender: {d.Gender ?? 'N'}, Shiny: {d.Shiny}", ConsoleColor.Cyan);
                break;
            case MoveElement moveMessage:
                ConsoleWrite($"{source} used {moveMessage.Move}!", BattleAction);
                break;
            case FailElement:
            case NoTargetElement:
                ConsoleWrite("But it failed!", BattleFollowup);
                break;
            case BlockElement blockMessage:
                var name = PokemonID.Parse(blockMessage.Pokemon).Name;
                ConsoleWrite($"But {name} blocked it!", BattleFollowup);
                break;
            case MissElement:
                ConsoleWrite("But it missed!", BattleFollowup);
                break;
            case SwitchElement switchMessage:
                if (target.Player != null)
                    ConsoleWrite($"Player {target.PlayerName} switches to {target.PokeName}", BattleAction);
                break;
            case DragElement dragMessage:
                if (target.Player != null)
                    ConsoleWrite($"Player {target.PlayerName} had their Pokémon forcefully switched to {target.PokeName}!", BattleAction);
                break;
            case DetailsChangeElement detailsChangeMessage:
                // THINGS
                ConsoleWrite(nameof(DetailsChangeElement), Error);
                break;
            case FormeChangeElement formeChangeMessage:
                // THINGS
                ConsoleWrite(nameof(FormeChangeElement), Error);
                break;
            case ReplaceElement replaceMessage:
                // Illusion does bring about an interesting question.
                // i think when starting a battle with someone,you should wait until both have sent their teams,
                // which is obvious, but then after that, a client should override what it thinks a remote client's
                // team looks like given what's given in a public PokeElement (or wherever the fake Pokémon imitated by Illusion is shown to a client)
                // otherwise, GetPokemonFromShowdown is gonna spazz out, 100%
                // THINGS
                ConsoleWrite(nameof(ReplaceElement), Error);
                break;
            case SwapElement swapMessage:
                // idk what this is tbh
                ConsoleWrite(nameof(SwapElement), Error);
                break;
            case CantElement cantMessage:
                ConsoleWrite($"{target} couldn't use the move {cantMessage.Move}: {cantMessage.Reason}", BattleFollowup);
                break;
            case TurnElement turnMessage:
                ConsoleWrite($"It is turn {turnMessage.Number}", MetaProgress);
                break;
            case RequestElement request:
                JsonObject o = JsonSerializer.Deserialize<JsonObject>(request.Request);
                if (o.ContainsKey("teamPreview"))
                    break;
                var side = o["side"];
                if (side is null)
                    break;
                int plrID = side["id"].ToString()[1] - '0';
                bool forceSwitch = o.ContainsKey("forceSwitch");
                bool wait = o.ContainsKey("wait");
                ConsoleWrite($"Request was made for player {plrID} to {(forceSwitch ? "switch Pokémon" : wait ? "wait" : "make a move")}", Meta);
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
                break;
            case ErrorElement error:
                ConsoleWrite(error.ToString(), Error);
                if (error.Type == ErrorType.Other)
                    break;
                CanChoose = true;
                break;
            case DamageElement damageMessage:
                ushort newHP = ushort.Parse(damageMessage.HP.Split('/', 2)[0]);
                ConsoleWrite($"{target} got hit and lost {target.HP - newHP} HP!", BattleReceive);
                target.HP = newHP;
                break;
            case HealElement healMessage:
                newHP = ushort.Parse(healMessage.HP.Split('/', 2)[0]);
                ConsoleWrite($"{target} was healed for {newHP - target.HP}!", BattleReceive);
                target.HP = newHP;
                break;
            case SetHPElement setHPMessage:
                newHP = ushort.Parse(setHPMessage.HP.Split('/', 2)[0]);
                ConsoleWrite($"{target} now has {newHP} HP!", BattleReceive);
                target.HP = newHP;
                break;
            case CritElement:
                ConsoleWrite("It was a critical hit!", BattleReceiveFollowup);
                break;
            case SuperEffectiveElement:
                ConsoleWrite("It was super effective!", BattleReceiveFollowup);
                break;
            case ResistedElement:
                ConsoleWrite("It wasn't very effective...", BattleReceiveFollowup);
                break;
            case ImmuneElement immuneMessage:
                ConsoleWrite($"But {target} was immune!", BattleFollowup);
                break;
            case HitCountElement hitCountMessage:
                ConsoleWrite($"{target} was hit {hitCountMessage.Num} times!", BattleReceiveFollowup);
                break;
            case StatusElement statusMessage:
                ConsoleWrite($"{target} has been inflicted with the {statusMessage.Status} status!", BattleReceiveFollowup);
                target.Status = statusMessage.Status;
                break;
            case CureStatusElement cureStatusMessage:
                ConsoleWrite($"{target} has recovered from {cureStatusMessage.Status}!", ChronoAction);
                target.Data.CureStatus();
                break;
            case CureTeamElement cureTeamMessage:
                int playerTeam = cureTeamMessage.Pokemon[1] - '0';

                ConsoleWrite($"Everyone in team {playerTeam} has been healed!", BattleReceive);

                PokemonData[] party = playerTeam == 1 ? p1.Party : foeTeam;
                foreach (var member in party)
                    member?.CureStatus();
                break;
            case BoostElement boostMessage:
                var stat = boostMessage.Stat;
                ConsoleWrite($"{target} has had their {stat} raised by {boostMessage.Amount}!", BattleReceiveFollowup);
                int currentStage = target.StatStages[stat];
                int newStage = Math.Clamp(currentStage + boostMessage.Amount, -7, 7);
                if (currentStage != newStage)
                    target.StatStages[stat] = newStage;
                else
                    ConsoleWrite($"But the cap for {stat} was reached!", MetaFollowup);
                break;
            case UnboostElement unboostMessage:
                stat = unboostMessage.Stat;
                ConsoleWrite($"{target} has had their {stat} lowered by {unboostMessage.Amount}!", BattleReceiveFollowup);
                currentStage = target.StatStages[stat];
                newStage = Math.Clamp(currentStage - unboostMessage.Amount, -7, 7);
                if (currentStage != newStage)
                    target.StatStages[stat] = newStage;
                else
                    ConsoleWrite($"But {stat} is already as low as possible!", MetaFollowup);
                break;
            case SetBoostElement setBoostMessage:
                stat = setBoostMessage.Stat;
                ConsoleWrite($"{target} has had their {stat} set to {setBoostMessage.Amount}!", BattleReceiveFollowup);
                newStage = Math.Clamp(setBoostMessage.Amount, -7, 7);
                if (setBoostMessage.Amount != newStage)
                    target.StatStages[stat] = newStage;
                else
                    ConsoleWrite($"But the value was clamped!", MetaFollowup);
                break;
            case SwapBoostElement swapBoostMessage:
                var stats = swapBoostMessage.Stats;
                ConsoleWrite($"{source} swapped their own {string.Join(", ", stats)} with {target}'s!", BattleAction);
                for (int i = 0; i < stats.Length; i++)
                {
                    stat = stats[i];
                    (target.StatStages[stat], source.StatStages[stat]) = (source.StatStages[stat], target.StatStages[stat]);
                }
                break;
            case InvertBoostElement invertBoostMessage:
                ConsoleWrite($"{target} has had their stat boosts inverted!", BattleReceive);
                for (int i = 0; i <= (int)StatID.Spe; i++)
                {
                    target.StatStages.Packed ^= (uint)(0b1000 << (i * 4));
                }
                break;
            case ClearBoostElement clearBoostMessage:
                ConsoleWrite($"{target} has had their stat boosts reset to 0!", BattleReceive);
                target.StatStages.Packed = 0;
                break;
            case ClearAllBoostElement clearAllBoostMessage:
                foreach (var member in p1.Party)
                {
                    if (member != null)
                        member.StatStages.Packed = 0;
                }
                foreach (var member in foeTeam)
                {
                    if (member != null)
                        member.StatStages.Packed = 0;
                }
                break;
            case ClearPositiveBoostElement clearPositiveBoostMessage:
                ConsoleWrite($"{target} had their positive stat boosts reset to 0!", BattleReceive);
                for (int i = 0; i <= (int)StatID.Spe; i++)
                {
                    uint mask = (uint)(0b1000 << (i * 4));
                    if ((target.StatStages.Packed & mask) != 0)
                        target.StatStages.Packed ^= mask;
                }
                break;
            case ClearNegativeBoostElement clearNegativeBoostMessage:
                ConsoleWrite($"{target} had their negative stat boosts reset to 0!", BattleReceive);
                for (int i = 0; i <= (int)StatID.Spe; i++)
                {
                    uint mask = (uint)(0b1000 << (i * 4));
                    if ((target.StatStages.Packed & mask) == 0)
                        target.StatStages.Packed ^= mask;
                }
                break;
            case CopyBoostElement copyBoostMessage:
                ConsoleWrite($"{target} copied {source}'s stat boosts!", BattleAction);
                target.StatStages.Packed = source.StatStages.Packed;
                break;
            case WeatherElement weatherMessage:
                if (weatherMessage.Weather is null)
                    ConsoleWrite("The weather has expired!", FieldAction);
                else if (!weatherMessage.Upkeep)
                    ConsoleWrite($"The weather is now {weatherMessage.Weather}.", FieldAction);
                break;
            case FieldStartElement fieldStartMessage:
                ConsoleWrite($"Field condition {fieldStartMessage.Condition} has started.", FieldAction);
                break;
            case FieldEndElement fieldEndMessage:
                ConsoleWrite($"Field condition {fieldEndMessage.Condition} has ended.", FieldAction);
                break;
            case SideStartElement sideStartMessage:
                ConsoleWrite($"Side condition {sideStartMessage.Condition} has started for side {sideStartMessage.Side}!", FieldAction);
                break;
            case SideEndElement sideEndMessage:
                ConsoleWrite($"Side condition {sideEndMessage.Condition} has ended for side {sideEndMessage.Side}!", FieldAction);
                break;
            case SwapSideConditionsElement swapSideConditionsMessage:
                // we probably wanna keep track of side conditions in two variables eventually, for the visual effects
                // THINGS
                break;
            case StartVolatileElement startVolatileMessage:
                ConsoleWrite($"Volatile effect {startVolatileMessage.Effect} was applied to {target}.", BattleReceive);
                break;
            case EndVolatileElement endVolatileMessage:
                ConsoleWrite($"Volatile effect {endVolatileMessage.Effect} ended for {target}.", ChronoAction);
                break;
            case FaintElement faintMessage:
                ConsoleWrite($"{target} has fainted!", Faint);
                target.Data.Faint();
                break;
            case WinElement winMessage:
                if (p1.Player.name == winMessage.Username)
                    ConsoleWrite("You winned :)", Win);
                else if (p2 != null && p2.Player.name == winMessage.Username)
                    ConsoleWrite("The other guy winned :/", NotWin);
                else
                    ConsoleWrite("That wild mon done did wonned...", NotWin);
                Stop();
                break;
            case TieElement:
                ConsoleWrite("The battle has ended in a tie.", NotWin);
                Stop();
                break;
        }
    }
}
