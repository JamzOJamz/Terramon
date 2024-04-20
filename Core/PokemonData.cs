using System;
using Terramon.ID;
using Terraria.ModLoader.IO;

namespace Terramon.Core;

public class PokemonData : TagSerializable
{
    private const ushort VERSION = 0;

    // ReSharper disable once UnusedMember.Global
    public static readonly Func<TagCompound, PokemonData> DESERIALIZER = Load;
    public byte Ball = BallID.PokeBall;
    public Gender Gender = Gender.Unspecified;
    public ushort ID;
    public bool IsShiny;
    public byte Level = 1;
    private string OT;
    private uint PersonalityValue = (uint)Main.rand.Next(int.MinValue, int.MaxValue);
    public string Variant;

    private PokemonData()
    {
    }

    public PokemonData(ushort id, byte level = 1, bool shinyLocked = false)
    {
        ID = id;
        Level = level;
        OT = Main.LocalPlayer.name;
        Gender = Terramon.DetermineGender(id, PersonalityValue);
        IsShiny = !shinyLocked && Terramon.RollShiny(Main.LocalPlayer);
    }

    public TagCompound SerializeData()
    {
        var tag = new TagCompound
        {
            ["pv"] = PersonalityValue,
            ["ball"] = Ball,
            ["id"] = ID,
            ["isShiny"] = IsShiny,
            ["lvl"] = Level,
            ["ot"] = OT,
            ["version"] = VERSION
        };
        if (Variant != null)
            tag["variant"] = Variant;
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
            ID = (ushort)tag.GetShort("id"),
            IsShiny = tag.GetBool("isShiny"),
            Level = tag.GetByte("lvl"),
            OT = tag.GetString("ot")
        };
        if (tag.TryGet<byte>("ball", out var ball))
            data.Ball = ball;
        if (tag.TryGet<string>("variant", out var variant))
            data.Variant = variant;
        if (tag.TryGet<uint>("pv", out var pv))
            data.PersonalityValue = pv;
        data.Gender = Terramon.DetermineGender(data.ID, data.PersonalityValue);
        return data;
    }
}