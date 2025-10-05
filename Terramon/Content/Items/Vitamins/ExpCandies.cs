using Terramon.Content.Configs;
using Terramon.Core.Systems.PokemonDirectUseSystem;
using Terramon.Helpers;
using Terraria.Audio;
using Terraria.Localization;

namespace Terramon.Content.Items;

public abstract class ExpCandy : Vitamin, IPokemonDirectUse
{
    protected override int UseRarity { get; } = ModContent.RarityType<ExpCandyRarity>();

    /// <summary>
    ///     The amount of experience points granted by this Exp. Candy.
    /// </summary>
    protected abstract int Points { get; }

    public bool AffectedByPokemonDirectUse(PokemonData data)
    {
        return data.Level < Terramon.MaxPokemonLevel;
    }

    public int PokemonDirectUse(Player player, PokemonData data, int amount = 1)
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
        var clientConfig = ClientConfig.Instance;
        var fastEvolution = clientConfig.FastEvolution;

        // Gain the experience points
        var oldLevel = data.Level;
        var totalExpToGain = Points * amount;
        data.GainExperience(totalExpToGain, out var levelsGained, out var overflow);
        Main.NewText(Language.GetTextValue("Mods.Terramon.Misc.ExpCandyUse", totalExpToGain - overflow));
        while (overflow > 0) // Use overflow to calculate actual amount of candies used
        {
            amount--;
            overflow -= Points;
        }

        // Visual feedback effects
        SoundEngine.PlaySound(SoundID.Item4);
        for (var j = 0; j < 40; j++)
        {
            var speed = Main.rand.NextVector2CircularEdge(1f, 1f);
            var d = Dust.NewDustPerfect(player.Center + speed * 26, DustID.FrostHydra);
            d.noGravity = true;
        }

        if (levelsGained > 0)
        {
            Main.NewText(
                Language.GetTextValue("Mods.Terramon.Misc.RareCandyUse", data.DisplayName, data.Level));
            CombatText.NewText(player.getRect(), Color.White, $"Lv. {oldLevel} > {data.Level}");
            SoundEngine.PlaySound(SoundID.Item20);
            var queuedEvolution = data.GetQueuedEvolution(EvolutionTrigger.LevelUp);
            if (queuedEvolution != 0)
            {
                if (!fastEvolution)
                {
                    Main.NewText(
                        Language.GetTextValue("Mods.Terramon.Misc.PokemonEvolutionReady", data.DisplayName),
                        new Color(50, 255, 130));
                    return amount;
                }
                TerramonWorld.PlaySoundOverBGM(new SoundStyle("Terramon/Sounds/pkball_catch_pla"));
                var modPlayer = player.GetModPlayer<TerramonPlayer>();
                var showPokedexRegistrationMessages = clientConfig.ShowPokedexRegistrationMessages;
                while (queuedEvolution != 0)
                {
                    var evolvedSpeciesName = Terramon.DatabaseV2.GetLocalizedPokemonNameDirect(queuedEvolution);
                    Main.NewText(
                        Language.GetTextValue("Mods.Terramon.Misc.PokemonEvolved", data.DisplayName,
                            evolvedSpeciesName), new Color(50, 255, 130));
                    data.EvolveInto(queuedEvolution);
                    var justRegistered =
                        modPlayer.UpdatePokedex(queuedEvolution, PokedexEntryStatus.Registered, shiny: data.IsShiny);
                    if (justRegistered && showPokedexRegistrationMessages)
                    {
                        Main.NewText(Language.GetTextValue("Mods.Terramon.Misc.PokedexRegistered", evolvedSpeciesName),
                            new Color(159, 162, 173));
                    }
                    queuedEvolution = data.GetQueuedEvolution(EvolutionTrigger.LevelUp);
                }
            }
        }

        return amount;
    }

    public override void SetStaticDefaults()
    {
        base.SetStaticDefaults();
        Item.ResearchUnlockCount = 50;
        TerramonItemAPI.Sets.Unobtainable.Add(Type); // To be made obtainable in a future update post-0.1 beta
    }

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.UseSound = SoundID.Item4;
    }
}

public class ExpCandyXS : ExpCandy
{
    protected override int Points => 100;

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.width = 22;
        Item.height = 18;
    }
}

public class ExpCandyS : ExpCandy
{
    protected override int Points => 800;

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.width = 26;
        Item.height = 22;
    }
}

public class ExpCandyM : ExpCandy
{
    protected override int Points => 3000;

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.width = 24;
        Item.height = 22;
    }
}

public class ExpCandyL : ExpCandy
{
    protected override int Points => 10000;

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.width = 22;
        Item.height = 28;
    }
}

public class ExpCandyXL : ExpCandy
{
    protected override int Points => 30000;

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.width = 26;
        Item.height = 34;
    }
}

public class ExpCandyRarity : ModRarity
{
    public override Color RarityColor { get; } = ColorUtils.FromHexRGB(0x76C2F2);
}