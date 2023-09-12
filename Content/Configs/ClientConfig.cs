using System.ComponentModel;
using Terraria.ModLoader.Config;

// ReSharper disable UnassignedField.Global

namespace Terramon.Content.Configs;

public class ClientConfig : ModConfig
{
    [Header("Miscellaneous")]

    [DefaultValue(false)]
    public bool ReducedAudio;

    public override ConfigScope Mode => ConfigScope.ClientSide;
}