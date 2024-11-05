/*using System.Collections.Generic;

namespace Terramon.Core.PokeBalls;

public static class PokeBallLoader
{
    private static readonly IDictionary<int, ModPokeBall> pokeBalls = new Dictionary<int, ModPokeBall>();

    private static int PokeBallCount { get; set; }
    
    internal static void RegisterPokeBall(ModPokeBall modPokeBall)
    {
        var id = ++PokeBallCount;

        modPokeBall.Type = id;

        pokeBalls[id] = modPokeBall;
    }
}*/