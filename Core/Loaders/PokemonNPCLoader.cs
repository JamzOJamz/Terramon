using Terramon.Content.NPCs.Pokemon;

namespace Terramon.Core.Loaders;

public class PokemonNPCLoader : ModSystem
{
    public override void OnModLoad()
    {
        foreach (var kv in Terramon.Database.Pokemon)
        {
            var pokemon = kv.Value;
            if (pokemon.ID > Terramon.MaxPokemonID) continue;
            var npcSchema = pokemon.NPC;
            var aiInfo = npcSchema.AIInfo;
            var aiType = Mod.Code.GetType($"Terramon.Content.AI.{aiInfo.Type}AI");
            var aiParams = aiInfo.Parameters.ToArray();
            var npc = new PokemonNPC(pokemon.ID, pokemon.Name, npcSchema.Width, npcSchema.Height, aiType, aiParams,
                npcSchema.SpawnInfo?.Conditions, npcSchema.SpawnInfo?.Chance ?? 0f);
            Mod.AddContent(npc);
        }
    }
}