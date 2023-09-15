using System.ComponentModel;
using Terraria.ModLoader.Config;

// ReSharper disable UnassignedField.Global

namespace Terramon.Content.Configs;

public class GameplayConfig : ModConfig
{
    [Header("Miscellaneous")] [DefaultValue(false)]
    public bool FastAnimations;

    [DefaultValue(4096)] [Range(1, int.MaxValue)]
    public int ShinySpawnRate;

    public override ConfigScope Mode => ConfigScope.ServerSide;
}