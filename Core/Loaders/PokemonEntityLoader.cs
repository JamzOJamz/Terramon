using Terramon.Content.NPCs.Pokemon;

namespace Terramon.Core.Loaders;

public class PokemonEntityLoader : ModSystem
{
    public static Dictionary<ushort, int> IDToNPCType { get; private set; }
    public static Dictionary<int, ushort> NPCTypeToID { get; private set; }

    public override void OnModLoad()
    {
        foreach (var (id, pokemon) in Terramon.DatabaseV2.Pokemon)
        {
            if (id > Terramon.MaxPokemonID) continue;
            var schemaPath = $"Content/Pokemon/{pokemon.Identifier}.hjson";
            if (!Mod.FileExists(schemaPath)) continue;
            var pokemonNpc = new PokemonNPC(id, pokemon);
            Mod.AddContent(pokemonNpc);
            IDToNPCType.Add(id, pokemonNpc.NPC.type);
        }

        NPCTypeToID = IDToNPCType.ToDictionary(x => x.Value, x => x.Key);
    }

    public override void Load()
    {
        IDToNPCType = new Dictionary<ushort, int>();
    }

    public override void Unload()
    {
        IDToNPCType = null;
        NPCTypeToID = null;
    }
}