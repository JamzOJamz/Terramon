using System.ComponentModel;
using Terraria.ModLoader.Config;

// ReSharper disable UnassignedField.Global
// ReSharper disable UnusedMember.Global

namespace Terramon.Content.Configs;

public class GameplayConfig : ModConfig
{
    public static GameplayConfig Instance;

    [Header("Spawning")] [DefaultValue(1f)] [Range(0f, 1f)]
    public float PokemonSpawnRateMultiplier;
    
    [DefaultValue(1f)] [Range(0f, 1f)]
    public float NonPokemonSpawnRateMultiplier;
    
    [DefaultValue(SpawningStage.Stage1)]
    public SpawningStage SpawningStage;
    
    [DefaultValue(true)]
    public bool EncourageDespawning;
    
    [DefaultValue(4096)] [Range(1, int.MaxValue)]
    public int ShinySpawnRate;
    
    [Header("Visuals")] [DefaultValue(false)]
    public bool FastAnimations;
    
    [Header("Miscellaneous")] [DefaultValue(false)]
    public bool ShinyLockedStarters;
    
    [Header("Advanced")] [DefaultValue(false)]
    public bool DebugMode;

    public override ConfigScope Mode => ConfigScope.ServerSide;
}

public enum SpawningStage
{
    Basic = 1,
    Stage1,
    Stage2,
    Legendary
}