using System;
using System.IO;
using Terramon.Content.Configs;
using Terramon.Content.NPCs.Pokemon;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.Localization;

namespace Terramon.Content.Items.Mechanical;

internal abstract class BasePkballProjectile : ModProjectile
{
    private float animSpeedMultiplier = 1;
    private int bounces = 5;
    private PokemonNPC capture; //Type of pokemon to be caught
    private float catchRandom = -1;
    private int catchTries = 3;
    private bool caught;
    private bool hasCalculatedCapture;

    private bool hasContainedLocal;
    private float rotation;
    private float rotationVelocity;
    public virtual int pokeballCapture => ModContent.ItemType<BasePkballItem>();
    public virtual float catchModifier { get; set; }

    public override string Texture => "Terramon/Assets/Items/PokeBalls/" + GetType().Name;

    public override LocalizedText DisplayName =>
        Language.GetText($"Terramon.Items.{GetType().Name.Replace("Projectile", "Item")}.DisplayName");

    public override void SetStaticDefaults()
    {
        Main.projFrames[Projectile.type] = 4; //frames in spritesheet
        Projectile.tileCollide = true; // Can the projectile collide with tiles?
    }

    public override void SetDefaults()
    {
        Projectile.width = 18; //Set to size of spritesheet
        Projectile.height = 18;
        //Projectile.damage = 1;
        Projectile.aiStyle = -1; //aiStyle -1 so no vanilla styles interfere with custom ai
        Projectile.penetrate = -1; //How many npcs to collide before being deleted (-1 makes this infinite)
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        //return base.OnTileCollide(oldVelocity);
        if (bounces > 0)
        {
            bounces -= 1;
            Projectile.velocity.Y = oldVelocity.Y *= -0.7f;
            Projectile.velocity.X = oldVelocity.X *= 0.5f;

            SoundEngine.PlaySound(new SoundStyle("Terramon/Assets/Audio/Sounds/pkball_bounce"), Projectile.position);

            if (Projectile.velocity.Length() < 1.5f)
                bounces = 0;
        }
        else if (Projectile.ai[1] == 0)
        {
            SoundEngine.PlaySound(SoundID.Dig, Projectile.position);
            for (var i = 0; i < 14; i++)
            {
                var d = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Marble);
                d.noGravity = true;
            }

            return true;
        }
        else if (Projectile.ai[1] == 1 && bounces == 0) //only randomise catch number and play sound once
        {
            //caught = CatchPokemonChances(capture);
            if (Main.player[Projectile.owner].whoAmI ==
                Main.myPlayer) //Generate new catch chance, (will switch pokeball to catching anim when value is recieved by clients)
                catchRandom = Main.rand.NextFloat(0, 1);
            SoundEngine.PlaySound(new SoundStyle("Terramon/Assets/Audio/Sounds/pkball_bounce"), Projectile.position);
            bounces = -1;
        }

