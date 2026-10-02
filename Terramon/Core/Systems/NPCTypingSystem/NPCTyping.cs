using CsvHelper;
using System.Globalization;
using Terramon.ID;

namespace Terramon.Core.Systems;

/// <summary>
///     Gives Pokémon typings to vanilla Terraria NPCs so they can interact with the Terramon combat system.
///     This helps support type effectiveness and adds more depth and strategy when fighting against regular enemies.
/// </summary>
public sealed class NPCTyping : ModSystem
{
    // ReSharper disable once CollectionNeverQueried.Local
    private static readonly (PokemonType, PokemonType)[] NetIDTypings = new (PokemonType, PokemonType)[-NPCID.NegativeIDCount - 1];

    public override void SetStaticDefaults()
    {
        using var reader = new StreamReader(Mod.GetFileStream("Assets/Data/TerraTypings.csv"));
        using var csv = new CsvReader(reader);

        csv.ReadHeader();
        while (csv.Read())
        {
            var npcId = csv.GetField<int>(0);
            var primaryType = csv.GetField<PokemonType>(1);
            csv.TryGetField(2, out PokemonType secondaryType);

            if (npcId < 0)
            {
                // This is a negative NPC ID representing a variant, and needs to be handled differently
                NetIDTypings[-npcId - 1] = (primaryType, secondaryType);
                continue;
            }

            // Register the primary and secondary typings for the NPC
            Sets.PrimaryTyping[npcId] = primaryType;
            Sets.SecondaryTyping[npcId] = secondaryType;
        }
    }

    public static (PokemonType, PokemonType) Get(NPC npc)
    {
        var t = npc.netID;
        if (t < 0)
            return NetIDTypings[-t - 1];
        return (Sets.PrimaryTyping[t], Sets.SecondaryTyping[t]);
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
