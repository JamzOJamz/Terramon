using System.ComponentModel;
using Terraria.ModLoader.Config;
// ReSharper disable UnassignedField.Global

namespace Terramon.Content.Configs;

public class GameplayConfig : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ServerSide;

    [Header("Miscellaneous")]
    [DefaultValue(4096)]
    [Range(1, int.MaxValue)]
    public int ShinySpawnRate;
}