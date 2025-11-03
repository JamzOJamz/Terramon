using Terraria.Audio;
using Terraria.Localization;

namespace Terramon.Content.Items;

public abstract class BaseReviveItem : RecoveryItem
{
    /// <summary>
    ///     The percentage of HP restored by this revive item as a float between 0 and 1.
    /// </summary>
    protected abstract float RestorationPercentage { get; }

    public override string Texture => "Terramon/Assets/Items/Recovery/Revives/" + GetType().Name;

    public override bool AffectedByPokemonDirectUse(PokemonData data)
    {
        return data.HP == 0;
    }

    public override int PokemonDirectUse(Player player, PokemonData data, int amount = 1)
    {
        if (player.whoAmI != Main.myPlayer)
        {
            SoundEngine.PlaySound(SoundID.Item29, player.position);
            return 0;
        }

        // Restore the Pokémon's HP from a fainted state based on the defined percentage
        data.HP = (ushort)Math.Ceiling(data.MaxHP * RestorationPercentage);

        Main.NewText(
            Language.GetTextValue(
                RestorationPercentage == 1f ? "Mods.Terramon.Misc.MaxReviveUse" : "Mods.Terramon.Misc.ReviveUse",
                data.DisplayName));

        SoundEngine.PlaySound(SoundID.Item29);

        return 1;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        base.ModifyTooltips(tooltips);
        tooltips.Insert(tooltips.FindIndex(t => t.Name == "Tooltip0"),
            new TooltipLine(Mod, "Revive", Language.GetTextValue("Mods.Terramon.CommonTooltips.Revive")));
    }
}