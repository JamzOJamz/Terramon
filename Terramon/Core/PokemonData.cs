global using NonVolatileStatus = Showdown.NET.Definitions.StatusID;
using System.Diagnostics.CodeAnalysis;
using ReLogic.Content;
using Showdown.NET.Definitions;
using Terramon.Content.Configs;
using Terramon.Content.Items;
using Terramon.Core.Battling.BattlePackets;
using Terramon.ID;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.IO;
using Terraria.Utilities;

namespace Terramon.Core;

public class PokemonData
{
    private const ushort Version = 0;

    private static readonly int NatureCount = Enum.GetValues<NatureID>().Length;

    private Item _heldItem;
    private ushort _hp;
    private ushort _id;
    private DateTime? _metDate;
    private byte _metLevel;
    private string _ot;
    private uint _personalityValue;
    private string _worldName;
    public AbilityID Ability;
    public BallID Ball = BallID.PokeBall;
    public PokemonEVs EVs;
    public Gender Gender;
    public byte Happiness;
    public bool IsShiny;
    public PokemonIVs IVs;
    public byte Level = 1;
    public PokemonMoves Moves;
    public NatureID Nature;
    public string Nickname;
    public bool Participated;
    public StatStages StatStages;
    public NonVolatileStatus Status;
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

    public ushort MaxHP
    {
        get
        {
            var evFactor = Math.Truncate(EVs.HP / 4d);
            var mainCalc = Math.Truncate(2d * Schema.BaseStats.HP + IVs.HP + evFactor + 100d);
            var afterMult = mainCalc * Level;
            var afterDiv = afterMult / 100d;
            // Console.WriteLine($"BaseHP: {Schema.BaseStats.HP}, HPIV: {IVs.HP}, MainCalc: {mainCalc}, EVFactor: {evFactor}, AfterMult: {afterMult}, AfterDiv: {afterDiv}");
            return (ushort)Math.Truncate(afterDiv + 10d);
        }
    }

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
            Nature = DetermineNature(value);
            Ability = DetermineAbility(Schema, value);
            _personalityValue = value;
        }
    }

    public ref Item HeldItem => ref _heldItem;

    public void Damage(ushort amount, bool isRealtime = false)
    {
        if (isRealtime && RegenHP == 0)
            RegenHP = _hp;

        // Prevent underflow - ensure HP doesn't go below 0
        if (HP > amount)
            HP -= amount;
        else
            HP = 0;
    }

    public void Heal(ushort amount)
    {
        // Prevent overflow - ensure HP doesn't exceed MaxHP
        if (HP <= MaxHP - amount)
            HP += amount;
        else
            HP = MaxHP;

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
        TotalEXP = Math.Min(TotalEXP,
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

    public int ExperienceFromDefeat(PokemonData defeated, float shareMultiplier, TerramonPlayer myOwner)
    {
        const float smallBonus = 4915f / 4096f;

        var b = defeated.Schema.BaseExp;
        var e = _heldItem.ModItem is LuckyEgg ? 1.5f : 1f;
        var f = Happiness >= 220 ? smallBonus : 1f;
        var L = defeated.Level;
        var Lp = Level;
        var p = myOwner.HasExpCharm ? 1.5f : 1f;
        var s = shareMultiplier;
        var t = _ot.Equals(myOwner.Player.name) ? 1f : 1.5f;
        var v = (Schema.Evolution is not null && Level >= Schema.Evolution.AtLevel) ? smallBonus : 1f;
        var pow = (2 * L + 10) / (L + Lp + 10);
        var powFinal = pow * pow * Math.Sqrt(pow);
        var firstMult = b * L * 0.2f * s * powFinal + 1;
        var finalExp = firstMult * t * e * v * f * p;
        return (int)finalExp;
    }

    public byte TrainEV(StatID stat, byte effortIncrease)
    {
        return EVs.Increase(stat, effortIncrease);
    }

    public void TrainEVs(IEnumerable<(StatID Stat, byte EffortIncrease)> evs,
        out (StatID Stat, byte Overflow)[] overflows)
    {
        overflows = null;
        if (evs is null)
            return;
        overflows = new (StatID Stat, byte Overflow)[6];
        int cur = 0;
        foreach ((StatID stat, byte effortIncrease) in evs)
        {
            var overflow = TrainEV(stat, effortIncrease);
            if (overflow != 0)
                overflows[cur++] = (stat, overflow);
        }

        Array.Resize(ref overflows, cur);
    }

    public IEnumerable<(StatID Stat, byte EffortIncrease)> EVsFromDefeat(PokemonData defeated, bool disabled)
    {
        if (disabled)
            yield break;

        var mult = 1;

        _ = this;
        /*
        if (_heldItem.ModItem is MachoBrace)
            mult *= 2;
        if (Pokerus)
            mult *= 2;
        */

        var stats = defeated.Schema.BaseStats;
        for (StatID i = StatID.HP; i <= StatID.Spe; i++)
        {
            byte e = stats.GetEffort(i);
            if (e != 0)
                yield return (i, (byte)(e * mult));
        }
    }

    /// <summary>
    ///     Adjusts current <see cref="HP" /> proportionally when <see cref="MaxHP" /> changes. Used during level-ups and
    ///     evolution.
    /// </summary>
    private void AdjustHPForMaxHPChange(ushort oldMaxHP, ushort oldHP)
    {
        var newMaxHP = MaxHP;

        // Official formula: HP increases by how much MaxHP increased
        var hpDiff = newMaxHP - oldMaxHP;
        var newHP = (ushort)(oldHP + hpDiff);
        HP = newHP;

        // RegenHP should reflect the *actual HP gained*, not the theoretical hpDiff
        // (e.g. if HP was near max and clamped, actual gain may be smaller)
        var actualHpGain = (ushort)(HP - oldHP);
        if (RegenHP != 0)
            RegenHP += actualHpGain;
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

        var oldMaxHP = MaxHP;
        var oldHP = HP;

        Level++;

        AdjustHPForMaxHPChange(oldMaxHP, oldHP);

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
        if (_heldItem.ModItem is EvolutionaryItem item && item.Trigger == trigger)
            return item.GetEvolvedSpecies(this);
        return 0;
    }

    /// <summary>
    ///     Evolves the Pokémon into the specified species.
    /// </summary>
    /// <param name="id">The ID of the species to evolve into.</param>
    public void EvolveInto(ushort id)
    {
        var oldMaxHP = MaxHP;
        var oldHP = HP;

        ID = id;

        AdjustHPForMaxHPChange(oldMaxHP, oldHP);
    }

    public static Builder Create(ushort id, byte level = 1)
    {
        return new Builder(id, level);
    }

    private static bool RollShiny(Player player)
    {
        var shinyChance = GameplayConfig.Instance.ShinySpawnRate;
        var rolls = player.Terramon().HasShinyCharm ? 3 : 1;
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

    private static NatureID DetermineNature(uint pv)
    {
        return (NatureID)(pv % NatureCount);
    }

    private static AbilityID DetermineAbility(DatabaseV2.PokemonSchema schema, uint pv)
    {
        var useSecond = pv % 2 == 1 && schema.Abilities.Ability2 != AbilityID.None;
        return useSecond ? schema.Abilities.Ability2 : schema.Abilities.Ability1;
    }

    private PokemonMoves GetInitialMoves(int maxMoves = 4)
    {
        if (Schema == null)
            throw new InvalidOperationException("Pokémon schema is not initialized.");

        var movesAboveLevelOne = Schema.LevelUpLearnset
            .Where(moveEntry => moveEntry.AtLevel > 1 && moveEntry.AtLevel <= Level)
            .OrderByDescending(moveEntry => moveEntry.AtLevel)
            .Take(maxMoves)
            .ToList();

        var learnset = new List<DatabaseV2.LevelEntrySchema>();

        if (movesAboveLevelOne.Count < maxMoves)
        {
            var remainingSlots = maxMoves - movesAboveLevelOne.Count;

            var levelOneMoves = Schema.LevelUpLearnset
                .Where(moveEntry => moveEntry.AtLevel == 1);
            levelOneMoves = levelOneMoves as DatabaseV2.LevelEntrySchema[] ?? levelOneMoves.ToArray();

            if (levelOneMoves.Count() > remainingSlots)
            {
                levelOneMoves = levelOneMoves
                    .OrderBy(_ => Main.rand.Next())
                    .Take(remainingSlots);
            }
            else
            {
                levelOneMoves = levelOneMoves.Take(remainingSlots);
            }

            learnset.AddRange(levelOneMoves);
        }

        // Add higher-level moves last
        learnset.AddRange(movesAboveLevelOne);

        // Finally we sort moves in ascending order of level
        return new PokemonMoves(learnset.OrderBy(moveEntry => moveEntry.AtLevel).Select(e => (MoveID)e.ID).ToArray());
    }

    public Asset<Texture2D> GetMiniSprite(AssetRequestMode mode = AssetRequestMode.AsyncLoad)
    {
        return ModContent.Request<Texture2D>(
            $"Terramon/Assets/Pokemon/{Schema.Identifier}{(!string.IsNullOrEmpty(Variant) ? "_" + Variant : string.Empty)}_Mini{(IsShiny ? "_S" : string.Empty)}",
            mode);
    }

    public string GetPacked(int? partyIndex = null)
    {
        var nickname = partyIndex.HasValue ? partyIndex.Value.ToString() : Nickname ?? Schema.Identifier;
        var speciesName = nickname == Schema.Identifier ? null : Schema.Identifier;
        var heldItem = _heldItem.IsAir ? null : ItemID.Search.GetName(_heldItem.type);
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

    public SimplePackedPokemon GetNetPacked(int? partyIndex = null)
    {
        var nickOverride = partyIndex.HasValue ? partyIndex.Value.ToString() : null;
        return new(this, nickOverride);
    }

    public void SetFromNetPacked(in SimplePackedPokemon packed, bool setNickname = false)
    {
        if (setNickname)
            Nickname = packed.Nickname;
        if (_heldItem.type != packed.Item)
            _heldItem = new(packed.Item);
        Gender = packed.Gender;
        Ball = packed.Ball;
        ID = packed.Species;
        Ability = packed.Ability;
        Nature = packed.Nature;
        IVs = packed.IVs;
        EVs = packed.EVs;
        Happiness = packed.Happiness;
        IsShiny = packed.IsShiny;
        Level = packed.Level;
        Moves = packed.Moves;
    }

    public bool CureStatus()
    {
        if (Status == NonVolatileStatus.Fnt)
            return false;
        Status = NonVolatileStatus.None;
        return true;
    }

    public bool Faint()
    {
        if (Status == NonVolatileStatus.Fnt)
            return false;
        Status = NonVolatileStatus.Fnt;
        return true;
    }

    public PokemonData ShallowCopy()
    {
        return (PokemonData)MemberwiseClone();
    }

    public class Builder
    {
        private readonly PokemonData _pokemon;
        private Player _shinyPlayer;

        public Builder(ushort id, byte level)
        {
            _pokemon = new PokemonData
            {
                ID = id, // Setting the ID automatically assigns the corresponding Schema
                Level = level,
                _metDate = DateTime.Now,
                _metLevel = level,
                _worldName = Main.worldName,
                _heldItem = new Item(),
                PersonalityValue = (uint)Main.rand.Next(int.MinValue, int.MaxValue),
                IVs = PokemonIVs.Random()
            };

            _pokemon.TotalEXP = ExperienceLookupTable.GetLevelTotalExp(level, _pokemon.Schema.GrowthRate);
            _pokemon.Happiness = _pokemon.Schema.BaseHappiness;
            _pokemon.Moves = _pokemon.GetInitialMoves();
        }

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
            ["hap"] = Happiness,
            ["ivs"] = IVs.Packed,
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

        if (!_heldItem.IsAir)
            tag["item"] = new ItemDefinition(_heldItem.type);

        if (_metDate.HasValue)
            tag["met"] = _metDate.Value.ToBinary();

        if (_metLevel != 0)
            tag["metlvl"] = _metLevel;

        if (!string.IsNullOrEmpty(_worldName))
            tag["world"] = _worldName;

        var moves = Moves.SerializeData();
        if (moves != null)
            tag["moves"] = moves;

        return tag;
    }

    public static PokemonData Load(TagCompound tag)
    {
        // Handle versioning
        var loadedVersion = tag.ContainsKey("version") ? tag.Get<ushort>("version") : (ushort)0;

        if (loadedVersion > Version)
            Terramon.Instance.Logger.Warn("Unsupported PokemonData version " + loadedVersion +
                                          ". This may lead to undefined behaviour!");
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
        else
            data._heldItem = new Item();

        if (tag.TryGet<long>("met", out var metDate))
            data._metDate = DateTime.FromBinary(metDate);

        data._metLevel = tag.TryGet<byte>("metlvl", out var metLevel) ? metLevel : data.Level;

        if (tag.TryGet<string>("world", out var worldName))
            data._worldName = worldName;

        data.Happiness = tag.TryGet("hap", out byte hap)
            ? hap
            : data.Schema.BaseHappiness;

        data.IVs = tag.TryGet<uint>("ivs", out var ivs)
            ? new PokemonIVs(ivs)
            : PokemonIVs.Random();

        data.Moves = tag.TryGet<TagCompound>("moves", out var moves)
            ? PokemonMoves.Load(moves)
            : data.GetInitialMoves();

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
        if ((compareFields & BitHeldItem) != 0 && _heldItem.type != compareData._heldItem.type)
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
        if ((fields & BitHeldItem) != 0) writer.Write7BitEncodedInt(_heldItem.type);
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
            _heldItem = heldItem == 0 ? new Item() : new Item(heldItem);
        }

        if ((fields & BitEXP) != 0) TotalEXP = reader.ReadInt32();
        if ((fields & BitHP) != 0) _hp = (ushort)reader.Read7BitEncodedInt();

        return this;
    }

    #endregion
}

#region Substructures

public struct PokemonIVs
{
    public uint Packed;
    private const uint HPMask = 0x1F;
    private const uint AttackMask = HPMask << 5;
    private const uint DefenseMask = AttackMask << 5;
    private const uint SpAtkMask = DefenseMask << 5;
    private const uint SpDefMask = SpAtkMask << 5;
    private const uint SpeedMask = SpDefMask << 5;

    public byte HP
    {
        readonly get => (byte)(Packed & HPMask);
        init
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 31);
            Packed = (Packed & ~HPMask) | value;
        }
    }

    public byte Attack
    {
        readonly get => (byte)((Packed & AttackMask) >> 5);
        init
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 31);
            Packed = (Packed & ~AttackMask) | ((uint)value << 5);
        }
    }

    public byte Defense
    {
        readonly get => (byte)((Packed & DefenseMask) >> 10);
        init
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 31);
            Packed = (Packed & ~DefenseMask) | ((uint)value << 10);
        }
    }

    public byte SpAtk
    {
        readonly get => (byte)((Packed & SpAtkMask) >> 15);
        init
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 31);
            Packed = (Packed & ~SpAtkMask) | ((uint)value << 15);
        }
    }

    public byte SpDef
    {
        readonly get => (byte)((Packed & SpDefMask) >> 20);
        init
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 31);
            Packed = (Packed & ~SpDefMask) | ((uint)value << 20);
        }
    }

    public byte Speed
    {
        readonly get => (byte)((Packed & SpeedMask) >> 25);
        init
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 31);
            Packed = (Packed & ~SpeedMask) | ((uint)value << 25);
        }
    }

    public byte this[StatID iv]
    {
        readonly get
        {
            var move = 5 * (int)iv;
            return (byte)(Packed & (HPMask << (move)) >> (move));
        }
        set
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 31);
            var move = 5 * (int)iv;
            Packed = (Packed & ~(HPMask << move)) | ((uint)value << move);
        }
    }

    public PokemonIVs(uint packed)
    {
        Packed = packed;
    }

    public PokemonIVs(byte hp, byte attack, byte defense, byte spAtk, byte spDef, byte speed)
    {
        HP = hp;
        Attack = attack;
        Defense = defense;
        SpAtk = spAtk;
        SpDef = spDef;
        Speed = speed;
    }

    public static PokemonIVs Random()
    {
        return new PokemonIVs(
            (byte)Main.rand.Next(0, 32),
            (byte)Main.rand.Next(0, 32),
            (byte)Main.rand.Next(0, 32),
            (byte)Main.rand.Next(0, 32),
            (byte)Main.rand.Next(0, 32),
            (byte)Main.rand.Next(0, 32)
        );
    }

    public readonly string PackedString()
    {
        return $"{HP},{Attack},{Defense},{SpAtk},{SpDef},{Speed}";
    }

    public readonly override string ToString()
    {
        return $"HP: {HP}, Atk: {Attack}, Def: {Defense}, SpA: {SpAtk}, SpD: {SpDef}, Spe: {Speed}";
    }
}

