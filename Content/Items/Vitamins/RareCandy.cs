using System.Collections.Generic;
using Terramon.Content.Configs;
using Terramon.Content.Items.Evolutionary;
using Terramon.Helpers;
using Terraria.Audio;
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
    }

    public override bool AffectedByPokemonDirectUse(PokemonData data)
    {
        return data.Level < Terramon.MaxPokemonLevel;
    }

    public override int PokemonDirectUse(Player player, PokemonData data, int amount = 1)
    {
        if (player.whoAmI != Main.myPlayer)
        {
            for (var j = 0; j < 40; j++)
            {
                var speed = Main.rand.NextVector2CircularEdge(1f, 1f);
                var d = Dust.NewDustPerfect(player.Center + speed * 26, DustID.FrostHydra);
                d.noGravity = true;
            }

            SoundEngine.PlaySound(SoundID.Item4, player.position);
            return 0;
        }

        // Get whether fast evolution is enabled
        var fastEvolution = ModContent.GetInstance<ClientConfig>().FastEvolution;

        // Level up as many times as possible given the amount used
        var oldLevel = data.Level;
        var origSpecies = data.ID;
        ushort queuedEvolution = 0;
        var evolutions = new List<ushort>();
        for (var i = 0; i < amount; i++)
        {
            if (!data.LevelUp()) break;
            // Check if the Pokémon is ready to evolve after leveling up
            queuedEvolution = data.GetQueuedEvolution(EvolutionTrigger.LevelUp);
            if (!fastEvolution || queuedEvolution == 0 || evolutions.Contains(queuedEvolution)) continue;
            evolutions.Add(queuedEvolution);
            data.EvolveInto(queuedEvolution);
            queuedEvolution = 0;
        }

        data.ID = origSpecies; // Reset the ID to the original species (hacky)

        Main.NewText(
            Language.GetTextValue("Mods.Terramon.Misc.RareCandyUse", data.DisplayName, data.Level),
            Color.White);

        // Test effect
        CombatText.NewText(player.getRect(), Color.White, $"Lv. {oldLevel} > {data.Level}");
        SoundEngine.PlaySound(SoundID.Item20);
        for (var j = 0; j < 40; j++)
        {
            var speed = Main.rand.NextVector2CircularEdge(1f, 1f);
            var d = Dust.NewDustPerfect(player.Center + speed * 26, DustID.FrostHydra);
            d.noGravity = true;
        }

        SoundEngine.PlaySound(SoundID.Item4, player.position);

        if (evolutions.Count > 0) // Check if the Pokémon evolved
        {
            TerramonWorld.PlaySoundOverBGM(new SoundStyle("Terramon/Sounds/pkball_catch_pla"));
            // Iterate through all evolutions
            foreach (var evolution in evolutions)
            {
                var evolvedSpeciesName = Terramon.DatabaseV2.GetLocalizedPokemonNameDirect(evolution);
                Main.NewText(
                    Language.GetTextValue("Mods.Terramon.Misc.PokemonEvolved", data.DisplayName,
                        evolvedSpeciesName), new Color(50, 255, 130));
                data.EvolveInto(evolution);
                var justRegistered = player.GetModPlayer<TerramonPlayer>()
                    .UpdatePokedex(evolution, PokedexEntryStatus.Registered, shiny: data.IsShiny);
                if (!justRegistered || !ModContent.GetInstance<ClientConfig>().ShowPokedexRegistrationMessages)
                    continue;
                Main.NewText(Language.GetTextValue("Mods.Terramon.Misc.PokedexRegistered", evolvedSpeciesName),
                    new Color(159, 162, 173));
            }
        }
        else if (queuedEvolution != 0) // Check if the Pokémon is ready to evolve
        {
            Main.NewText(
                Language.GetTextValue("Mods.Terramon.Misc.PokemonEvolutionReady", data.DisplayName),
                new Color(50, 255, 130));
        }

        return data.Level - oldLevel;
    }
}

public class RareCandyRarity : ModRarity
{
    public override Color RarityColor { get; } = ColorUtils.FromHex(0x6299E5);
}