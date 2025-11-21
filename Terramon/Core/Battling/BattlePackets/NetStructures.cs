using Showdown.NET.Definitions;
using Terramon.ID;

namespace Terramon.Core.Battling.BattlePackets;

public readonly record struct SimpleMonPair
{
    public readonly byte Packed;

    public readonly int SourceSide => (Packed & 0b1) + 1;
    public readonly int SourceSlot => (Packed >> 1) & 0b111;
    public readonly int TargetSide => ((Packed >> 4) & 0b1) + 1;
    public readonly int TargetSlot => (Packed >> 5) & 0b111;

    public readonly SimpleMon Source => new(SourceSide, SourceSlot);
    public readonly SimpleMon Target => new(TargetSide, TargetSlot);

    public SimpleMonPair(byte packed) => Packed = packed;
    public SimpleMonPair(string source, string target) : this(PokemonID.Parse(source), PokemonID.Parse(target)) { }
    public SimpleMonPair(in PokemonID source, in PokemonID target)
    {
        if (source.Player == 2)
            Packed |= 0b1;
        if (target.Player == 2)
            Packed |= 0b10000;

        var sourceSlot = source.Name[0] - '0';
        var targetSlot = target.Name[0] - '0';

        Packed |= (byte)(sourceSlot << 1);
        Packed |= (byte)(targetSlot << 5);
    }
}

public readonly record struct SimpleMon
{
    public readonly byte Packed;
    public SimpleMon(byte packed) => Packed = packed;
    public SimpleMon(int side, int slot) : this((byte)((slot << 4) | side)) { }
    public SimpleMon(string showdownMon)
    {
        var id = PokemonID.Parse(showdownMon);
        var side = id.Player;
        var slot = id.Name[0] - '0' - 1;
        Packed = (byte)((slot << 4) | side);
    }
    public readonly int Side => Packed & 0xF;
    public readonly int Slot => Packed >> 4;
}
public readonly record struct SimpleHP
{
    public readonly uint Packed;
    public SimpleHP(uint packed)
    {
        Packed = packed;
    }
    public SimpleHP(string showdownHP)
    {
        var parts = showdownHP.Split('/', 2);
        var hp = uint.Parse(parts[0]);

        if (parts.Length != 2)
        {
            Packed = hp;
        }
        else
        {
            var maxHp = uint.Parse(parts[1]);
            Packed = (maxHp << 8) | hp;
        }
    }
    public readonly ushort HP => (ushort)(Packed & 0xFF);
    public readonly ushort MaxHP => (ushort)(Packed >> 8);

}
public readonly record struct SimpleDetails
{
    public readonly uint Packed;
    public readonly ushort Species => (ushort)(Packed & 0xFFF);
    public readonly Gender Gender => (Gender)((Packed >> 12) & 0b11);
    public readonly bool Shiny => (Packed & (1 << 14)) != 0;
    public readonly byte Level => (byte)((Packed >> 16) & 0xFF);
    public readonly PokemonType Terastallized => (PokemonType)(Packed >> 24);
    public SimpleDetails(uint packed) => Packed = packed;
    public SimpleDetails(string showdownDetails) : this(Details.Parse(showdownDetails)) { }
    public SimpleDetails(ushort species, Gender gender, bool shiny, int level, PokemonType terastallized)
    {
        var g = (uint)gender << 12;
        var s = shiny ? (1u << 14) : 0u;
        var l = (uint)level << 16;
        var t = (uint)terastallized << 24;
        Packed = species | g | s | l | t;
    }
    public SimpleDetails(in Details original) : this(
        NationalDexID.FromSpecies(original.Species),
        FromShowdownChar(original.Gender),
        original.Shiny,
        original.Level,
        original.Terastallized is null ? PokemonType.None : Enum.Parse<PokemonType>(original.Terastallized))
    {

    }
}

