using System;
using System.Collections.Generic;
using Terraria.ModLoader.IO;

namespace Terramon.Core;

/// <summary>
///     Service class for managing Pokémon storage in PC boxes.
/// </summary>
public class PCService
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="PCService" /> class, and creates 8 empty PC boxes.
    /// </summary>
    public PCService()
    {
        for (var i = 0; i < DefaultBoxes; i++) Boxes.Add(new PCBox());
    }

    #region Pokémon Storage System

    /// <summary>
    ///     Default number of PC boxes.
    /// </summary>
    private const byte DefaultBoxes = 8;

    /// <summary>
    ///     The amount of boxes to add when expanding the PC's storage capacity.
    /// </summary>
    private const byte ExpansionBoxes = 8;

    /// <summary>
    ///     Maximum number of PC boxes that can be unlocked through expansion.
    /// </summary>
    private const byte MaxBoxes = 32;

    /// <summary>
    ///     List of PC boxes for storing Pokémon.
    /// </summary>
    public readonly List<PCBox> Boxes = new();

    /// <summary>
    ///     Stores a Pokémon in the first available slot in the PC.
    ///     Returns the box where the Pokémon was stored, or null if the PC is full.
    /// </summary>
    public PCBox StorePokemon(PokemonData data)
    {
        var slot = FindEmptySpace();
        if (slot == -1)
            return null;
        Boxes[slot / PCBox.Capacity][slot % PCBox.Capacity] = data;
        CheckBoxExpansion();
        return Boxes[slot / PCBox.Capacity];
    }

    /// <summary>
    ///     Checks if the PC's total storage capacity should be increased.
    ///     If every box contains at least one Pokémon, and there are less than 32 total boxes, 8 more boxes are added.
    /// </summary>
    private void CheckBoxExpansion()
    {
        if (Boxes.Count >= MaxBoxes) return;
        foreach (var box in Boxes)
        {
            var hasPokemon = false;
            for (var i = 0; i < PCBox.Capacity; i++)
            {
                if (box[i] == null) continue;
                hasPokemon = true;
                break;
            }

            if (!hasPokemon) return;
        }

        for (var i = 0; i < ExpansionBoxes; i++) Boxes.Add(new PCBox());
    }

    /// <summary>
    ///     Finds the first empty space in the PC.
    /// </summary>
    private int FindEmptySpace()
    {
        for (var i = 0; i < Boxes.Count; i++)
        for (var j = 0; j < PCBox.Capacity; j++)
            if (Boxes[i][j] == null)
                return i * PCBox.Capacity + j;

        return -1;
    }

    #endregion
}

/// <summary>
///     Class representing an individual PC box capable of storing Pokémon.
/// </summary>
public class PCBox : TagSerializable
{
    // ReSharper disable once UnusedMember.Global
    public const byte Capacity = 30;
    public static readonly Func<TagCompound, PCBox> DESERIALIZER = Load;
    private readonly PokemonData[] Slots = new PokemonData[30];
    public string GivenName;

    public PokemonData this[int slot]
    {
        get => Slots[slot];
        set => Slots[slot] = value;
    }

    public TagCompound SerializeData()
    {
        var tag = new TagCompound();
        if (!string.IsNullOrEmpty(GivenName))
            tag["name"] = GivenName;
        for (var i = 0; i < Slots.Length; i++)
            if (Slots[i] != null)
                tag[$"s{i}"] = Slots[i].SerializeData();
        return tag;
    }

    private static PCBox Load(TagCompound tag)
    {
        var box = new PCBox();
        if (tag.ContainsKey("name"))
            box.GivenName = tag.GetString("name");
        for (var i = 0; i < box.Slots.Length; i++)
        {
            var tagName = $"s{i}";
            if (tag.ContainsKey(tagName))
                box.Slots[i] = tag.Get<PokemonData>(tagName);
        }

        return box;
    }
}