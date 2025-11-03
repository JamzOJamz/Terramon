using Terraria.Audio;
using Terraria.Localization;

namespace Terramon.Content.Items;

public abstract class BasePotionItem : RecoveryItem
{
    /// <summary>
    ///     The amount of HP restored by this potion.
    /// </summary>
    protected abstract ushort HealAmount { get; }
    
    public override string Texture => "Terramon/Assets/Items/Recovery/Potions/" + GetType().Name;

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.width = 24;
        Item.height = 32;
    }

    public override bool AffectedByPokemonDirectUse(PokemonData data)
    {
        return data.HP < data.MaxHP;
    }

    public override int PokemonDirectUse(Player player, PokemonData data, int amount = 1)
    {
        if (player.whoAmI != Main.myPlayer)
        {
            SoundEngine.PlaySound(SoundID.Item13, player.position);
            return 0;
        }
        
        // Store HP before healing
        var oldHP = data.HP;
        
        // Process the amount of potions used
        var potionsUsed = 0;
        for (var i = 0; i < amount; i++)
        {
            // Heal the Pokémon by the specified amount
            data.Heal(HealAmount);
            
            potionsUsed++;
            
            // If the Pokémon is fully healed, break out of the loop
            if (data.HP >= data.MaxHP)
                break;
        }
        
        // Find the difference in HP to determine actual amount restored
        var hpRestored = data.HP - oldHP;
        
        Main.NewText(
            Language.GetTextValue("Mods.Terramon.Misc.PotionUse", data.DisplayName, hpRestored));
        
        SoundEngine.PlaySound(SoundID.Item13);
        
        // Show healing text above the Pokémon pet (if it is active)
        var activePet = player.GetModPlayer<TerramonPlayer>().ActivePetProjectile;
        if (activePet != null && activePet.Data == data)
        {
            CombatText.NewText(activePet.Projectile.getRect(), CombatText.HealLife, hpRestored);
        }
        
        return potionsUsed;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        base.ModifyTooltips(tooltips);
        tooltips.Insert(tooltips.FindIndex(t => t.Name == "Tooltip0"),
            new TooltipLine(Mod, "Potion", Language.GetTextValue("Mods.Terramon.CommonTooltips.Potion")));
    }
}