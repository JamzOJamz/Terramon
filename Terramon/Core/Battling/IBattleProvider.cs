namespace Terramon.Core.Battling;
public interface IBattleProvider
{
    BattleProviderType ProviderType { get; }
    BattleClient BattleClient { get; }
    Entity SyncedEntity { get; }
    string BattleName { get; }
    PokemonData[] GetBattleTeam();
    void StartBattleEffects();
    void StopBattleEffects();
}

public static class BattleProviderExtensions
{
    public static BattleParticipant GetParticipantID(this IBattleProvider provider)
        => new((byte)provider.SyncedEntity.whoAmI, provider.ProviderType);
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
