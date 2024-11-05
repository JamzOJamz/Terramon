using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Hjson;
using Newtonsoft.Json.Linq;
using ReLogic.Content;
using Terramon.Content.Configs;
using Terramon.Content.Dusts;
using Terramon.Content.Items.PokeBalls;
using Terramon.Core.NPCComponents;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.Localization;

namespace Terramon.Content.NPCs.Pokemon;

[Autoload(false)]
public class PokemonNPC(ushort id, string identifier) : ModNPC
{
    private static Dictionary<ushort, JToken> _schemaCache;
    private static Dictionary<ushort, Asset<Texture2D>> _glowTextureCache;
    private static BitArray _hasGenderDifference;
    private static MethodInfo _enableComponentMethod;

    private int _cryTimer;
    private Asset<Texture2D> _mainTexture;
    private int _plasmaStateTime;
    private Vector2 _plasmaStateVelocity;
    private int _shinySparkleTimer;
    public ushort ID { get; } = id;
    public PokemonData Data { get; set; }

    protected override bool CloneNewInstances => true;

    public override string Name { get; } = identifier + "NPC";

    public override LocalizedText DisplayName => Terramon.DatabaseV2.GetLocalizedPokemonName(ID);

    public override string Texture { get; } = "Terramon/Assets/Pokemon/" + identifier;

    public bool PlasmaState { get; private set; }

    public override void SetDefaults()
    {
        NPC.lifeMax = 100;
        NPC.HitSound = SoundID.NPCHit1;
        NPC.value = 0f;
        NPC.knockBackResist = 0.75f;
        NPC.despawnEncouraged = ModContent.GetInstance<GameplayConfig>().EncourageDespawning;
        NPC.friendly = true;

        // Start a stopwatch to measure the time it takes to apply all components.
        // var stopwatch = Stopwatch.StartNew();

        foreach (var component in _schemaCache[ID].Children<JProperty>())
        {
            var componentType = Mod.Code.GetType($"Terramon.Content.NPCs.NPC{component.Name}");
            if (componentType == null)
                // Remove the component from the schema if it doesn't exist.
                // _schemaCache[ID].First(x => x.Path == component.Path).Remove();
                continue;
            var enableComponentRef = _enableComponentMethod.MakeGenericMethod(componentType);
            var instancedComponent = enableComponentRef.Invoke(null, [NPC, null]);
            foreach (var prop in component.Value.Children<JProperty>())
            {
                var fieldInfo = componentType.GetRuntimeField(prop.Name);
                if (fieldInfo == null) continue;
                fieldInfo.SetValue(instancedComponent, prop.Value.ToObject(fieldInfo.FieldType));
            }
        }

        // Stop the stopwatch and log the time taken to apply all components.
        // stopwatch.Stop();
        // Mod.Logger.Debug("Time taken to apply components: " + stopwatch.Elapsed + "ms");
    }

    public override void SetStaticDefaults()
    {
        // Load gender-specific texture if it exists.
        _hasGenderDifference[ID - 1] = ModContent.HasAsset(Texture + "F");

        // Load glowmask texture if it exists.
        if (ModContent.RequestIfExists<Texture2D>(Texture + "_Glow", out var glowTex))
            _glowTextureCache[ID] = glowTex;

        // Hide from the bestiary (they will appear in the Pokédex instead)
        var drawModifier = new NPCID.Sets.NPCBestiaryDrawModifiers
        {
            Hide = true
        };
        NPCID.Sets.NPCBestiaryDrawOffset.Add(NPC.type, drawModifier);
    }

