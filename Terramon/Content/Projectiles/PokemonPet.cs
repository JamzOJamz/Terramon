using System.Reflection;
using Newtonsoft.Json.Linq;
using ReLogic.Content;
using Terramon.Content.Buffs;
using Terramon.Content.Configs;
using Terramon.Content.Dusts;
using Terramon.Content.NPCs;
using Terramon.Core.Abstractions;
using Terramon.Core.Battling;
using Terramon.Core.Loaders;
using Terramon.Core.ProjectileComponents;
using Terramon.Helpers;
using Terramon.ID;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;

namespace Terramon.Content.Projectiles;

[Autoload(false)]
public sealed class PokemonPet(ushort id, DatabaseV2.PokemonSchema schema) : ModProjectile, IPokemonEntity
{
    public delegate void CustomFindFrame(PokemonPet proj);

    private static readonly Asset<Texture2D> ResourceBarStartTexture;
    private static readonly Asset<Texture2D> ResourceBarMiddleTexture;
    private static readonly Asset<Texture2D> ResourceBarEndTexture;

    private static readonly HashSet<int> PetsNeedingHPBars = [];
    private int _activeAttackTimer;
    private int _attackCooldown = 60;

    private Vector2? _attackDirection;

    private ushort _cachedID;
    private int _cryTimer;

    private int _lastHPState = -1;
    private Asset<Texture2D> _mainTexture;
    private bool _regeneratingHealth;
    private int _regenStartTarget;
    private int _regenStartTimer;
    private float _regenTimer;
    private int _shinySparkleTimer;

    private NPC _target;

    private int _visualImmunityFrames;
    public int CustomFrameCounter;
    public int? CustomSpriteDirection;
    public Vector2? CustomTargetPosition;
    public CustomFindFrame FindFrame;

    static PokemonPet()
    {
        // Don't run this on the server
        if (Main.dedServ) return;

        // Load resource bar textures
        ResourceBarStartTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/Realtime/ResourceBarStart");
        ResourceBarMiddleTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/Realtime/ResourceBarMiddle");
        ResourceBarEndTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/Realtime/ResourceBarEnd");

        // Draw HP bars on proper layer
        On_Main.DrawInterface_14_EntityHealthBars += (orig, self) =>
        {
            orig(self);

            // Draw HP bars for all marked pets
            foreach (var projIndex in PetsNeedingHPBars)
            {
                var proj = Main.projectile[projIndex];
                if (!proj.active || proj.ModProjectile is not PokemonPet pet) continue;

                DrawHealthBar(pet, proj);
            }

            // Clear the collection for next frame
            PetsNeedingHPBars.Clear();
        };

        // Draw the player's active Pokémon pet in the character select UI
        On_UICharacter.ctor += (orig, self, player, animated, panel, scale, clone) =>
        {
            orig(self, player, animated, panel, scale, clone);

            // Get the pet projectiles from the UICharacter
            ref var petProjectiles = ref self._petProjectiles;
            if (petProjectiles.Length > 0) return;

            var modPlayer = player.GetModPlayer<TerramonPlayer>();
            var activePokemon = modPlayer.GetActivePokemon();

            if (activePokemon == null) return;

            // Create a dummy pet projectile and assign it to the UICharacter
            var projectile = new Projectile();
            projectile.SetDefaults(PokemonEntityLoader.IDToPetType[activePokemon.ID]);
            projectile.isAPreviewDummy = true;
            ((PokemonPet)projectile.ModProjectile).Data =
                activePokemon; // Set the pet's data to the player's active Pokémon (hacky)
            petProjectiles = [projectile];
        };
        return;
    }


    protected override bool CloneNewInstances => true;

    public override string Name { get; } = schema.Identifier + "Pet";

    public override LocalizedText DisplayName => DatabaseV2.GetLocalizedPokemonName(Schema);

    public override string Texture { get; } = "Terramon/Assets/Pokemon/" + schema.Identifier;

    public ushort ID { get; } = id;

