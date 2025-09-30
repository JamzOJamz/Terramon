/*using Terraria.Localization;

namespace Terramon.Core.PokeBalls;

public abstract class ModPokeBall : ModType, ILocalizedModType
{
    public int Type { get; internal set; }
    
    public virtual string LocalizationCategory => "PokeBalls";
    
    public virtual LocalizedText DisplayName => this.GetLocalization(nameof(DisplayName), PrettyPrintName);
    
    protected override void Register()
    {
        ModTypeLookup<ModPokeBall>.Register(this);
        PokeBallLoader.RegisterPokeBall(this);
    }
}*/