using System.Diagnostics;
using System.Reflection;
using Newtonsoft.Json.Linq;
using ReLogic.Content;
using Terramon.Content.Configs;
using Terramon.Content.Dusts;
using Terramon.Content.Items.PokeBalls;
using Terramon.Content.Projectiles;
using Terramon.Core.Abstractions;
using Terramon.Core.Loaders;
using Terramon.Core.NPCComponents;
using Terramon.ID;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.Localization;
using Terraria.UI.Chat;

namespace Terramon.Content.NPCs;

[Autoload(false)]
public class PokemonNPC(ushort id, DatabaseV2.PokemonSchema schema) : ModNPC, IPokemonEntity
{
    /// <summary>
    ///     The index of the Pokémon NPC under the mouse cursor.
    /// </summary>
    private static int? _highlightedNPCIndex;

    private int _cryTimer;
    private PokemonData _data;
    private Asset<Texture2D> _mainTexture;
    private int _mouseHoverTimer;
    private int _plasmaStateTime;
    private Vector2 _plasmaStateVelocity;
    private int _shinySparkleTimer;

    static PokemonNPC()
    {
        On_Main.DoUpdateInWorld += static (orig, self, sw) =>
        {
            _highlightedNPCIndex = null;
            orig(self, sw);
        };

        On_Main.DrawNPCs += (orig, self, tiles) =>
        {
            orig(self, tiles);

            if (!_highlightedNPCIndex.HasValue) return;

            var highlightedNPC = Main.npc[_highlightedNPCIndex.Value];
            DrawLevelText(Main.spriteBatch, highlightedNPC);
        };
    }

    protected override bool CloneNewInstances => true;

    public override string Name { get; } = schema.Identifier + "NPC";

    public override LocalizedText DisplayName => DatabaseV2.GetLocalizedPokemonName(Schema);

    public bool PlasmaState { get; private set; }

    public override string Texture { get; } = "Terramon/Assets/Pokemon/" + schema.Identifier;

    public ushort ID { get; } = id;

    public DatabaseV2.PokemonSchema Schema { get; } = schema;

    public PokemonData Data
    {
        get => _data;
        set
        {
            _data = value;
            NPC.lifeMax = _data.MaxHP;
            NPC.life = _data.HP;
        }
    }

    public override void SetStaticDefaults()
    {
        // Hide from the bestiary (they will appear in the Pokédex instead)
        var drawModifier = new NPCID.Sets.NPCBestiaryDrawModifiers
        {
            Hide = true
        };
        NPCID.Sets.NPCBestiaryDrawOffset.Add(NPC.type, drawModifier);
    }

    public override void SetDefaults()
    {
        NPC.lifeMax = 250;
        NPC.HitSound = SoundID.NPCHit1;
        NPC.value = 0f;
        NPC.knockBackResist = 0.75f;
        NPC.npcSlots = 0.2f;
        NPC.despawnEncouraged = GameplayConfig.Instance.EncourageDespawning;
        NPC.friendly = true;

        // Start a stopwatch to measure the time it takes to apply all components.
        // var stopwatch = Stopwatch.StartNew();

        foreach (var component in PokemonEntityLoader.NPCSchemaCache[ID].Children<JProperty>())
        {
            var componentType = Mod.Code.GetType($"Terramon.Content.NPCs.NPC{component.Name}");
            if (componentType == null)
                // Remove the component from the schema if it doesn't exist.
                // _schemaCache[ID].First(x => x.Path == component.Path).Remove();
                continue;
            var enableComponentRef = NPCComponentExtensions.EnableComponentMethod.MakeGenericMethod(componentType);
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

            _cryTimer = 30;
        }

        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        var spawningPlayer = Player.FindClosest(NPC.Center, NPC.width, NPC.height);
        Data = PokemonData.Create(Main.player[spawningPlayer], ID, 5);
        NPC.netUpdate = true;
    }

    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        _mainTexture ??= PokemonEntityLoader.RequestTexture(this);
        var mainTextureValue = _mainTexture.Value;

