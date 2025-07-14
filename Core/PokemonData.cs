using ReLogic.Content;
using Terramon.Content.Configs;
using Terramon.Content.Items;
using Terramon.Content.Items.Evolutionary;
using Terramon.Content.Items.KeyItems;
using Terramon.ID;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.IO;
using Terraria.Utilities;

namespace Terramon.Core;

public class PokemonData
{
    private const ushort Version = 0;

    private Item _heldItem;
    private ushort _hp;
    private ushort _id;
    private DateTime? _metDate;
    private byte _metLevel;
    private string _ot;
    private uint _personalityValue;
    private string _worldName;
    public BallID Ball = BallID.PokeBall;
    public Gender Gender;
    public bool IsShiny;
    public byte Level = 1;
    public string Nickname;
    public string Variant;

    public ushort ID
    {
        get => _id;
        set
        {
            _id = value;
            Schema = Terramon.DatabaseV2.GetPokemon(value);
        }
    }

    /// <summary>
    ///     The display name of the Pokémon. If a nickname is set, it will be used. Otherwise, the localized name will be used.
    /// </summary>
    public string DisplayName =>
        string.IsNullOrEmpty(Nickname) ? DatabaseV2.GetLocalizedPokemonNameDirect(Schema) : Nickname;

    /// <summary>
    ///     The localized name of the Pokémon.
    /// </summary>
    public string LocalizedName => DatabaseV2.GetLocalizedPokemonNameDirect(Schema);

    /// <summary>
    ///     The internal name of the Pokémon. This is unaffected by localization.
    /// </summary>
    public string InternalName => Schema.Identifier;

    /// <summary>
    ///     The cached database schema corresponding to this Pokémon's species.
    ///     This is updated automatically whenever the Pokémon's <see cref="ID" /> changes.
    /// </summary>
    public DatabaseV2.PokemonSchema Schema { get; private set; }

    public ushort HP
    {
        get => _hp;
        set => _hp = Math.Clamp(value, (ushort)0, MaxHP);
    }

    public ushort MaxHP =>
        (ushort)(Math.Floor(2 * Schema.Stats.HP * Level / 100f) + Level + 10);
    
    public ushort RegenHP { get; private set; }

    /// <summary>
    ///     The total experience points the Pokémon has gained.
    /// </summary>
    public int TotalEXP { get; private set; }

    private uint PersonalityValue
    {
        get => _personalityValue;
        set
        {
            Gender = DetermineGender(Schema, value);
            _personalityValue = value;
        }
    }

    public void Damage(ushort amount, bool isRealtime = false)
    {
        if (isRealtime && RegenHP == 0)
            RegenHP = _hp;
        
        HP -= amount;
    }

    public void Heal(ushort amount)
    {
        HP += amount;

        if (RegenHP != 0 && _hp >= RegenHP)
            RegenHP = 0;
    }

    public void GainExperience(int amount, out int levelsGained, out int overflow)
    {
        if (Level >= Terramon.MaxPokemonLevel)
        {
            levelsGained = 0;
            overflow = 0;
            return;
        }

        var growthRate = Schema.GrowthRate;

        // Increase the total experience points by the specified amount
        TotalEXP += amount;

        // Clamp the total experience points to an appropriate range
        var oldTotalEXP = TotalEXP;
        TotalEXP = Math.Clamp(TotalEXP, 0,
            ExperienceLookupTable.GetLevelTotalExp(Terramon.MaxPokemonLevel, growthRate));
        overflow = oldTotalEXP - TotalEXP;

        //Main.NewText("New total EXP: " + TotalEXP);

        // Level up the Pokémon if it has enough experience points
        levelsGained = 0;
        while (Level < Terramon.MaxPokemonLevel &&
               TotalEXP >= ExperienceLookupTable.GetLevelTotalExp(Level + 1, growthRate))
        {
            LevelUp(false);
            //Main.NewText("Level up!");
            levelsGained++;
        }
    }