[SuppressMessage("ReSharper", "UnassignedField.Global")]
public struct PokemonEVs
{
    private const ushort MaxTotal = 510;
    private const byte MaxSingle = 252;
    public byte HP, Attack, Defense, SpAttack, SpDefense, Speed;
    public readonly ushort Sum => (ushort)(HP + Attack + Defense + SpAttack + SpDefense + Speed);

    /// <summary>
    ///     Increases a given EV by some amount, bound to the rules of EVs:
    ///     <para />
    ///     <list type="bullet">
    ///         <item>Each EV cannot exceed 252.</item>
    ///         <item>The sum of all EVs cannot exceed 510.</item>
    ///     </list>
    /// </summary>
    /// <param name="ev">The EV to increase.</param>
    /// <param name="amount">The amount to increase the EV by.</param>
    /// <returns>The amount that the EV was actually increased by.</returns>
    public unsafe byte Increase(StatID ev, byte amount)
    {
        var max = (ushort)(MaxTotal - Sum);
        if (max <= 0)
            return 0;
        amount = (byte)Math.Min(amount, max);
        // you pepole and your fancy ref conditionals will never know the pleasure of shaving off 0.0000000001 nanoseconds
        fixed (byte* p = &HP)
        {
            ref var stat = ref *(p + (int)ev);
            var statRoom = (byte)(MaxSingle - stat);
            var applied = Math.Min(amount, statRoom);
            stat += applied;
            return applied;
        }
    }