        var frameSize = NPC.frame.Size();
        var effects = NPC.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        var drawPos = NPC.Center - screenPos +
                      new Vector2(0f, NPC.gfxOffY + DrawOffsetY + (int)Math.Ceiling(NPC.height / 2f) + 4);

        if (!PlasmaState)
        {
            if (NPC.whoAmI == _highlightedNPCIndex &&
                Vector2.Distance(Main.LocalPlayer.Center, NPC.Center) < 300f) // Up to 20 blocks away
            {
                if (!PokemonEntityLoader.HighlightTextures.TryGetValue(ID, out var highlightTexture))
                    highlightTexture = CreateHighlightTexture(); // Creates highlight texture and adds it to cache

                foreach (var off in ChatManager.ShadowDirections) // For each shadow direction
                {
                    var offset = off;
                    offset *= 2;
                    spriteBatch.Draw(highlightTexture, drawPos + offset, NPC.frame,
                        drawColor, NPC.rotation, frameSize / new Vector2(2, 1), NPC.scale, effects, 0f);
                }
            }
        }

        if (_plasmaStateTime <= 20)
        {
            // Desaturate the lightColor for Gastly
            var adjustedColor = ID == NationalDexID.Gastly
                ? PokemonPet.GrayscaleColor(drawColor)
                : drawColor;

            spriteBatch.Draw(mainTextureValue,
                drawPos,
                NPC.frame, adjustedColor, NPC.rotation,
                frameSize / new Vector2(2, 1), NPC.scale, effects, 0f);

            var glowCache = Data?.IsShiny ?? false
                ? PokemonEntityLoader.ShinyGlowTextureCache
                : PokemonEntityLoader.GlowTextureCache;
            if (glowCache.TryGetValue(ID, out var glowTexture) && !PlasmaState)
            {
                drawColor = Color.White;
                if (ID == NationalDexID.Gastly) drawColor.A = 128;
                spriteBatch.Draw(glowTexture.Value, drawPos, NPC.frame, drawColor, NPC.rotation,
                    frameSize / new Vector2(2, 1), NPC.scale, effects, 0f);
            }
        }

        if (!PlasmaState) return false;

        // Draw the Pokémon with the fade shader
        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Immediate, null, Main.DefaultSamplerState, DepthStencilState.None,
            Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

        // Apply fade shader
        GameShaders.Misc[$"{nameof(Terramon)}FadeToColor"].UseColor(drawColor);
        GameShaders.Misc[$"{nameof(Terramon)}FadeToColor"]
            .UseOpacity(_plasmaStateTime <= 20 ? _plasmaStateTime / 7.5f : NPC.Opacity);
        GameShaders.Misc[$"{nameof(Terramon)}FadeToColor"].Apply();

        spriteBatch.Draw(mainTextureValue,
            NPC.Center - screenPos +
            new Vector2(0f, NPC.gfxOffY + DrawOffsetY - (frameSize.Y - NPC.height) / 2f + 4),
            NPC.frame, drawColor, NPC.rotation,
            frameSize / new Vector2(2, 2), NPC.scale, effects, 0f);

        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
            DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

