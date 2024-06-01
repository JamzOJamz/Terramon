using Terraria.Localization;

namespace Terramon.Content.Items.Materials;

public abstract class ApricornItem : Material
{
    public override ItemLoadPriority LoadPriority => ItemLoadPriority.Apricorns;
    protected override bool Obtainable => false;
    
    [field: CloneByReference]
    public override LocalizedText Tooltip { get; } = Language.GetText("Mods.Terramon.CommonTooltips.Apricorn");

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.width = 26;
        Item.height = 26;
    }
}