public readonly record struct SimplePackedPokemon
{
    /// <summary>
    ///     Contains presence of nickname, presence of item, gender, and ball.
    /// </summary>
    public readonly byte Header;
    /// <summary>
    ///     Contains species, ability, nature, IVs, happiness, and shinyness.
    /// </summary>
    public readonly ulong Bulk;

    public readonly string Nickname;
    public readonly ushort Item;
    public readonly PokemonEVs EVs;
    public readonly byte Level;
    /// <summary>
    ///     Contains moves 1-3, and 2 bits of move 4
    /// </summary>
    public readonly uint MovesA;
    /// <summary>
    ///     Contains 8 bits of move 4
    /// </summary>
    public readonly byte MovesB;

    public readonly bool HasNickname => (Header & 0b1) != 0;
    public readonly bool HasItem => (Header & 0b10) != 0;
    public readonly Gender Gender => (Gender)((Header >> 2) & 0x3);
    public readonly BallID Ball => (BallID)((Header >> 4) + 1);
    public readonly ushort Species => (ushort)(Bulk & 0x7FF);
    public readonly AbilityID Ability => (AbilityID)((Bulk >> 11) & 0x1FF);
    public readonly NatureID Nature => (NatureID)((Bulk >> 20) & 0x1F);
    public readonly PokemonIVs IVs => new((uint)((Bulk >> 25) & 0x3FFFFFFF));
    public readonly byte Happiness => (byte)((Bulk >> 55) & 0xFF);
    public readonly bool IsShiny => (Bulk & (1ul << 63)) != 0;
    public readonly PokemonMoves Moves => new(MovesA, MovesB);

    public SimplePackedPokemon(PokemonData data, string nicknameOverride = null)
    {
        ref var moves = ref data.Moves;
        var nick = nicknameOverride ?? data.Nickname;
        if (nick != null)
        {
            Header |= 0b1;
            Nickname = nick!;
        }
        if (!data.HeldItem.IsAir)
        {
            Header |= 0b10;
            Item = (ushort)data.HeldItem.type;
        }
        EVs = data.EVs;
        Level = data.Level;

        Header |= (byte)((uint)data.Gender << 2);
        Header |= (byte)((uint)(data.Ball > BallID.CherishBall ? BallID.PokeBall - 1 : data.Ball - 1) << 4);

        // 11 bits
        Bulk |= data.ID;
        // 9 bits [20]
        Bulk |= (uint)data.Ability << 11;
        // 5 bits [25]
        Bulk |= (uint)data.Nature << 20;
        // 30 bits [55]
        Bulk |= (ulong)data.IVs.Packed << 25;
        // 8 bits [63]
        Bulk |= (ulong)data.Happiness << 55;
        // 1 bit [64]
        if (data.IsShiny)
            Bulk |= 1ul << 63;

        MovesA |= (uint)moves[0].ID;

        var id1 = (uint)moves[1].ID;
        if (id1 != 0u)
            MovesA |= id1 << 10;

        var id2 = (uint)moves[2].ID;
        if (id2 != 0u)
            MovesA |= id2 << 20;

        var id3 = (uint)moves[3].ID;
        if (id3 != 0u)
        {
            MovesA |= id3 << 30;
            MovesB = (byte)(id3 >> 2);
        }

        // TODO: other data (hiddenPowerType, gmax, dmaxLevel and teratype)
    }

    public SimplePackedPokemon(BinaryReader r)
    {
        Header = r.ReadByte();
        if (HasNickname)
            Nickname = r.ReadString();
        if (HasItem)
            Item = r.ReadUInt16();
        Bulk = r.ReadUInt64();
        EVs = r.ReadEVs();
        Level = r.ReadByte();
        MovesA = r.ReadUInt32();
        MovesB = r.ReadByte();
    }

    public void Write(BinaryWriter w)
    {
        w.Write(Header);
        if (HasNickname)
            w.Write(Nickname);
        if (HasItem)
            w.Write(Item);
        w.Write(Bulk);
        w.Write(EVs);
        w.Write(Level);
        w.Write(MovesA);
        w.Write(MovesB);
    }

    public override string ToString()
    {
        var schema = Terramon.DatabaseV2.GetPokemon(Species);
        var nickname = Nickname ?? schema.Identifier;
        var speciesName = nickname == schema.Identifier ? null : schema.Identifier;
        var heldItem = HasItem ? ItemID.Search.GetName(Item) : null;
        var shiny = IsShiny ? "S" : null;
        string hiddenPowerType = null;
        string gmax = null;
        byte dmaxLevel = 0;
        string teratype = null;

        return
            $"{nickname}|" +
            $"{speciesName}|" +
            $"{heldItem}|" +
            $"{Ability}|" +
            $"{Moves.PackedString()}|" +
            $"{Nature}|" +
            $"{EVs.PackedString()}|" +
            $"{Gender.ToShowdownChar()}|" +
            $"{IVs.PackedString()}|" +
            $"{shiny}|" +
            $"{Level}|" +
            $"{Happiness},{Ball},{hiddenPowerType},{gmax},{dmaxLevel},{teratype}";
    }

    public static SimplePackedPokemon[] Team(PokemonData[] team, bool nicknamesArePartySlots = true)
    {
        var arrLength = 0;
        for (var i = 0; i < team.Length; i++)
        {
            if (team[i] == null) break;
            arrLength++;
        }
        var final = new SimplePackedPokemon[arrLength];
        for (var i = 0; i < final.Length; i++)
            // because of how the switch choice works in showdown, the number can't actually be the slot directly
            final[i] = new(team[i], nicknamesArePartySlots ? (i + 1).ToString() : null);
        return final;
    }
}

public readonly record struct BattleParticipant(byte WhoAmI, BattleProviderType Type)
{
    public static BattleParticipant None => new();
    public BattleClient Client => BattleManager.GetClient(this);
    public IBattleProvider Provider => BattleManager.GetProvider(this);
}
