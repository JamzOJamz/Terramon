using Terramon.Content.Items.Evolutionary;
using Terramon.Core.Helpers;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.Localization;

namespace Terramon.Content.Items.Vitamins;

public class RareCandy : Vitamin
{
    protected override int UseRarity { get; } = ModContent.RarityType<RareCandyRarity>();

    public override void SetStaticDefaults()
    {
        base.SetStaticDefaults();
        CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 50;
    }

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.width = 28;
        Item.height = 28;
        Item.UseSound = SoundID.Item4;
    }

    protected override bool AffectedByPokemonDirectUse(PokemonData data)
    {
        return data.Level < 100;
    }

    protected override void PokemonDirectUse(Player player, PokemonData data)
    {
        data.LevelUp();
        Main.NewText(
            Language.GetTextValue("Mods.Terramon.Misc.RareCandyUse", data.DisplayName, data.Level),
            new Color(73, 158, 255));
        var queuedEvolution = data.GetQueuedEvolution(EvolutionTrigger.LevelUp);
        if (queuedEvolution == 0) return;
        Main.NewText(
            Language.GetTextValue("Mods.Terramon.Misc.PokemonEvolved", data.DisplayName,
                Terramon.DatabaseV2.GetLocalizedPokemonName(queuedEvolution)), new Color(50, 255, 130));
        data.EvolveInto(queuedEvolution);
        player.GetModPlayer<TerramonPlayer>().UpdatePokedex(queuedEvolution, PokedexEntryStatus.Registered);
    }
}

public class RareCandyRarity : ModRarity
{
    public override Color RarityColor { get; } = ColorUtils.FromHex(0x6299E5);
}