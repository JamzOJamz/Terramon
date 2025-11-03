using System.Collections;
using System.Text;
using Hjson;
using Newtonsoft.Json.Linq;
using ReLogic.Content;
using Terramon.Content.NPCs;
using Terramon.Content.Projectiles;
using Terramon.Content.Tiles.Banners;
using Terramon.Core.Abstractions;
using Terraria.Graphics.Shaders;

namespace Terramon.Core.Loaders;

/// <summary>
///     A system that loads handles the manual loading of Pokémon NPCs and pet projectiles.
/// </summary>
[Autoload(false)]
public class PokemonEntityLoader : ModSystem
{
    public static Dictionary<ushort, Asset<Texture2D>> GlowTextureCache { get; private set; }
    public static Dictionary<ushort, Asset<Texture2D>> ShinyGlowTextureCache { get; private set; }
    public static Dictionary<ushort, int> IDToNPCType { get; private set; }
    public static Dictionary<ushort, int> IDToPetType { get; private set; }
    public static Dictionary<ushort, int> IDToBannerType { get; private set; }
    public static Dictionary<ushort, JToken> NPCSchemaCache { get; private set; }
    public static Dictionary<ushort, JToken> PetSchemaCache { get; private set; }
    private static BitArray HasGenderDifference { get; set; }
    private static BitArray HasPetExclusiveTexture { get; set; }
    private static List<PokeBannerItem> ShinyBanners { get; set; }

    public override void OnModLoad()
    {
        // The initialization of these arrays is done here rather than in Load to avoid a null ref exception reading HighestPokemonID
        var highestPokemonID = Terramon.HighestPokemonID;
        HasGenderDifference = new BitArray(highestPokemonID);
        HasPetExclusiveTexture = new BitArray(highestPokemonID);

        // Load the fade shader for Pokémon
        if (!Main.dedServ)
            GameShaders.Misc[$"{nameof(Terramon)}FadeToColor"] =
                new MiscShaderData(Mod.Assets.Request<Effect>("Assets/Effects/FadeToColor"), "FadePass");

        // Start a stopwatch to measure the time taken to load Pokémon entities
        //var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        foreach (var (id, pokemon) in Terramon.DatabaseV2.Pokemon)
        {
            if (id > Terramon.MaxPokemonIDToLoad) continue;
            if (!HjsonSchemaExists(pokemon.Identifier)) continue;
            LoadEntities(id, pokemon);
        }
        
        // Done in a second pass to add them all after the standard banners are loaded
        LoadShinyBanners();

        /*stopwatch.Stop();
        Mod.Logger.Info($"Loaded Pokémon entities in {stopwatch.ElapsedMilliseconds}ms");*/
    }

    private bool HjsonSchemaExists(string identifier)
    {
        return Mod.FileExists($"Content/Pokemon/{identifier}.hjson");
    }

