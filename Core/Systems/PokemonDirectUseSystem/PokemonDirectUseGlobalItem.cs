using Terramon.Content.Commands;
using Terraria.Localization;

namespace Terramon.Core.Systems.PokemonDirectUseSystem;

public class PokemonDirectUseGlobalItem : GlobalItem
{
    public override bool AppliesToEntity(Item entity, bool lateInstantiation)
    {
        return entity.ModItem is IPokemonDirectUse;
    }

    public override void SetDefaults(Item item)
    {
        item.useTime = 30;
        item.useAnimation = 30;
        item.useStyle = ItemUseStyleID.HoldUp;
        item.consumable = true;
    }

    public override bool CanUseItem(Item item, Player player)
    {
        var activePokemonData = player.GetModPlayer<TerramonPlayer>().GetActivePokemon();
        if (activePokemonData == null)
        {
            player.NewText(Language.GetTextValue("Mods.Terramon.Misc.NoActivePokemon"),
                TerramonCommand.ChatColorYellow);
            return false;
        }

        var directUseItem = (IPokemonDirectUse)item.ModItem;
        if (directUseItem.AffectedByPokemonDirectUse(activePokemonData)) return true;
        player.NewText(Language.GetTextValue("Mods.Terramon.Misc.ItemNoEffect", activePokemonData.DisplayName),
            TerramonCommand.ChatColorYellow);
        return false;
    }

    public override bool? UseItem(Item item, Player player)
    {
        var modPlayer = player.GetModPlayer<TerramonPlayer>();
        var directUseItem = (IPokemonDirectUse)item.ModItem;
        directUseItem.PokemonDirectUse(player, modPlayer.GetActivePokemon());
        return true;
    }
}