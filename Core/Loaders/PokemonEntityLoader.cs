using Terramon.Content.NPCs.Pokemon;

namespace Terramon.Core.Loaders;

public class PokemonEntityLoader : ModSystem
{
    public override void OnModLoad()
    {
        foreach (var (id, pokemon) in Terramon.DatabaseV2.Pokemon)
        {
            if (id > Terramon.MaxPokemonID) continue;
            var schemaPath = $"Content/Pokemon/{pokemon.Identifier}.hjson";
            if (!Mod.FileExists(schemaPath)) continue;
            var pokemonNpc = new PokemonNPC(id, pokemon.Identifier);
            Mod.AddContent(pokemonNpc);
        }
    }
}