    /// <summary>
    ///     Increases the Pokémon's level by 1.
    ///     Returns false if the Pokémon is already at level 100.
    /// </summary>
    /// <param name="ensureMinimumExperience">
    ///     Whether to ensure that the Pokémon has at least the minimum experience required
    ///     for the new level.
    /// </param>
    public bool LevelUp(bool ensureMinimumExperience = true)
    {
        if (Level >= Terramon.MaxPokemonLevel)
            return false;
        Level++;
        if (ensureMinimumExperience)
            TotalEXP = Math.Max(TotalEXP, ExperienceLookupTable.GetLevelTotalExp(Level, Schema.GrowthRate));
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

    public static Builder Create(ushort id, byte level = 1)
    {
        return new Builder(id, level);
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

    private static Gender DetermineGender(DatabaseV2.PokemonSchema schema, uint pv)
    {
        var genderRatio = schema.GenderRatio;
        return genderRatio >= 0
            ? new FastRandom(pv).Next(8) < genderRatio ? Gender.Female : Gender.Male
            : Gender.Unspecified;
    }

    public Asset<Texture2D> GetMiniSprite(AssetRequestMode mode = AssetRequestMode.AsyncLoad)
    {
        return ModContent.Request<Texture2D>(
            $"Terramon/Assets/Pokemon/{Schema.Identifier}{(!string.IsNullOrEmpty(Variant) ? "_" + Variant : string.Empty)}_Mini{(IsShiny ? "_S" : string.Empty)}",
            mode);
    }

    public PokemonData ShallowCopy()
    {
        return (PokemonData)MemberwiseClone();
    }

    public class Builder(ushort id, byte level)
    {
        private readonly PokemonData _pokemon = new()
        {
            ID = id,
            Level = level,
            TotalEXP = ExperienceLookupTable.GetLevelTotalExp(level, Terramon.DatabaseV2.GetPokemon(id).GrowthRate),
            _metDate = DateTime.Now,
            _metLevel = level,
            _worldName = Main.worldName,
            PersonalityValue = (uint)Main.rand.Next(int.MinValue, int.MaxValue)
        };

        private Player _shinyPlayer;

        public Builder CaughtBy(Player player)
        {
            return ForPlayer(player).OwnedBy(player);
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public Builder OwnedBy(Player player)
        {
            _pokemon._ot = player?.name;
            return this;
        }

        public Builder ForPlayer(Player player)
        {
            _shinyPlayer = player;
            return this;
        }

        public Builder WithBall(BallID ball)
        {
            _pokemon.Ball = ball;
            return this;
        }

        public Builder WithNickname(string nickname)
        {
            _pokemon.Nickname = nickname;
            return this;
        }

        public Builder WithVariant(string variant)
        {
            _pokemon.Variant = variant;
            return this;
        }

        public Builder ForceShiny(bool isShiny = true)
        {
            _pokemon.IsShiny = isShiny;
            _shinyPlayer = null; // Don't roll if forced
            return this;
        }

        public PokemonData Build()
        {
            // Only roll for shiny if not already forced and we have a player
            if (_shinyPlayer != null && !_pokemon.IsShiny)
                _pokemon.IsShiny = RollShiny(_shinyPlayer);

            _pokemon._hp = _pokemon.MaxHP;
            return _pokemon;
        }
    }

    #region NBT Serialization

    public TagCompound SerializeData()
    {
        var tag = new TagCompound
        {
            ["id"] = ID,
            ["lvl"] = Level,
            ["hp"] = _hp,
            ["exp"] = TotalEXP,
            ["ot"] = _ot,
            ["pv"] = PersonalityValue,
            ["version"] = Version
        };

        // Optional fields - only serialize if different from defaults
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

        if (_metDate.HasValue)
            tag["met"] = _metDate.Value.ToBinary();

        if (_metLevel != 0)
            tag["metlvl"] = _metLevel;

        if (!string.IsNullOrEmpty(_worldName))
            tag["world"] = _worldName;

        return tag;
    }

    public static PokemonData Load(TagCompound tag)
    {
        // Handle versioning
        var loadedVersion = tag.ContainsKey("version") ? tag.Get<ushort>("version") : (ushort)0;

        if (loadedVersion > Version)
            Terramon.Instance.Logger.Warn($"Unsupported PokemonData version {loadedVersion}. " +
                                          "This may lead to undefined behaviour!");
        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        /*else if (loadedVersion < Version)
            Upgrade(tag, loadedVersion);*/

        // Load required fields
        var data = new PokemonData
        {
            ID = (ushort)tag.GetShort("id"),
            Level = tag.GetByte("lvl"),
            _ot = tag.GetString("ot"),
            PersonalityValue = tag.Get<uint>("pv")
        };

        // Load optional fields
        data._hp = tag.TryGet<ushort>("hp", out var hp) ? hp : data.MaxHP;
        
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

        if (tag.TryGet<long>("met", out var metDate))
            data._metDate = DateTime.FromBinary(metDate);

        data._metLevel = tag.TryGet<byte>("metlvl", out var metLevel) ? metLevel : data.Level;

        if (tag.TryGet<string>("world", out var worldName))
            data._worldName = worldName;

        // Set experience last to ensure proper level calculation
        var expToSet = tag.TryGet<int>("exp", out var exp)
            ? exp
            : ExperienceLookupTable.GetLevelTotalExp(data.Level, data.Schema.GrowthRate);

        data.GainExperience(expToSet, out _, out _);

        return data;
    }

    /*private static readonly Dictionary<ushort, Action<TagCompound>> UpgradeSteps = new()
    {
        { 0, UpgradeFromV0 }
    };

    private static void Upgrade(TagCompound tag, ushort oldVersion)
    {
        Terramon.Instance.Logger.Debug($"Upgrading PokemonData from version {oldVersion} to {Version}");

        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        while (oldVersion < Version)
        {
            if (UpgradeSteps.TryGetValue(oldVersion, out var upgrade))
                upgrade(tag);
            oldVersion++;
        }

        tag["version"] = Version;
    }

    private static void UpgradeFromV0(TagCompound tag) // TODO: Implement upgrade logic when necessary
    {
    }*/

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
    public const int BitEXP = 1 << 9;
    public const int BitHP = 1 << 10;

    public const int AllFieldsBitmask = BitID | BitLevel | BitBall | BitIsShiny | BitPersonalityValue | BitNickname |
                                        BitVariant | BitOT | BitHeldItem | BitEXP | BitHP;

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
        if ((compareFields & BitEXP) != 0 && TotalEXP != compareData.TotalEXP) dirtyFields |= BitEXP;
        if ((compareFields & BitHP) != 0 && _hp != compareData._hp) dirtyFields |= BitHP;

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
        if ((fields & BitEXP) != 0) target.TotalEXP = TotalEXP;
        if ((fields & BitHP) != 0) target._hp = _hp;
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
        if ((fields & BitEXP) != 0) writer.Write(TotalEXP);
        if ((fields & BitHP) != 0) writer.Write7BitEncodedInt(_hp);
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

        if ((fields & BitEXP) != 0) TotalEXP = reader.ReadInt32();
        if ((fields & BitHP) != 0) _hp = (ushort)reader.Read7BitEncodedInt();

        return this;
    }

    #endregion
}