    /// <summary>
    ///     Creates an NPC and pet projectile for the given Pokémon and loads them as mod content.
    /// </summary>
    private void LoadEntities(ushort id, DatabaseV2.PokemonSchema schema)
    {
        // Load corresponding schema from HJSON file
        var hjsonStream = Mod.GetFileStream($"Content/Pokemon/{schema.Identifier}.hjson");
        using var hjsonReader = new StreamReader(hjsonStream);
        var jsonText = HjsonValue.Load(hjsonReader).ToString();
        hjsonReader.Close();
        var hjsonSchema = JObject.Parse(jsonText);

        // Load glowmask textures if they exist
        if (ModContent.RequestIfExists<Texture2D>($"Terramon/Assets/Pokemon/{schema.Identifier}_Glow", out var glowTex))
            GlowTextureCache[id] = glowTex;
        if (ModContent.RequestIfExists<Texture2D>($"Terramon/Assets/Pokemon/{schema.Identifier}_S_Glow",
                out var shinyGlowTex))
            ShinyGlowTextureCache[id] = shinyGlowTex;

        // Check if this Pokémon has a gender difference (alternate texture)
        HasGenderDifference[id - 1] = ModContent.HasAsset($"Terramon/Assets/Pokemon/{schema.Identifier}F");

        // Check if this Pokémon has a pet-exclusive texture
        HasPetExclusiveTexture[id - 1] = ModContent.HasAsset($"Terramon/Assets/Pokemon/{schema.Identifier}_Pet");

        // Get common components
        var commonSchema = hjsonSchema.GetValue("Common");

        // Load Pokémon NPC
        if (hjsonSchema.TryGetValue("NPC", out var npcSchema))
        {
            // Add common components to NPC schema
            if (commonSchema != null)
                foreach (var kvp in commonSchema.Children<JProperty>())
                    npcSchema[kvp.Name] ??= kvp.Value;

            NPCSchemaCache.Add(id, npcSchema);
            var npc = new PokemonNPC(id, schema);
            Mod.AddContent(npc);
            IDToNPCType.Add(id, npc.NPC.type);
        }

        // Load Pokémon pet projectile
        if (hjsonSchema.TryGetValue("Projectile", out var petSchema))
        {
            // Add common components to pet schema
            if (commonSchema != null)
                foreach (var kvp in commonSchema.Children<JProperty>())
                    petSchema[kvp.Name] ??= kvp.Value;

            PetSchemaCache.Add(id, petSchema);
            var pet = new PokemonPet(id, schema);
            Mod.AddContent(pet);
            IDToPetType.Add(id, pet.Projectile.type);
        }
        
        // Load Pokémon banner
        if (ModContent.HasAsset($"Terramon/Assets/Tiles/Banners/{schema.Identifier}Banner")) LoadBanner(id, schema);
    }

    private void LoadBanner(ushort id, DatabaseV2.PokemonSchema schema)
    {
        // Load banner item
        var banner = new PokeBannerItem(id, schema);
        Mod.AddContent(banner);
        IDToBannerType.Add(id, banner.Type);
        
        ShinyBanners.Add(new PokeBannerItem(id, schema, banner.Type));
    }

    private void LoadShinyBanners()
    {
        // Add shiny banners to mod content
        foreach (var banner in ShinyBanners) Mod.AddContent(banner);

        ShinyBanners = null;
    }

    public static Asset<Texture2D> RequestTexture(IPokemonEntity entity)
    {
        var pathBuilder = new StringBuilder(entity.Texture);
        var data = entity.Data;
        var i = entity.ID - 1;
        if (HasGenderDifference[i])
            if ((data != null ? data.Gender == Gender.Female ? 1 : 0 : 0) != 0)
                pathBuilder.Append('F');
        if (HasPetExclusiveTexture[i] && entity.GetType() == typeof(PokemonPet))
            pathBuilder.Append("_Pet");
        var str = data?.Variant;
        if (!string.IsNullOrEmpty(str))
            pathBuilder.Append('_').Append(data.Variant);
        if (data is { IsShiny: true })
            pathBuilder.Append("_S");
        return ModContent.Request<Texture2D>(pathBuilder.ToString());
    }


    public override void Load()
    {
        IDToNPCType = new Dictionary<ushort, int>();
        IDToPetType = new Dictionary<ushort, int>();
        IDToBannerType = new Dictionary<ushort, int>();
        NPCSchemaCache = new Dictionary<ushort, JToken>();
        PetSchemaCache = new Dictionary<ushort, JToken>();
        GlowTextureCache = new Dictionary<ushort, Asset<Texture2D>>();
        ShinyGlowTextureCache = new Dictionary<ushort, Asset<Texture2D>>();
        ShinyBanners = [];
    }

    public override void Unload()
    {
        IDToNPCType = null;
        IDToPetType = null;
        IDToBannerType = null;
        NPCSchemaCache = null;
        PetSchemaCache = null;
        HasGenderDifference = null;
        HasPetExclusiveTexture = null;
        GlowTextureCache = null;
        ShinyGlowTextureCache = null;
        ShinyBanners = null;
    }
}