    public override void OnSpawn(IEntitySource source)
    {
        if (source is { Context: "PokemonRelease" })
        {
            var dust = ModContent.DustType<SummonCloud>();
            for (var i = 0; i < 4; i++)
            {
                var angle = MathHelper.PiOver2 * i;
                var x = (float)Math.Cos(angle);
                var y = (float)Math.Sin(angle);
                Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y), NPC.width, NPC.height, dust, x / 2, y / 2);
                Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y), NPC.width, NPC.height, dust, x, y);
            }

            _cryTimer = 10;
        }

        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        var spawningPlayer = Player.FindClosest(NPC.Center, NPC.width, NPC.height);
        Data = PokemonData.Create(Main.player[spawningPlayer], ID, 5);
        NPC.netUpdate = true;
    }

    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        if (_mainTexture == null)
        {
            var pathBuilder = new StringBuilder(Texture);

            if (_hasGenderDifference[ID - 1] && Data?.Gender == Gender.Female)
                pathBuilder.Append('F');
            if (!string.IsNullOrEmpty(Data?.Variant))
                pathBuilder.Append('_').Append(Data.Variant);
            if (Data is { IsShiny: true })
                pathBuilder.Append("_S");

            var path = pathBuilder.ToString();
            _mainTexture = ModContent.Request<Texture2D>(path);
        }

        var frameSize = NPC.frame.Size();
        var effects = NPC.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

        if (_plasmaStateTime <= 20)
        {
            spriteBatch.Draw(_mainTexture.Value,
                NPC.Center - screenPos +
                new Vector2(0f, NPC.gfxOffY + DrawOffsetY + (int)Math.Ceiling(NPC.height / 2f) + 4),
                NPC.frame, drawColor, NPC.rotation,
                frameSize / new Vector2(2, 1), NPC.scale, effects, 0f);

            if (_glowTextureCache.TryGetValue(ID, out var glowTexture) && !PlasmaState)
                spriteBatch.Draw(glowTexture.Value,
                    NPC.Center - screenPos +
                    new Vector2(0f, NPC.gfxOffY + DrawOffsetY + (int)Math.Ceiling(NPC.height / 2f) + 4),
                    NPC.frame, Color.White, NPC.rotation,
                    frameSize / new Vector2(2, 1), NPC.scale, effects, 0f);
        }

        if (!PlasmaState) return false;

        // Draw the Pokemon with the fade shader
        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Immediate, null, Main.DefaultSamplerState, DepthStencilState.None,
            Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

        // Apply fade shader
        GameShaders.Misc[$"{nameof(Terramon)}FadeToColor"].UseColor(drawColor);
        GameShaders.Misc[$"{nameof(Terramon)}FadeToColor"]
            .UseOpacity(_plasmaStateTime <= 20 ? _plasmaStateTime / 7.5f : NPC.Opacity);
        GameShaders.Misc[$"{nameof(Terramon)}FadeToColor"].Apply();

        spriteBatch.Draw(_mainTexture.Value,
            NPC.Center - screenPos +
            new Vector2(0f, NPC.gfxOffY + DrawOffsetY - (frameSize.Y - NPC.height) / 2f + 4),
            NPC.frame, drawColor, NPC.rotation,
            frameSize / new Vector2(2, 2), NPC.scale, effects, 0f);

        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
            DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

        return false;
    }

    public override void SendExtraAI(BinaryWriter writer)
    {
        Data.NetWrite(writer, PokemonData.BitIsShiny | PokemonData.BitPersonalityValue | PokemonData.BitVariant);
        writer.Write(PlasmaState);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        Data ??= new PokemonData
        {
            ID = ID,
            Level = 5
        };
        Data.NetRead(reader);
        PlasmaState = reader.ReadBoolean();
    }

    public override void AI()
    {
        if (_cryTimer > 0)
        {
            _cryTimer--;
            if (_cryTimer == 0 && Data != null && Main.netMode != NetmodeID.Server)
            {
                var cry = new SoundStyle("Terramon/Sounds/Cries/" + Data.InternalName)
                    { Volume = 0.21f };
                SoundEngine.PlaySound(cry, NPC.position);
            }
        }

        if (NPC.life < NPC.lifeMax) NPC.life = NPC.lifeMax;
        if (PlasmaState)
        {
            var lightIntensity = 0.57f - _plasmaStateTime / 70f;
            if (lightIntensity > 0)
                Lighting.AddLight(NPC.Center, 178f / 255f * lightIntensity, 223f / 255f * lightIntensity,
                    lightIntensity);
            if (_plasmaStateTime > 20)
            {
                NPC.velocity = _plasmaStateVelocity;
                NPC.scale *= 0.85f;
                NPC.alpha += 25;
                if (NPC.alpha >= 255) NPC.alpha = 255;
            }
            else
            {
                NPC.velocity = Vector2.Zero;
            }

            if (NPC.scale < 0.02f)
            {
                NPC.active = false;
                NPC.netUpdate = true;
                return;
            }

            _plasmaStateTime++;
            return;
        }

        if (Data is { IsShiny: true }) ShinyEffect();
    }

    private void ShinyEffect()
    {
        Lighting.AddLight(NPC.Center, 0.5f, 0.5f, 0.5f);
        _shinySparkleTimer++;
        if (_shinySparkleTimer < 6) return;
        for (var i = 0; i < 2; i++)
        {
            var dust = Dust.NewDustDirect(
                NPC.position + new Vector2(Main.rand.NextFloat(-3, 3), Main.rand.NextFloat(-3, 3)), NPC.width,
                NPC.height, DustID.TreasureSparkle);
            dust.velocity = NPC.velocity;
            dust.noGravity = true;
            dust.scale *= 1f + Main.rand.NextFloat(-0.03f, 0.03f);
        }

        _shinySparkleTimer = 0;
    }

    public override bool? CanBeHitByProjectile(Projectile projectile)
    {
        return projectile.ModProjectile is BasePkballProjectile;
    }

    /*public override bool CanBeHitByNPC(NPC attacker)
    {
        return false;
    }*/

    public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers)
    {
        if (projectile.ModProjectile is BasePkballProjectile)
            modifiers.HideCombatText();
    }

    public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position)
    {
        return false;
    }

    public void Encapsulate(Vector2 pokeballPos)
    {
        if (PlasmaState) return;

        var dust = ModContent.DustType<SummonCloud>();
        for (var i = 0; i < 4; i++)
        {
            var angle = MathHelper.PiOver2 * i;
            var x = (float)Math.Cos(angle);
            var y = (float)Math.Sin(angle);
            Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y), NPC.width, NPC.height, dust, x / 2, y / 2);
            Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y), NPC.width, NPC.height, dust, x, y);
        }

        // Enable plasma state
        PlasmaState = true;

        // Set velocity to move towards pokeball
        _plasmaStateVelocity = (pokeballPos - NPC.Center).ToRotation().ToRotationVector2() * 2f;

        NPC.noGravity = true; // Disable gravity
        NPC.netUpdate = true;
    }

    public override void Load()
    {
        // First time loading any PokemonNPC, initialize static fields.
        if (_schemaCache == null)
        {
            _schemaCache = new Dictionary<ushort, JToken>();
            _glowTextureCache = new Dictionary<ushort, Asset<Texture2D>>();
            _hasGenderDifference = new BitArray(Terramon.LoadedPokemonCount);
            _enableComponentMethod = typeof(NPCComponentExtensions).GetMethod("EnableComponent");
            if (Main.netMode != NetmodeID.Server)
                GameShaders.Misc[$"{nameof(Terramon)}FadeToColor"] =
                    new MiscShaderData(Mod.Assets.Request<Effect>("Assets/Effects/FadeToColor"), "FadePass");
        }

        // Load schema from HJSON file and cache it.
        var hjsonStream = Mod.GetFileStream($"Content/Pokemon/{identifier}.hjson");
        using var hjsonReader = new StreamReader(hjsonStream);
        var jsonText = HjsonValue.Load(hjsonReader).ToString();
        hjsonReader.Close();
        var schema = JObject.Parse(jsonText);
        if (!schema.TryGetValue("NPC", out var value)) return;
        _schemaCache.Add(ID, value);
    }

    public override void Unload()
    {
        if (_schemaCache == null) return;
        _schemaCache = null;
        _glowTextureCache = null;
        _hasGenderDifference = null;
        _enableComponentMethod = null;
    }
}