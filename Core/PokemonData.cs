using System;
using Terramon.ID;
using Terraria.ModLoader.IO;
using Terraria.Utilities;

namespace Terramon.Core;

public class PokemonData : TagSerializable
{
    private const ushort VERSION = 0;

    // ReSharper disable once UnusedMember.Global
    public static readonly Func<TagCompound, PokemonData> DESERIALIZER = Load;

    private readonly uint _personalityValue;
    public byte Ball = BallID.PokeBall;
    public Gender Gender;
    public ushort ID;
    public bool IsShiny;
    public byte Level = 1;
    public string Nickname;
    private string OT;
    public string Variant;

    public string DisplayName => Nickname ?? Terramon.DatabaseV2.GetLocalizedPokemonName(ID).Value;

    private uint PersonalityValue
    {
        get => _personalityValue;
        init
        {
            Gender = DetermineGender(ID, value);
            _personalityValue = value;
        }
    }

    public TagCompound SerializeData()
    {
        var tag = new TagCompound
        {
            ["ball"] = Ball,
            ["id"] = ID,
            ["lvl"] = Level,
            ["ot"] = OT,
            ["pv"] = PersonalityValue,
            ["version"] = VERSION
        };
        if (IsShiny)
            tag["isShiny"] = true;
        if (Nickname != null)
            tag["n"] = Nickname;
        if (Variant != null)
            tag["variant"] = Variant;
        return tag;
    }

    public static PokemonData Create(Player player, ushort id, byte level = 1)
    {
        return new PokemonData
        {
            ID = id,
            Level = level,
            OT = player.name,
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

    private static PokemonData Load(TagCompound tag)
    {
        // Try to load the tag version. If it doesn't exist, it's version 0.
        var loadedVersion = 0;
        if (tag.ContainsKey("version"))
            loadedVersion = tag.Get<ushort>("version");

        switch (loadedVersion)
        {
            case < VERSION:
                Terramon.Instance.Logger.Debug("Upgrading PokemonData from version " + loadedVersion + " to " +
                                               VERSION);
                break;
            case > VERSION:
                Terramon.Instance.Logger.Warn("Unsupported PokemonData version " + loadedVersion +
                                              ". This may lead to undefined behaviour!");
                break;
        }

        var data = new PokemonData
        {
            Ball = tag.GetByte("ball"),
            ID = (ushort)tag.GetShort("id"),
            Level = tag.GetByte("lvl"),
            OT = tag.GetString("ot"),
            PersonalityValue = tag.Get<uint>("pv")
        };
        if (tag.TryGet<bool>("isShiny", out var isShiny))
            data.IsShiny = isShiny;
        if (tag.TryGet<string>("n", out var nickname))
            data.Nickname = nickname;
        if (tag.TryGet<string>("variant", out var variant))
            data.Variant = variant;
        return data;
    }
}