    public DatabaseV2.PokemonSchema Schema { get; } = schema;

    public PokemonData Data { get; set; }

    public override void SetStaticDefaults()
    {
        Main.projPet[Projectile.type] = true;
    }

    public override void SetDefaults()
    {
        foreach (var component in PokemonEntityLoader.PetSchemaCache[ID].Children<JProperty>())
        {
            var componentType = Mod.Code.GetType($"Terramon.Content.Projectiles.Projectile{component.Name}");
            if (componentType == null)
                // Remove the component from the schema if it doesn't exist.
                // _schemaCache[ID].First(x => x.Path == component.Path).Remove();
                continue;
            var enableComponentRef =
                ProjectileComponentExtensions.EnableComponentMethod.MakeGenericMethod(componentType);
            var instancedComponent = enableComponentRef.Invoke(null, [Projectile, null]);
            foreach (var prop in component.Value.Children<JProperty>())
            {
                var fieldInfo = componentType.GetRuntimeField(prop.Name);
                if (fieldInfo == null) continue;
                fieldInfo.SetValue(instancedComponent, prop.Value.ToObject(fieldInfo.FieldType));
            }
        }
    }

    public override void OnSpawn(IEntitySource source)
    {
        var owningPlayer = Main.player[Projectile.owner];
        var modPlayer = owningPlayer.Terramon();
        
        // Move ahead of player
        var direction = owningPlayer.direction;
        CustomSpriteDirection = direction;
        Projectile.position.X += direction * (MathF.Abs(Main.player[Projectile.owner].velocity.X) < 1.5f ? 40 : -30);

        var dust = ModContent.DustType<SummonCloud>();
        var mainPosition = new Vector2(Projectile.position.X - Projectile.width / 2f,
            Projectile.position.Y - Projectile.height / 2f);
        Dust.NewDust(new Vector2(mainPosition.X, mainPosition.Y + 4), Projectile.width, Projectile.height, dust);
        Dust.NewDust(new Vector2(mainPosition.X, mainPosition.Y - 4), Projectile.width, Projectile.height, dust);
        Dust.NewDust(new Vector2(mainPosition.X + 4, mainPosition.Y), Projectile.width, Projectile.height, dust);
        Dust.NewDust(new Vector2(mainPosition.X - 4, mainPosition.Y), Projectile.width, Projectile.height, dust);
        Dust.NewDust(new Vector2(mainPosition.X + 2, mainPosition.Y + 2), Projectile.width, Projectile.height, dust);
        Dust.NewDust(new Vector2(mainPosition.X + 2, mainPosition.Y - 2), Projectile.width, Projectile.height, dust);
        Dust.NewDust(new Vector2(mainPosition.X - 2, mainPosition.Y + 2), Projectile.width, Projectile.height, dust);
        Dust.NewDust(new Vector2(mainPosition.X - 2, mainPosition.Y - 2), Projectile.width, Projectile.height, dust);

        ConfrontFoe(modPlayer.Battle);
        
        Data = modPlayer.GetActivePokemon();
        modPlayer.ActivePetProjectile = this;
        _cachedID = ID;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        _mainTexture ??= PokemonEntityLoader.RequestTexture(this);

        if (Projectile.isAPreviewDummy)
            // Call FindFrame delegate to determine the frame of the pet (set by behaviour components)
            FindFrame?.Invoke(this);

        if (_visualImmunityFrames > 0 && _visualImmunityFrames % 6 < 3)
            return false; // Skip draw to create flashing

        var projFrameCount = Main.projFrames[Type];
        var drawPos = Projectile.Center - Main.screenPosition +
                      new Vector2(0f,
                          Projectile.gfxOffY + DrawOriginOffsetY + (int)Math.Ceiling(Projectile.height / 2f) + 4);

        // Apply attack animation offset
        if (_activeAttackTimer > 0)
        {
            _attackDirection ??= _target == null ? Vector2.Zero : Vector2.Normalize(_target.Center - Projectile.Center);

            var normalizedTime = (16 - _activeAttackTimer) / 16f;
            float offsetIntensity;

            if (normalizedTime < 0.3f)
            {
                // Fast attack movement
                var attackTime = normalizedTime / 0.3f;
                offsetIntensity = (float)Math.Sin(attackTime * Math.PI * 0.5f);
            }
            else
            {
                // Slow return with easing
                var returnTime = (normalizedTime - 0.3f) / 0.7f;
                offsetIntensity = (float)Math.Cos(returnTime * Math.PI * 0.5f);
            }

            const float maxOffset = 16f;

            var attackOffset = _attackDirection.Value * (offsetIntensity * maxOffset);
            drawPos += attackOffset;
        }
        else
        {
            _attackDirection = null;
        }

        var sourceRect = _mainTexture.Frame(1, projFrameCount, frameY: Projectile.frame);
        var frameSize = sourceRect.Size();
        var origin = frameSize / new Vector2(2, 1);
        var effects = CustomSpriteDirection.HasValue
            ? CustomSpriteDirection.Value == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None
            : Projectile.spriteDirection == -1
                ? SpriteEffects.FlipHorizontally
                : SpriteEffects.None;

        // Draw name on mouse hover
        if (!Projectile.isAPreviewDummy && ClientConfig.Instance.ShowPetNameOnHover)
        {
            var originOffsetDrawPos = drawPos - origin;
            var drawRect = new Rectangle((int)originOffsetDrawPos.X, (int)originOffsetDrawPos.Y, (int)frameSize.X,
                (int)frameSize.Y);
            if (drawRect.Contains(Main.mouseX, Main.mouseY))
            {
                var mouseTextMult = Main.mouseTextColor / 255f;
                var subColor = new Color((byte)(182f * mouseTextMult), (byte)(187f * mouseTextMult),
                    (byte)(203f * mouseTextMult));
                Main.instance.MouseText(
                    $"{Data.DisplayName}: {Data.HP}/{Data.MaxHP}\n[c/{subColor.ToHexString()}:Following]");
                Main.LocalPlayer.cursorItemIconEnabled = false;
            }
        }

        // Desaturate the lightColor for Gastly
        var adjustedColor = ID == NationalDexID.Gastly
            ? GrayscaleColor(lightColor)
            : lightColor;

        Main.EntitySpriteDraw(_mainTexture.Value,
            drawPos,
            sourceRect, adjustedColor,
            Projectile.rotation,
            origin, Projectile.scale, effects);

        var glowCache = Data?.IsShiny ?? false
            ? PokemonEntityLoader.ShinyGlowTextureCache
            : PokemonEntityLoader.GlowTextureCache;
        if (!glowCache.TryGetValue(ID, out var glowTexture)) return false;

        // Draw the glowmask texture for the pet
        lightColor = Color.White;
        if (ID == NationalDexID.Gastly) lightColor.A = 128;
        Main.EntitySpriteDraw(glowTexture.Value,
            drawPos,
            sourceRect, lightColor,
            Projectile.rotation,
            origin, Projectile.scale, effects);

        return false;
    }