    /// <summary>
    ///     Decreases a given EV by some amount.
    /// </summary>
    /// <param name="ev">The EV to decrease.</param>
    /// <param name="amount">The amount to decrease the EV by.</param>
    /// <returns>The amount that the EV was actually decreased by.</returns>
    public unsafe byte Decrease(StatID ev, byte amount)
    {
        if (amount == 0)
            return 0;

        fixed (byte* p = &HP)
        {
            ref var stat = ref *(p + (int)ev);
            var applied = (byte)(amount - (byte)(byte.MaxValue - stat));
            stat -= applied;
            return applied;
        }
    }

    /// <summary>
    ///     Changes a given EV by some amount, bound to the rules of EVs:
    ///     <para />
    ///     <list type="bullet">
    ///         <item>Each EV cannot exceed 252.</item>
    ///         <item>The sum of all EVs cannot exceed 510.</item>
    ///     </list>
    /// </summary>
    /// <param name="ev">The EV to change.</param>
    /// <param name="amount">The amount to change the EV by.</param>
    /// <returns>The amount that the EV was actually changed by.</returns>
    public byte Change(StatID ev, short amount)
    {
        return amount < 0 ? Decrease(ev, (byte)-amount) : Increase(ev, (byte)amount);
    }

    public readonly string PackedString()
    {
        return $"{HP},{Attack},{Defense},{SpAttack},{SpDefense},{Speed}";
    }

