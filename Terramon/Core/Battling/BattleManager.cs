using Showdown.NET.Protocol;
using System.Text;
using System.Text.Json;
using Terramon.Content.Commands;
using Terramon.Content.NPCs;
using Terramon.Core.Battling.BattlePackets;
using Terramon.Core.Battling.BattlePackets.Messages;
using Terramon.ID;

namespace Terramon.Core.Battling;

/// <summary>
///     Contains information related to the toClients version of a Showdown battle, instanced locally on Singleplayer and server-side on Multiplayer.
///     It is in charge of receiving, managing and synchronizing battle requests and actions made by players in battle.
/// </summary>
public sealed class BattleManager
{
    public static Terramon Mod => Terramon.Instance;

    public static BattleManager Instance { get; set; }
    private readonly Dictionary<BattleParticipant, BattleInstance> _activeBattles = [];

    public void Reply(BattleMessage m)
    {
        // Even though the server owns NPCs, messages sent to those are received specifically by them
        // This method only takes into account messages sent specifically to the server

        switch (m)
        {
            case ForfeitOrder:
                // Send the forfeit message to self (server)
                var forfeit = new ForfeitStatement(forfeiter: m.Sender);
                forfeit.Send();
                break;
            case ForfeitStatement w:
                // Remove from active battle list
                var inst = _activeBattles[w.Forfeiter.ID];
                _activeBattles.Remove(inst.ClientA.ID);
                _activeBattles.Remove(inst.ClientB.ID);

                // Reset client fields
                BattleInstance.Destroy(inst);
                break;
            case WinStatement w:
                // Remove from active battle list
                inst = _activeBattles[w.Winner.ID];
                _activeBattles.Remove(inst.ClientA.ID);
                _activeBattles.Remove(inst.ClientB.ID);

                // Reset client fields
                BattleInstance.Destroy(inst);
                break;
            case SlotChoice:
                // Received when both have chosen
                inst = _activeBattles[m.Sender.ID];

                // Set states
                inst.ClientA.State = inst.ClientB.State = ClientBattleState.PollingTeam;

                // Start the stream
                inst.EnsureStreamStarted();

                // Ask both participants for their team
                var teamQuestion = new TeamQuestion();
                teamQuestion.Send(inst.ClientA.Provider);
                teamQuestion.Send(inst.ClientB.Provider);
                break;
            case TeamAnswer s:

                // Received once for each participant
                inst = _activeBattles[s.Sender.ID];
                // Set state
                Console.WriteLine($"Setting state of {s.Sender.BattleName} to SetTeam");
                s.Sender.State = ClientBattleState.SetTeam;

                var shouldStart =
                    inst.ClientA.State == ClientBattleState.SetTeam &&
                    inst.ClientB.State == ClientBattleState.SetTeam;

                // Check if both participants have submitted their team
                if (shouldStart)
                {
                    // If so, start the battle
                    Console.WriteLine("Setting both clients states to Ongoing");
                    inst.StartEffects();

                    // Send start message to self
                    // TODO: Test to make sure this doesn't require some sort of delay later
                    var start = new StartBattleStatement(inst.ClientA.Provider);
                    start.Send();
                }

                inst.SubmitTeam(s.Sender.BattleClient, s.Team);
                break;
            case TieStatement t:
                // Remove from active battles list
                inst = _activeBattles[t.EitherParticipant.ID];
                _activeBattles.Remove(inst.ClientA.ID);
                _activeBattles.Remove(inst.ClientB.ID);

                // Reset client fields
                BattleInstance.Destroy(inst);
                break;
        }
    }
    public bool Witness(BattleMessage m)
    {
        // Messages that are meant for a client or server-owned provider pass through here first
        // Return false to intercept the message if it shouldn't be sent to the recipient

        Console.WriteLine($"Server: Witnessing a {m.GetType().Name}");

        switch (m)
        {
            case ResetEverythingStatement:
                foreach (var inst0 in _activeBattles.Values)
                    inst0.Stop();
                _activeBattles.Clear();
                break;

            // Messages related to challenging someone to a battle

            case ChallengeQuestion:

                // This only checks if the recipient is already in a battle,
                // since the ability for a client to request a battle is handled clientside
                if (_activeBattles.ContainsKey(m.Recipient.ID))
                    // If they're already in a battle, intercept with an error
                    return m.Return<ChallengeError>();

                var inst = BattleInstance.Create(
                    m.Sender.BattleClient,
                    m.Recipient.BattleClient);

                // Add the requester
                // The requestee is only added if they accept
                _activeBattles.Add(m.Sender.ID, inst);

                // Set states
                m.Sender.State = ClientBattleState.Requested;
                break;
            case ChallengeTakeback:

                // Sent by original requester

                // Remove from active battles list
                _activeBattles.Remove(m.Sender.ID, out inst);

                // Reset requester's fields
                inst.ClientA.Battle = null;
                inst.ClientA.Foe = null;
                inst.ClientA.State = ClientBattleState.None;
                break;
            case ChallengeAnswer c:

                // It's possible that when trying to answer a question,
                // that provider's question has already been taken back.
                // In that case, intercept, since they don't need the answer anymore
                if (!_activeBattles.TryGetValue(m.Recipient.ID, out inst))
                    return false;

                if (c.Yes) // If the sender accepted
                {
                    // Add the requestee
                    _activeBattles.Add(m.Sender.ID, inst);

                    // Set states
                    inst.State = BattleState.Picking;
                    m.Sender.State = m.Recipient.State = ClientBattleState.PollingSlot;
                }
                else // If the sender declined
                {
                    // Reset client fields
                    BattleInstance.Destroy(inst);

                    // Remove from active battles list
                    _activeBattles.Remove(m.Recipient.ID);
                }
                break;

            // Messages related to setting up a battle

            case SlotChoice s:
                m.Sender.Pick = s.Slot;
                m.Sender.State = ClientBattleState.SetSlot;
                break;

            // Messages related to intentionally ending a battle

            case TieQuestion:

                // Set tie request flag
                m.Sender.TieRequest = true;

                // Check if the other client also requested a tie
                if (m.Recipient.TieRequest)
                {
                    // If so, intercept, and send the agreed tie message to self (server)
                    var agreedTie = new TieStatement(eitherParticipant: m.Sender, type: TieStatement.TieType.Agreed);
                    agreedTie.Send();
                    return false;
                }
                break;
            case TieTakeback:

                // Unset tie request flag
                m.Sender.TieRequest = false;
                break;

            // Messages related to ending a battle

        }

        return true;
    }
    public void HandleSingleElement(BattleInstance source, BinaryWriter w, ProtocolElement element, int toSide)
    {
        switch (element)
        {
            case CantElement:
            case FailElement:
            case BlockElement:
            case NoTargetElement:
            case MissElement:
            case ImmuneElement:
                Write(BattleActionID.ActionFail);
                break;
            case PrepareElement prep:
                var side = prep.Attacker[1] - '0';
                Write(BattleActionID.PokemonWait);
                {
                    w.Write((byte)side);
                }
                break;
            case MustRechargeElement prep:
                side = prep.Pokemon[1] - '0';
                Write(BattleActionID.PokemonWait);
                {
                    w.Write((byte)side);
                }
                break;
            case MoveElement move:
                var mvD = move.Details;
                Write(BattleActionID.MoveAnimation, mvD.Still, mvD.Miss);
                {
                    w.Write(new SimpleMon(move.Pokemon));
                    var anim = mvD.Animation ?? move.Move;
                    if (!Enum.TryParse(anim.Replace(" ", string.Empty), out MoveID animID))
                        Mod.Logger.Error($"{anim} not recognized as a move in MoveID");
                    w.Write((ushort)animID);
                }
                break;
            case DamageElement hp:
                Write(BattleActionID.SetPokemonHP, false); // set indirectly
                {
                    w.Write(new SimpleMon(hp.Pokemon));
                    w.Write(new SimpleHP(hp.HP));
                }
                break;
            case HealElement hp:
                Write(BattleActionID.SetPokemonHP, false);
                {
                    w.Write(new SimpleMon(hp.Pokemon));
                    w.Write(new SimpleHP(hp.HP));
                }
                break;
            case SetHPElement hp:
                Write(BattleActionID.SetPokemonHP, true); // set directly
                {
                    w.Write(new SimpleMon(hp.Pokemon));
                    w.Write(new SimpleHP(hp.HP));
                }
                break;
            case FaintElement hp:
                Write(BattleActionID.SetPokemonHP, true);
                {
                    w.Write(new SimpleMon(hp.Pokemon));
                    w.Write(0u);
                }
                break;
            case SwitchElement sw:
                var pk = new SimpleMon(sw.Pokemon);
                Write(BattleActionID.SwitchPokemon);
                {
                    w.Write(pk);
                }
                Write(BattleActionID.SetPokemonHP, true);
                {
                    w.Write(pk);
                    w.Write(new SimpleHP(sw.HP));
                }
                Write(BattleActionID.SetPokemonStatus);
                {
                    w.Write(pk);
                    w.Write((byte)sw.Status);
                }
                break;
            case DragElement sw:
                pk = new SimpleMon(sw.Pokemon);
                Write(BattleActionID.SwitchPokemon);
                {
                    w.Write(pk);
                }
                Write(BattleActionID.SetPokemonHP, true);
                {
                    w.Write(pk);
                    w.Write(new SimpleHP(sw.HP));
                }
                Write(BattleActionID.SetPokemonStatus);
                {
                    w.Write(pk);
                    w.Write((byte)sw.Status);
                }
                break;
            case DetailsChangeElement dc:
                pk = new SimpleMon(dc.Pokemon);
                Write(BattleActionID.SetPokemonDetails);
                {
                    w.Write(pk);
                    w.Write(new SimpleDetails(dc.Details));
                }
                Write(BattleActionID.SetPokemonHP);
                {
                    w.Write(pk);
                    w.Write(new SimpleHP(dc.HP));
                }
                break;
            case FormeChangeElement dc:
                pk = new SimpleMon(dc.Pokemon);
                Write(BattleActionID.SetPokemonSpecies);
                {
                    w.Write(pk);
                    w.Write(NationalDexID.FromSpecies(dc.Species));
                }
                Write(BattleActionID.SetPokemonHP);
                {
                    w.Write(pk);
                    w.Write(new SimpleHP(dc.HP));
                }
                break;
            case ReplaceElement dc:
                Write(BattleActionID.SetPokemonDetails);
                {
                    w.Write(new SimpleMon(dc.Pokemon));
                    w.Write(new SimpleDetails(dc.Details));
                }
                break;
            case StatusElement nvs:
                Write(BattleActionID.SetPokemonStatus, false);
                {
                    w.Write(new SimpleMon(nvs.Pokemon));
                    w.Write((byte)nvs.Status);
                }
                break;
            case CureStatusElement nvs:
                Write(BattleActionID.SetPokemonStatus, true);
                {
                    w.Write(new SimpleMon(nvs.Pokemon));
                }
                break;
            case CureTeamElement nvs:
                side = nvs.Pokemon[1] - '0';
                var sideTeamCount = source.Omniscient[side].TeamCount;
                for (int i = 0; i < sideTeamCount; i++)
                {
                    Write(BattleActionID.SetPokemonStatus, true);
                    {
                        w.Write(new SimpleMon(side, i));
                    }
                }
                break;
            case BoostElement bst:
                Write(BattleActionID.SetPokemonBoost, false); // set indirectly
                {
                    w.Write(new SimpleMon(bst.Pokemon));
                    w.Write((byte)bst.Stat);
                    w.Write((sbyte)bst.Amount);
                }
                break;
            case UnboostElement bst:
                Write(BattleActionID.SetPokemonBoost, false);
                {
                    w.Write(new SimpleMon(bst.Pokemon));
                    w.Write((byte)bst.Stat);
                    w.Write((sbyte)-bst.Amount);
                }
                break;
            case SetBoostElement bst:
                Write(BattleActionID.SetPokemonBoost, true); // set directly
                {
                    w.Write(new SimpleMon(bst.Pokemon));
                    w.Write((byte)bst.Stat);
                    w.Write((sbyte)bst.Amount);
                }
                break;
            case SwapBoostElement bst:
                var pair = new SimpleMonPair(bst.Source, bst.Target);
                for (int i = 0; i < bst.Stats.Length; i++)
                {
                    Write(BattleActionID.SetPokemonBoost, true, true); // is swap
                    {
                        w.Write(pair);
                        w.Write((byte)bst.Stats[i]);
                    }
                }
                break;
            case CopyBoostElement bst:
                Write(BattleActionID.SetPokemonBoost, true, false, true); // is copy
                {
                    w.Write(new SimpleMonPair(bst.Source, bst.Target));
                }
                break;
            case InvertBoostElement mbst:
                Write(BattleActionID.AllPokemonBoost);
                {
                    w.Write(new SimpleMon(mbst.Pokemon));
                    w.Write((byte)BoostModifierAction.Invert);
                }
                break;
            case ClearBoostElement mbst:
                Write(BattleActionID.AllPokemonBoost);
                {
                    w.Write(new SimpleMon(mbst.Pokemon));
                    w.Write((byte)BoostModifierAction.ClearAll);
                }
                break;
            case ClearNegativeBoostElement mbst:
                Write(BattleActionID.AllPokemonBoost);
                {
                    w.Write(new SimpleMon(mbst.Pokemon));
                    w.Write((byte)BoostModifierAction.ClearNegative);
                }
                break;
            case ClearPositiveBoostElement mbst:
                Write(BattleActionID.AllPokemonBoost);
                {
                    w.Write(new SimpleMon(mbst.Pokemon));
                    w.Write((byte)BoostModifierAction.ClearPositive);
                }
                break;
            case ClearAllBoostElement:
                Write(BattleActionID.AllPokemonBoost, true);
                break;
            case TurnElement:
                Write(BattleActionID.AdvanceTurn);
                break;
            case WeatherElement wth:
                Write(BattleActionID.SetWeather, wth.Upkeep);
                {
                    if (wth.Weather is null || wth.Weather[0] is 'n')
                        w.Write((byte)0);
                    else
                        w.Write((byte)Enum.Parse<BattleWeather>(wth.Weather));
                }
                break;
            case FieldStartElement fld:
                Write(BattleActionID.SetFieldCondition);
                {
                    var sanitizedName = fld.Condition[6..].Replace(" ", string.Empty);
                    w.Write((byte)Enum.Parse<FieldCondition>(sanitizedName));
                }
                break;
            case FieldEndElement:
                Write(BattleActionID.SetFieldCondition);
                {
                    w.Write((byte)0);
                }
                break;
            case SideStartElement sd:
                Write(BattleActionID.SetSideCondition, sd.Tags != null && sd.Tags.Contains("[persistent]"));
                {
                    var sanitizedName = (sd.Condition[0] is 'm' ? sd.Condition[6..] : sd.Condition).Replace(" ", string.Empty);
                    w.Write((byte)sd.Side.Player);
                    w.Write((byte)Enum.Parse<SideCondition>(sanitizedName));
                }
                break;
            case SideEndElement sd:
                Write(BattleActionID.SetSideCondition);
                {
                    w.Write((byte)sd.Side.Player);
                    w.Write((byte)0);
                }
                break;
            case SwapSideConditionsElement:
                Write(BattleActionID.SetSideCondition, true); // is swap
                break;
            case StartVolatileElement vol:
                Write(BattleActionID.SetPokemonVolatile, true, vol.Tags != null && vol.Tags.Contains("[silent]"), vol.Tags != null && vol.Tags.Contains("[upkeep]")); // is start
                {
                    w.Write(new SimpleMon(vol.Pokemon));
                    w.Write((byte)GetVolatile(vol.Effect));
                }
                break;
            case EndVolatileElement vol:
                Write(BattleActionID.SetPokemonVolatile, false); // is end
                {
                    w.Write(new SimpleMon(vol.Pokemon));
                    w.Write((byte)GetVolatile(vol.Effect));
                }
                break;
            case ItemElement it:
                // my brain is too bad to know what exactly i should do here but ik you can save
                // some writes here
                Write(BattleActionID.PokemonItem, true); // is reveal
                {
                    w.Write(new SimpleMon(it.Pokemon));
                    w.Write(ItemID.Search.Terramon(it.Item));
                }
                break;
            case EndItemElement it:
                Write(BattleActionID.PokemonItem, false);
                {
                    w.Write(new SimpleMon(it.Pokemon));
                }
                break;
            case AbilityElement ab:
                // my brain is too bad to know what exactly i should do here but ik you can save
                // some writes here
                Write(BattleActionID.PokemonAbility, true); // is reveal
                {
                    w.Write(new SimpleMon(ab.Pokemon));
                    w.Write((ushort)Enum.Parse<AbilityID>(ab.Ability));
                }
                break;
            case EndAbilityElement ab:
                Write(BattleActionID.PokemonAbility, false);
                {
                    w.Write(new SimpleMon(ab.Pokemon));
                }
                break;
            // Messages with special handling
            case RequestElement req:
                HandleRequest(source, req.Request);
                break;
            case ErrorElement err:
                HandleError(source, err, LatestInteractor);
                break;
            case WinElement win:
                var winner = source[win.Username[0] - '0'].Provider;
                var dubs = new WinStatement(winner);
                dubs.Send();
                break;
            case TieElement:
                var tie = new TieStatement(eitherParticipant: source.ClientA.Provider, TieStatement.TieType.Regular);
                tie.Send();
                break;
        }

        void Write(BattleActionID id, params bool[] flags)
        {
            BitsByte b = default;
            for (int i = 0; i < flags.Length; i++)
                b[i] = flags[i];
            w.Write((byte)id);
            w.Write(b);
        }
    }
    private static VolatileEffect GetVolatile(string rawName)
    {
        // ooh boy
        var effect = VolatileEffect.None;

        var extraThing = 0;
        var sanitized = (rawName[0] is 'm' ? rawName[6..] : rawName).Replace(" ", string.Empty);
        if (sanitized.StartsWith("perish"))
        {
            extraThing = sanitized[^1] - '0';
            effect = VolatileEffect.Perish;
        }
        else if (sanitized.StartsWith("fallen"))
        {
            extraThing = sanitized[^1] - '0';
            effect = VolatileEffect.Fallen;
        }

        if (effect != VolatileEffect.None)
            return effect;

        if (Enum.TryParse<VolatileEffect>(sanitized, true, out var vol))
            return vol;
        if (Enum.TryParse<NatureID>(sanitized, out var nat))
        {
            extraThing = (int)nat;
            _ = extraThing;
            return VolatileEffect.NatureEffect;
        }

        _ = extraThing;

        throw new Exception($"'{rawName}' wasn't recognized as a valid volatile effect");
    }
    public static void HandleError(BattleInstance source, ErrorElement error, int toSide)
    {
        if (toSide <= 0)
            return;

        var id = BattleErrorParser.Parse(error.Message);
        var client = source[toSide];
        if (Main.dedServ && client.Provider is TerramonPlayer plr)
        {
            var sendErr = new BattleErrorRpc(error.Type, id);
            Mod.SendPacket(sendErr, plr.Player.whoAmI);
        }
        else
        {
            client.CurrentRequest = ShowdownRequest.None;
            // do stuff with error type and subtype
        }
    }
    public static void HandleRequest(BattleInstance source, string rawRequest)
    {
        using var req = JsonDocument.Parse(rawRequest);

        var root = req.RootElement;

        if (!root.TryGetProperty("side", out var side))
            return;

        var sd = side.GetProperty("id").ToString()[1] - '0';
        var client = source[sd];

        if (root.TryGetProperty("teamPreview", out _))
        {
            var spec = client.CachedTeamSpec;
            Console.WriteLine(spec);
            source.Stream.Write(spec);
            client.CachedTeamSpec = null;
            return;
        }

        var forceSwitch = root.TryGetProperty("forceSwitch", out _);
        var wait = root.TryGetProperty("wait", out _);

        var sr = forceSwitch ? ShowdownRequest.ForcedSwitch : wait ? ShowdownRequest.Wait : ShowdownRequest.Any;

        if (Main.dedServ && client.Provider.SyncedEntity is Player plr)
        {
            var sendReq = new ShowdownRequestRpc(sr);
            Mod.SendPacket(sendReq, toClient: plr.whoAmI);
        }
        else
        {
            client.CurrentRequest = sr;
            if (client.Provider is PokemonNPC npc)
            {
                Console.WriteLine($"{npc.BattleName} is responding automatically...");
                npc.AutoBattleChoice();
            }
        }
    }
    public int LatestInteractor;
    public void HandleChoice(BattleParticipant participant, BattleChoice choice, int operand)
    {
        var b = _activeBattles[participant];
        var c = participant.Client;

        LatestInteractor = c == b.ClientA ? 1 : 2;

        b.SubmitChoice(LatestInteractor, choice, operand + 1);
    }
    public void Observe(BattleParticipant battleOwner, MemoryStream buffer, bool onlyToSelf)
    {
        buffer.Position = 0;
        using var reader = new BinaryReader(buffer, Encoding.UTF8, true);
        var b = _activeBattles[battleOwner];

        b.Omniscient.Receive(reader);
        buffer.Position = 0;

        if (!onlyToSelf)
        {
            b.ClientA.Battle.Receive(reader);
        }

        reader.Dispose();
    }

    public static IBattleProvider GetProvider(byte whoAmI, BattleProviderType type = BattleProviderType.Player)
    {
        /*
        var asker = Main.myPlayer;
        var askFor = whoAmI;

        Mod.Logger.Error($"Request made for provider by {asker} for {askFor}");
        */

        return type switch
        {
            BattleProviderType.Player => Main.player[whoAmI].Terramon(),
            BattleProviderType.PokemonNPC => Main.npc[whoAmI].Pokemon(),
            _ => null,
        };
    }
    public static IBattleProvider GetProvider(BattleParticipant participant)
        => GetProvider(participant.WhoAmI, participant.Type);

    public static BattleClient GetClient(byte whoAmI, BattleProviderType type = BattleProviderType.Player)
        => GetProvider(whoAmI, type).BattleClient;
    public static BattleClient GetClient(BattleParticipant participant)
        => GetClient(participant.WhoAmI, participant.Type);
}
