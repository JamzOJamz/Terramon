using System;
using Terraria.ModLoader.IO;

namespace Terramon.Core;

public class PokemonData : TagSerializable
{
    // ReSharper disable once UnusedMember.Global
    public static readonly Func<TagCompound, PokemonData> DESERIALIZER = Load;
    public Gender Gender = Gender.Unspecified;
    public ushort ID;
    public bool IsShiny;
    public byte Level = 1;
    public string Variant;
    private string OT;

    private PokemonData()
    {
    }

    public PokemonData(ushort id, byte level = 1, bool shinyLocked = false)
    {
        ID = id;
        Level = level;
        OT = Main.LocalPlayer.name;
        Gender = Terramon.RollGender(id);
        IsShiny = !shinyLocked && Terramon.RollShiny(Main.LocalPlayer);
    }

    public TagCompound SerializeData()
    {
        var tag = new TagCompound
        {
            ["id"] = ID,
            ["isShiny"] = IsShiny,
            ["gender"] = (byte)Gender,
            ["lvl"] = Level,
            ["ot"] = OT
        };
        if (Variant != null)
            tag["variant"] = Variant;
        return tag;
    }

    private static PokemonData Load(TagCompound tag)
    {
        var data = new PokemonData
        {
            ID = (ushort)tag.GetShort("id"),
            IsShiny = tag.GetBool("isShiny"),
            Gender = (Gender)tag.GetByte("gender"),
            Level = tag.GetByte("lvl"),
            OT = tag.GetString("ot")
        };
        if (tag.TryGet<string>("variant", out var variant))
            data.Variant = variant;
        return data;
    }
}