    public readonly override string ToString()
    {
        return $"HP: {HP}, Atk: {Attack}, Def: {Defense}, SpA: {SpAttack}, SpD: {SpDefense}, Spe: {Speed}";
    }
}

public struct StatStages
{
    public uint Packed;
    private const uint HPMask = 0xF;
    private const uint AttackMask = HPMask << 4;
    private const uint DefenseMask = AttackMask << 4;
    private const uint SpAtkMask = DefenseMask << 4;
    private const uint SpDefMask = SpAtkMask << 4;
    private const uint SpeedMask = SpDefMask << 4;

    public int HP
    {
        readonly get => Signed4Bit(Packed & HPMask);
        set
        {
            CheckError(value);
            Packed = (Packed & ~HPMask) | (byte)value;
        }
    }

    public int Attack
    {
        readonly get => Signed4Bit((Packed & AttackMask) >> 4);
        set
        {
            CheckError(value);
            Packed = (Packed & ~AttackMask) | ((uint)value << 4);
        }
    }

    public int Defense
    {
        readonly get => Signed4Bit((Packed & DefenseMask) >> 8);
        set
        {
            CheckError(value);
            Packed = (Packed & ~DefenseMask) | ((uint)value << 8);
        }
    }

    public int SpAtk
    {
        readonly get => Signed4Bit((Packed & SpAtkMask) >> 12);
        set
        {
            CheckError(value);
            Packed = (Packed & ~SpAtkMask) | ((uint)value << 12);
        }
    }

