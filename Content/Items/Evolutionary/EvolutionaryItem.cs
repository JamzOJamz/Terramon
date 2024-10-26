using System.Collections.Generic;
using Terramon.Content.Configs;
using Terraria.Audio;
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

    public override bool HasPokemonDirectUse => Trigger == EvolutionTrigger.DirectUse;

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

    public override bool AffectedByPokemonDirectUse(PokemonData data)
    {
        return GetEvolvedSpecies(data) != 0;
    }

    public override int PokemonDirectUse(Player player, PokemonData data, int amount = 1)
    {
        if (player.whoAmI != Main.myPlayer) return 0;
        var evolvedSpecies = GetEvolvedSpecies(data);
        var evolvedSpeciesName = Terramon.DatabaseV2.GetLocalizedPokemonNameDirect(evolvedSpecies);
        Main.NewText(
            Language.GetTextValue("Mods.Terramon.Misc.PokemonEvolved", data.DisplayName,
                evolvedSpeciesName), new Color(50, 255, 130));
        data.EvolveInto(evolvedSpecies);
        TerramonWorld.PlaySoundOverBGM(new SoundStyle("Terramon/Sounds/pkball_catch_pla"));
        var justRegistered = player.GetModPlayer<TerramonPlayer>()
            .UpdatePokedex(evolvedSpecies, PokedexEntryStatus.Registered, shiny: data.IsShiny);
        if (!justRegistered || !ModContent.GetInstance<ClientConfig>().ShowPokedexRegistrationMessages) return 1;
        Main.NewText(Language.GetTextValue("Mods.Terramon.Misc.PokedexRegistered", evolvedSpeciesName),
            new Color(159, 162, 173));
        return 1;
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