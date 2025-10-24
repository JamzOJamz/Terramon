using System.ComponentModel;
using System.Text.Json.Serialization;
using Terraria.ModLoader.Config;

// ReSharper disable UnassignedField.Global

namespace Terramon.Content.Configs;

public class ClientConfig : ModConfig
{
#pragma warning disable CA2211
    public static ClientConfig Instance;
#pragma warning restore CA2211
    
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

    [DefaultValue(false)]
    public bool ThickHighlights;

    [JsonIgnore]
    public static readonly Color DefaultHighlightColor = new(252, 252, 84, 255);

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
