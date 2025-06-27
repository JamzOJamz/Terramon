using Microsoft.VisualBasic.FileIO;
using Terramon.ID;

namespace Terramon.Core.Systems;

/// <summary>
///     Gives Pokémon typings to vanilla Terraria NPCs so they can interact with the Terramon combat system.
///     This helps support type effectiveness and adds more depth and strategy when fighting against regular enemies.
/// </summary>
public sealed class TerraTypingDX : ModSystem
{
    // ReSharper disable once CollectionNeverQueried.Local
    private static readonly Dictionary<int, (PokemonType, PokemonType)> NetIDTypings = new();

    public override void PostSetupContent()
    {
        using var typingsFile = Mod.GetFileStream("Assets/Data/TerraTypings.csv");
        var typingsData = ParseCsv(typingsFile);

        foreach (var row in typingsData)
        {
            Mod.Logger.Debug($"Processing row: {string.Join(", ", row)}");

            if (row.Length < 3) continue;

            if (!int.TryParse(row[0], out var npcId) || !Enum.TryParse(row[1], out PokemonType primaryType))
                continue;

            var secondaryType = Enum.TryParse(row[2], out PokemonType parsedSecondaryType)
                ? parsedSecondaryType
                : PokemonType.None;

            if (npcId < 0)
            {
                // This is a negative NPC ID representing a variant, and needs to be handled differently
                NetIDTypings[npcId] = (primaryType, secondaryType);

                continue;
            }

            // Register the primary and secondary typings for the NPC
            Sets.PrimaryTyping[npcId] = primaryType;
            Sets.SecondaryTyping[npcId] = secondaryType;
        }
    }

    private static List<string[]> ParseCsv(Stream csvStream, bool ignoreHeader = true)
    {
        var result = new List<string[]>();

        using var reader = new StreamReader(csvStream);
        using var parser = new TextFieldParser(reader);
        parser.TextFieldType = FieldType.Delimited;
        parser.SetDelimiters(",");

        if (ignoreHeader && !parser.EndOfData)
            parser.ReadLine(); // Skip the header row

        while (!parser.EndOfData)
        {
            var fields = parser.ReadFields();
            result.Add(fields);
        }

        return result;
    }

    /// <remarks>
    ///     TODO: Add descriptions to help other modders understand how to use these sets.
    /// </remarks>
    [ReinitializeDuringResizeArrays]
    // ReSharper disable once MemberCanBePrivate.Global
    public static class Sets
    {
        public static readonly PokemonType[] PrimaryTyping = NPCID.Sets.Factory.CreateNamedSet("PrimaryTyping")
            .RegisterCustomSet(PokemonType.None);

        public static readonly PokemonType[] SecondaryTyping = NPCID.Sets.Factory.CreateNamedSet("SecondaryTyping")
            .RegisterCustomSet(PokemonType.None);
    }
}