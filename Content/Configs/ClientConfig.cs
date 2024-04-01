using System.ComponentModel;
using Terraria.ModLoader.Config;

// ReSharper disable UnassignedField.Global

namespace Terramon.Content.Configs;

public class ClientConfig : ModConfig
{
    // TODO: Add this back once alternate mod icons are redesigned to fit the new style
    //[Header("Graphics")] public ModIconType ModIconType;

    [Header("Miscellaneous")] [DefaultValue(false)]
    public bool ReducedAudio;

    public override ConfigScope Mode => ConfigScope.ClientSide;
}

public enum ModIconType
{
    // ReSharper disable once UnusedMember.Global
    Main,
    Alternate,
    Classic
}