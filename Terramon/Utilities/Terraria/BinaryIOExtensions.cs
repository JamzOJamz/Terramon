using Terramon.Core.Battling;
using Terramon.Core.Battling.BattlePackets;

namespace Terramon.Utilities.Terraria;

/// <summary>
///     Extension methods for <see cref="BinaryWriter" /> and <see cref="BinaryReader" />
///     to serialize/deserialize battle-related data structures.
/// </summary>
public static class BinaryIOExtensions
{
    public static void Write(this BinaryWriter writer, IBattleProvider participant)
    {
        var type = participant?.ProviderType ?? BattleProviderType.None;
        writer.Write((byte)type);
        if (type != BattleProviderType.None)
            writer.Write((byte)participant!.SyncedEntity.whoAmI);
    }

    public static IBattleProvider ReadParticipant(this BinaryReader reader)
    {
        var type = (BattleProviderType)reader.ReadByte();
        byte whoAmI = 0;
        if (type != BattleProviderType.None)
            whoAmI = reader.ReadByte();
        return BattleManager.GetProvider(whoAmI, type);
    }

    public static void Write(this BinaryWriter writer, SimpleMon mon) => writer.Write(mon.Packed);

    public static SimpleMon ReadPokemonID(this BinaryReader reader) => new(reader.ReadByte());

    public static void Write(this BinaryWriter writer, SimpleMonPair pair) => writer.Write(pair.Packed);

    public static SimpleMonPair ReadPokemonIDs(this BinaryReader reader) => new(reader.ReadByte());

    public static void Write(this BinaryWriter writer, SimpleHP hp) => writer.Write(hp.Packed);

    public static SimpleHP ReadPokemonHP(this BinaryReader reader) => new(reader.ReadUInt32());

    public static void Write(this BinaryWriter writer, SimpleDetails details) => writer.Write(details.Packed);

    public static SimpleDetails ReadPokemonDetails(this BinaryReader reader) => new(reader.ReadUInt32());

    public static void Write(this BinaryWriter writer, PokemonEVs evs)
    {
        writer.Write(evs.HP);
        writer.Write(evs.Attack);
        writer.Write(evs.Defense);
        writer.Write(evs.SpAttack);
        writer.Write(evs.SpDefense);
        writer.Write(evs.Speed);
    }

    public static PokemonEVs ReadEVs(this BinaryReader reader)
    {
        return new PokemonEVs
        {
            HP = reader.ReadByte(),
            Attack = reader.ReadByte(),
            Defense = reader.ReadByte(),
            SpAttack = reader.ReadByte(),
            SpDefense = reader.ReadByte(),
            Speed = reader.ReadByte(),
        };
    }
}