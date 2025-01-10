using System.Collections;
using Hjson;
using Newtonsoft.Json.Linq;
using Terramon.Content.NPCs;
using Terramon.Content.Projectiles;

namespace Terramon.Core.Loaders;

/// <summary>
///     A system that loads handles the manual loading of Pokémon NPCs and pet projectiles.
/// </summary>
public class PokemonEntityLoader : ModSystem
{
    public static Dictionary<ushort, int> IDToNPCType { get; private set; }
    public static Dictionary<ushort, int> IDToPetType { get; private set; }
    public static Dictionary<ushort, JToken> NPCSchemaCache { get; private set; }
    public static Dictionary<ushort, JToken> PetSchemaCache { get; private set; }
    public static BitArray HasGenderDifference { get; private set; }

    public override void OnModLoad()
    {
        // The initialization of this array is done here rather than in Load to avoid a null ref exception reading LoadedPokemonCount
        HasGenderDifference = new BitArray(Terramon.LoadedPokemonCount);
        
        foreach (var (id, pokemon) in Terramon.DatabaseV2.Pokemon)
        {
            if (id > Terramon.MaxPokemonID) continue;
            if (!HjsonSchemaExists(pokemon.Identifier)) continue;
            LoadEntities(id, pokemon);
        }
    }

    private bool HjsonSchemaExists(string identifier)
    {
        return Mod.FileExists($"Content/Pokemon/{identifier}.hjson");
    }

    /// <summary>
    ///     Creates an NPC and pet projectile for the given Pokémon and loads them as mod content.
    /// </summary>
    private void LoadEntities(ushort id, DatabaseV2.PokemonSchema schema)
    {
        // Load corresponding schema from HJSON file
        var hjsonStream = Mod.GetFileStream($"Content/Pokemon/{schema.Identifier}.hjson");
        using var hjsonReader = new StreamReader(hjsonStream);
        var jsonText = HjsonValue.Load(hjsonReader).ToString();
        hjsonReader.Close();
        var hjsonSchema = JObject.Parse(jsonText);
        
        // Check if this Pokémon has a gender difference (alternate texture)
        HasGenderDifference[id - 1] = ModContent.HasAsset($"Terramon/Assets/Pokemon/{schema.Identifier}F");

        // Load Pokémon NPC
        if (hjsonSchema.TryGetValue("NPC", out var npcSchema))
        {
            NPCSchemaCache.Add(id, npcSchema);
            var npc = new PokemonNPC(id, schema);
            Mod.AddContent(npc);
            IDToNPCType.Add(id, npc.NPC.type);
        }

        // Load Pokémon pet projectile
        if (!hjsonSchema.TryGetValue("Projectile", out var petSchema)) return;
        PetSchemaCache.Add(id, petSchema);
        var pet = new PokemonPet(id, schema);
        Mod.AddContent(pet);
        IDToPetType.Add(id, pet.Projectile.type);
    }

    public override void Load()
    {
        IDToNPCType = new Dictionary<ushort, int>();
        IDToPetType = new Dictionary<ushort, int>();
        NPCSchemaCache = new Dictionary<ushort, JToken>();
        PetSchemaCache = new Dictionary<ushort, JToken>();
    }

    public override void Unload()
    {
        IDToNPCType = null;
        IDToPetType = null;
        NPCSchemaCache = null;
        PetSchemaCache = null;
        HasGenderDifference = null;
    }
}