        return false;
    }

    /// <summary>
    ///     Draws the NPC’s level text above its sprite.
    /// </summary>
    private static void DrawLevelText(SpriteBatch spriteBatch, NPC npc)
    {
        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
            DepthStencilState.None, Main.Rasterizer, null);

        const string text = "Lv. 5";
        var computedScale = 0.8f * Main.GameZoomTarget;
        var useFont = computedScale > 1f ? FontAssets.DeathText.Value : FontAssets.MouseText.Value;

        if (computedScale > 1f)
            computedScale /= 2.5f;

        var textScale = new Vector2(computedScale);
        var textSize = ChatManager.GetStringSize(useFont, text, Vector2.One) * textScale.X / Main.GameZoomTarget;
        var textDrawPos = npc.position - Main.screenPosition - textSize + new Vector2(8, npc.gfxOffY);

        if (Main.GameZoomTarget == 1f)
        {
            // Clamp to pixel values
            textDrawPos.X = (int)textDrawPos.X;
            textDrawPos.Y = (int)textDrawPos.Y;
        }

        var transformedPos = Vector2.Transform(textDrawPos, Main.GameViewMatrix.ZoomMatrix);
        ChatManager.DrawColorCodedStringWithShadow(spriteBatch, useFont, text, transformedPos,
            Main.MouseTextColorReal, 0f, Vector2.Zero, textScale);

        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
            DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
    }

    public override void SendExtraAI(BinaryWriter writer)
    {
        Data.NetWrite(writer, PokemonData.BitIsShiny | PokemonData.BitPersonalityValue | PokemonData.BitVariant);
        writer.Write(PlasmaState);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        var isFirstSync = Data == null;
        Data ??= new PokemonData
        {
            ID = ID,
            Level = 5
        };

        Data.NetRead(reader);
        PlasmaState = reader.ReadBoolean();

        if (isFirstSync)
            // In multiplayer, load the proper texture after receiving the data from the server
            _mainTexture = PokemonEntityLoader.RequestTexture(this);
    }

    public override void AI()
    {
        if (_cryTimer > 0)
        {
            _cryTimer--;
            if (_cryTimer == 0 && Data != null && Main.netMode != NetmodeID.Server)
            {
                var cry = new SoundStyle("Terramon/Sounds/Cries/" + Data.InternalName)
                    { Volume = 0.15f };
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

        // Check for highlighting (mouse hovering)
        // TODO: Checking this in AI() might result in flicker frames when using HFPSS
        var boundingBox = new Rectangle((int)NPC.Bottom.X - NPC.frame.Width / 2,
            (int)NPC.Bottom.Y - NPC.frame.Height, NPC.frame.Width, NPC.frame.Height);
        var mouseRectangle = new Rectangle((int)(Main.mouseX + Main.screenPosition.X),
            (int)(Main.mouseY + Main.screenPosition.Y), 1, 1);
        var isMouseHovering = mouseRectangle.Intersects(boundingBox) ||
                              (Main.SmartInteractShowingGenuine && Main.SmartInteractNPC == NPC.whoAmI);

        if (isMouseHovering && _highlightedNPCIndex == null)
        {
            _highlightedNPCIndex = NPC.whoAmI;
            if (Main.mouseRight && Main.mouseRightRelease)
            {
                Main.NewText("Starting battle!");
            }
        }

        if (Data is not { IsShiny: true }) return;

        if (_mainTexture == null)
            SoundEngine.PlaySound(SoundID.Item30, NPC.position);

        ShinyEffect();
    }

    private void ShinyEffect()
    {
        // Disable shiny effect lighting for Haunter and Gengar
        if (ID != NationalDexID.Haunter && ID != NationalDexID.Gengar) Lighting.AddLight(NPC.Center, 0.5f, 0.5f, 0.5f);
        _shinySparkleTimer++;
        if (_shinySparkleTimer < 12) return;
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
        return projectile.ModProjectile is BasePkballProjectile && !PlasmaState;
    }

/*
    public override bool CanBeHitByNPC(NPC attacker)
    {
        return false;
    }
*/

    public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers)
    {
        if (projectile.ModProjectile is BasePkballProjectile)
            modifiers.HideCombatText();
    }

    public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position)
    {
        return false;
    }

    public override void ModifyHoverBoundingBox(ref Rectangle boundingBox)
    {
        if (_mouseHoverTimer == -1) return;

        var mouseRectangle = new Rectangle((int)(Main.mouseX + Main.screenPosition.X),
            (int)(Main.mouseY + Main.screenPosition.Y), 1, 1);
        if (mouseRectangle.Intersects(boundingBox))
        {
            _mouseHoverTimer++;
            if (_mouseHoverTimer !=
                60) return; // 1 second assuming 60 FPS. TODO: Make independent of framerate, but only if it matters
            // Register as seen in the player's Pokédex
            TerramonPlayer.LocalPlayer.UpdatePokedex(ID, PokedexEntryStatus.Seen,
                shiny: Data?.IsShiny ?? false);
            _mouseHoverTimer = -1;
        }
        else
        {
            _mouseHoverTimer = 0;
        }
    }

    public void Encapsulate(Vector2 pokeballPos)
    {
        if (PlasmaState) return;

        var dust = ModContent.DustType<SummonCloud>();
        for (var i = 0; i < 4; i++)
        {
            var angle = MathHelper.PiOver2 * i;
            var x = MathF.Cos(angle);
            var y = MathF.Sin(angle);
            Dust.NewDust(NPC.position, NPC.width, NPC.height, dust, x / 2, y / 2);
            Dust.NewDust(NPC.position, NPC.width, NPC.height, dust, x, y);
        }

        // Enable plasma state
        PlasmaState = true;

        // Set velocity to move towards pokeball
        _plasmaStateVelocity = pokeballPos - NPC.Center;
        if (_plasmaStateVelocity != Vector2.Zero) _plasmaStateVelocity = Vector2.Normalize(_plasmaStateVelocity) * 2f;

        NPC.noGravity = true; // Disable gravity
        NPC.ShowNameOnHover = false; // Disable showing name on hover
        NPC.netUpdate = true;
    }

    private Texture2D CreateHighlightTexture()
    {
        var sw = Stopwatch.StartNew();

        var mainTextureValue = _mainTexture.Value;

        // Get base pixel data
        var baseData = new Color[mainTextureValue.Width * mainTextureValue.Height];
        mainTextureValue.GetData(baseData);

        // Make buffer for AA
        var aaDataBuffer = new Color[baseData.Length];

        // Define colors
        var outlineColor = new Color(252, 252, 84);
        var aaColor = new Color(255, 208, 15);

        // First pass: Fill AA data buffer
        for (var i = 0; i < baseData.Length; i++)
        {
            if (baseData[i].A <= 0) continue;

            baseData[i] = outlineColor;

            // Get 2D position of current pixel
            var x = i % mainTextureValue.Width;
            var y = i / mainTextureValue.Width;

            // Skip edge pixels
            if (x == 0 || x == mainTextureValue.Width - 1 ||
                y == 0 || y == mainTextureValue.Height - 1)
                continue;

            var leftIndex = y * mainTextureValue.Width + (x - 1);
            var rightIndex = y * mainTextureValue.Width + (x + 1);
            var upIndex = (y - 1) * mainTextureValue.Width + x;
            var downIndex = (y + 1) * mainTextureValue.Width + x;

            var hasLeft = baseData[leftIndex].A > 0;
            var hasRight = baseData[rightIndex].A > 0;
            var hasUp = baseData[upIndex].A > 0;
            var hasDown = baseData[downIndex].A > 0;

            // Check if all four directions from this pixel have solid pixels
            if (hasLeft && hasRight && hasUp && hasDown)
            {
                // Check diagonal directions for transparent pixels
                var topLeftIndex = (y - 1) * mainTextureValue.Width + (x - 1);
                var topRightIndex = (y - 1) * mainTextureValue.Width + x + 1;
                var bottomLeftIndex = (y + 1) * mainTextureValue.Width + (x - 1);
                var bottomRightIndex = (y + 1) * mainTextureValue.Width + x + 1;

                // Set any transparent diagonal pixels to red in a 2x2 pattern

                // Top-left 2x2 region
                if (baseData[topLeftIndex].A == 0)
                {
                    aaDataBuffer[topLeftIndex] = Color.Red;
                    aaDataBuffer[(y - 1) * mainTextureValue.Width + (x - 2)] = Color.Red;
                    aaDataBuffer[(y - 2) * mainTextureValue.Width + (x - 1)] = Color.Red;
                    aaDataBuffer[(y - 2) * mainTextureValue.Width + (x - 2)] = Color.Red;
                }

                // Top-right 2x2 region
                if (baseData[topRightIndex].A == 0)
                {
                    aaDataBuffer[topRightIndex] = Color.Red;
                    aaDataBuffer[(y - 1) * mainTextureValue.Width + (x + 2)] = Color.Red;
                    aaDataBuffer[(y - 2) * mainTextureValue.Width + (x + 1)] = Color.Red;
                    aaDataBuffer[(y - 2) * mainTextureValue.Width + (x + 2)] = Color.Red;
                }

                // Bottom-left 2x2 region
                if (baseData[bottomLeftIndex].A == 0)
                {
                    aaDataBuffer[bottomLeftIndex] = Color.Red;
                    aaDataBuffer[(y + 1) * mainTextureValue.Width + (x - 2)] = Color.Red;
                    aaDataBuffer[(y + 2) * mainTextureValue.Width + (x - 1)] = Color.Red;
                    aaDataBuffer[(y + 2) * mainTextureValue.Width + (x - 2)] = Color.Red;
                }

                // Bottom-right 2x2 region
                if (baseData[bottomRightIndex].A == 0)
                {
                    aaDataBuffer[bottomRightIndex] = Color.Red;
                    aaDataBuffer[(y + 1) * mainTextureValue.Width + (x + 2)] = Color.Red;
                    aaDataBuffer[(y + 2) * mainTextureValue.Width + (x + 1)] = Color.Red;
                    aaDataBuffer[(y + 2) * mainTextureValue.Width + (x + 2)] = Color.Red;
                }
            }
        }

        // Second pass: Apply AA outline color
        for (var i = 0; i < baseData.Length; i++)
        {
            if (baseData[i].A <= 0) continue;

            // Get 2D position of current pixel
            var x = i % mainTextureValue.Width;
            var y = i / mainTextureValue.Width;

            // We only want to process "real" pixels
            if (x % 2 != 0 || y % 2 != 0) continue;

            // Skip edge pixels
            if (x == 0 || x == mainTextureValue.Width - 2 ||
                y == 0 || y == mainTextureValue.Height - 2)
                continue;

            // Keep track of bordering AA pixel count
            var borderingCount = 0;

            // Check up direction
            var upIndex = (y - 1) * mainTextureValue.Width + x;
            if (aaDataBuffer[upIndex].A > 0)
                borderingCount++;

            // Check down direction
            var downIndex = (y + 2) * mainTextureValue.Width + x;
            if (aaDataBuffer[downIndex].A > 0)
                borderingCount++;

            // Check left direction
            var leftIndex = y * mainTextureValue.Width + (x - 1);
            if (aaDataBuffer[leftIndex].A > 0)
                borderingCount++;

            // Check right direction
            var rightIndex = y * mainTextureValue.Width + (x + 2);
            if (aaDataBuffer[rightIndex].A > 0)
                borderingCount++;

            // If 2 or more bordering AA pixels...
            if (borderingCount < 2) continue;

            // ...then fill this 2x2 block with AA color
            baseData[i] = aaColor;
            baseData[i + 1] = aaColor;
            baseData[i + mainTextureValue.Width] = aaColor;
            baseData[i + mainTextureValue.Width + 1] = aaColor;
        }

        var highlightTexture = new Texture2D(Main.graphics.GraphicsDevice, mainTextureValue.Width,
            mainTextureValue.Height);
        highlightTexture.SetData(baseData);
        PokemonEntityLoader.HighlightTextures.Add(ID, highlightTexture);
        Main.NewText($"Generated highlight texture for {ID} in {sw.Elapsed}");

        return highlightTexture;
    }
}