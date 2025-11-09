using EasyPacketsLib;
using ReLogic.Reflection;
using Terramon.Content.NPCs;
using Terramon.Core.Battling;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace Terramon.Helpers;

public static class VanillaExtensions
{
    public static TerramonPlayer Terramon(this Player player)
    {
        return player.GetModPlayer<TerramonPlayer>();
    }
    
    public static PokemonNPC Pokemon(this NPC npc)
    {
        return (PokemonNPC)npc.ModNPC;
    }

    /// <summary>
    ///     A wrapper for <see cref="Main.NewText(object, Color?)" /> that only sends the message if the player is the local
    ///     player.
    /// </summary>
    public static void NewText(this Player player, object o, Color? color = null)
    {
        if (player.whoAmI != Main.myPlayer) return;
        Main.NewText(o, color);
    }

    /// <summary>
    ///     Returns true or false with equal chance.
    /// </summary>
    public static bool NextBool(this ref FastRandom r)
    {
        return r.NextFloat() < .5;
    }

    /// <summary>
    ///     Generates a random value between <paramref name="minValue" /> (inclusive) and <paramref name="maxValue" />
    ///     (exclusive). <br />It will not return <paramref name="maxValue" />.
    /// </summary>
    public static float NextFloat(this ref FastRandom r, float minValue, float maxValue)
    {
        return r.NextFloat() * (maxValue - minValue) + minValue;
    }

    /// <summary>
    ///     Converts a <see cref="Gender" /> to its Showdown char representation ('M', 'F', 'N').
    /// </summary>
    public static char ToShowdownChar(this Gender gender) =>
        gender switch
        {
            Gender.Male => 'M',
            Gender.Female => 'F',
            Gender.Unspecified => 'N',
            _ => throw new ArgumentOutOfRangeException(nameof(gender), gender, null)
        };

    /// <summary>
    ///     Converts a Showdown char ('M', 'F', 'N') to its <see cref="Gender" />representation.
    /// </summary>
    public static Gender FromShowdownChar(char? c) =>
        c switch
        {
            'M' => Gender.Male,
            'F' => Gender.Female,
            'N' or null => Gender.Unspecified,
            _ => throw new ArgumentOutOfRangeException(nameof(c), c, null)
        };
    public static ushort Terramon(this IdDictionary search, string name)
        => (ushort)search.GetId($"{nameof(Terramon)}/{name}");
    public static void Write(this BinaryWriter writer, BattleParticipant participant)
    {
        writer.Write(participant.WhoAmI);
        writer.Write((byte)participant.Type);
    }
    public static BattleParticipant ReadParticipant(this BinaryReader reader)
        => new(reader.ReadByte(), (BattleProviderType)reader.ReadByte());
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
    public static void DebugLog<T>(this IEasyPacket<T> packet, string extraMessage = null) where T : struct, IEasyPacket<T>
    {
        ModContent.GetInstance<Terramon>().Logger.Debug($"Received {packet.GetType().Name} on {(Main.dedServ ? "server" : "client")} " + extraMessage);
    }
}