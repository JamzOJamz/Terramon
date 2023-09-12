using System.ComponentModel;
using Terraria.ModLoader.Config;

// ReSharper disable UnassignedField.Global

namespace Terramon.Content.Configs;

public class GameplayConfig : ModConfig
{
    [Header("Miscellaneous")]

    [DefaultValue(4096)] [Range(1, int.MaxValue)]
    public int ShinySpawnRate;

    [DefaultValue(false)]
    public bool FastAnimations;

    public override ConfigScope Mode => ConfigScope.ServerSide;
}