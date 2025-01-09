using Terramon.Content.Configs;
using Terramon.Content.Items;
using Terramon.Content.Items.PokeBalls;
using Terramon.Content.Tiles.MusicBoxes;
using Terramon.ID;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.Personalities;
using Terraria.Localization;
using Terraria.Utilities;
using static Terraria.GameContent.Profiles;

namespace Terramon.Content.NPCs.Town;

[AutoloadHead]
public class PokemartClerk : ModNPC
{
    private static int _shimmerHeadIndex;
    private static StackedNPCProfile _npcProfile;

    private static readonly Condition TrainerSetCondition =
        new("ClerkTrainerSale", () => Condition.IsNpcShimmered.IsMet() || Main.halloween);

    public override string Texture => "Terramon/Assets/NPCs/" + GetType().Name;

    public override void Load()
    {
        // Adds our Shimmer Head to the NPCHeadLoader.
        _shimmerHeadIndex = Mod.AddNPCHeadTexture(Type, Texture + "_Shimmer_Head");
    }

    public override void SetStaticDefaults()
    {
        // DisplayName.SetDefault("Poke Mart Clerk");

        Main.npcFrameCount[Type] = 26; // The amount of frames the NPC has
        NPCID.Sets.ExtraFramesCount[Type] =
            9; // Generally for Town NPCs, but this is how the NPC does extra things such as sitting in a chair and talking to other NPCs.
        NPCID.Sets.AttackFrameCount[Type] = 4;
        NPCID.Sets.DangerDetectRange[Type] =
            700; // The amount of pixels away from the center of the npc that it tries to attack enemies.
        NPCID.Sets.AttackType[Type] = -1;
        NPCID.Sets.AttackTime[Type] =
            90; // The amount of time it takes for the NPC's attack animation to be over once it starts.
        NPCID.Sets.AttackAverageChance[Type] = 30;
        NPCID.Sets.HatOffsetY[Type] = 2; // For when a party is active, the party hat spawns at a Y offset.
        NPCID.Sets.ShimmerTownTransform[Type] = true;

        // Influences how the NPC looks in the Bestiary
        var drawModifiers = new NPCID.Sets.NPCBestiaryDrawModifiers
        {
            Velocity = 1f // Draws the NPC in the bestiary as if its walking +1 tiles in the x direction
        };

        NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, drawModifiers);

        NPC.Happiness
            .SetBiomeAffection<OceanBiome>(AffectionLevel.Like)
            .SetBiomeAffection<ForestBiome>(AffectionLevel.Like)
            .SetBiomeAffection<CorruptionBiome>(AffectionLevel.Dislike)
            .SetBiomeAffection<CrimsonBiome>(AffectionLevel.Dislike)
            .SetNPCAffection(NPCID.Mechanic, AffectionLevel.Like)
            .SetNPCAffection(NPCID.Merchant, AffectionLevel.Like)
            .SetNPCAffection(NPCID.Pirate, AffectionLevel.Dislike)
            .SetNPCAffection(NPCID.ArmsDealer, AffectionLevel.Hate);

        //breeder - bestiarygirl, nurse    stylist, partygirl

