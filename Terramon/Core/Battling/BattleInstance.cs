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
    /// Gets the corresponding Pokémon data for a Pokémon given its ID as output by Showdown. 
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
    private bool HandleSingleElement(ProtocolElement element, TerramonPlayer p1, TerramonPlayer p2, PokemonNPC wild)
    {
        PokemonData[] foeTeam = wild is null ? p2.Party : [wild.Data];

        ShowdownPokemonData source = new();
        ShowdownPokemonData target = new();

        if (element is IPokemonArgs args)
        {
            GetPokemonFromShowdown(args.Source ?? args.Attacker, out source);
            GetPokemonFromShowdown(args.Target ?? args.Defender ?? args.Pokemon, out target);
        }

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
                return false;
            // spacers
            case ClearPokeElement:
            case TeamPreviewElement:
            case SpacerElement:
            case StartElement:
                Console.WriteLine();
                return false;
            // basic info
            case PokeElement pokeMessage:
                Details d = Details.Parse(pokeMessage.Details);
                ConsoleWrite($"Player: {pokeMessage.Player}, Species: {d.Species}, Level: {d.Level}, Gender: {d.Gender ?? 'N'}, Shiny: {d.Shiny}", ConsoleColor.Cyan);
                return false;
            case MoveElement moveMessage:
                ConsoleWrite($"{target} used {moveMessage.Move}!", BattleAction);
                return false;
            case FailElement:
            case NoTargetElement:
                ConsoleWrite("But it failed!", BattleFollowup);
                return false;
            case BlockElement blockMessage:
                var name = PokemonID.Parse(blockMessage.Pokemon).Name;
                ConsoleWrite($"But {name} blocked it!", BattleFollowup);
                return false;
            case MissElement:
                ConsoleWrite("But it missed!", BattleFollowup);
                return false;
            case SwitchElement switchMessage:
                if (target.Player != null)
                    ConsoleWrite($"Player {target.PlayerName} switches to {target.PokeName}", BattleAction);
                return false;
            case DragElement dragMessage:
                if (target.Player != null)
                    ConsoleWrite($"Player {target.PlayerName} had their Pokémon forcefully switched to {target.PokeName}!", BattleAction);
                return false;
            case DetailsChangeElement detailsChangeMessage:
                // THINGS
                ConsoleWrite(nameof(DetailsChangeElement), Error);
                return false;
            case FormeChangeElement formeChangeMessage:
                // THINGS
                ConsoleWrite(nameof(FormeChangeElement), Error);
                return false;
            case ReplaceElement replaceMessage:
                // Illusion does bring about an interesting question.
                // i think when starting a battle with someone,you should wait until both have sent their teams,
                // which is obvious, but then after that, a client should override what it thinks a remote client's
                // team looks like given what's given in a public PokeElement (or wherever the fake Pokémon imitated by Illusion is shown to a client)
                // otherwise, GetPokemonFromShowdown is gonna spazz out, 100%
                // THINGS
                ConsoleWrite(nameof(ReplaceElement), Error);
                return false;
            case SwapElement swapMessage:
                // idk what this is tbh
                ConsoleWrite(nameof(SwapElement), Error);
                return false;
            case CantElement cantMessage:
                ConsoleWrite($"{target} couldn't use the move {cantMessage.Move}: {cantMessage.Reason}", BattleFollowup);
                return false;
            case TurnElement turnMessage:
                ConsoleWrite($"It is turn {turnMessage.Number}", MetaProgress);
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
                return false;
            case ErrorElement error:
                if (error.Type == ErrorType.Other)
                    return true;
                CanChoose = true;
                ConsoleWrite(error.ToString(), Error);
                return false;
            case DamageElement damageMessage:
                ushort newHP = ushort.Parse(damageMessage.HP.Split('/', 2)[0]);
                ConsoleWrite($"{target} got hit and lost {target.HP - newHP} HP!", BattleReceive);
                target.HP = newHP;
                return false;
            case HealElement healMessage:
                newHP = ushort.Parse(healMessage.HP.Split('/', 2)[0]);
                ConsoleWrite($"{target} was healed for {newHP - target.HP}!", BattleReceive);
                target.HP = newHP;
                return false;
            case SetHPElement setHPMessage:
                newHP = ushort.Parse(setHPMessage.HP.Split('/', 2)[0]);
                ConsoleWrite($"{target} now has {newHP} HP!", BattleReceive);
                target.HP = newHP;
                return false;
            case CritElement:
                ConsoleWrite("It was a critical hit!", BattleReceiveFollowup);
                return false;
            case SuperEffectiveElement:
                ConsoleWrite("It was super effective!", BattleReceiveFollowup);
                return false;
            case ResistedElement:
                ConsoleWrite("It wasn't very effective...", BattleReceiveFollowup);
                return false;
            case ImmuneElement immuneMessage:
                ConsoleWrite($"But {target} was immune!", BattleFollowup);
                return false;
            case HitCountElement hitCountMessage:
                ConsoleWrite($"{target} was hit {hitCountMessage.Num} times!", BattleReceiveFollowup);
                return false;
            case StatusElement statusMessage:
                ConsoleWrite($"{target} has been inflicted with the {statusMessage.Status} status!", BattleReceiveFollowup);
                target.Status = statusMessage.Status;
                return false;
            case CureStatusElement cureStatusMessage:
                ConsoleWrite($"{target} has recovered from {cureStatusMessage.Status}!", ChronoAction);
                target.Data.CureStatus();
                return false;
            case CureTeamElement cureTeamMessage:
                int playerTeam = cureTeamMessage.Pokemon[1] - '0';

                ConsoleWrite($"Everyone in team {playerTeam} has been healed!", BattleReceive);

                PokemonData[] party = playerTeam == 1 ? p1.Party : foeTeam;
                foreach (var member in party)
                    member?.CureStatus();
                return false;
            case BoostElement boostMessage:
                var stat = boostMessage.Stat;
                ConsoleWrite($"{target} has had their {stat} raised by {boostMessage.Amount}!", BattleReceiveFollowup);
                int currentStage = target.StatStages[stat];
                int newStage = Math.Clamp(currentStage + boostMessage.Amount, -7, 7);
                if (currentStage != newStage)
                    target.StatStages[stat] = newStage;
                else
                    ConsoleWrite($"But the cap for {stat} was reached!", MetaFollowup);
                return false;
            case UnboostElement unboostMessage:
                stat = unboostMessage.Stat;
                ConsoleWrite($"{target} has had their {stat} lowered by {unboostMessage.Amount}!", BattleReceiveFollowup);
                currentStage = target.StatStages[stat];
                newStage = Math.Clamp(currentStage - unboostMessage.Amount, -7, 7);
                if (currentStage != newStage)
                    target.StatStages[stat] = newStage;
                else
                    ConsoleWrite($"But {stat} is already as low as possible!", MetaFollowup);
                return false;
            case SetBoostElement setBoostMessage:
                stat = setBoostMessage.Stat;
                ConsoleWrite($"{target} has had their {stat} set to {setBoostMessage.Amount}!", BattleReceiveFollowup);
                newStage = Math.Clamp(setBoostMessage.Amount, -7, 7);
                if (setBoostMessage.Amount != newStage)
                    target.StatStages[stat] = newStage;
                else
                    ConsoleWrite($"But the value was clamped!", MetaFollowup);
                return false;
            case SwapBoostElement swapBoostMessage:
                var stats = swapBoostMessage.Stats;
                ConsoleWrite($"{source} swapped their own {string.Join(", ", stats)} with {target}'s!", BattleAction);
                for (int i = 0; i < stats.Length; i++)
                {
                    stat = stats[i];
                    (target.StatStages[stat], source.StatStages[stat]) = (source.StatStages[stat], target.StatStages[stat]);
                }
                return false;
            case InvertBoostElement invertBoostMessage:
                ConsoleWrite($"{target} has had their stat boosts inverted!", BattleReceive);
                for (int i = 0; i <= (int)StatID.Spe; i++)
                {
                    target.StatStages.Packed ^= (uint)(0b1000 << (i * 4));
                }
                return false;
            case ClearBoostElement clearBoostMessage:
                ConsoleWrite($"{target} has had their stat boosts reset to 0!", BattleReceive);
                target.StatStages.Packed = 0;
                return false;
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
                return false;
            case ClearPositiveBoostElement clearPositiveBoostMessage:
                ConsoleWrite($"{target} had their positive stat boosts reset to 0!", BattleReceive);
                for (int i = 0; i <= (int)StatID.Spe; i++)
                {
                    uint mask = (uint)(0b1000 << (i * 4));
                    if ((target.StatStages.Packed & mask) != 0)
                        target.StatStages.Packed ^= mask;
                }
                return false;
            case ClearNegativeBoostElement clearNegativeBoostMessage:
                ConsoleWrite($"{target} had their negative stat boosts reset to 0!", BattleReceive);
                for (int i = 0; i <= (int)StatID.Spe; i++)
                {
                    uint mask = (uint)(0b1000 << (i * 4));
                    if ((target.StatStages.Packed & mask) == 0)
                        target.StatStages.Packed ^= mask;
                }
                return false;
            case CopyBoostElement copyBoostMessage:
                ConsoleWrite($"{target} copied {source}'s stat boosts!", BattleAction);
                target.StatStages.Packed = source.StatStages.Packed;
                return false;
            case WeatherElement weatherMessage:
                if (weatherMessage.Weather is null)
                    ConsoleWrite("The weather has expired!", FieldAction);
                else if (!weatherMessage.Upkeep)
                    ConsoleWrite($"The weather is now {weatherMessage.Weather}.", FieldAction);
                return false;
            case FieldStartElement fieldStartMessage:
                ConsoleWrite($"Field condition {fieldStartMessage.Condition} has started.", FieldAction);
                return false;
            case FieldEndElement fieldEndMessage:
                ConsoleWrite($"Field condition {fieldEndMessage.Condition} has ended.", FieldAction);
                return false;
            case SideStartElement sideStartMessage:
                ConsoleWrite($"Side condition {sideStartMessage.Condition} has started for side {sideStartMessage.Side}!", FieldAction);
                return false;
            case SideEndElement sideEndMessage:
                ConsoleWrite($"Side condition {sideEndMessage.Condition} has ended for side {sideEndMessage.Side}!", FieldAction);
                return false;
            case SwapSideConditionsElement swapSideConditionsMessage:
                // we probably wanna keep track of side conditions in two variables eventually, for the visual effects
                // THINGS
                return false;
            case StartVolatileElement startVolatileMessage:
                ConsoleWrite($"Volatile effect {startVolatileMessage.Effect} was applied to {target}.", BattleReceive);
                return false;
            case EndVolatileElement endVolatileMessage:
                ConsoleWrite($"Volatile effect {endVolatileMessage.Effect} ended for {target}.", ChronoAction);
                return false;
            case FaintElement faintMessage:
                ConsoleWrite($"{target} has fainted!", Faint);
                target.Data.Faint();
                return false;
            case WinElement winMessage:
                if (p1.Player.name == winMessage.Username)
                    ConsoleWrite("You winned :)", Win);
                else if (p2 != null && p2.Player.name == winMessage.Username)
                    ConsoleWrite("The other guy winned :/", NotWin);
                else
                    ConsoleWrite("That wild mon done did wonned...", NotWin);
                EndEverywhere();
                return false;
            case TieElement:
                ConsoleWrite("The battle has ended in a tie.", NotWin);
                EndEverywhere();
                return true;

        }
        return true;
    }
    private static void ConsoleWrite(string str, ConsoleColor color)
    {
        var prev = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine(str);
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