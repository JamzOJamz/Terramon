using EasyPacketsLib;
using ReLogic.Reflection;
using Terramon.Content.NPCs;
using Terramon.Core.Battling;
using Terramon.Core.Battling.BattlePackets;
using Terraria.GameContent.UI;
using Terraria.GameContent.UI.Chat;
using Terraria.Utilities;

namespace Terramon.Helpers;

public static class VanillaExtensions
{
    /// <summary>
    ///     Gets the <see cref="TerramonPlayer" /> instance associated with this <see cref="Player" />.
    /// </summary>
    /// <param name="player">The player whose Terramon mod player data should be retrieved.</param>
    /// <returns>The <see cref="TerramonPlayer" /> tied to the given <see cref="Player" />.</returns>
    public static TerramonPlayer Terramon(this Player player) => player.GetModPlayer<TerramonPlayer>();

    /// <summary>
    ///     Gets the <see cref="PokemonNPC" /> instance associated with this <see cref="NPC" />.
    /// </summary>
    /// <param name="npc">The NPC whose <see cref="PokemonNPC" /> should be retrieved.</param>
    /// <returns>
    ///     The <see cref="PokemonNPC" /> instance backing the given <see cref="NPC" />.
    /// </returns>
    /// <exception cref="InvalidCastException">
    ///     Thrown if the NPC's <see cref="ModNPC" /> is not a <see cref="PokemonNPC" />.
    /// </exception>
    public static PokemonNPC Pokemon(this NPC npc) => (PokemonNPC)npc.ModNPC;

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

    /// <summary>
    ///     Returns the item name normalized, prefixed with an item chat tag, and with the color of its rarity.
    /// </summary>
    public static string PrettyName(this Item i, bool itemIcon = true)
    {
        var oldStack = i.stack;
        i.stack = 1;

        var rarityColor = i.rare switch
        {
            ItemRarityID.Expert => Main.DiscoColor,
            ItemRarityID.Master => new Color(255, (byte)(Main.masterColor * 200f), 0),
            >= ItemRarityID.Count => RarityLoader.GetRarity(i.rare).RarityColor,
            _ => ItemRarity._rarities.GetValueOrDefault(i.rare, Color.White)
        };

        var result = (itemIcon ? ItemTagHandler.GenerateTag(i) + ' ' : string.Empty) + $"[c/{rarityColor.ToHexString()}:{i.Name}]";
        i.stack = oldStack;
        return result;
    }
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

    private static void DebugLog(this IEasyPacket packet, string pre, string post = null)
    {
        var msg = (Main.dedServ ? "Server: " : "Client: ") + pre + $" {packet.GetType().Name} " + post;
        ModContent.GetInstance<Terramon>().Logger.Debug(msg);
        if (Main.dedServ)
            Console.WriteLine(msg);
    }
    
    public static void ReceiveLog(this IEasyPacket packet, string post = null)
    {
        DebugLog(packet, "Received", post);
    }
    
    public static void SendLog(this IEasyPacket packet, string post = null)
    {
        DebugLog(packet, "Sent", post);
    }
}