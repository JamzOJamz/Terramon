using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Newtonsoft.Json.Linq;
using ReLogic.Content;
using Terramon.Content.Buffs;
using Terramon.Content.Dusts;
using Terramon.Core.Abstractions;
using Terramon.Core.Loaders;
using Terramon.Core.ProjectileComponents;
using Terraria.DataStructures;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;

namespace Terramon.Content.Projectiles;

[Autoload(false)]
public class PokemonPet(ushort id, DatabaseV2.PokemonSchema schema) : ModProjectile, IPokemonEntity
{
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
            ((PokemonPet)projectile.ModProjectile).Data = activePokemon; // Set the pet's data to the player's active Pokémon (hacky)
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
    
    private Asset<Texture2D> _mainTexture;
    public int CustomFrameCounter;
    private int? _customSpriteDirection;
    private int _shinySparkleTimer;
    private ushort _cachedID;

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
        _customSpriteDirection = direction;
        Projectile.position.X += direction * 40;

        var dust = ModContent.DustType<SummonCloud>();
        var mainPosition = new Vector2(Projectile.position.X - Projectile.width / 2f, Projectile.position.Y - Projectile.height / 2f);
        Dust.NewDust(new Vector2(mainPosition.X, mainPosition.Y + 4), Projectile.width, Projectile.height, dust);
        Dust.NewDust(new Vector2(mainPosition.X, mainPosition.Y - 4), Projectile.width, Projectile.height, dust);
        Dust.NewDust(new Vector2(mainPosition.X + 4, mainPosition.Y), Projectile.width, Projectile.height, dust);
        Dust.NewDust(new Vector2(mainPosition.X - 4, mainPosition.Y), Projectile.width, Projectile.height, dust);
        Dust.NewDust(new Vector2(mainPosition.X + 2, mainPosition.Y + 2), Projectile.width, Projectile.height, dust);
        Dust.NewDust(new Vector2(mainPosition.X + 2, mainPosition.Y - 2), Projectile.width, Projectile.height, dust);
        Dust.NewDust(new Vector2(mainPosition.X - 2, mainPosition.Y + 2), Projectile.width, Projectile.height, dust);
        Dust.NewDust(new Vector2(mainPosition.X - 2, mainPosition.Y - 2), Projectile.width, Projectile.height, dust);
        
        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        var owningPlayer = Main.player[Projectile.owner];
        Data = owningPlayer.GetModPlayer<TerramonPlayer>().GetActivePokemon();
        _cachedID = ID;
        Projectile.netUpdate = true;
    }
    
    public delegate void CustomFindFrame(PokemonPet proj);
    
    public CustomFindFrame FindFrame;

    public override bool PreDraw(ref Color lightColor)
    {
        if (_mainTexture == null)
        {
            var pathBuilder = new StringBuilder(Texture);

            if (PokemonEntityLoader.HasGenderDifference[ID - 1] && Data?.Gender == Gender.Female)
                pathBuilder.Append('F');
            if (!string.IsNullOrEmpty(Data?.Variant))
                pathBuilder.Append('_').Append(Data.Variant);
            if (Data is { IsShiny: true })
                pathBuilder.Append("_S");

            var path = pathBuilder.ToString();
            _mainTexture = ModContent.Request<Texture2D>(path);
        }
        
        // Call FindFrame delegate to determine the frame of the pet (set by behaviour components)
        FindFrame?.Invoke(this);
        
        var projFrameCount = Main.projFrames[Type];
        var sourceRect = _mainTexture.Frame(1, projFrameCount, frameY: Projectile.frame);
        var frameSize = sourceRect.Size();
        var effects = _customSpriteDirection.HasValue ? _customSpriteDirection.Value == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None : Projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        
        Main.EntitySpriteDraw(_mainTexture.Value,
            Projectile.Center - Main.screenPosition +
            new Vector2(0f, Projectile.gfxOffY + DrawOriginOffsetY + (int)Math.Ceiling(Projectile.height / 2f) + 4), sourceRect, lightColor,
            Projectile.rotation,
            frameSize / new Vector2(2, 1), Projectile.scale, effects);

        return false;
    }

    public override void AI()
    {
        var owningPlayer = Main.player[Projectile.owner];
        var activePokemon = owningPlayer.GetModPlayer<TerramonPlayer>().GetActivePokemon();

        // Handles keeping the pet alive until it should despawn
        if (!owningPlayer.dead && owningPlayer.HasBuff(ModContent.BuffType<PokemonCompanion>()) &&
            activePokemon == Data && activePokemon.ID == _cachedID) Projectile.timeLeft = 2;
        
        if (Projectile.velocity.X != 0) _customSpriteDirection = null;
        
        if (Data is { IsShiny: true }) ShinyEffect();
    }
    
    private void ShinyEffect()
    {
        Lighting.AddLight(Projectile.Center, 0.5f, 0.5f, 0.5f);
        _shinySparkleTimer++;
        if (_shinySparkleTimer < 12) return;
        for (var i = 0; i < 2; i++)
        {
            var dust = Dust.NewDustDirect(
                Projectile.position + new Vector2(Main.rand.NextFloat(-3, 3), Main.rand.NextFloat(-3, 3)), Projectile.width,
                Projectile.height, DustID.TreasureSparkle);
            dust.velocity = Projectile.velocity;
            dust.noGravity = true;
            dust.scale *= 1f + Main.rand.NextFloat(-0.03f, 0.03f);
        }

        _shinySparkleTimer = 0;
    }
}