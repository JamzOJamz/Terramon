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

    public override void PokemonDirectUse(Player player, PokemonData data)
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
            return;
        }

        data.LevelUp();
        Main.NewText(
            Language.GetTextValue("Mods.Terramon.Misc.RareCandyUse", data.DisplayName, data.Level),
            Color.White);
        
        // Test effect
        CombatText.NewText(player.getRect(), Color.White, $"Lv. {data.Level - 1} > {data.Level}");
        SoundEngine.PlaySound(SoundID.Item20);
        for (var j = 0; j < 40; j++)
        {
            var speed = Main.rand.NextVector2CircularEdge(1f, 1f);
            var d = Dust.NewDustPerfect(player.Center + speed * 26, DustID.FrostHydra);
            d.noGravity = true;
        }
        
        SoundEngine.PlaySound(SoundID.Item4, player.position);
        
        var queuedEvolution = data.GetQueuedEvolution(EvolutionTrigger.LevelUp);
        if (queuedEvolution == 0) return;
        
        if (ModContent.GetInstance<ClientConfig>().FastEvolution)
        {
            TerramonWorld.PlaySoundOverBGM(new SoundStyle("Terramon/Sounds/pkball_catch_pla"));
            var queuedEvolutionName = Terramon.DatabaseV2.GetLocalizedPokemonNameDirect(queuedEvolution);
            Main.NewText(
                Language.GetTextValue("Mods.Terramon.Misc.PokemonEvolved", data.DisplayName,
                    queuedEvolutionName), new Color(50, 255, 130));
            data.EvolveInto(queuedEvolution);
            var justRegistered = player.GetModPlayer<TerramonPlayer>()
                .UpdatePokedex(queuedEvolution, PokedexEntryStatus.Registered, shiny: data.IsShiny);
            if (!justRegistered || !ModContent.GetInstance<ClientConfig>().ShowPokedexRegistrationMessages) return;
            Main.NewText(Language.GetTextValue("Mods.Terramon.Misc.PokedexRegistered", queuedEvolutionName),
                new Color(159, 162, 173));
        }
        else
        {
            Main.NewText(
                Language.GetTextValue("Mods.Terramon.Misc.PokemonEvolutionReady", data.DisplayName),
                new Color(50, 255, 130));
        }
    }
}

public class RareCandyRarity : ModRarity
{
    public override Color RarityColor { get; } = ColorUtils.FromHex(0x6299E5);
}