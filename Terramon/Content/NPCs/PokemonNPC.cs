using System.Reflection;
using Newtonsoft.Json.Linq;
using ReLogic.Content;
using Terramon.Content.Commands;
using Terramon.Content.Configs;
using Terramon.Content.Dusts;
using Terramon.Content.Items;
using Terramon.Content.Items.PokeBalls;
using Terramon.Content.Projectiles;
using Terramon.Core.Abstractions;
using Terramon.Core.Battling;
using Terramon.Core.Loaders;
using Terramon.Core.NPCComponents;
using Terramon.Helpers;
using Terramon.ID;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
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
        On_Main.DrawNPCs += (orig, self, tiles) =>
        {
            orig(self, tiles);

            if (!_highlightedNPCIndex.HasValue ||
                TerramonPlayer.LocalPlayer.Battle?.WildNPCIndex == _highlightedNPCIndex) return;

            var highlightedNPC = Main.npc[_highlightedNPCIndex.Value];
            DrawLevelText(Main.spriteBatch, highlightedNPC);
        };
    }

    protected override bool CloneNewInstances => true;

    public override string Name { get; } = schema.Identifier + "NPC";

    public override LocalizedText DisplayName => DatabaseV2.GetLocalizedPokemonName(Schema);

    public bool PlasmaState { get; private set; }

    public BattleInstance Battle { get; private set; }

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
                var (y, x) = MathF.SinCos(angle);
                Dust.NewDust(NPC.position, NPC.width, NPC.height, dust, x / 2, y / 2);
                Dust.NewDust(NPC.position, NPC.width, NPC.height, dust, x, y);
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

        var isHighlighted = NPC.whoAmI == _highlightedNPCIndex;
        if (isHighlighted)
        {
            _highlightedNPCIndex = null;
        }

        if (!PlasmaState && TerramonPlayer.LocalPlayer.Battle == null)
        {
            if (isHighlighted && NPC.DistanceSQ(Main.LocalPlayer.Center) < 300f * 300f)
            {
                spriteBatch.End();
                spriteBatch.Begin(SpriteSortMode.Immediate, null, Main.DefaultSamplerState, DepthStencilState.None,
                    Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

                // Apply outline shader
                var highlightColor = Data.IsShiny
                    ? ModContent.GetInstance<KeyItemRarity>().RarityColor
                    : ClientConfig.Instance.HighlightColor;
                var outlineShader = ShaderAssets.Outline;
                outlineShader.Shader.Parameters["uThickOutline"].SetValue(ClientConfig.Instance.ThickHighlights);
                outlineShader.Shader.Parameters["uImageSize0"].SetValue(_mainTexture.Size());
                outlineShader
                    .UseColor(highlightColor.MultiplyRGB(drawColor))
                    .UseSecondaryColor(highlightColor.HueShift(0.035f, -0.08f).MultiplyRGB(drawColor))
                    .Apply();

                /*if (!PokemonEntityLoader.HighlightTextures.TryGetValue(ID, out var highlightTexture))
                    highlightTexture = CreateHighlightTexture(); // Creates highlight texture and adds it to cache

                foreach (var off in ChatManager.ShadowDirections) // For each shadow direction
                {
                    var offset = off;
                    offset *= 2;
                    spriteBatch.Draw(highlightTexture, drawPos + offset, NPC.frame,
                        drawColor, NPC.rotation, frameSize / new Vector2(2, 1), NPC.scale, effects, 0f);
                }*/
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

            if (isHighlighted)
            {
                spriteBatch.End();
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                    DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            }

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
        ShaderAssets.FadeToColor
            .UseColor(drawColor)
            .UseOpacity(_plasmaStateTime <= 20 ? _plasmaStateTime / 7.5f : NPC.Opacity)
            .Apply();

        spriteBatch.Draw(mainTextureValue,
            NPC.Center - screenPos + new Vector2(0f, NPC.gfxOffY + DrawOffsetY - (frameSize.Y - NPC.height) / 2f + 4),
            NPC.frame, drawColor, NPC.rotation, frameSize / new Vector2(2, 2), NPC.scale, effects, 0f);

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

        if (Math.Abs(Main.GameZoomTarget - 1f) < 1e-5)
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

        if (Data != null && Data.HP != 0) NPC.life = Data.HP;
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
        if (_shinySparkleTimer < 15) return;
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

    public override bool PreHoverInteract(bool mouseIntersects)
    {
        if (_highlightedNPCIndex == null)
        {
            _highlightedNPCIndex = NPC.whoAmI;
            if (NPC.DistanceSQ(Main.LocalPlayer.Center) < 300f * 300f)
            {
                var modPlayer = TerramonPlayer.LocalPlayer;
                if (modPlayer.Battle == null && Main.mouseRight && Main.mouseRightRelease)
                {
                    if (!modPlayer.HasChosenStarter)
                    {
                        Main.NewText(Language.GetTextValue("Mods.Terramon.Misc.RequireStarter"),
                            TerramonCommand.ChatColorYellow);
                    }
                    else if (modPlayer.GetActivePokemon() == null)
                    {
                        Main.NewText(Language.GetTextValue("Mods.Terramon.Misc.NoActivePokemon"),
                            TerramonCommand.ChatColorYellow);
                    }
                    else
                    {
                        SoundEngine.PlaySound(SoundID.MenuTick);
                        StartBattle();
                    }
                }
            }
        }

        if (_mouseHoverTimer == -1)
            return true;

        if (mouseIntersects)
        {
            if (++_mouseHoverTimer != 60) // 1 second assuming 60 FPS. TODO: Make independent of framerate, but only if it matters
                return true;
            // Register as seen in the player's Pokédex
            TerramonPlayer.LocalPlayer.UpdatePokedex(ID, PokedexEntryStatus.Seen,
                shiny: Data?.IsShiny ?? false);
            _mouseHoverTimer = -1;
        }
        else
        {
            _mouseHoverTimer = 0;
        }
        
        return true;
    }

    public void Encapsulate(Vector2 pokeballPos)
    {
        if (PlasmaState) return;

        var dust = ModContent.DustType<SummonCloud>();
        for (var i = 0; i < 4; i++)
        {
            var angle = MathHelper.PiOver2 * i;
            var (y, x) = MathF.SinCos(angle);
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

    private void StartBattle()
    {
        // Main.NewText($"Starting battle with wild {DisplayName}");

        var player = Main.LocalPlayer;

        // Create a new BattleInstance for this battle
        var battle = new BattleInstance
        {
            Player1Index = player.whoAmI,
            WildNPCIndex = NPC.whoAmI
        };

        // Keep references to the battle alive on both the player and the NPC
        player.Terramon().Battle = battle;
        Battle = battle;
        
        // Start the battle
        battle.Start();
    }
    
    public void EndBattle()
    {
        Battle?.BattleStream?.Dispose();
        Battle = null;
        NPC.ShowNameOnHover = true;
    }
}