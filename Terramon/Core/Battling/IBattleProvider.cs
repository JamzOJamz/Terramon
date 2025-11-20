using Terramon.Core.Battling.BattlePackets;
using Terramon.Core.Battling.BattlePackets.Messages;

namespace Terramon.Core.Battling;
public interface IBattleProvider
{
    BattleProviderType ProviderType { get; }
    BattleClient BattleClient { get; }
    Entity SyncedEntity { get; }
    string BattleName { get; }
    PokemonData[] GetBattleTeam();
    /// <summary>
    ///     Things this <see cref="IBattleProvider"/> should do the moment it starts a battle.
    ///     Called twice on the server, local, and remote clients. In Singleplayer, called only by <see cref="BattleManager"/>.
    /// </summary>
    /// <param name="before">Whether the method is running before the <see cref="Foe"/> has gotten the chance to.</param>
    void StartBattleEffects(bool before);
    void StopBattleEffects();
    void SetActiveSlot(byte newActive);
    /// <summary>
    ///     Reply to this message, which was sent for this provider
    /// </summary>
    /// <param name="message"></param>
    void Reply(BattleMessage message);
    /// <summary>
    ///     Handle this message, which was witnessed being sent by this provider
    /// </summary>
    /// <param name="message"></param>
    void Witness(BattleMessage message);

    public bool IsLocal => SyncedEntity is Player plr && plr.whoAmI == Main.myPlayer;
    public ModSide OwningSide => ProviderType switch
    {
        BattleProviderType.None => ModSide.NoSync,
        BattleProviderType.Player => ModSide.Client,
        _ => ModSide.Server
    };
    public BattleParticipant ID => new((byte)SyncedEntity.whoAmI, ProviderType);
    public IBattleProvider Foe
    {
        get => BattleClient.Foe;
        set => BattleClient.Foe = value;
    }
    public BattleParticipant FoeID => Foe.ID;
    public ref ClientBattleState State => ref BattleClient.State;
    public ref bool TieRequest => ref BattleClient.TieRequest;
    public ref byte Pick => ref BattleClient.Pick;
    public ref BattleField Field => ref BattleClient.Battle;
    public void BattleStopped() => BattleClient.BattleStopped();
}

public static class BattleProviderExtensions
{
    public static SimplePackedPokemon[] GetNetTeam(this IBattleProvider provider)
        => SimplePackedPokemon.Team(provider.GetBattleTeam());
}

public enum BattleProviderType : byte
{
    None,
    Player,
    PokemonNPC,
    TrainerNPC,
}
