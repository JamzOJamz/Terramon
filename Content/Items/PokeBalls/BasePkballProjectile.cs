using System;
using System.IO;
using Terramon.Content.Configs;
using Terramon.Content.NPCs.Pokemon;
using Terramon.ID;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.Localization;

namespace Terramon.Content.Items.PokeBalls;

internal abstract class BasePkballProjectile : ModProjectile
{
    private float _animSpeedMultiplier = 1;
    private int _bounces = 5;
    private PokemonNPC _capture; //Type of pokemon to be caught
    private float _catchRandom = -1;
    private int _catchTries = 3;
    private bool _caught;
    private bool _hasCalculatedCapture;

    private bool _hasContainedLocal;
    private float _rotation;
    private bool _rotationDirection;
    private float _rotationVelocity;
    protected virtual int PokeballItem => ModContent.ItemType<BasePkballItem>();
    protected virtual float CatchModifier { get; set; }

    /// <summary>
    ///     The chance (1/x) to drop this Poké Ball's item when the projectile is destroyed without catching a Pokémon. Default
    ///     is 3.
    /// </summary>
    protected virtual int DropItemChance => 3;

    public override string Texture => "Terramon/Assets/Items/PokeBalls/" + GetType().Name;

    public override LocalizedText DisplayName =>
        Language.GetText($"Terramon.Items.{GetType().Name.Replace("Projectile", "Item")}.DisplayName");

    public override void SetStaticDefaults()
    {
        Main.projFrames[Type] = 4; //frames in spritesheet
        Projectile.tileCollide = true; // Can the projectile collide with tiles?
    }

    public override void SetDefaults()
    {
        Projectile.width = 14; //Set to size of spritesheet
        Projectile.height = 14;
        //Projectile.damage = 1;
        Projectile.friendly = true;
        Projectile.aiStyle = -1; //aiStyle -1 so no vanilla styles interfere with custom ai
        Projectile.penetrate = -1; //How many npcs to collide before being deleted (-1 makes this infinite)
    }

    public override bool PreDraw(ref Color lightColor)
    {
        var texture = TextureAssets.Projectile[Type].Value;

        var drawOrigin = new Vector2(texture.Width * 0.5f, 24 * 0.5f);
        var drawPos = Projectile.position - Main.screenPosition + drawOrigin + new Vector2(Projectile.gfxOffY);
        Main.EntitySpriteDraw(texture, drawPos - new Vector2(5, 5), new Rectangle(0, Projectile.frame * 24, 24, 24),
            Projectile.GetAlpha(lightColor), Projectile.rotation, drawOrigin, Projectile.scale,
            SpriteEffects.None);

        return false;
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        //return base.OnTileCollide(oldVelocity);
        if (_bounces > 0)
        {
            _bounces -= 1;
            SoundEngine.PlaySound(new SoundStyle("Terramon/Sounds/pkball_bounce"), Projectile.position);

            // If the projectile hits the left or right side of the tile, reverse the X velocity
            if (Math.Abs(Projectile.velocity.X - oldVelocity.X) > float.Epsilon) Projectile.velocity.X = -oldVelocity.X;

            // If the projectile hits the top or bottom side of the tile, reverse the Y velocity
            if (Math.Abs(Projectile.velocity.Y - oldVelocity.Y) > float.Epsilon) Projectile.velocity.Y = -oldVelocity.Y;

            Projectile.velocity.Y *= 0.7f;
            Projectile.velocity.X *= 0.5f;

            if (Projectile.velocity.Length() < 1.5f)
                _bounces = 0;
        }
        else
        {
            switch (Projectile.ai[1])
            {
                case 0:
                {
                    SoundEngine.PlaySound(SoundID.Dig, Projectile.position);
                    for (var i = 0; i < 14; i++)
                    {
                        var d = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height,
                            DustID.Marble);
                        d.noGravity = true;
                    }

                    return true;
                }
                //only randomise catch number and play sound once
                case 1 when _bounces == 0:
                {
                    //caught = CatchPokemonChances(capture);
                    if (Main.player[Projectile.owner].whoAmI ==
                        Main.myPlayer) //Generate new catch chance, (will switch pokeball to catching anim when value is recieved by clients)
                        _catchRandom = Main.rand.NextFloat(0, 1);
                    SoundEngine.PlaySound(new SoundStyle("Terramon/Sounds/pkball_bounce"), Projectile.position);
                    _bounces = -1;
                    break;
                }
            }
        }

