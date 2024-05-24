using System;
using System.IO;
using Terramon.Content.Items.Evolutionary;
using Terramon.ID;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.IO;
using Terraria.Utilities;

namespace Terramon.Core;

public class PokemonData : TagSerializable
{
    private const ushort Version = 0;

    // ReSharper disable once UnusedMember.Global
    // ReSharper disable once InconsistentNaming
    public static readonly Func<TagCompound, PokemonData> DESERIALIZER = Load;

    private Item _heldItem;
    private string _ot;
    private uint _personalityValue;
    public byte Ball = BallID.PokeBall;
    public Gender Gender;
    public ushort ID;
    public bool IsShiny;
    public byte Level = 1;
    public string Nickname;
    public string Variant;

    /// <summary>
    ///     The display name of the Pokémon. If a nickname is set, it will be used. Otherwise, the localized name will be used.
    /// </summary>
    public string DisplayName =>
        string.IsNullOrEmpty(Nickname) ? Terramon.DatabaseV2.GetLocalizedPokemonName(ID).Value : Nickname;

    private uint PersonalityValue
    {
        get => _personalityValue;
        set
        {
            Gender = DetermineGender(ID, value);
            _personalityValue = value;
        }
    }

    /// <summary>
    ///     Increases the Pokémon's level by 1.
    ///     Returns false if the Pokémon is already at level 100.
    /// </summary>
    public bool LevelUp()
    {
        if (Level >= 100)
            return false;
        Level++;
        return true;
    }

    /// <summary>
    ///     Returns the ID of the species the Pokémon should evolve into.
    /// </summary>
    /// <param name="trigger">The trigger that prompted the evolution.</param>
    public ushort GetQueuedEvolution(EvolutionTrigger trigger)
    {
        switch (trigger)
        {
            case EvolutionTrigger.DirectUse:
                return 0;
            case EvolutionTrigger.LevelUp:
            {
                var naturalEvolution = Terramon.DatabaseV2.GetEvolutionAtLevel(ID, Level);
                if (naturalEvolution != 0)
                    return naturalEvolution;
                break;
            }
            case EvolutionTrigger.Trade:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(trigger), trigger, null);
        }

        // Check for Pokémon that evolve through held items
        if (_heldItem?.ModItem is EvolutionaryItem item && item.Trigger == trigger)
            return item.GetEvolvedSpecies(this);
        return 0;
    }

    /// <summary>
    ///     Evolves the Pokémon into the specified species.
    /// </summary>
    /// <param name="id">The ID of the species to evolve into.</param>
    public void EvolveInto(ushort id)
    {
        ID = id;
    }

    public static PokemonData Create(Player player, ushort id, byte level = 1)
    {
        return new PokemonData
        {
            ID = id,
            Level = level,
            _ot = player.name,
            PersonalityValue = (uint)Main.rand.Next(int.MinValue, int.MaxValue),
            IsShiny = Terramon.RollShiny(player)
        };
    }

    private static Gender DetermineGender(ushort id, uint pv)
    {
        var genderRate = Terramon.DatabaseV2.GetPokemon(id).GenderRate;
        return genderRate >= 0
            ? new FastRandom(pv).Next(8) < genderRate ? Gender.Female : Gender.Male
            : Gender.Unspecified;
    }

    #region NBT Serialization

    public TagCompound SerializeData()
    {
        var tag = new TagCompound
        {
            ["id"] = ID,
            ["lvl"] = Level,
            ["ot"] = _ot,
            ["pv"] = PersonalityValue,
            ["version"] = Version
        };
        if (Ball != BallID.PokeBall)
            tag["ball"] = Ball;
        if (IsShiny)
            tag["isShiny"] = true;
        if (!string.IsNullOrEmpty(Nickname))
            tag["n"] = Nickname;
        if (!string.IsNullOrEmpty(Variant))
            tag["variant"] = Variant;
        if (_heldItem != null)
            tag["item"] = new ItemDefinition(_heldItem.type);
        return tag;
    }

    private static PokemonData Load(TagCompound tag)
    {
        // Try to load the tag version. If it doesn't exist, it's version 0.
        var loadedVersion = 0;
        if (tag.ContainsKey("version"))
            loadedVersion = tag.Get<ushort>("version");

        switch (loadedVersion)
        {
            case < Version:
                Terramon.Instance.Logger.Debug("Upgrading PokemonData from version " + loadedVersion + " to " +
                                               Version);
                break;
            case > Version:
                Terramon.Instance.Logger.Warn("Unsupported PokemonData version " + loadedVersion +
                                              ". This may lead to undefined behaviour!");
                break;
        }

        var data = new PokemonData
        {
            ID = (ushort)tag.GetShort("id"),
            Level = tag.GetByte("lvl"),
            _ot = tag.GetString("ot"),
            PersonalityValue = tag.Get<uint>("pv")
        };
        if (tag.TryGet<byte>("ball", out var ball))
            data.Ball = ball;
        if (tag.TryGet<bool>("isShiny", out var isShiny))
            data.IsShiny = isShiny;
        if (tag.TryGet<string>("n", out var nickname))
            data.Nickname = nickname;
        if (tag.TryGet<string>("variant", out var variant))
            data.Variant = variant;
        if (tag.TryGet<ItemDefinition>("item", out var itemDefinition))
            data._heldItem = new Item(itemDefinition.Type);
        return data;
    }

    #endregion

    #region Network Sync

    /// <summary>
    ///     Writes this Pokémon's data to the specified writer.
    ///     This method is used for network synchronization.
    /// </summary>
    public void NetSend(BinaryWriter writer)
    {
        writer.Write7BitEncodedInt(ID);
        writer.Write(Level);
        writer.Write(Ball);
        writer.Write(IsShiny);
        writer.Write(PersonalityValue);
        writer.Write(Nickname ?? string.Empty);
        writer.Write(Variant ?? string.Empty);
        writer.Write(_ot ?? string.Empty);
        writer.Write7BitEncodedInt(_heldItem?.type ?? 0);
    }

    /// <summary>
    ///     Reads this Pokémon's data from the specified reader.
    ///     This method is used for network synchronization.
    /// </summary>
    public void NetReceive(BinaryReader reader)
    {
        ID = (ushort)reader.Read7BitEncodedInt();
        Level = reader.ReadByte();
        Ball = reader.ReadByte();
        IsShiny = reader.ReadBoolean();
        PersonalityValue = reader.ReadUInt32();
        Nickname = reader.ReadString();
        Variant = reader.ReadString();
        _ot = reader.ReadString();
        var heldItem = reader.Read7BitEncodedInt();
        _heldItem = heldItem == 0 ? null : new Item(heldItem);
    }

    #endregion
}