        // This creates a "profile" for our NPC, which allows for different textures during a party and/or while the NPC is shimmered.
        _npcProfile = new StackedNPCProfile(
            new DefaultNPCProfile(Texture, NPCHeadLoader.GetHeadSlot(HeadTexture)),
            new DefaultNPCProfile(Texture + "_Shimmer", _shimmerHeadIndex, Texture + "_Shimmer_Hatless")
        );
    }

    public override void SetDefaults()
    {
        NPC.CloneDefaults(NPCID.Merchant);
        NPC.townNPC = true; // Sets NPC to be a Town NPC
        NPC.friendly = true; // NPC Will not attack player
        NPC.width = 18;
        NPC.height = 40;
        NPC.aiStyle = 7;
        NPC.damage = 20;
        NPC.defense = 15;
        NPC.lifeMax = 250;
        NPC.HitSound = SoundID.NPCHit1;
        NPC.DeathSound = SoundID.NPCDeath1;
        NPC.knockBackResist = 0.5f;

        AnimationType = NPCID.Guide;
    }

    public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
    {
        // We can use AddRange instead of calling Add multiple times in order to add multiple items at once
        bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[]
        {
            // Sets the preferred biomes of this town NPC listed in the bestiary.
            // With Town NPCs, you usually set this to what biome it likes the most in regards to NPC happiness.
            BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Surface,

            // Sets your NPC's flavor text in the bestiary.
            new FlavorTextBestiaryInfoElement("Mods.Terramon.NPCs.PokemartClerk.BestiaryText")
        });
    }

    public override bool CanTownNPCSpawn(int numTownNPCs)
    {
        return true;
    }

    public override ITownNPCProfile TownNPCProfile()
    {
        return _npcProfile;
    }

    public override List<string> SetNPCNameList()
    {
        return
        [
            "Martin",
            "Tom",
            "Dave",
            "Morshu",
            "Terry",
            "Steven",
            "Xavier",
            "Asher",
            "Pikachu",
            "Lance"
        ];
    }

    public override string GetChat()
    {
        var player = TerramonPlayer.LocalPlayer;
        var activePokemonData = player.GetActivePokemon();

        //TODO: Re-enable dialogue CheckBack when he's given more sales
        var chat = new WeightedRandom<string>();
        /*chat.Add(Language.GetTextValue("Mods.Terramon.NPCs.PokemartClerk.Dialogue.Catchem"));
        chat.Add(Language.GetTextValue("Mods.Terramon.NPCs.PokemartClerk.Dialogue.Furret"));
        //chat.Add(Language.GetTextValue("Mods.Terramon.NPCs.PokemartClerk.Dialogue.CheckBack"));
        chat.Add(Language.GetTextValue("Mods.Terramon.NPCs.PokemartClerk.Dialogue.Biomes"));
        chat.Add(Language.GetTextValue("Mods.Terramon.NPCs.PokemartClerk.Dialogue.Regions"));
        chat.Add(Language.GetTextValue("Mods.Terramon.NPCs.PokemartClerk.Dialogue.EvolutionStones"));
        chat.Add(Language.GetTextValue("Mods.Terramon.NPCs.PokemartClerk.Dialogue.SickBurn"));
        chat.Add(Language.GetTextValue("Mods.Terramon.NPCs.PokemartClerk.Dialogue.Dedication", Main.worldName));
        chat.Add(Language.GetTextValue("Mods.Terramon.NPCs.PokemartClerk.Dialogue.NoBattleRip"));*/

        var itemName = WorldGen.SavedOreTiers.Iron == TileID.Lead ? "MapObject.Lead" : "MapObject.Iron";
        chat.Add(Language.GetTextValue("Mods.Terramon.NPCs.PokemartClerk.Dialogue.Crafting", Language.GetTextValue(itemName).ToLower()));

        if (NPC.GivenName == "Pikachu")
            chat.Add(Language.GetTextValue("Mods.Terramon.NPCs.PokemartClerk.Dialogue.BadName"));

        //only add chat about Pokemon if it exists
        if (activePokemonData != null)
        {
            chat.Add(Language.GetTextValue("Mods.Terramon.NPCs.PokemartClerk.Dialogue.PokemonHello",
                activePokemonData.LocalizedName,
                activePokemonData.DisplayName));

            /*if (pokemon.data.Nickname == null)
                chat.Add(Language.GetTextValue("Mods.Terramon.NPCs.PokemartClerk.Dialogue.PokemonNicknameHowto"));
            else
                chat.Add(Language.GetTextValue("Mods.Terramon.NPCs.PokemartClerk.Dialogue.PokemonNickname", pokemon.));*/

            var pokemonType = activePokemonData.Schema.Types[0];
            // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
            switch (pokemonType)
            {
                case PokemonType.Grass:
                    chat.Add(Language.GetTextValue("Mods.Terramon.NPCs.PokemartClerk.Dialogue.PokemonGrass"));
                    break;
                case PokemonType.Ice:
                    chat.Add(Language.GetTextValue("Mods.Terramon.NPCs.PokemartClerk.Dialogue.PokemonIce"));
                    break;
            }

            if (activePokemonData.IsShiny)
                chat.Add(Language.GetTextValue("Mods.Terramon.NPCs.PokemartClerk.Dialogue.PokemonShiny"));
            //else if (player.usePokeIsShimmer)
            //chat.Add(Language.GetTextValue("Mods.Terramon.NPCs.PokemartClerk.Dialogue.PokemonShimmer"));
        }

        chat.Add(NPC.IsShimmerVariant
            ? Language.GetTextValue("Mods.Terramon.NPCs.PokemartClerk.Dialogue.Shimmer")
            : Language.GetTextValue("Mods.Terramon.NPCs.PokemartClerk.Dialogue.ShimmerQuery"));

        var merchant = NPC.FindFirstNPC(NPCID.Merchant);
        if (merchant >= 0)
            chat.Add(Language.GetTextValue("Mods.Terramon.NPCs.PokemartClerk.Dialogue.MerchantComment",
                Main.npc[merchant].GivenName));

        var mechanic = NPC.FindFirstNPC(NPCID.Mechanic);
        if (mechanic >= 0)
            chat.Add(Language.GetTextValue("Mods.Terramon.NPCs.PokemartClerk.Dialogue.MechanicComment",
                Main.npc[mechanic].GivenName));

        var pirate = NPC.FindFirstNPC(NPCID.Pirate);
        if (pirate >= 0)
            chat.Add(Language.GetTextValue("Mods.Terramon.NPCs.PokemartClerk.Dialogue.PirateComment",
                Main.npc[pirate].GivenName));

        var armsDealer = NPC.FindFirstNPC(NPCID.ArmsDealer);
        if (armsDealer >= 0)
            chat.Add(Language.GetTextValue("Mods.Terramon.NPCs.PokemartClerk.Dialogue.ArmsDealerComment",
                Main.npc[armsDealer].GivenName));

        return chat; // chat is implicitly cast to a string.
    }

    public override void SetChatButtons(ref string button, ref string button2)
    {
        // What the chat buttons are when you open up the chat UI
        button = Language.GetTextValue("LegacyInterface.28");

        var player = Main.LocalPlayer.GetModPlayer<TerramonPlayer>();
        var activePokemonData = player.GetActivePokemon();
        if (activePokemonData != null && activePokemonData.GetQueuedEvolution(EvolutionTrigger.LevelUp) != 0)
            button2 = Language.GetTextValue("Mods.Terramon.NPCs.PokemartClerk.EvolveButton",
                activePokemonData.DisplayName);
    }

    public override void OnChatButtonClicked(bool firstButton, ref string shopName)
    {
        if (firstButton) shopName = "Shop";
        else
        {
            var player = Main.LocalPlayer.GetModPlayer<TerramonPlayer>();
            var activePokemonData = player.GetActivePokemon();
            if (activePokemonData == null) return;
            var queuedEvolution = activePokemonData.GetQueuedEvolution(EvolutionTrigger.LevelUp);
            if (queuedEvolution == 0) return;
            var queuedEvolutionName = Terramon.DatabaseV2.GetLocalizedPokemonNameDirect(queuedEvolution);
            TerramonWorld.PlaySoundOverBGM(new SoundStyle("Terramon/Sounds/pkball_catch_pla"));
            Main.npcChatText = Language.GetTextValue("Mods.Terramon.NPCs.PokemartClerk.Dialogue.EvolutionCongrats",
                activePokemonData.DisplayName, queuedEvolutionName);
            activePokemonData.EvolveInto(queuedEvolution);
            var justRegistered = player.UpdatePokedex(queuedEvolution, PokedexEntryStatus.Registered,
                shiny: activePokemonData.IsShiny);
            if (!justRegistered || !ModContent.GetInstance<ClientConfig>().ShowPokedexRegistrationMessages) return;
            Main.NewText(Language.GetTextValue("Mods.Terramon.Misc.PokedexRegistered", queuedEvolutionName),
                new Color(159, 162, 173));
        }
    }

    public override void AddShops()
    {
        var npcShop = new NPCShop(Type)
            .Add<PokeBallItem>()
            .Add<GreatBallItem>()
            .Add<UltraBallItem>()
            .Add<TrainerCap>(TrainerSetCondition)
            .Add<TrainerTorso>(TrainerSetCondition)
            .Add<TrainerLegs>(TrainerSetCondition)
            .Add<MusicItemWildBattle>();

        npcShop.Register(); // Name of this shop tab
    }

    // Make this Town NPC teleport to the King and/or Queen statue when triggered.
    public override bool CanGoToStatue(bool toKingStatue)
    {
        return true;
    }

    public override void TownNPCAttackProj(ref int projType, ref int attackDelay)
    {
        //projType = ModContent.ProjectileType<PokeBomb>();
        attackDelay = 1;
    }
}