    public override void PostDraw(Color lightColor)
    {
        // Marks this pet for HP bar rendering if it needs one
        if (!Projectile.isAPreviewDummy && Data != null && Data.HP != Data.MaxHP)
            PetsNeedingHPBars.Add(Projectile.whoAmI);
    }

    private static void DrawHealthBar(PokemonPet pet, Projectile proj)
    {
        Main.spriteBatch.End();
        Main.spriteBatch.Begin(default, null, Main.DefaultSamplerState, null, Main.Rasterizer, null,
            Main.GameViewMatrix.TransformationMatrix);

        // Get the main texture to determine Pokemon width (and subsequently bar width)
        const int barMinWidth = 28;
        const int barMaxWidth = 48;

        var pokemonWidth = pet._mainTexture.Value.Width;
        var barWidth = Math.Clamp(pokemonWidth, barMinWidth, barMaxWidth);
        var barXOffset = (barWidth - pokemonWidth) / -2;

        // Calculate health bar position
        var drawPos = proj.Center - Main.screenPosition +
                      new Vector2(barXOffset,
                          proj.gfxOffY + pet.DrawOriginOffsetY + (int)Math.Ceiling(proj.height / 2f) + 4);
        var sourceRect = pet._mainTexture.Frame(1, Main.projFrames[pet.Type], frameY: proj.frame);
        var frameSize = sourceRect.Size();
        var origin = frameSize / new Vector2(2, 1);

        // Health bar dimensions
        var healthPercent = (float)pet.Data.HP / pet.Data.MaxHP;
        var regenHealthPercent = (float)pet.Data.RegenHP / pet.Data.MaxHP;

        // Draw health bar background (full width)
        var barStartPos = drawPos - origin + new Vector2(0, frameSize.Y + 6);
        barStartPos.X = (int)barStartPos.X;
        barStartPos.Y = (int)barStartPos.Y;

        const float lightMultiplier = 3.75f;
        var lightColor = Lighting.GetColor((int)(proj.Center.X / 16f), (int)(proj.Center.Y / 16f));
        var barColor = new Color(
            Math.Min((int)(lightColor.R * lightMultiplier), 255),
            Math.Min((int)(lightColor.G * lightMultiplier), 255),
            Math.Min((int)(lightColor.B * lightMultiplier), 255),
            255
        );

        // Draw start cap
        Main.spriteBatch.Draw(ResourceBarStartTexture.Value, barStartPos, null, barColor,
            0f, Vector2.Zero, 1f, SpriteEffects.None, 0);

        // Draw middle section (stretched to fill the width)
        var middleWidth = barWidth - ResourceBarStartTexture.Value.Width - ResourceBarEndTexture.Value.Width;
        var middlePos = barStartPos + new Vector2(ResourceBarStartTexture.Value.Width, 0);

        // ReSharper disable once PossibleLossOfFraction
        Main.spriteBatch.Draw(ResourceBarMiddleTexture.Value, middlePos, null, barColor, 0f, Vector2.Zero,
            new Vector2(middleWidth / ResourceBarMiddleTexture.Value.Width, 1f), SpriteEffects.None, 0);

        // Draw end cap
        Main.spriteBatch.Draw(ResourceBarEndTexture.Value,
            barStartPos + new Vector2(barWidth - ResourceBarEndTexture.Value.Width, 0),
            null, barColor,
            0f, Vector2.Zero, 1f, SpriteEffects.None, 0);

        var healthFillRect = new Rectangle((int)barStartPos.X + 4, (int)barStartPos.Y + 4, barWidth - 8, 4);
        var originalHealthFillRectWidth = healthFillRect.Width;

        // Draw health fill background
        Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, healthFillRect,
            ColorUtils.MultiplyBlend(barColor, new Color(30, 40, 43)));

