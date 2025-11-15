using Showdown.NET.Definitions;
using Terramon.Content.NPCs;
using Terramon.Core.Battling.BattlePackets;
using Terramon.ID;

namespace Terramon.Core.Battling;

public struct BattlePokemon()
{
    public BattleSide Side;
    public byte Slot;
    private ushort _heldItem;
    public ushort HeldItem
    {
        readonly get => (ushort)(Data?.HeldItem.type ?? _heldItem);
        set
        {
            if (Data != null)
                Data.HeldItem = value == 0 ? null : new(value);
            _heldItem = value;
        }
    }
    public readonly BattleSide OppositeSide => Side.Opposite;
    public bool Participated;
    public PokemonData Data;
    public readonly string DisplayName => Data?.DisplayName ?? Terramon.DatabaseV2.GetPokemon(_species).Identifier;
    public readonly string OwnerName
        => Side.Provider switch
        {
            TerramonPlayer plr => plr.Player.name,
            _ => null,
        };

    public readonly string PokeMessage
        => Side.Provider switch
        {
            TerramonPlayer plr => $"{plr.Player.name}'s {DisplayName}",
            PokemonNPC => $"Wild {DisplayName}",
            _ => null
        };

    public HashSet<VolatileEffect> Volatiles = [];
    private ushort _hp;
    private ushort _species;
    private Gender _gender;
    private bool _shiny;
    private byte _level;
    private PokemonType _terastallized;
    private NonVolatileStatus _status;
    private StatStages _statStages;
    private AbilityID _ability;
    public bool AbilitySuppressed;
    public ushort HP
    {
        readonly get => Data?.HP ?? _hp;
        set
        {
            if (Data != null)
                Data.HP = value;
            _hp = value;
        }
    }
    public ushort Species
    {
        readonly get => Data?.ID ?? _species;
        set
        {
            if (Data != null)
                Data.ID = value;
            _species = value;
        }
    }
    public Gender Gender
    {
        readonly get => Data?.Gender ?? _gender;
        set
        {
            if (Data != null)
                Data.Gender = value;
            _gender = value;
        }
    }
    public bool IsShiny
    {
        readonly get => Data?.IsShiny ?? _shiny;
        set
        {
            if (Data != null)
                Data.IsShiny = value;
            _shiny = value;
        }
    }
    public byte Level
    {
        readonly get => Data?.Level ?? _level;
        set
        {
            if (Data != null)
                Data.Level = value;
            _level = value;
        }
    }
    public PokemonType Terastallized
    {
        readonly get => _terastallized;
        set => _terastallized = value;
    }
    public AbilityID Ability
    {
        readonly get => Data?.Ability ?? _ability;
        set
        {
            if (Data != null)
                Data.Ability = value;
            _ability = value;
        }
    }
    public SimpleDetails Details
    {
        readonly get => new(Species, Gender, IsShiny, Level, Terastallized);
        set
        {
            Species = value.Species;
            Gender = value.Gender;
            IsShiny = value.Shiny;
            Level = value.Level;
            Terastallized = value.Terastallized;
        }
    }
    public SimpleHP HPData
    {
        set
        {
            HP = value.HP;
            if (value.MaxHP == 0) // faint
            {
                Faint();
            }
            else if (Data != null)
            {
                if (Data.MaxHP != value.MaxHP)
                    Terramon.Instance.Logger.Warn(
                        $"Value of Max HP given by Pokémon Showdown and the value calculated by Terramon for Pokémon of species {Species} differ:\n" +
                        $"Terramon: {Data.MaxHP}\n" +
                        $"Showdown: {value.MaxHP}");
            }
        }
    }
    public NonVolatileStatus Status
    {
        readonly get => Data?.Status ?? _status;
        set
        {
            if (Data != null)
                Data.Status = value;
            _status = value;
        }
    }
    public StatStages StatStages
    {
        readonly get => Data?.StatStages ?? _statStages;
        set
        {
            if (Data != null)
                Data.StatStages = value;
            _statStages = value;
        }
    }
    public void SetStat(byte stat, int value, bool setDirectly)
        => SetStat((StatID)stat, value, setDirectly);
    public void SetStat(StatID stat, int value, bool setDirectly)
    {
        int newValue = Math.Clamp(setDirectly ? value : (Data is null ? _statStages[stat] : Data.StatStages[stat]) + value, -7, 7);
        if (Data != null)
            Data.StatStages[stat] = newValue;
        _statStages[stat] = newValue;
    }
    public uint PackedStats
    {
        readonly get => Data?.StatStages.Packed ?? _statStages.Packed;
        set
        {
            if (Data != null)
                Data.StatStages.Packed = value;
            _statStages.Packed = value;
        }
    }
    public readonly string PokeName => Data.DisplayName;
    public readonly void PlayMoveAnimation(ushort move)
    {
        _ = _gender;
    }
    public void SetAsActive()
    {
        Side.SetActivePokemon(Slot);
        Participated = true;
    }
    public void Faint()
    {
        Data?.Faint();
        _status = NonVolatileStatus.Fnt;
    }
    public void CureStatus()
    {
        Data?.CureStatus();
        if (Status == NonVolatileStatus.Fnt)
            return;
        Status = NonVolatileStatus.None;
        return;
    }
    public void ModifyBoosts(BoostModifierAction action)
    {
        uint? clear = null;
        switch (action)
        {
            case BoostModifierAction.Invert:
                PackedStats ^= 0b100010001000100010001000u;
                break;
            case BoostModifierAction.ClearAll:
                for (int i = 0; i <= (int)StatID.Spe; i++)
                {
                    // normalizes position of the last bit so it's at 0b1, then masks the rest out.
                    // clear = 0, then, leads to positive numbers being skipped, and 1 to negative numbers being skipped
                    var shift = i * 4;
                    if (clear.HasValue &&
                        ((PackedStats >> (3 + shift)) & 1) == clear.Value)
                            continue;
                    PackedStats &= ~(0xFu << shift);
                }
                break;
            case BoostModifierAction.ClearPositive:
                clear = 1u;
                goto case BoostModifierAction.ClearAll;
            case BoostModifierAction.ClearNegative:
                clear = 0u;
                goto case BoostModifierAction.ClearAll;
        }
    }
}

public enum BoostModifierAction : byte
{
    Invert,
    ClearAll,
    ClearPositive,
    ClearNegative
}
