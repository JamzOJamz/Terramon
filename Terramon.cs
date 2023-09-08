global using Terraria.ModLoader;
using Terramon.Content.Databases;
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
        return Main.rand.NextBool(1, 4096);
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