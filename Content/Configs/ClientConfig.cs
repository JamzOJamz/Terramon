using System.ComponentModel;
using Terramon.Core.Battling.TurnBased;
using Terraria.ModLoader.Config;

// ReSharper disable UnassignedField.Global

namespace Terramon.Content.Configs;

public class ClientConfig : ModConfig
{
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

    [Header("Accessibility")] [DefaultValue(true)]
    [ReloadRequired]
    public bool AnimatedModIcon;
    
    [DefaultValue(true)]
    public bool RainbowBuffText;
    
    [Header("Advanced")]
    public TurnEngineExecutionModel TurnEngineExecutionModel;

    public override ConfigScope Mode => ConfigScope.ClientSide;

    public override void OnChanged()
    {
        if (Terramon.Instance.File.IsOpen && TurnEngineExecutionModel == TurnEngineExecutionModel.ExternalProcess)
            NodeShowdownService.SetupEnvironment();
    }
}

/// <summary>
/// Defines the execution model for the turn-based battle engine.
/// </summary>
public enum TurnEngineExecutionModel
{
    /// <summary>
    /// Executes the battle engine in a separate process. This can reduce memory usage
    /// compared to running it in-process, and is available only on Windows.
    /// </summary>
    ExternalProcess,

    /// <summary>
    /// Executes the battle engine within the main game process using an internal interpreter.
    /// This model is more memory-intensive but may be preferred on platforms where spawning
    /// external processes is restricted or unsupported.
    /// </summary>
    EmbeddedInterpreter,
}

public enum ModIconType
{
    // ReSharper disable once UnusedMember.Global
    Main,
    Alternate,
    Classic
}