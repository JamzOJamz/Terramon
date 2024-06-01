using System.Collections.Generic;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.Localization;

namespace Terramon.Content.Items.Evolutionary;

public abstract class EvolutionaryItem : TerramonItem
{
    public override ItemLoadPriority LoadPriority => ItemLoadPriority.EvolutionaryItems;

    public override string Texture => "Terramon/Assets/Items/Evolutionary/" + GetType().Name;

    /// <summary>
    ///     The trigger method that causes the evolution. Defaults to <see cref="EvolutionTrigger.DirectUse" />.
    /// </summary>
    public virtual EvolutionTrigger Trigger => EvolutionTrigger.DirectUse;

    protected override bool HasPokemonDirectUse => Trigger == EvolutionTrigger.DirectUse;

    public override void SetStaticDefaults()
    {
        CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 3;
    }

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.maxStack = 1;
        Item.value = 50000;
        if (!HasPokemonDirectUse) return;
        Item.UseSound = SoundID.Item28;
    }

    /// <summary>
    ///     Determines the species to which a given Pokémon evolves with this item.
    /// </summary>
    /// <param name="data">The data of the Pokémon trying to be evolved.</param>
    /// <returns>
    ///     The ID of the evolved Pokémon. If no evolution is possible, return 0.
    /// </returns>
    public virtual ushort GetEvolvedSpecies(PokemonData data)
    {
        return 0;
    }

    protected override bool AffectedByPokemonDirectUse(PokemonData data)
    {
        return GetEvolvedSpecies(data) != 0;
    }

    protected override void PokemonDirectUse(Player player, PokemonData data)
    {
        if (player.whoAmI != Main.myPlayer) return;
        var evolvedSpecies = GetEvolvedSpecies(data);
        Main.NewText(
            Language.GetTextValue("Mods.Terramon.Misc.PokemonEvolved", data.DisplayName,
                Terramon.DatabaseV2.GetLocalizedPokemonName(evolvedSpecies)), new Color(50, 255, 130));
        data.EvolveInto(evolvedSpecies);
        player.GetModPlayer<TerramonPlayer>().UpdatePokedex(evolvedSpecies, PokedexEntryStatus.Registered);
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        base.ModifyTooltips(tooltips);
        tooltips.Insert(tooltips.FindIndex(t => t.Name == "Tooltip0"),
            new TooltipLine(Mod, "EvolutionaryItem",
                Language.GetTextValue("Mods.Terramon.CommonTooltips.EvolutionaryItem")));
    }
}

public enum EvolutionTrigger
{
    /// <summary>
    ///     The item is used directly on the Pokémon.
    /// </summary>
    DirectUse,

    /// <summary>
    ///     The Pokémon levels up while holding the item.
    /// </summary>
    LevelUp,

    /// <summary>
    ///     The Pokémon is traded while holding the item.
    /// </summary>
    Trade
}