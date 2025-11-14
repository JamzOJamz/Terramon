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
    public BattleParticipant ID => Provider.ID;
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
    public BattleParticipant FoeID => _foe?.ID ?? BattleParticipant.None;
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
    public bool BattleOngoing
    {
        get
        {
            return State == ClientBattleState.Ongoing;
        }
    }
    public bool IsLocal => Provider.IsLocal;
    public int SideIndex
    {
        get
        {
            if (!BattleOngoing)
                return 0;
            return Battle.A.Provider == Provider ? 1 : 2;
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
            Terramon.Instance.SendPacket(packet);
        }
        else
        {
            BattleManager.Instance.HandleChoice(ID, choice, (byte)operand);
        }

        if (Foe is PokemonNPC npc)
        {
            npc.BattleClient.MakeChoice(BattleChoice.Default);
        }

        return true;
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
        if (IsLocal)
            EndLocalBattle();

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
