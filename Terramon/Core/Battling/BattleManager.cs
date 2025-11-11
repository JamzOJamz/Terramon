using EasyPacketsLib;
using Showdown.NET.Protocol;
using System.Text;
using System.Text.Json;
using Terramon.Content.GUI.TurnBased;
using Terramon.Core.Battling.BattlePackets;
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
                int side = nvs.Pokemon[1] - '0';
                var sideTeamCount = source.Omniscient.Field[side].TeamCount;
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
                Write(BattleActionID.SetSideCondition, sd.Tags.Contains("[persistent]"));
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
                Write(BattleActionID.SetPokemonVolatile, true, vol.Tags.Contains("[silent]"), vol.Tags.Contains("[upkeep]")); // is start
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
                EndBattle(source, win.Username[0] - '0', BattleOutcome.Win);
                break;
            case TieElement:
                EndBattle(source, 0, BattleOutcome.Tie);
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
            Mod.SendPacket(in sendErr, plr.Player.whoAmI);
        }
        else
        {
            client.Battle[toSide].CurrentRequest = ShowdownRequest.None;
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
            Mod.SendPacket(in sendReq, plr.whoAmI);
        }
        else
        {
            client.Battle[sd].CurrentRequest = sr;
        }
    }

    public void EndBattle(BattleInstance source, int winningSide, BattleOutcome outcome)
    {
        var winnerOrOwner = winningSide == 0 ? source.ClientA : source[winningSide];
        var loserOrOther = winnerOrOwner.Foe.BattleClient;

        var a = winnerOrOwner.Provider.GetParticipantID();
        var b = winnerOrOwner.Foe.GetParticipantID();

        source.Stop();

        if (Main.dedServ) // mp
        {
            var battleEnd = new EndBattleRpc(a, outcome);
            Mod.SendPacket(in battleEnd);
        }
        else
        {
            EndBattleRpc.EndMessage(outcome, winnerOrOwner, loserOrOther);
        }

        _activeBattles.Remove(a);
        _activeBattles.Remove(b);
    }

    public void SetPick(byte playerParticipant, byte pick)
    {
        var plr = Main.player[playerParticipant].Terramon();
        ref byte cur = ref plr._battleClient.Pick;

        /*
        if (cur != 0) // somehow already picked
            // ???
        else
        */
        cur = pick;
        plr._battleClient.State = ClientBattleState.SetSlot;

        var battle = _activeBattles[plr.GetParticipantID()];
        if (battle.ShouldStart)
        {
            // I'm unclear on whether it should immediately send a packet but I'm guessing yes?
            // Otherwise wait for a tick using an int timer
            var teamRequest = new BattleTeamRequestRpc();

            var a = battle.ClientA.Provider.SyncedEntity;
            var b = battle.ClientB.Provider.SyncedEntity;

            if (a is not Player pa) throw new Exception("hwoahwowa");
            if (b is not Player pb) throw new Exception("hwoahwowa");

            Mod.SendPacket(in teamRequest, pa.whoAmI);
            Mod.SendPacket(in teamRequest, pb.whoAmI);
        }
        else
        {
            Mod.Logger.Warn($"Battle didn't start because {battle.ClientA.Pick} and {battle.ClientB.Pick} and {battle.State}");
        }
    }

    public void SetTeam(byte playerParticipant, SimplePackedPokemon[] packedTeam)
        => SetTeam(new BattleParticipant(playerParticipant, BattleProviderType.Player), packedTeam);

    public void SetTeam(BattleParticipant participant, SimplePackedPokemon[] packedTeam)
    {
        var battle = _activeBattles[participant];
        var party = battle.SubmitTeam(participant, packedTeam).Provider.GetBattleTeam();

        // Replicate the client's local team on the server
        for (int i = 0; i < packedTeam.Length; i++)
        {
            party[i] ??= new();
            party[i].SetFromNetPacked(in packedTeam[i]);
        }

        // Check if both players have their team set
        // If so, start the battle
        if (battle.ShouldStart)
            StartBattle(battle);
    }

    public int LatestInteractor;

    public void HandleChoice(BattleParticipant participant, BattleChoice choice, int operand)
    {
        var b = _activeBattles[participant];
        var c = participant.Client;

        LatestInteractor = c == b.ClientA ? 1 : 2;

        b.SubmitChoice(LatestInteractor, choice, operand + 1);
    }

    public static void StartBattle(BattleInstance battle)
    {
        battle.Start();

        if (Main.dedServ)
        {
            var bf = new BattleField()
            {
                A = new(battle.ClientA.Provider),
                B = new(battle.ClientB.Provider),
            };

            battle.ClientA.Battle = bf;
            battle.ClientB.Battle = bf;

            battle.Omniscient = new(bf);

            // Send message to every client communicating of the start of the battle
            var startBattleMessage = new BattleStartRpc(
                battle.ClientA.Provider.GetParticipantID(),
                battle.ClientB.Provider.GetParticipantID());

            Mod.SendPacket(in startBattleMessage);
        }

        battle.ClientA.Foe = battle.ClientB.Provider;
        battle.ClientB.Foe = battle.ClientA.Provider;

        battle.State = BattleState.Ongoing;
    }

    public void StartNPCBattle(BattleParticipant requester, BattleParticipant requestee)
    {
        var battle = _activeBattles[requester];
        _activeBattles.Add(requestee, battle);

        var c = requestee.Client;
        c.Pick = 1;
        SetTeam(requestee, c.Provider.GetNetTeam());

        var req = new BattleTeamRequestRpc();
        Mod.SendPacket(in req, requester.WhoAmI);
    }

    public void SubmitEndRequest(byte requester, bool resign = true)
    {
        var participant = new BattleParticipant(requester, BattleProviderType.Player);
        var b = _activeBattles[participant];
        var c = participant.Client;

        if (resign)
        {
            int winSide = c == b.ClientA ? 2 : 1;
            EndBattle(b, winSide, BattleOutcome.AgreedWin);
        }
        else
        {
            if (c.TieRequest)
                return;

            c.TieRequest = true;
            if (c.Foe.BattleClient.TieRequest)
            {
                EndBattle(b, 0, BattleOutcome.AgreedTie);
            }
        }
    }

    public void SubmitRequest(BattleParticipant requester, BattleParticipant requestee)
    {
        // this only checks if the requestee is already in a battle,
        // since the ability for a client to request a battle is handled clientside
        if (_activeBattles.TryGetValue(requestee, out var instance) &&
            instance.State is BattleState.Picking or BattleState.Ongoing)
        {
            DeclineRequest(requester, requestee, error: true);
            return;
        }
        var inst = new BattleInstance
        {
            ClientA = requester.Client,
            ClientB = requestee.Client,
        };
        // add the requester. the requestee is only added if they accept or if they're an NPC
        _activeBattles.Add(requester, inst);
        if (requestee.Type != BattleProviderType.Player)
        {
            StartNPCBattle(requester, requestee);
        }
        else
        {
            var request = new BattleRequestRpc(BattleRequestType.Request, requester, requestee);
            // send back to all clients
            Mod.SendPacket(in request);
        }
    }

    public void CancelRequest(BattleParticipant requester, BattleParticipant requestee)
    {
        // attempt to remove requester from battles list
        // if not found, that means other clients don't know about the request anyway, so just return
        if (!_activeBattles.Remove(requester))
            return;
        var cancellation = new BattleRequestRpc(BattleRequestType.Cancel, requester, requestee);
        // send back to all clients
        Mod.SendPacket(in cancellation);
    }

    public void AcceptRequest(BattleParticipant requester, BattleParticipant requestee)
    {
        // it is possible that when attempting to accept a request,
        // that person's request has already been cancelled
        if (!_activeBattles.TryGetValue(requester, out var instance))
        {
            // simply return here.
            // cancellation will be replicated shortly, so no need to send another packet
            return;
        }
        // change the requester's battle state to Picking and add the requestee to avoid any other requests
        instance.State = BattleState.Picking;
        _activeBattles.Add(requestee, instance);
        var response = new BattleRequestRpc(BattleRequestType.Accept, requestee, requester);
        // send back to all clients
        Mod.SendPacket(in response);
    }

    public void DeclineRequest(BattleParticipant requester, BattleParticipant requestee, bool error = false)
    {
        // runs on the server
        var response = new BattleRequestRpc(error ? BattleRequestType.Error : BattleRequestType.Decline, requestee, requester);
        // if the request ends up in _activeBattles, that means every client knows about the request (data stored in _battleClient)
        // so the packet has to be forwarded to everyone
        bool broadcast = _activeBattles.Remove(requester);
        Mod.SendPacket(in response, broadcast ? -1 : requester.WhoAmI);
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
            b.ClientA.Battle.Observer.Receive(reader);
        }

        reader.Dispose();
    }

    // Called in singleplayer
    public void QuickStartBattle(BattleParticipant requester, BattleParticipant requestee)
    {
        var inst = BattleInstance.Create(requester.Client, requestee.Client);

        _activeBattles.Add(requester, inst);
        _activeBattles.Add(requestee, inst);

        inst.State = BattleState.Picking;

        SetTeam(requester, requester.Provider.GetNetTeam());
        SetTeam(requestee, requestee.Provider.GetNetTeam());
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
            _ => throw new Exception(),
        };
    }
    public static IBattleProvider GetProvider(BattleParticipant participant)
        => GetProvider(participant.WhoAmI, participant.Type);

    public static BattleClient GetClient(byte whoAmI, BattleProviderType type = BattleProviderType.Player)
        => GetProvider(whoAmI, type).BattleClient;
    public static BattleClient GetClient(BattleParticipant participant)
        => GetClient(participant.WhoAmI, participant.Type);
}