        return false;
    }

    public override bool? CanHitNPC(NPC target)
    {
        return target.ModNPC is PokemonNPC;
    }

    public override void SendExtraAI(BinaryWriter writer)
    {
        var captureId = -1;
        if (_capture != null)
            captureId = _capture.NPC.whoAmI; //BinaryWriter can't send whole NPC, so we send the NPC's number instead

        writer.Write(_catchRandom);
        writer.Write(captureId);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        var catchRandomVar = reader.ReadSingle();
        var captureId = reader.ReadInt32();

        if (_catchRandom == -1)
            _catchRandom = catchRandomVar;
        if (_capture == null && captureId != -1) //only change capture if none exists
            _capture = Main.npc[captureId].ModNPC as PokemonNPC;
    }

    public override void OnSpawn(IEntitySource source)
    {
        Projectile.velocity += Main.player[Projectile.owner].velocity * 0.75f;
        if (Sandstorm.Happening && Main.player[Projectile.owner].ZoneDesert)
            Projectile.velocity.X -= 1.75f;
    }

    public override void OnKill(int timeLeft)
    {
        // Prevent duplicate drops in multiplayer or if a Pokémon was already caught
        if (Projectile.owner != Main.myPlayer || _hasContainedLocal) return;

        // Drop the item when the projectile is destroyed
        var item = 0;
        if (Main.rand.NextBool(DropItemChance))
            item = Item.NewItem(Projectile.GetSource_DropAsItem(), Projectile.getRect(), PokeballItem);

        // Sync the drop for multiplayer
        // Note the usage of Terraria.ID.MessageID, please use this!
        if (Main.netMode == NetmodeID.MultiplayerClient && item >= 0)
            NetMessage.SendData(MessageID.SyncItem, -1, -1, null, item, 1f);
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
                if (Main.projectile[shimmer].ModProjectile is BasePkballProjectile shimmerProj)
                    shimmerProj.CatchModifier = CatchModifier;
                Projectile.Kill();
            }
            else if (_bounces > 0 && Projectile.velocity.Y < 0)
            {
                Projectile.shimmerWet = false;
                Projectile.velocity.Y *= -0.8f;
                SoundEngine.PlaySound(new SoundStyle("Terramon/Sounds/pkball_bounce"),
                    Projectile.position);
                _bounces -= 1;
            }
        }


        //Main.NewText(catchRandom, Color.Pink);
        if (ModContent.GetInstance<GameplayConfig>().FastAnimations)
            _animSpeedMultiplier = 0.7f;

        Projectile.damage = Projectile.ai[1] == 0 ? 1 : 0;
        Projectile.ai[0]++;
        //Projectile.spriteDirection = Projectile.direction;
        if (_hasContainedLocal == false && _capture != null)
            HitPkmn(_capture.NPC);
        if (_catchRandom > -1 && !_hasCalculatedCapture)
        {
            _hasCalculatedCapture = true;
            _caught = CatchPokemonChances(_capture, _catchRandom);
            Projectile.ai[0] = 0;
            Projectile.ai[1] = 2;
        }

        switch (Projectile.ai[1])
        {
            case 0:
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

                break;
            }
            case 1:
            {
                _capture?.Destroy(); //Destroy Pokemon NPC

                if (Projectile.ai[0] <
                    35 * _animSpeedMultiplier) //Stay still (no velocity) if 50 frames havent passed yet (60fps)
                {
                    Projectile.frame = (int)Frame.Catch;
                    Projectile.rotation = _rotation;
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

                break;
            }
            case 2:
            {
                const float shakeIntensity = 0.15f;
                var shakeAtTick = 75 * _animSpeedMultiplier;
                Projectile.rotation += _rotationVelocity;
                if (Projectile.ai[0] >= shakeAtTick)
                {
                    //Main.NewText(catchTries, Color.CornflowerBlue);
                    if (_catchTries == 0 || ModContent.GetInstance<GameplayConfig>().FastAnimations)
                    {
                        if (_caught)
                        {
                            Projectile.frame = (int)Frame.CaptureComplete;
                            SoundEngine.PlaySound(new SoundStyle("Terramon/Sounds/pkball_catch"),
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
                        _catchTries -= 1;
                        _rotationDirection = !_rotationDirection;
                        _rotationVelocity = _rotationDirection ? shakeIntensity : -shakeIntensity;
                        SoundEngine.PlaySound(new SoundStyle("Terramon/Sounds/pkball_shake"),
                            Projectile.position);
                    }

                    Projectile.ai[0] = 0;
                    Projectile.rotation = 0;
                }
                else if (Projectile.ai[0] > shakeAtTick * 0.2 && _rotationVelocity != 0)
                {
                    _rotationVelocity = 0;
                    Projectile.rotation = 0;
                }
                else if (Projectile.ai[0] > shakeAtTick * 0.15 && _rotationVelocity != 0)
                {
                    _rotationVelocity = _rotationDirection ? shakeIntensity : -shakeIntensity;
                }
                else if (Projectile.ai[0] > shakeAtTick * 0.05 && _rotationVelocity != 0)
                {
                    _rotationVelocity = _rotationDirection ? -shakeIntensity : shakeIntensity;
                }

                break;
            }
            case 3:
            {
                var catchSuccessAtTick = 90f * _animSpeedMultiplier;
                if (Projectile.ai[0] == 1)
                    for (var i = 0; i < 3; i++)
                        Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.YellowStarDust);
                if (Projectile.ai[0] > catchSuccessAtTick / 2) Projectile.alpha += 18;
                if (Projectile.ai[0] >= catchSuccessAtTick)
                    PokemonCatchSuccess();
                break;
            }
        }
    }

    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        if (_capture == null)
            HitPkmn(target);
    }

    protected virtual bool CatchPokemonChances(PokemonNPC target, float random)
    {
        CatchModifier = ChangeCatchModifier(target); //Change modifier (can take into account values like pokemon type)

        const float
            catchChance =
                0.5f; //Terramon.Database.GetPokemon(capture.useId) * 0.85f; //would / 3 to match game but we can't damage pokemon so that would be too hard
        //TODO: pull actual data from pokemon when possible
        //Main.NewText($"chance {catchChance * catchModifier}, random {random}");
        if (_catchRandom < catchChance * CatchModifier)
            return true;

        const float split = (1 - catchChance) /
                            4; //Determine amount of times pokeball will rock (based on closeness to successful catch)

        _catchTries = random switch
        {
            < catchChance + split * 1 => 3,
            < catchChance + split * 2 => 2,
            < catchChance + split * 3 => 1,
            _ => 0
        };

        return false;
    }

    protected virtual float ChangeCatchModifier(PokemonNPC target)
    {
        return CatchModifier;
    }

    private void PokemonCatchSuccess()
    {
        SoundEngine.PlaySound(new SoundStyle("Terramon/Sounds/pkball_catch_pla"));
        Projectile.Kill();
        var ballName = GetType().Name.Split("Projectile")[0];
        _capture.Data.Ball = (byte)BallID.Search.GetId(ballName);
        var player = TerramonPlayer.LocalPlayer;
        var isCaptureRegisteredInPokedex = player.GetPokedex().Entries.TryGetValue(_capture.UseId, out var status) &&
                                           status == PokedexEntryStatus.Registered;
        var addSuccess = player.AddPartyPokemon(_capture.Data, !isCaptureRegisteredInPokedex);
        if (addSuccess)
        {
            Main.NewText(Language.GetTextValue("Mods.Terramon.Misc.CatchSuccess",
                TypeID.GetColor(Terramon.DatabaseV2.GetPokemon(_capture.UseId).Types[0]), _capture.DisplayName));
        }
        else
        {
            var box = player.TransferPokemonToPC(_capture.Data);
            Main.NewText(box != null
                ? Language.GetTextValue("Mods.Terramon.Misc.CatchSuccessPC",
                    TypeID.GetColor(Terramon.DatabaseV2.GetPokemon(_capture.UseId).Types[0]), _capture.DisplayName,
                    box.GivenName ?? player.GetDefaultNameForPCBox(box), player.Player.name)
                : Language.GetTextValue("Mods.Terramon.Misc.CatchSuccessPCNoRoom",
                    TypeID.GetColor(Terramon.DatabaseV2.GetPokemon(_capture.UseId).Types[0]), _capture.DisplayName,
                    player.Player.name));
        }

        if (isCaptureRegisteredInPokedex ||
            !ModContent.GetInstance<ClientConfig>().ShowPokedexRegistrationMessages) return;
        Main.NewText(Language.GetTextValue("Mods.Terramon.Misc.PokedexRegistered", _capture.DisplayName),
            new Color(159, 162, 173));
    }

    private void HitPkmn(NPC target)
    {
        _hasContainedLocal = true;
        //Main.NewText($"Contain success", Color.Orange);
        _capture = (PokemonNPC)target.ModNPC;

        // Register as seen in the player's Pokedex
        var ownerPlayer = Main.player[Projectile.owner].GetModPlayer<TerramonPlayer>();
        var pokedex = ownerPlayer.GetPokedex();
        if (pokedex.Entries.TryGetValue(_capture.UseId, out var status) && status == PokedexEntryStatus.Undiscovered)
            ownerPlayer.UpdatePokedex(_capture.UseId, PokedexEntryStatus.Seen);

        Projectile.ai[1] = 1;
        Projectile.ai[0] = 0;
        if (!ModContent.GetInstance<GameplayConfig>().FastAnimations)
            _bounces = 5; //If fast animation disabled, reset bounces to 5 (so the animation isn't shorter if it's already hit the ground)
        else if (_bounces > 2)
            _bounces = 2;

        _rotation = (target.Center - Projectile.Center).SafeNormalize(Vector2.Zero)
            .ToRotation(); //Rotate to face Pokemon

        if (Math.Abs(_rotation) > 1.5) //Stuff to make sure Pokeball doesn't appear upside down or reversed
        {
            if (_rotation > 0)
                _rotation -= 3;
            else
                _rotation += 3;

            Projectile.spriteDirection = 1;
        }
        else
        {
            Projectile.spriteDirection = -1;
        }

        Projectile.netUpdate = true;
    }

    private void ReleasePokemon()
    {
        if (_capture == null) return;
        SoundEngine.PlaySound(new SoundStyle("Terramon/Sounds/pkmn_spawn"), Projectile.position);
        var source = Entity.GetSource_FromThis();

        var newNPC =
            NPC.NewNPC(source, (int)Projectile.Center.X, (int)Projectile.Center.Y,
                _capture.Type); // spawn a new NPC at the new position
        var newPoke = (PokemonNPC)Main.npc[newNPC].ModNPC;
        newPoke.Data = _capture.Data;
        newPoke.NPC.spriteDirection = _capture.NPC.spriteDirection;
        //newPoke.isShimmer = capture.isShimmer;
        //newPoke.level = capture.level;
        //newPoke.catchAttempts = capture.catchAttempts + 1;
        //Main.NewText($"Catch attempts: {newPoke.catchAttempts}", Color.Firebrick);
        _capture = null;
    }

    private enum Frame //Here we label all of the frames in the spritesheet for better readability
    {
        Throw,
        Catch,
        Capture,
        CaptureComplete
    }
}