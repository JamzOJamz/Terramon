using System;
using Terramon.ID;
using Terraria.ModLoader.IO;

namespace Terramon.Core;

public class PokemonData : TagSerializable
{
    // ReSharper disable once UnusedMember.Global
    public static readonly Func<TagCompound, PokemonData> DESERIALIZER = Load;
    public byte Gender = GenderID.Unknown;
    public ushort ID;
    public bool IsShiny;
    public byte Level = 1;
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
        return new TagCompound
        {
            ["id"] = ID,
            ["isShiny"] = IsShiny,
            ["gender"] = Gender,
            ["lvl"] = Level,
            ["ot"] = OT
        };
    }

    private static PokemonData Load(TagCompound tag)
    {
        return new PokemonData
        {
            ID = (ushort)tag.GetShort("id"),
            IsShiny = tag.GetBool("isShiny"),
            Gender = tag.GetByte("gender"),
            Level = tag.GetByte("lvl"),
            OT = tag.GetString("ot")
        };
    }
}