    public int SpDef
    {
        readonly get => Signed4Bit((Packed & SpDefMask) >> 16);
        set
        {
            CheckError(value);
            Packed = (Packed & ~SpDefMask) | ((uint)value << 16);
        }
    }

    public int Speed
    {
        readonly get => Signed4Bit((Packed & SpeedMask) >> 20);
        set
        {
            CheckError(value);
            Packed = (Packed & ~SpeedMask) | ((uint)value << 20);
        }
    }

    public int this[StatID iv]
    {
        readonly get
        {
            var move = 4 * (int)iv;
            return Signed4Bit(Packed & (HPMask << (move)) >> (move));
        }
        set
        {
            CheckError(value);
            var move = 4 * (int)iv;
            Packed = (Packed & ~(HPMask << move)) | ((uint)value << move);
        }
    }

    private static sbyte Signed4Bit(uint value)
    {
        if ((value & 0x8) != 0)
            return (sbyte)((int)value - 16);
        return (sbyte)value;
    }

    private static void CheckError(int value)
    {
        if (value < -8 || value > 7)
            throw new ArgumentOutOfRangeException(nameof(value));
    }
}

public readonly struct PokemonMoves
{
    private const ushort IDMask = 0x3FF;
    private readonly uint[] _moves = new uint[4];

    public MoveData this[int move]
    {
        get
        {
            Deconstruct(_moves[move], out var id, out var pp, out var ppUp);
            return new MoveData(id, pp, ppUp);
        }
        set => _moves[move] = Construct(value.ID, value.PP, value.PPUp);
    }

    public PokemonMoves(params MoveData?[] moves)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(moves.Length, 4);
        for (var i = 0; i < moves.Length; i++)
        {
            var move = moves[i];
            if (!move.HasValue)
                continue;
            this[i] = move.Value;
        }
    }

    public PokemonMoves(params MoveID[] moves)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(moves.Length, 4);
        for (var i = 0; i < moves.Length; i++)
        {
            var move = moves[i];
            if (move == MoveID.None)
                continue;
            // TODO: look for max pp for move and do stuff here to then pass into movedata ctor
            this[i] = new MoveData(move, 0, 0);
        }
    }

    public PokemonMoves(uint movesA, byte movesB) : this
    (
        (MoveID)(movesA & 0x3FF),
        (MoveID)((movesA >> 10) & 0x3FF),
        (MoveID)((movesA >> 20) & 0x3FF),
        (MoveID)(((movesA) >> 30) | ((uint)movesB << 2))
    )
    {
    }

    private PokemonMoves(params uint[] moves)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(moves.Length, 4);
        for (var i = 0; i < moves.Length; i++)
            _moves[i] = moves[i];
    }

    private static void Deconstruct(uint move, out MoveID id, out byte pp, out byte ppUp)
    {
        id = (MoveID)(move & IDMask);
        pp = (byte)((move >> 10) & 0x3F);
        ppUp = (byte)(move >> 16); // mask out the rest if more data is added
    }

    private static uint Construct(MoveID id, byte pp, byte ppUp)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pp, 63);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pp, 3);
        return (uint)id | ((uint)pp << 10) | ((uint)ppUp << 16);
    }

    public string PackedString(bool withStruggle = true)
    {
        var final = string.Empty;
        for (var i = 0; i < 4; i++)
        {
            var moveID = (MoveID)(_moves[i] & IDMask);
            if (moveID == MoveID.None)
                continue;
            final += $"{moveID}{(i == 3 && !withStruggle ? null : ",")}";
        }

        if (withStruggle)
            final += "Struggle";
        return final;
    }

    public TagCompound SerializeData()
    {
        var tag = new TagCompound();
        if (_moves[0] != 0)
            tag["1"] = _moves[0];
        if (_moves[1] != 0)
            tag["2"] = _moves[1];
        if (_moves[2] != 0)
            tag["3"] = _moves[2];
        if (_moves[3] != 0)
            tag["4"] = _moves[3];
        return tag.Count == 0 ? null : tag;
    }

    public static PokemonMoves Load(TagCompound tag)
    {
        var arr = new uint[4];
        if (tag.ContainsKey("1"))
            arr[0] = tag.Get<uint>("1");
        if (tag.ContainsKey("2"))
            arr[1] = tag.Get<uint>("2");
        if (tag.ContainsKey("3"))
            arr[2] = tag.Get<uint>("3");
        if (tag.ContainsKey("4"))
            arr[3] = tag.Get<uint>("4");
        return new PokemonMoves(arr);
    }
}

public readonly record struct MoveData(MoveID ID, byte PP, byte PPUp)
{
    public DatabaseV2.MoveSchema Schema => Terramon.DatabaseV2.GetMove(ID);
}

#endregion