using Terramon.Content.Configs;
using Terramon.Content.Items;
using Terramon.ID;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.IO;
using Terraria.Utilities;

namespace Terramon.Core;

public class PokemonData
{
    private const ushort Version = 0;

    private readonly Guid _uniqueId = Guid.NewGuid();

    private Item _heldItem;
    private string _ot;
    private uint _personalityValue;
    public BallID Ball = BallID.PokeBall;
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

    /// <summary>
    ///     The localized name of the Pokémon.
    /// </summary>
    public string LocalizedName => Terramon.DatabaseV2.GetLocalizedPokemonNameDirect(ID);

    /// <summary>
    ///     The internal name of the Pokémon. This is unaffected by localization.
    /// </summary>
    public string InternalName => Terramon.DatabaseV2.GetPokemonName(ID);

    public ushort HP =>
        (ushort)(Math.Floor(2 * Terramon.DatabaseV2.GetPokemon(ID).Stats.HP * Level / 100f) + Level + 10);

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
            IsShiny = RollShiny(player)
        };
    }

    private static bool RollShiny(Player player)
    {
        var shinyChance = ModContent.GetInstance<GameplayConfig>().ShinySpawnRate;
        var rolls = player.HasItemInInventoryOrOpenVoidBag(ModContent.ItemType<ShinyCharm>()) ? 3 : 1;
        for (var i = 0; i < rolls; i++)
            if (Main.rand.NextBool(shinyChance))
                return true;

        return false;
    }

    private static Gender DetermineGender(ushort id, uint pv)
    {
        var genderRatio = Terramon.DatabaseV2.GetPokemon(id).GenderRatio;
        return genderRatio >= 0
            ? new FastRandom(pv).Next(8) < genderRatio ? Gender.Female : Gender.Male
            : Gender.Unspecified;
    }

    public PokemonData ShallowCopy()
    {
        return (PokemonData)MemberwiseClone();
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
            tag["ball"] = (byte)Ball;
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

    public static PokemonData Load(TagCompound tag)
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
                //Upgrade(tag, loadedVersion); TODO: Implement upgrade logic
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
            data.Ball = (BallID)ball;
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

    public const int BitID = 1 << 0;
    public const int BitLevel = 1 << 1;
    private const int BitBall = 1 << 2;
    public const int BitIsShiny = 1 << 3;
    public const int BitPersonalityValue = 1 << 4;
    public const int BitNickname = 1 << 5;
    public const int BitVariant = 1 << 6;
    private const int BitOT = 1 << 7;
    private const int BitHeldItem = 1 << 8;

    public const int AllFieldsBitmask = BitID | BitLevel | BitBall | BitIsShiny | BitPersonalityValue | BitNickname |
                                        BitVariant | BitOT | BitHeldItem;

    /// <summary>
    ///     Determines whether the Pokémon's network state has changed compared to the specified data,
    ///     considering only the specified fields for comparison.
    ///     This method is used for network synchronization.
    /// </summary>
    /// <param name="compareData">The Pokémon data to compare against.</param>
    /// <param name="compareFields">The bitmask representing the fields to compare.</param>
    /// <param name="dirtyFields">The bitmask representing the fields that have changed.</param>
    /// <returns>True if any of the specified fields have changed; otherwise, false.</returns>
    public bool IsNetStateDirty(PokemonData compareData, int compareFields, out int dirtyFields)
    {
        dirtyFields = 0;

        if ((compareFields & BitID) != 0 && ID != compareData.ID) dirtyFields |= BitID;
        if ((compareFields & BitLevel) != 0 && Level != compareData.Level) dirtyFields |= BitLevel;
        if ((compareFields & BitBall) != 0 && Ball != compareData.Ball) dirtyFields |= BitBall;
        if ((compareFields & BitIsShiny) != 0 && IsShiny != compareData.IsShiny) dirtyFields |= BitIsShiny;
        if ((compareFields & BitPersonalityValue) != 0 && PersonalityValue != compareData.PersonalityValue)
            dirtyFields |= BitPersonalityValue;
        if ((compareFields & BitNickname) != 0 && Nickname != compareData.Nickname) dirtyFields |= BitNickname;
        if ((compareFields & BitVariant) != 0 && Variant != compareData.Variant) dirtyFields |= BitVariant;
        if ((compareFields & BitOT) != 0 && _ot != compareData._ot) dirtyFields |= BitOT;
        if ((compareFields & BitHeldItem) != 0 && _heldItem?.type != compareData._heldItem?.type)
            dirtyFields |= BitHeldItem;

        return dirtyFields != 0;
    }


    /// <summary>
    ///     Copies the network state of this Pokémon to the specified target.
    ///     This method is used for network synchronization.
    /// </summary>
    public void CopyNetStateTo(PokemonData target, int fields)
    {
        if ((fields & BitID) != 0) target.ID = ID;
        if ((fields & BitLevel) != 0) target.Level = Level;
        if ((fields & BitBall) != 0) target.Ball = Ball;
        if ((fields & BitIsShiny) != 0) target.IsShiny = IsShiny;
        if ((fields & BitPersonalityValue) != 0) target.PersonalityValue = PersonalityValue;
        if ((fields & BitNickname) != 0) target.Nickname = Nickname;
        if ((fields & BitVariant) != 0) target.Variant = Variant;
        if ((fields & BitOT) != 0) target._ot = _ot;
        if ((fields & BitHeldItem) != 0) target._heldItem = _heldItem;
    }

    /// <summary>
    ///     Writes this Pokémon's data to the specified writer.
    ///     This method is used for network synchronization.
    /// </summary>
    public void NetWrite(BinaryWriter writer, int fields = AllFieldsBitmask)
    {
        writer.Write7BitEncodedInt(fields);

        if ((fields & BitID) != 0) writer.Write7BitEncodedInt(ID);
        if ((fields & BitLevel) != 0) writer.Write(Level);
        if ((fields & BitBall) != 0) writer.Write((byte)Ball);
        if ((fields & BitIsShiny) != 0) writer.Write(IsShiny);
        if ((fields & BitPersonalityValue) != 0) writer.Write(PersonalityValue);
        if ((fields & BitNickname) != 0) writer.Write(Nickname ?? string.Empty);
        if ((fields & BitVariant) != 0) writer.Write(Variant ?? string.Empty);
        if ((fields & BitOT) != 0) writer.Write(_ot ?? string.Empty);
        if ((fields & BitHeldItem) != 0) writer.Write7BitEncodedInt(_heldItem?.type ?? 0);
    }

    /// <summary>
    ///     Reads this Pokémon's data from the specified reader.
    ///     This method is used for network synchronization.
    /// </summary>
    /// <returns>The instance the method was called on.</returns>
    public PokemonData NetRead(BinaryReader reader)
    {
        var fields = reader.Read7BitEncodedInt();

        if ((fields & BitID) != 0) ID = (ushort)reader.Read7BitEncodedInt();
        if ((fields & BitLevel) != 0) Level = reader.ReadByte();
        if ((fields & BitBall) != 0) Ball = (BallID)reader.ReadByte();
        if ((fields & BitIsShiny) != 0) IsShiny = reader.ReadBoolean();
        if ((fields & BitPersonalityValue) != 0) PersonalityValue = reader.ReadUInt32();
        if ((fields & BitNickname) != 0) Nickname = reader.ReadString();
        if ((fields & BitVariant) != 0) Variant = reader.ReadString();
        if ((fields & BitOT) != 0) _ot = reader.ReadString();
        if ((fields & BitHeldItem) != 0)
        {
            var heldItem = reader.Read7BitEncodedInt();
            _heldItem = heldItem == 0 ? null : new Item(heldItem);
        }

        return this;
    }

    #endregion
}