        return false;
    }

    public override void SendExtraAI(BinaryWriter writer)
    {
        var captureId = -1;
        if (capture != null)
            captureId = capture.NPC.whoAmI; //BinaryWriter can't send whole NPC, so we send the NPC's number instead

        writer.Write(catchRandom);
        writer.Write(captureId);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        var catchRandomVar = reader.ReadSingle();
        var captureId = reader.ReadInt32();

        if (catchRandom == -1)
            catchRandom = catchRandomVar;
        if (capture == null && captureId != -1) //only change capture if none exists
            capture = Main.npc[captureId].ModNPC as PokemonNPC;
    }

    public override void OnSpawn(IEntitySource source)
    {
        Projectile.velocity += Main.player[Projectile.owner].velocity * 0.75f;
        if (Sandstorm.Happening && Main.player[Projectile.owner].ZoneDesert)
            Projectile.velocity.X -= 1.75f;
    }

    public override void AI()
    {
        if (Projectile.shimmerWet)
        {
            if (Type != ModContent.ProjectileType<PremierBallProjectile>())
            {
                var shimmer = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position,
                    new Vector2(Projectile.velocity.X, Projectile.velocity.Y * -0.8f),
                    ModContent.ProjectileType<PremierBallProjectile>(), 0, 0, Projectile.owner);
                var shimmerProj = Main.projectile[shimmer].ModProjectile as BasePkballProjectile;
                shimmerProj.catchModifier = catchModifier;
                Projectile.Kill();
            }
            else if (bounces > 0 && Projectile.velocity.Y < 0)
            {
                Projectile.shimmerWet = false;
                Projectile.velocity.Y *= -0.8f;
                SoundEngine.PlaySound(new SoundStyle("Terramon/Assets/Audio/Sounds/pkball_bounce"),
                    Projectile.position);
                bounces -= 1;
            }
        }


        //Main.NewText(catchRandom, Color.Pink);
        if (ModContent.GetInstance<GameplayConfig>().FastAnimations)
            animSpeedMultiplier = 0.65f;

        Projectile.damage = 1;
        Projectile.ai[0]++;
        ///Projectile.spriteDirection = Projectile.direction;
        ///
        if (hasContainedLocal == false && capture != null)
            HitPkmn(capture.NPC);
        if (catchRandom > -1 && !hasCalculatedCapture)
        {
            hasCalculatedCapture = true;
            caught = CatchPokemonChances(capture, catchRandom);
            Projectile.ai[0] = 0;
            Projectile.ai[1] = 2;
        }

        if (Projectile.ai[1] == 0)
        {
            Projectile.frame = (int)Frame.Throw; //At state 1 should use throw sprite
            Projectile.rotation +=
                Projectile.velocity.X *
                0.05f; //Spin in air (feels better than static) based on current velocity so it slows down once it hits the ground
            if (Projectile.ai[0] >= 10f)
            {
                Projectile.ai[0] =
                    10f; //Wait 10 frames before apply gravity, then keep timer at 10 so it gets constantly applied
                Projectile.velocity.Y = Projectile.velocity.Y + 0.25f; //(positive Y value makes projectile go down)
            }
        }
        else if (Projectile.ai[1] == 1)
        {
            capture?.Destroy(); //Destroy Pokemon NPC

            if (Projectile.ai[0] <
                35 * animSpeedMultiplier) //Stay still (no velocity) if 50 frames havent passed yet (60fps)
            {
                Projectile.frame = (int)Frame.Catch;
                Projectile.rotation = rotation;
                Projectile.velocity.X = 0;
                Projectile.velocity.Y = 0;
            }
            else
            {
                Projectile.frame = (int)Frame.Capture;
                Projectile.rotation = 0;
                Projectile.velocity.Y +=
                    0.25f; //Add to Y velocity so projectile moves downwards (i subtracted this in testing - the pokeball flew into the sky and disappeared)
            }
        }
        else if (Projectile.ai[1] == 2)
        {
            Projectile.rotation += rotationVelocity;
            if (Projectile.ai[0] >= 75 * animSpeedMultiplier)
            {
                //Main.NewText(catchTries, Color.CornflowerBlue);
                if (catchTries == 0 || ModContent.GetInstance<GameplayConfig>().FastAnimations)
                {
                    if (caught)
                    {
                        Projectile.frame = (int)Frame.CaptureComplete;
                        SoundEngine.PlaySound(new SoundStyle("Terramon/Assets/Audio/Sounds/pkball_catch"),
                            Projectile.position);
                        Projectile.ai[1] = 3;
                        Projectile.ai[0] = 0;
                    }
                    else
                    {
                        ReleasePokemon();
                        Projectile.Kill();
                    }
                }
                else
                {
                    catchTries -= 1;
                    rotationVelocity = 0.2f;
                    SoundEngine.PlaySound(new SoundStyle("Terramon/Assets/Audio/Sounds/pkball_shake"),
                        Projectile.position);
                }

                Projectile.ai[0] = 0;
            }
            else if (Math.Abs(Projectile.rotation) < 0.1f)
            {
                Projectile.rotation = 0;
                rotationVelocity = 0;
            }
            else if (rotationVelocity > -0.2f)
            {
                rotationVelocity -= 0.05f;
            }
        }
        else if (Projectile.ai[1] == 3)
        {
            if (Projectile.ai[0] == 1)
                for (var i = 0; i < 3; i++)
                    Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.YellowStarDust);
            if (Projectile.ai[0] >= 90f * animSpeedMultiplier)
                PokemonCatchSuccess();
        }
    }

    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        modifiers.FinalDamage.Scale(0);
        //damage = 0;
        //if (capture == null)
        //Main.NewText($"{target.ModNPC != null}, {target.ModNPC is PokemonNPC}, {(target.ModNPC.Type)}");

        if (capture == null && target.ModNPC != null && target.ModNPC is PokemonNPC)
            HitPkmn(target);
    }

    public virtual bool CatchPokemonChances(PokemonNPC capture, float random)
    {
        catchModifier = ChangeCatchModifier(capture); //Change modifier (can take into account values like pokemon type)

        var
            catchChance =
                0.5f; //Terramon.Database.GetPokemon(capture.useId) * 0.85f; //would / 3 to match game but we can't damage pokemon so that would be too hard
        //TODO: pull actual data from pokemon when possible
        //Main.NewText($"chance {catchChance * catchModifier}, random {random}");
        if (catchRandom < catchChance * catchModifier)
            return true;

        var split = (1 - catchChance) /
                    4; //Determine amount of times pokeball will rock (based on closeness to successful catch)

        if (random < catchChance + split * 1)
            catchTries = 3;
        else if (random < catchChance + split * 2)
            catchTries = 2;
        else if (random < catchChance + split * 3)
            catchTries = 1;
        else
            catchTries = 0;

        return false;
    }

    public virtual float ChangeCatchModifier(PokemonNPC capture)
    {
        return catchModifier;
    }

    private void PokemonCatchSuccess()
    {
        const byte level = 5;

        //TODO: add level from pokemonnpc
        TerramonPlayer.LocalPlayer.AddPartyPokemon(new PokemonData(capture.useId, level)
        {
            IsShiny = capture.isShiny
        });

        SoundEngine.PlaySound(new SoundStyle("Terramon/Assets/Audio/Sounds/pkball_catch_pla"));
        Main.NewText(Language.GetTextValue("Mods.Terramon.Misc.CatchSuccess", level, capture.DisplayName));
        Projectile.Kill();
    }

    private void HitPkmn(NPC target)
    {
        hasContainedLocal = true;
        //Main.NewText($"Contain success", Color.Orange);
        capture = (PokemonNPC)target.ModNPC;

        // Register as seen in the player's Pokedex
        var ownerPlayer = Main.player[Projectile.owner].GetModPlayer<TerramonPlayer>();
        var pokedex = ownerPlayer.GetPokedex();
        if (pokedex.Entries.TryGetValue(capture.useId, out var status) && status == PokedexEntryStatus.Undiscovered)
            ownerPlayer.UpdatePokedex(capture.useId, PokedexEntryStatus.Seen);

        Projectile.ai[1] = 1;
        Projectile.ai[0] = 0;
        if (!ModContent.GetInstance<GameplayConfig>().FastAnimations)
            bounces = 5; //If fast animation disabled, reset bounces to 5 (so the animation isn't shorter if it's already hit the ground)
        else if (bounces > 2)
            bounces = 2;

        rotation = (target.Center - Projectile.Center).SafeNormalize(Vector2.Zero)
            .ToRotation(); //Rotate to face Pokemon

        if (Math.Abs(rotation) > 1.5) //Stuff to make sure Pokeball doesn't appear upside down or reversed
        {
            if (rotation > 0)
                rotation -= 3;
            else
                rotation += 3;

            Projectile.spriteDirection = 1;
        }
        else
        {
            Projectile.spriteDirection = -1;
        }

        Projectile.netUpdate = true;
    }

    public void ReleasePokemon()
    {
        if (capture != null)
        {
            SoundEngine.PlaySound(new SoundStyle("Terramon/Assets/Audio/Sounds/pkmn_spawn"), Projectile.position);
            var source = Entity.GetSource_FromThis();

            var e = new NPC();
            var newNPC =
                NPC.NewNPC(source, (int)Projectile.Center.X, (int)Projectile.Center.Y,
                    capture.Type); // spawn a new NPC at the new position
            var newPoke = (PokemonNPC)Main.npc[newNPC].ModNPC;
            newPoke.isShiny = capture.isShiny;
            //newPoke.isShimmer = capture.isShimmer;
            //newPoke.level = capture.level;
            //newPoke.catchAttempts = capture.catchAttempts + 1;
            //Main.NewText($"Catch attempts: {newPoke.catchAttempts}", Color.Firebrick);
            capture = null;
        }
    }

    private enum Frame //Here we label all of the frames in the spritesheet for better readability
    {
        Throw,
        Catch,
        Capture,
        CaptureComplete
    }
}