        // Draw regen health fill
        if (regenHealthPercent > 0f)
        {
            var pulseProgress = (float)Main.timeForVisualEffects / 60f % 1f;
            var pulseOpacity = 1f - pulseProgress; // Fade from full opacity to 0

            var regenColor = new Color(120, 150, 156) * pulseOpacity;

            healthFillRect.Width = (int)(originalHealthFillRectWidth * regenHealthPercent);
            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, healthFillRect,
                ColorUtils.MultiplyBlend(barColor, regenColor));
        }

        // Draw health fill
        if (!(healthPercent > 0f)) return;

        var currentHpState = healthPercent switch
        {
            > 0.5f => 2,
            > 0.2f => 1,
            _ => 0
        };

        if (currentHpState != pet._lastHPState)
        {
            if (pet._lastHPState >= 0 && currentHpState < pet._lastHPState)
                pet._cryTimer = 30;
            pet._lastHPState = currentHpState;
        }

        var healthColor = currentHpState switch
        {
            2 => new Color(0x36, 0xFF, 0x66),
            1 => new Color(0xFF, 0xE6, 0x43),
            _ => new Color(0xF0, 0x29, 0x29)
        };

        healthFillRect.Width = (int)(originalHealthFillRectWidth * healthPercent);
        Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, healthFillRect,
            ColorUtils.MultiplyBlend(barColor, healthColor));
    }

    public static Color GrayscaleColor(Color color)
    {
        // Compute luminance using the weighted average formula
        var grayValue = (int)(0.299f * color.R + 0.587f * color.G + 0.114f * color.B);
        return new Color(grayValue, grayValue, grayValue, color.A); // Maintain original alpha
    }

    public override void SendExtraAI(BinaryWriter writer)
    {
        Data.NetWrite(writer, PokemonData.BitIsShiny | PokemonData.BitPersonalityValue | PokemonData.BitVariant);
        writer.Write(CustomSpriteDirection.HasValue);
        if (CustomSpriteDirection.HasValue) writer.Write(CustomSpriteDirection.Value);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        Data ??= PokemonData.Create(ID).Build();
        Data.NetRead(reader);
        CustomSpriteDirection = reader.ReadBoolean() ? reader.ReadInt32() : null;
    }

    public override void AI()
    {
        var owningPlayer = Main.player[Projectile.owner];
        var activePokemon = owningPlayer.GetModPlayer<TerramonPlayer>().GetActivePokemon();

        var isShiny = Data is { IsShiny: true };

        if (ID == NationalDexID.Gastly)
        {
            var lightColor = isShiny
                ? new Vector3(63 / 255f, 109 / 255f, 239 / 255f)
                : new Vector3(169 / 255f, 124 / 255f, 226 / 255f);
            Lighting.AddLight(Projectile.Center, lightColor * 1.25f * (Main.raining || Projectile.wet ? 1 - 0.5f : 1));
        }

        // Handles keeping the pet alive until it should despawn
        if (!owningPlayer.dead && owningPlayer.HasBuff(ModContent.BuffType<PokemonCompanion>()) &&
            activePokemon == Data && activePokemon.ID == _cachedID) Projectile.timeLeft = 2;

        // Attacking NPCs
        const float maxDetectRadius = 400f;
        const float maxAttackRadius = 500f;
        
        if (_target != null)
        {
            var distanceToTarget = Vector2.Distance(_target.Center, Projectile.Center);
        
            // Retargets if current target is too far away or no longer valid
            if (distanceToTarget > maxAttackRadius || !IsValidTarget(_target))
                _target = null;
        }
        
        _target ??= FindClosestNPC(maxDetectRadius);

        if (Data != null && _target != null)
        {
            var dir = (Projectile.position.X < _target.position.X).ToDirectionInt();
            CustomSpriteDirection = dir;

            if (_attackCooldown == 0)
            {
                _activeAttackTimer = 16;
                _attackCooldown = 120;
                SoundEngine.PlaySound(new SoundStyle("Terraria/Sounds/Item_7") { Volume = 1f });
            }
            else if (_activeAttackTimer == 8)
            {
                _target.SimpleStrikeNPC(1, dir, false, CalculateKnockback());
                CreateHitEffect(_target.Center);
            }
        }

        if (_attackCooldown > 0) _attackCooldown--;
        _activeAttackTimer--;

        // Health regen
        if (Data != null && Projectile.owner == Main.myPlayer)
        {
            if (Data.HP < Data.RegenHP)
            {
                if (_regenStartTarget == 0)
                    _regenStartTarget = Main.rand.Next(6, 1021); // Between 1/10 of a second and 17 seconds

                _regenStartTimer++;

                if (_regenStartTimer == _regenStartTarget)
                {
                    _regenStartTimer = 0;
                    _regenStartTarget = 0;
                    _regeneratingHealth = true;
                }

                if (_regeneratingHealth)
                {
                    // Regen rate scales with MaxHP (20 is baseline), so more MaxHP = faster regen
                    var regenScale = 20f / Data.MaxHP;
                    var regenIncrement = Math.Abs(owningPlayer.velocity.X) == 0 ? 1.25f : 0.5f;
                    _regenTimer += regenIncrement / regenScale;

                    if (_regenTimer >= 30f)
                    {
                        Data.Heal(1);
                        _regenTimer = 0f;
                    }
                }
            }
            else
            {
                CancelRegen();
            }
        }

        // Handle decreasing of visual immunity frames
        if (_visualImmunityFrames > 0) _visualImmunityFrames--;

        // Handle disabling custom sprite direction
        if (CustomSpriteDirection.HasValue && Projectile.velocity.X != 0 && _activeAttackTimer <= 0)
        {
            CustomSpriteDirection = null;
            Projectile.netUpdate = true;
        }

        // Play cry sound effect if queued
        if (_cryTimer > 0)
        {
            _cryTimer--;
            if (_cryTimer == 0 && Data != null && Main.netMode != NetmodeID.Server)
            {
                var cry = new SoundStyle("Terramon/Sounds/Cries/" + Data.InternalName)
                    { Volume = 0.15f };
                SoundEngine.PlaySound(cry, Projectile.position);
            }
        }

        if (isShiny) ShinyEffect();
    }

    private NPC FindClosestNPC(float maxDetectDistance)
    {
        NPC closestNPC = null;

        var sqrMaxDetectDistance = maxDetectDistance * maxDetectDistance;

        foreach (var target in Main.ActiveNPCs)
        {
            if (!IsValidTarget(target)) continue;

            var sqrDistanceToTarget = Vector2.DistanceSquared(target.Center, Projectile.Center);

            if (!(sqrDistanceToTarget < sqrMaxDetectDistance)) continue;
            sqrMaxDetectDistance = sqrDistanceToTarget;
            closestNPC = target;
        }

        return closestNPC;
    }

    private bool IsValidTarget(NPC target)
    {
        return target.CanBeChasedBy() &&
               Collision.CanHit(Projectile.Center, 1, 1, target.position, target.width, target.height);
    }

    public void RealtimeHit()
    {
        CancelRegen();
        _visualImmunityFrames = 24;
    }

    private void CancelRegen()
    {
        _regeneratingHealth = false;
        _regenTimer = 0f;
        _regenStartTimer = 0;

        // Only set new target if we don't already have one from recent damage
        if (_regenStartTarget == 0)
            _regenStartTarget = Main.rand.Next(300, 600); // 5-10 seconds
    }

    private static void CreateHitEffect(Vector2 hitPosition)
    {
        var particleCount = Main.rand.Next(12, 18);

        for (var i = 0; i < particleCount; i++)
        {
            var angle = (float)(i * 2 * Math.PI / particleCount);

            angle += Main.rand.NextFloat(-0.3f, 0.3f);

            var speed = Main.rand.NextFloat(1.5f, 2.5f);
            var velocity = new Vector2(
                (float)Math.Cos(angle) * speed,
                (float)Math.Sin(angle) * speed
            );

            var dustColor = Main.rand.Next(6) switch
            {
                0 => Color.Red,
                1 => Color.Orange,
                2 => Color.Yellow,
                3 => Color.OrangeRed,
                4 => Color.Crimson,
                _ => Color.Gold
            };

            var spawnPos = hitPosition + new Vector2(
                Main.rand.NextFloat(-3, 3),
                Main.rand.NextFloat(-3, 3)
            );

            var dust = Dust.NewDustDirect(
                spawnPos,
                0, 0,
                DustID.PortalBolt,
                velocity.X,
                velocity.Y,
                100,
                dustColor,
                Main.rand.NextFloat(0.8f, 1.3f) // Varied scale for depth
            );

            dust.noGravity = true;
            dust.fadeIn = Main.rand.NextFloat(0.2f, 0.5f);

            if (i % 3 == 0)
            {
                dust.velocity *= 0.6f;
                dust.scale *= 1.2f;
            }
        }

        for (var i = 0; i < Main.rand.Next(3, 6); i++)
        {
            var flashDust = Dust.NewDustDirect(
                hitPosition + new Vector2(Main.rand.NextFloat(-2, 2), Main.rand.NextFloat(-2, 2)),
                0, 0,
                DustID.PortalBolt,
                Main.rand.NextFloat(-0.5f, 0.5f),
                Main.rand.NextFloat(-0.5f, 0.5f),
                100,
                Color.LightYellow,
                Main.rand.NextFloat(1.0f, 1.5f)
            );

            flashDust.noGravity = true;
            flashDust.fadeIn = 0.1f;
        }

        for (var i = 0; i < Main.rand.Next(4, 8); i++)
        {
            var sparkAngle = Main.rand.NextFloat(0, (float)(2 * Math.PI));
            var sparkSpeed = Main.rand.NextFloat(1f, 2f);

            var sparkVelocity = new Vector2(
                (float)Math.Cos(sparkAngle) * sparkSpeed,
                (float)Math.Sin(sparkAngle) * sparkSpeed
            );

            var spark = Dust.NewDustDirect(
                hitPosition,
                0, 0,
                DustID.PortalBolt,
                sparkVelocity.X,
                sparkVelocity.Y,
                100,
                Color.Orange,
                Main.rand.NextFloat(0.4f, 0.8f)
            );

            spark.noGravity = false;
            spark.fadeIn = Main.rand.NextFloat(0.4f, 0.8f);
        }
    }

    private float CalculateKnockback()
    {
        // Base attack of 255 = 2.5x multiplier
        var multiplier = 1f + Schema.BaseStats.Attack / 255f * 1.5f;
        return 2f * multiplier;
    }

    public override void PostAI()
    {
        // Call FindFrame delegate to determine the frame of the pet (set by behaviour components)
        FindFrame?.Invoke(this);
    }

    public const float DistanceFromFoe = 128f;

    public void ConfrontFoe(BattleInstance battle = null)
    {
        if (battle is null)
        {
            CustomTargetPosition = null;
            return;
        }

        Vector2 foePos;
        int foeDir;

        PokemonNPC wild = battle.WildNPC;
        if (wild != null)
        {
            foePos = wild.NPC.Center;
            foeDir = wild.NPC.direction;
        }
        else
        {
            PokemonPet foePet = battle.Player2.ActivePetProjectile;
            foePos = foePet.Projectile.Center;
            foeDir = foePet.Projectile.direction;
        }

        float xTarget = foePos.X + (foeDir * DistanceFromFoe);
        CustomTargetPosition = new Vector2(xTarget, foePos.Y);
    }

    private void ShinyEffect()
    {
        if (_mainTexture == null) return;
        
        // Disable shiny effect lighting for Haunter and Gengar
        if (ID != NationalDexID.Haunter && ID != NationalDexID.Gengar)
            Lighting.AddLight(Projectile.Center, 0.5f, 0.5f, 0.5f);
        
        _shinySparkleTimer++;
        
        if (_shinySparkleTimer < 15) return;
        
        for (var i = 0; i < 2; i++)
        {
            // Spoof projectile width and height to match the texture size
            var oldWidth = Projectile.width;
            var oldHeight = Projectile.height;
            Projectile.width = (int)(_mainTexture.Value.Width * 0.8f);
            Projectile.height = (int)((float)_mainTexture.Value.Height / Main.projFrames[Type] * 0.8f);

            var dust = Dust.NewDustDirect(
                Projectile.position + new Vector2(Main.rand.NextFloat(-3, 3), Main.rand.NextFloat(-3, 3)) +
                new Vector2(oldWidth - Projectile.width, oldHeight - Projectile.height),
                Projectile.width,
                Projectile.height, DustID.TreasureSparkle);
            dust.velocity = Projectile.velocity;
            dust.noGravity = true;
            dust.scale *= 1f + Main.rand.NextFloat(-0.03f, 0.03f);

            // Reset projectile width and height to original values
            Projectile.width = oldWidth;
            Projectile.height = oldHeight;
        }

        _shinySparkleTimer = 0;
    }
}