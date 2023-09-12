global using Terraria.ModLoader;
using Terramon.Content.Configs;
using Terramon.Content.Databases;
using Terramon.ID;
using Terraria;

namespace Terramon;

public class Terramon : Mod
{
    /*public static Terramon Instance { get; private set; }

    public Terramon()
    {
        Instance = this;
    }*/

    public static PokemonDB Database { get; private set; }

    public static bool RollShiny()
    {
        return Main.rand.NextBool(ModContent.GetInstance<GameplayConfig>().ShinySpawnRate);
    }

    public static byte RollGender(ushort id)
    {
        var genderRate = Database.GetPokemon(id).GenderRate;
        return genderRate >= 0
            ? Main.rand.NextBool(genderRate, 8) ? GenderID.Female : GenderID.Male
            : GenderID.Unknown;
    }

    public override void Load()
    {
        Database = LoadPokemonDatabase();
    }

    private PokemonDB LoadPokemonDatabase()
    {
        var stream = GetFileStream("Content/Databases/pokemon-db.tmon");
        return PokemonDB.Deserialize(stream);
    }

    public override void Unload()
    {
        Database = null;
        //Instance = null;
    }
}