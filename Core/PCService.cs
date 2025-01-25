using Terramon.Content.GUI;
using Terraria.ModLoader.IO;

namespace Terramon.Core;

/// <summary>
///     Service class for managing Pokémon storage in PC boxes.
/// </summary>
public class PCService
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="PCService" /> class.
    /// </summary>
    public PCService()
    {
        for (var i = 0; i < DefaultBoxes; i++) Boxes.Add(new PCBox { Service = this });
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
    public readonly List<PCBox> Boxes = [];

    /// <summary>
    ///     Stores a Pokémon in the first available slot in the PC.
    ///     Returns the box where the Pokémon was stored, or null if the PC is full.
    /// </summary>
    public PCBox StorePokemon(PokemonData data)
    {
        var slot = FindEmptySpace();
        if (slot == -1)
            return null;
        var boxIndex = slot / PCBox.Capacity;
        var boxSlotIndex = slot % PCBox.Capacity;
        var box = Boxes[boxIndex];
        box[boxSlotIndex] = data;
        if (PCInterface.DisplayedBoxIndex == boxIndex)
            PCInterface.PopulateCustomSlots(box);
        return box;
    }

    /// <summary>
    ///     Checks if the PC's total storage capacity should be increased.
    ///     If every box contains at least one Pokémon, more boxes are added until the maximum is reached.
    /// </summary>
    public void CheckBoxExpansion()
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

        for (var i = 0; i < ExpansionBoxes; i++) Boxes.Add(new PCBox { Service = this });
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
public class PCBox
{
    public const byte Capacity = 30;

    private readonly PokemonData[] _slots = new PokemonData[30];

    /// <summary>
    ///     The display name of the box.
    /// </summary>
    public string GivenName;

    /// <summary>
    ///     The <see cref="PCService" /> that manages this box.
    /// </summary>
    public PCService Service;

    public PokemonData this[int slot]
    {
        get => _slots[slot];
        set
        {
            _slots[slot] = value;
            Service.CheckBoxExpansion();
        }
    }

    public TagCompound SerializeData()
    {
        var tag = new TagCompound();
        if (!string.IsNullOrEmpty(GivenName))
            tag["name"] = GivenName;
        for (var i = 0; i < _slots.Length; i++)
            if (_slots[i] != null)
                tag[$"s{i}"] = _slots[i].SerializeData();
        return tag;
    }

    public static PCBox Load(TagCompound tag)
    {
        var box = new PCBox();
        if (tag.ContainsKey("name"))
            box.GivenName = tag.GetString("name");
        for (var i = 0; i < box._slots.Length; i++)
        {
            var tagName = $"s{i}";
            if (tag.ContainsKey(tagName))
                box._slots[i] = PokemonData.Load(tag.Get<TagCompound>(tagName));
        }

        return box;
    }
}