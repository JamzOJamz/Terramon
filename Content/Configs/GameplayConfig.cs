using System.ComponentModel;
using Terraria.ModLoader.Config;

// ReSharper disable UnassignedField.Global

namespace Terramon.Content.Configs;

public class GameplayConfig : ModConfig
{
    [DefaultValue(false)] public bool FastAnimations;

    [Header("Miscellaneous")] [DefaultValue(4096)] [Range(1, int.MaxValue)]
    public int ShinySpawnRate;

    public override ConfigScope Mode => ConfigScope.ServerSide;
}