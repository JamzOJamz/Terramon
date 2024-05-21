using System.Collections.Generic;
using Terramon.Content.GUI;
using Terramon.Core.Loaders.UILoading;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.Localization;

namespace Terramon.Content.Items.Evolutionary;

public abstract class EvolutionaryItem : TerramonItem
{
    public override ItemLoadPriority LoadPriority => ItemLoadPriority.EvolutionaryItems;

    public override string Texture => "Terramon/Assets/Items/Evolutionary/" + GetType().Name;

    public override void SetStaticDefaults()
    {
        CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 3;
    }

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.maxStack = 1;
        Item.value = 50000;
        Item.useTime = 30;
        Item.useAnimation = 30;
        Item.useStyle = ItemUseStyleID.HoldUp;
        Item.UseSound = SoundID.Item28;
        Item.consumable = true;
    }

    public override bool CanUseItem(Player player)
    {
        var activePokemonData = player.GetModPlayer<TerramonPlayer>().GetActivePokemon();
        if (activePokemonData == null)
        {
            Main.NewText(Language.GetTextValue("Mods.Terramon.Misc.NoActivePokemon"), new Color(255, 240, 20));
            return false;
        }

        var evolvedSpecies = GetEvolvedSpecies(activePokemonData, EvolutionTrigger.DirectUse);
        if (evolvedSpecies != 0) return true;
        Main.NewText(Language.GetTextValue("Mods.Terramon.Misc.ItemNoEffect", activePokemonData.DisplayName),
            new Color(255, 240, 20));
        return false;
    }

    public override bool? UseItem(Player player)
    {
        var modPlayer = player.GetModPlayer<TerramonPlayer>();
        var activePokemonData = modPlayer.GetActivePokemon();
        if (activePokemonData == null) return null;
        var evolvedSpecies = GetEvolvedSpecies(activePokemonData, EvolutionTrigger.DirectUse);
        if (evolvedSpecies == 0) return null;
        
        Main.NewText(
            Language.GetTextValue("Mods.Terramon.Misc.PokemonEvolved", activePokemonData.DisplayName,
                Terramon.DatabaseV2.GetLocalizedPokemonName(evolvedSpecies)), new Color(50, 255, 130));
        activePokemonData.EvolveInto(evolvedSpecies);
        UILoader.GetUIState<PartyDisplay>().RecalculateSlot(modPlayer.ActiveSlot);
        modPlayer.UpdatePokedex(evolvedSpecies, PokedexEntryStatus.Registered);
        return true;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        base.ModifyTooltips(tooltips);
        tooltips.Insert(tooltips.FindIndex(t => t.Name == "Tooltip0"),
            new TooltipLine(Mod, "EvolutionaryItem",
                Language.GetTextValue("Mods.Terramon.CommonTooltips.EvolutionaryItem")));
    }

    /// <summary>
    ///     Determines the species to which a given Pokémon evolves with this item.
    /// </summary>
    /// <param name="data">The data of the Pokémon trying to be evolved.</param>
    /// <param name="trigger">The trigger that prompted the evolution.</param>
    /// <returns>
    ///     The ID of the evolved Pokémon. If no evolution is possible, return 0.
    /// </returns>
    public virtual ushort GetEvolvedSpecies(PokemonData data, EvolutionTrigger trigger)
    {
        return 0;
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