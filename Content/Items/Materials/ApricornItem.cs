using Terramon.Core.Loaders;
using Terraria.Localization;

namespace Terramon.Content.Items.Materials;

[LoadGroup("Apricorns")]
public abstract class ApricornItem : Material
{
    [field: CloneByReference]
    public override LocalizedText Tooltip { get; } = Language.GetText("Mods.Terramon.CommonTooltips.Apricorn");
    
    public override void SetStaticDefaults()
    {
        Item.ResearchUnlockCount = 20;
    }

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.width = 26;
        Item.height = 26;
        Item.value = 50;
    }
}