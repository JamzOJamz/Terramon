using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Hjson;
using Newtonsoft.Json.Linq;
using ReLogic.Content;
using Terramon.Content.AI;
using Terramon.Content.Dusts;
using Terramon.Content.Items.Mechanical;
using Terramon.Core.NPCComponents;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;

namespace Terramon.Content.NPCs.Pokemon;

[Autoload(false)]
public class PokemonNPC : ModNPC
{
    private static Dictionary<ushort, JToken> SchemaCache;
    public readonly ushort useId;
    private readonly string useName;
    private Asset<Texture2D> glowTexture;
    private bool isDestroyed;
    public bool isShiny;
    private int shinySparkleTimer;
    public string variant = null;

    public PokemonNPC(ushort useId, string useName)
    {
        this.useId = useId;
        this.useName = useName;
    }

    protected override bool CloneNewInstances => true;

    public override string Name => useName + "NPC";

    public override LocalizedText DisplayName => Terramon.DatabaseV2.GetLocalizedPokemonName(useId);

    private AIController Behaviour { get; set; }

    public override string Texture => "Terramon/Assets/Pokemon/" + useName;

    private bool ShouldManuallyDraw => isShiny || variant != null || glowTexture != null;

    public override void SetDefaults()
    {
        NPC.defense = int.MaxValue;
        NPC.lifeMax = 100;
        NPC.HitSound = SoundID.NPCHit1;
        NPC.value = 0f;
        NPC.knockBackResist = 0.75f;
        NPC.despawnEncouraged = true;
        NPC.friendly = true;

        // Load glowmask texture if it exists.
        if (ModContent.RequestIfExists<Texture2D>("Terramon/Assets/Pokemon/" + useName + "_Glow", out var texture))
            glowTexture = texture;

        // TODO: Optimize.
        if (!SchemaCache.TryGetValue(useId, out var npcSchema))
        {
            var hjsonStream = Mod.GetFileStream($"Content/Pokemon/{useName}.hjson");
            using var hjsonReader = new StreamReader(hjsonStream);
            var jsonText = HjsonValue.Load(hjsonReader).ToString();
            hjsonReader.Close();
            var schema = JObject.Parse(jsonText);
            if (!schema.ContainsKey("NPC")) return;
            npcSchema = schema["NPC"];
            SchemaCache.Add(useId, npcSchema);
        }

        var mi = typeof(NPCComponentExtensions).GetMethod("EnableComponent");
        foreach (var component in npcSchema!.Children<JProperty>())
        {
            var componentType = Mod.Code.GetType($"Terramon.Content.NPCs.NPC{component.Name}");
            if (componentType == null) continue;
            var enableComponentRef = mi!.MakeGenericMethod(componentType);
            var instancedComponent = enableComponentRef.Invoke(null, new object[] { NPC, null });
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
        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        var spawningPlayer = Player.FindClosest(NPC.Center, NPC.width, NPC.height);
        isShiny = Terramon.RollShiny(Main.player[spawningPlayer]);
        NPC.netUpdate = true;
    }

    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        return !ShouldManuallyDraw;
    }

    public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        if (!ShouldManuallyDraw) return;

        var path = Texture;
        if (variant != null)
            path += variant;
        if (isShiny)
            path += "_S";

        var texture = ModContent.Request<Texture2D>(path).Value;
        var effects = NPC.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        spriteBatch.Draw(texture, NPC.Center - screenPos + new Vector2(0f, NPC.gfxOffY),
            NPC.frame, drawColor, NPC.rotation,
            NPC.frame.Size() / 2f, NPC.scale, effects, 0f);

        if (glowTexture == null) return;
        spriteBatch.Draw(glowTexture.Value, NPC.Center - screenPos + new Vector2(0f, NPC.gfxOffY),
            NPC.frame, Color.White, NPC.rotation,
            NPC.frame.Size() / 2f, NPC.scale, effects, 0f);
    }

    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(isShiny);
        Behaviour?.SendExtraAI(writer);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        isShiny = reader.ReadBoolean();
        Behaviour?.ReceiveExtraAI(reader);
    }

    public override void AI()
    {
        if (NPC.life < NPC.lifeMax) NPC.life = NPC.lifeMax;
        if (isShiny) ShinyEffect();
        Behaviour?.AI();
    }

    private void ShinyEffect()
    {
        Lighting.AddLight(NPC.position, 0.5f, 0.5f, 0.5f);
        shinySparkleTimer++;
        if (shinySparkleTimer < 6) return;
        for (var i = 0; i < 2; i++)
        {
            const short dustType = 204;
            var dust = Dust.NewDustDirect(
                NPC.position + new Vector2(Main.rand.NextFloat(-3, 3), Main.rand.NextFloat(-3, 3)), NPC.width,
                NPC.height, dustType);
            dust.velocity = NPC.velocity;
            dust.noGravity = true;
            dust.scale *= 1f + Main.rand.NextFloat(-0.03f, 0.03f);
        }

        shinySparkleTimer = 0;
    }

    public override void FindFrame(int frameHeight)
    {
        Behaviour?.FindFrame(frameHeight);
    }

    public override bool? CanBeHitByProjectile(Projectile projectile)
    {
        return projectile.ModProjectile is BasePkballProjectile;
    }

    public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position)
    {
        return false;
    }

    public void Destroy()
    {
        if (isDestroyed) return;

        //TODO: Add shader animation (I already made this shader in my mod source but I couldn't figure out how to apply it properly)
        var dust = ModContent.DustType<SummonCloud>();
        Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y), NPC.width, NPC.height, dust, 0, 1);
        Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y), NPC.width, NPC.height, dust, 0, -1);
        Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y), NPC.width, NPC.height, dust, 1);
        Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y), NPC.width, NPC.height, dust, -1);
        Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y), NPC.width, NPC.height, dust, 0.5f, 0.5f);
        Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y), NPC.width, NPC.height, dust, 0.5f, -0.5f);
        Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y), NPC.width, NPC.height, dust, -0.5f, 0.5f);
        Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y), NPC.width, NPC.height, dust, -0.5f, -0.5f);

        NPC.netUpdate = true;
        NPC.active = false;
        isDestroyed = true;
    }

    public override void Load()
    {
        SchemaCache = new Dictionary<ushort, JToken>();
    }

    public override void Unload()
    {
        SchemaCache = null;
    }
}