using System.Reflection;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Linq;
using ReLogic.Content;
using Terramon.Content.Buffs;
using Terramon.Content.Configs;
using Terramon.Content.Dusts;
using Terramon.Core.Abstractions;
using Terramon.Core.Loaders;
using Terramon.Core.ProjectileComponents;
using Terramon.Helpers;
using Terramon.ID;
using Terraria.DataStructures;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;

namespace Terramon.Content.Projectiles;

[Autoload(false)]
public class PokemonPet(ushort id, DatabaseV2.PokemonSchema schema) : ModProjectile, IPokemonEntity
{
    public delegate void CustomFindFrame(PokemonPet proj);

    private ushort _cachedID;
    private Asset<Texture2D> _mainTexture;
    private int _shinySparkleTimer;
    public int CustomFrameCounter;
    public int? CustomSpriteDirection;
    public CustomFindFrame FindFrame;

    static PokemonPet()
    {
        // Don't run this on the server
        if (Main.dedServ) return;

        // Draw the player's active Pokémon pet in the character select UI
        On_UICharacter.ctor += (orig, self, player, animated, panel, scale, clone) =>
        {
            orig(self, player, animated, panel, scale, clone);

            // Get the pet projectiles from the UICharacter
            ref var petProjectiles = ref GetPetProjectiles(self);
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

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_petProjectiles")]
        static extern ref Projectile[] GetPetProjectiles(UICharacter instance);
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
        // Move ahead of player
        var direction = Main.player[Projectile.owner].direction;
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

        var owningPlayer = Main.player[Projectile.owner];
        Data = owningPlayer.GetModPlayer<TerramonPlayer>().GetActivePokemon();
        _cachedID = ID;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        _mainTexture ??= PokemonEntityLoader.RequestTexture(this);

        if (Projectile.isAPreviewDummy)
            // Call FindFrame delegate to determine the frame of the pet (set by behaviour components)
            FindFrame?.Invoke(this);

        var projFrameCount = Main.projFrames[Type];
        var drawPos = Projectile.Center - Main.screenPosition +
                      new Vector2(0f,
                          Projectile.gfxOffY + DrawOriginOffsetY + (int)Math.Ceiling(Projectile.height / 2f) + 4);
        var sourceRect = _mainTexture.Frame(1, projFrameCount, frameY: Projectile.frame);
        var frameSize = sourceRect.Size();
        var origin = frameSize / new Vector2(2, 1);
        var effects = CustomSpriteDirection.HasValue
            ? CustomSpriteDirection.Value == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None
            : Projectile.spriteDirection == -1
                ? SpriteEffects.FlipHorizontally
                : SpriteEffects.None;

        // Draw name on mouse hover
        if (!Projectile.isAPreviewDummy && ModContent.GetInstance<ClientConfig>().ShowPetNameOnHover)
        {
            var originOffsetDrawPos = drawPos - origin;
            var drawRect = new Rectangle((int)originOffsetDrawPos.X, (int)originOffsetDrawPos.Y, (int)frameSize.X,
                (int)frameSize.Y);
            if (drawRect.Contains(Main.MouseScreen.ToPoint()))
            {
                var mouseTextMult = Main.mouseTextColor / 255f;
                var subColor = new Color((byte)(198f * mouseTextMult), (byte)(198f * mouseTextMult),
                    (byte)(209f * mouseTextMult));
                Main.instance.MouseText(
                    $"{Data.DisplayName}: {Data.HP}/{Data.MaxHP}\n[c/{subColor.ToHexString()}:{Language.GetTextValue("Mods.Terramon.Misc.Trainer")}: {Main.player[Projectile.owner].name}]");
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
        Data ??= new PokemonData
        {
            ID = ID,
            Level = 5
        };
        Data.NetRead(reader);
        CustomSpriteDirection = reader.ReadBoolean() ? reader.ReadInt32() : null;
    }

    public override void AI()
    {
        var isShiny = Data is { IsShiny: true };

        if (ID == NationalDexID.Gastly)
        {
            var lightColor = isShiny
                ? new Vector3(63 / 255f, 109 / 255f, 239 / 255f)
                : new Vector3(169 / 255f, 124 / 255f, 226 / 255f);
            Lighting.AddLight(Projectile.Center, lightColor * 1.25f * (Main.raining || Projectile.wet ? 1 - 0.5f : 1));
        }

        var owningPlayer = Main.player[Projectile.owner];
        var activePokemon = owningPlayer.GetModPlayer<TerramonPlayer>().GetActivePokemon();

        // Handles keeping the pet alive until it should despawn
        if (!owningPlayer.dead && owningPlayer.HasBuff(ModContent.BuffType<PokemonCompanion>()) &&
            activePokemon == Data && activePokemon.ID == _cachedID) Projectile.timeLeft = 2;

        if (CustomSpriteDirection.HasValue && Projectile.velocity.X != 0)
        {
            CustomSpriteDirection = null;
            Projectile.netUpdate = true;
        }

        if (isShiny) ShinyEffect();
    }

    public override void PostAI()
    {
        // Call FindFrame delegate to determine the frame of the pet (set by behaviour components)
        FindFrame?.Invoke(this);
    }

    private void ShinyEffect()
    {
        if (_mainTexture == null) return;
        // Disable shiny effect lighting for Haunter and Gengar
        if (ID != NationalDexID.Haunter && ID != NationalDexID.Gengar)
            Lighting.AddLight(Projectile.Center, 0.5f, 0.5f, 0.5f);
        _shinySparkleTimer++;
        if (_shinySparkleTimer < 12) return;
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