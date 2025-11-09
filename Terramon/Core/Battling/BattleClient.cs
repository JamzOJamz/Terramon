using EasyPacketsLib;
using Terramon.Content.GUI.TurnBased;
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
    // This isn't used by clients at all, only on the server
    // But I didn't wanna make another wrapper class lol
    public byte Pick;
    public string Name => Provider.BattleName;
    public Entity Entity => Provider.SyncedEntity;
    public bool BattleOngoing => State == ClientBattleState.Ongoing;
    public bool IsLocal => Provider.SyncedEntity is Player plr && plr.whoAmI == Main.myPlayer;
    public void RequestBattleWith(IBattleProvider otherProvider)
    {
        // Singleplayer
        if (Main.netMode == NetmodeID.SinglePlayer)
        {
            switch (Provider.SyncedEntity)
            {
                case Player:
                    BattleManager mgr = BattleManager.Instance;
                    mgr.QuickStartBattle(Provider.GetParticipantID(), otherProvider.GetParticipantID());
                    break;
                case NPC:
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
