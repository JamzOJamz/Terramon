using EasyPacketsLib;
using Terramon.Content.GUI.TurnBased;
using Terramon.Content.NPCs;
using Terramon.Core.Battling.BattlePackets;

namespace Terramon.Core.Battling;

/// <summary>
///     Contains information related to the public version of a Showdown battle, instanced per-player to allow for visual replication across all clients.
///     On singleplayer, the methods used to interface with the <see cref="BattleManager"/> interact directly, otherwise they send packets.
/// </summary>
public sealed class BattleClient(IBattleProvider provider)
{
    public IBattleProvider Provider { get; } = provider;
    public BattleParticipant ID => Provider.GetParticipantID();
    private IBattleProvider _foe;
    public IBattleProvider Foe
    {
        get
        {
            return _foe;
        }
        set
        {
            if (value is null)
                State = ClientBattleState.None;
            _foe = value;
        }
    }
    public BattleParticipant FoeID => _foe?.GetParticipantID() ?? BattleParticipant.None;
    public ClientBattleState State;
    // Instance is shared between both battlers
    public BattleField Battle;
    public BattleSide Side => Battle is null ? null : Provider == Battle.A.Provider ? Battle[1] : Battle[2];

    // These arent't used by remote clients
    public byte Pick;
    public bool TieRequest;

    // This is only used by the server
    public string CachedTeamSpec;

    public string Name => Provider.BattleName;
    public Entity Entity => Provider.SyncedEntity;
    public bool BattleOngoing => State == ClientBattleState.Ongoing;
    public bool IsLocal => Provider.SyncedEntity is Player plr && plr.whoAmI == Main.myPlayer;
    public int SideIndex
    {
        get
        {
            if (!BattleOngoing)
                return 0;
            return Battle.A.Provider == Provider ? 1 : 2;
        }
    }
    public void RequestBattleWith(IBattleProvider otherProvider)
    {
        // Singleplayer
        if (Main.netMode == NetmodeID.SinglePlayer)
        {
            switch (Provider)
            {
                case TerramonPlayer plr:
                    BattleManager mgr = BattleManager.Instance;
                    var active = plr.ActiveSlot;
                    if (active == -1)
                        return;
                    Pick = (byte)(active + 1);
                    otherProvider.BattleClient.Pick = 1;
                    mgr.QuickStartBattle(Provider.GetParticipantID(), otherProvider.GetParticipantID());
                    break;
                case PokemonNPC:
                    Pick = 1;
                    break;
            }
            return;
        }

        // This method may be called from the client or the server
        // Called on local client when a player requests a battle with either another player or an NPC
        // Called on server when an NPC requests a battle with a player (such as a trainer spotting the player or an NPC encounter of some kind)
        switch (Provider.SyncedEntity)
        {
            case Player plr when plr.whoAmI == Main.myPlayer:
                var request = new BattleRequestRpc(BattleRequestType.Request, Provider.GetParticipantID(), otherProvider.GetParticipantID());
                Terramon.Instance.SendPacket(in request);
                break;
            case NPC npc when Main.dedServ:
                break;
            default:
                throw new Exception(
                $"A {Provider.SyncedEntity.GetType().Name} " +
                $"attempted to start a battle with a {otherProvider.SyncedEntity.GetType().Name}" +
                $"from the {(Main.dedServ ? "server" : "local client")}");
        }
    }

    public static bool LocalBattleOngoing
    {
        get
        {
            var local = LocalClient;
            return local != null && local.BattleOngoing;
        }
    }

    public static BattleClient LocalClient
    {
        get
        {
            var modPlayer = TerramonPlayer.LocalPlayer;
            modPlayer._battleClient ??= new(modPlayer);
            return modPlayer._battleClient;
        }
    }

    public bool CanMakeChoice(BattleChoice choice)
    {
        if (State != ClientBattleState.Ongoing)
            return false;

        var s = Side;
        if (s is null)
            return false;

        return s.CurrentRequest switch
        {
            ShowdownRequest.None => false,
            ShowdownRequest.Any => true,
            ShowdownRequest.Wait => false,
            ShowdownRequest.ForcedSwitch => choice is BattleChoice.Default or BattleChoice.Switch,
            _ => false,
        };
    }

    public bool MakeChoice(BattleChoice choice, int operand = -1)
    {
        if (!CanMakeChoice(choice))
            return false;

        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            var packet = new BattleChoiceRpc(choice, (byte)operand);
            Terramon.Instance.SendPacket(in packet);
        }
        else
        {
            BattleManager.Instance.HandleChoice(Provider.GetParticipantID(), choice, (byte)operand);
        }

        if (Foe is PokemonNPC npc)
        {
            npc.BattleClient.MakeChoice(BattleChoice.Default);
        }

        return true;
    }

    public void RequestBattleEnd(bool resign = true)
    {
        var m = Terramon.Instance;

        if (!resign)
        {
            if (TieRequest)
                return;
            TieRequest = true;
        }

        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            var packet = new EndBattleRequestRpc(resign);
            if (Foe.SyncedEntity is Player plr)
                m.SendPacket(in packet, plr.whoAmI, Main.myPlayer, true);
            else
                m.SendPacket(in packet);
        }
        else
        {
            BattleManager.Instance.SubmitEndRequest((byte)Main.myPlayer, resign);
        }
    }

    public static void StartLocalBattle()
    {
        // Clients receive almost no information about the current battle
        // It's not necessary because nearly everything is server-authoritative
        // And any data that does get sent is protected with an extra layer (BattlePokemon)
        // So what's done here is just the battle effects
        BattleUI.ApplyStartEffects();
    }

    public static void EndLocalBattle()
    {
        BattleUI.ApplyEndEffects();
    }

    public void BattleStopped()
    {
        Provider.StopBattleEffects();
        Battle = null;
        Pick = 0;
        TieRequest = false;
        Foe = null;
    }
}

public enum ClientBattleState : byte
{
    None,
    Requested,
    PollingSlot,
    SetSlot,
    PollingTeam,
    SetTeam,
    Ongoing,
}
