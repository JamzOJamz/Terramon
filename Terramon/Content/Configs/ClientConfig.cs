using System.ComponentModel;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.Config.UI;

// ReSharper disable UnassignedField.Global

namespace Terramon.Content.Configs;

public class ClientConfig : ModConfig
{
    public static ClientConfig Instance;
    // TODO: Add this back once alternate mod icons are redesigned to fit the new style
    //[Header("Graphics")] public ModIconType ModIconType;
    
    [Header("Miscellaneous")] [DefaultValue(true)]
    public bool FastEvolution;

    [DefaultValue(true)]
    public bool ShowPokedexRegistrationMessages;

    [Header("GUI")] [DefaultValue(false)]
    public bool ReducedAudio;

    [DefaultValue(false)] [ReloadRequired]
    public bool ReducedMotion;

    [DefaultValue(true)]
    public bool ShowPetNameOnHover;

    [Header("Accessibility")] [DefaultValue(true)]
    [ReloadRequired]
    public bool AnimatedModIcon;
    
    [DefaultValue(true)]
    public bool RainbowBuffText;

    [Header("Personalization")] [DefaultValue(true)]
    public bool ThickHighlights;

    [DefaultValue(typeof(Color), "252, 252, 84, 255")]
    public Color HighlightColor;
    public override ConfigScope Mode => ConfigScope.ClientSide;
}

public enum ModIconType
{
    // ReSharper disable once UnusedMember.Global
    Main,
    Alternate,
    Classic
}
