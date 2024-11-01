using System.Collections.Generic;
using Terramon.Content.Commands;
using Terramon.Helpers;
using Terraria.ID;
using Terraria.Localization;

namespace Terramon.Content.Items;

[Autoload(false)]
public abstract class TerramonItem : ModItem
{
    /// <summary>
    ///     The loading priority of this type. Lower values are loaded first.
    /// </summary>
    public virtual ItemLoadPriority LoadPriority => ItemLoadPriority.Unspecified;
    
    /// <summary>
    ///     Used for sorting items within the same load priority. Lower values are loaded first.
    /// </summary>
    public virtual int SubLoadPriority => 0;

    /// <summary>
    ///     Whether this item is legitimately obtainable in-game. If false, the item will have a tooltip indicating it is
    ///     unobtainable.
    /// </summary>
    public virtual bool Obtainable => true;

    /// <summary>
    ///     Whether this item can have an effect when used directly on a Pokémon.
    /// </summary>
    public virtual bool HasPokemonDirectUse => false;

    /// <summary>
    ///     The rarity of the item. Defaults to White.
    /// </summary>
    protected virtual int UseRarity => ItemRarityID.White;

    public override void SetDefaults()
    {
        Item.rare = UseRarity;
        Item.maxStack = Item.CommonMaxStack;
        if (!HasPokemonDirectUse) return;
        Item.useTime = 30;
        Item.useAnimation = 30;
        Item.useStyle = ItemUseStyleID.HoldUp;
        Item.consumable = true;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        if (Obtainable) return;
        tooltips.Add(new TooltipLine(Mod, "Unobtainable",
            $"[c/ADADC6:{Language.GetTextValue("Mods.Terramon.CommonTooltips.Unobtainable")}]"));
    }

    public override bool CanUseItem(Player player)
    {
        if (!HasPokemonDirectUse) return true;
        
        var activePokemonData = player.GetModPlayer<TerramonPlayer>().GetActivePokemon();
        if (activePokemonData == null)
        {
            player.NewText(Language.GetTextValue("Mods.Terramon.Misc.NoActivePokemon"), TerramonCommand.ChatColorYellow);
            return false;
        }

        if (AffectedByPokemonDirectUse(activePokemonData)) return true;
        player.NewText(Language.GetTextValue("Mods.Terramon.Misc.ItemNoEffect", activePokemonData.DisplayName),
            TerramonCommand.ChatColorYellow);
        return false;
    }

    public override bool? UseItem(Player player)
    {
        if (!HasPokemonDirectUse) return null;
        var modPlayer = player.GetModPlayer<TerramonPlayer>();
        PokemonDirectUse(player, modPlayer.GetActivePokemon());
        return true;
    }

    /// <summary>
    ///     Determines if the item will have an effect when used directly on a Pokémon and <see cref="HasPokemonDirectUse" />
    ///     is true.
    ///     If this method returns false, the item will not be usable on the Pokémon and
    ///     <see cref="PokemonDirectUse" /> will not be called. By default returns true.
    /// </summary>
    /// <param name="data">The Pokémon to check.</param>
    public virtual bool AffectedByPokemonDirectUse(PokemonData data)
    {
        return true;
    }

    /// <summary>
    ///     Called when the item is used directly on a compatible Pokémon. Any data changes should be done here.
    ///     This method is only called if <see cref="HasPokemonDirectUse" /> is true.
    /// </summary>
    /// <param name="player">The player using the item.</param>
    /// <param name="data">The Pokémon the item is being used on.</param>
    /// <param name="amount">The amount of the item being used.</param>
    /// <returns>The amount of the item actually used.</returns>
    public virtual int PokemonDirectUse(Player player, PokemonData data, int amount = 1)
    {
        return amount;
    }
}

public enum ItemLoadPriority
{
    PokeBalls,
    Apricorns,
    EvolutionaryItems,
    Vitamins,
    KeyItems,
    PokeBallMinis,
    Unspecified
}