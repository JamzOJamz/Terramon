using System;
using Terraria.ModLoader.IO;

namespace Terramon.Core;

public class PokemonData : TagSerializable
{
    // ReSharper disable once UnusedMember.Global
    public static readonly Func<TagCompound, PokemonData> DESERIALIZER = Load;

    public ushort ID;
    private bool IsShiny;

    private PokemonData()
    {
    }

    public PokemonData(ushort id, bool shinyLocked = false)
    {
        ID = id;
        IsShiny = !shinyLocked && Terramon.RollShiny();
    }

    public TagCompound SerializeData()
    {
        return new TagCompound
        {
            ["id"] = ID,
            ["isShiny"] = IsShiny
        };
    }

    private static PokemonData Load(TagCompound tag)
    {
        return new PokemonData
        {
            ID = (ushort)tag.GetShort("id"),
            IsShiny = tag.GetBool("isShiny")
        };
    }
}