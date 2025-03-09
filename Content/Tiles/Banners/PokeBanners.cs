using Terramon.Content.Items;
using Terraria.Localization;

namespace Terramon.Content.Tiles.Banners;

public class PokeBannerItem(ushort id, DatabaseV2.PokemonSchema schema) : TerramonItem
{
    private readonly LocalizedText _pokeName = Terramon.DatabaseV2.GetLocalizedPokemonName(id);
    protected override bool CloneNewInstances => true;

    public override LocalizedText DisplayName =>
        Language.GetText("Mods.Terramon.Items.PokeBannerItem.DisplayName").WithFormatArgs(_pokeName);

    public override LocalizedText Tooltip => Language.GetText("Mods.Terramon.Items.PokeBannerItem.Tooltip").WithFormatArgs(_pokeName);

    public override string Name { get; } = $"{schema.Identifier}Banner";

    protected override int UseRarity => ItemRarityID.Blue;

    public override void SetDefaults()
    {
        Item.DefaultToPlaceableTile(ModContent.TileType<PokeBannerTile>());
        base.SetDefaults();
    }
}

public class PokeBannerTile : ModTile
{
    public override string Texture => "Terraria/Images/NPC_0";
}