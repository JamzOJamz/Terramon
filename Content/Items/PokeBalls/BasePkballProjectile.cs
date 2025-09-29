using Terramon.Content.Configs;
using Terramon.Content.NPCs;
using Terramon.ID;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Events;
using Terraria.Localization;

namespace Terramon.Content.Items.PokeBalls;

internal abstract class BasePkballProjectile : ModProjectile
{
    private const int MaxBounces = 5;

    private readonly string[] _wobbleSoundPaths =
    [
        "Terramon/Sounds/ls_catch_wobble1",
        "Terramon/Sounds/ls_catch_wobble2",
        "Terramon/Sounds/ls_catch_wobble3"
    ];

    private float _animSpeedMultiplier = 1;
    private int _bounces = MaxBounces;
    private PokemonNPC _capture;
    private float _catchRandom = -1;
    private int _catchTries = 3;
    private bool _hasContainedLocal;
    private bool _hasReleasedLocal;
    private bool _isCaught;
    private float _rotation;
    private bool _rotationDirection;
    private float _rotationVelocity;

    protected virtual int PokeballItem => ModContent.ItemType<BasePkballItem>();
    protected virtual float CatchModifier { get; private set; }

    /// <summary>
    ///     The denominator (1/x) for the probability of dropping this projectile's respective Poké Ball item,
    ///     <see cref="PokeballItem" />, when killed without catching a Pokémon.
    ///     Default is 3, meaning a 1/3 or approximately 33.33% chance.
    /// </summary>
    protected virtual int DropItemChanceDenominator => 3;

    public override string Texture => "Terramon/Assets/Items/PokeBalls/" + GetType().Name;

    public override LocalizedText DisplayName =>
        Language.GetText($"Mods.Terramon.Items.{GetType().Name.Replace("Projectile", "Item")}.DisplayName");

    private ref float AITimer => ref Projectile.ai[0];

    private ref float AIState => ref Projectile.ai[1];

    public override void SetStaticDefaults()
    {
        Main.projFrames[Type] = 4;
        Projectile.tileCollide = true;
    }

    public override void SetDefaults()
    {
        Projectile.width = 14;
        Projectile.height = 14;
        Projectile.friendly = true;
        Projectile.aiStyle = -1;
        Projectile.penetrate = -1;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        var texture = TextureAssets.Projectile[Type].Value;

        var drawOrigin = new Vector2(texture.Width * 0.5f, 24 * 0.5f);
        var drawPos = Projectile.position - Main.screenPosition + drawOrigin + new Vector2(Projectile.gfxOffY);
        Main.EntitySpriteDraw(texture, drawPos - new Vector2(5, 5), new Rectangle(0, Projectile.frame * 24, 24, 24),
            Projectile.GetAlpha(lightColor), Projectile.rotation, drawOrigin, Projectile.scale,
            Projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None);

        return false;
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        if (_bounces > 0)
        {
            _bounces -= 1;
            SoundEngine.PlaySound(new SoundStyle("Terramon/Sounds/pkball_bounce") { Volume = 0.75f },
                Projectile.position);

            // If the projectile hits the left or right side of the tile, reverse the X velocity
            if (Math.Abs(Projectile.velocity.X - oldVelocity.X) > float.Epsilon) Projectile.velocity.X = -oldVelocity.X;

            // If the projectile hits the top or bottom side of the tile, reverse the Y velocity
            if (Math.Abs(Projectile.velocity.Y - oldVelocity.Y) > float.Epsilon) Projectile.velocity.Y = -oldVelocity.Y;

            Projectile.velocity.Y *= 0.7f;
            Projectile.velocity.X *= 0.55f;

            if (Projectile.velocity.Length() < 1.5f)
                _bounces = 0;
        }
        else
        {
            switch (AIState)
            {
                case (float)ActionState.Throw:
                    return true;
                case (float)ActionState.Catch when _bounces == 0:
                {
                    //caught = CatchPokemonChances(capture);
                    if (Projectile.owner == Main.myPlayer)
                    {
                        //Generate new catch chance, (will switch pokeball to catching anim when value is recieved by clients)
                        _catchRandom = Main.rand.NextFloat(0, 1);
                        Projectile.netUpdate = true;
                    }

                    SoundEngine.PlaySound(new SoundStyle("Terramon/Sounds/pkball_bounce") { Volume = 0.75f },
                        Projectile.position);
                    _bounces = -1;
                    break;
                }
            }
        }

        return false;
    }

    public override bool? CanHitNPC(NPC target)
    {
        return target.ModNPC is PokemonNPC { PlasmaState: false } && _capture == null;
    }

    public override void SendExtraAI(BinaryWriter writer)
    {
        var captureId = (byte)(_capture?.NPC.whoAmI ?? 255);
        writer.Write(captureId);
        writer.Write(_catchRandom);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        var captureId = reader.ReadByte();
        var catchRandomVar = reader.ReadSingle();

        if (_catchRandom == -1 && catchRandomVar != -1)
            _catchRandom = catchRandomVar;
        if (_capture == null && captureId != 255)
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
        // Don't drop the item or spawn dusts if the projectile has already contained a Pokémon
        if (_hasContainedLocal) return;

        // Play a sound and spawn dusts when the projectile is destroyed
        SoundEngine.PlaySound(SoundID.Dig, Projectile.position);
        for (var i = 0; i < 14; i++)
        {
            var d = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height,
                DustID.Marble);
            d.noGravity = true;
        }

        // Prevent duplicate drops in multiplayer
        if (Projectile.owner != Main.myPlayer) return;

        // Drop the item when the projectile is destroyed
        var item = 0;
        if (!Projectile.shimmerWet && Main.rand.NextBool(DropItemChanceDenominator))
            item = Item.NewItem(Projectile.GetSource_DropAsItem(), Projectile.getRect(), PokeballItem);

        // Sync the drop for multiplayer
        // Note the usage of Terraria.ID.MessageID, please use this!
        if (Main.netMode == NetmodeID.MultiplayerClient && item >= 0)
            NetMessage.SendData(MessageID.SyncItem, -1, -1, null, item, 1f);
    }

    public override void AI()
    {
        // Fade out any applied light before killing the projectile
        if (_hasReleasedLocal)
        {
            Projectile.light -= 0.025f;
            if (Projectile.light <= 0)
                Projectile.Kill();
            return;
        }

        if (Projectile.shimmerWet)
            ShimmerBehaviour();

        //if (AIState is > (float)ActionState.Throw and < (float)ActionState.CaptureComplete && Projectile.light < 0.35f)
        //    Projectile.light += 0.015f;

        if (ModContent.GetInstance<GameplayConfig>().FastAnimations)
            _animSpeedMultiplier = 0.7f;

        Projectile.damage = AIState == (float)ActionState.Throw ? 1 : 0;
        AITimer++;

        // Handle hitting the Pokémon on other clients
        if (_hasContainedLocal == false && _capture != null)
            HitPkmn(_capture.NPC);

        // Calculate if the Pokémon is successfully caught
        if (_catchRandom > -1 && AIState == (float)ActionState.Catch)
        {
            _isCaught = CatchPokemonChances(_capture, _catchRandom);
            AITimer = 0;
            AIState = (float)ActionState.Capture;
        }

        switch (AIState)
        {
            case (float)ActionState.Throw:
            {
                Projectile.frame = (int)ActionState.Throw; //At state 1 should use throw sprite
                Projectile.rotation +=
                    Projectile.velocity.X *
                    0.05f; //Spin in air (feels better than static) based on current velocity so it slows down once it hits the ground
                if (AITimer >= 10f)
                    Projectile.velocity.Y += 0.25f; //(positive Y value makes projectile go down)

                // Sparkle particles
                if (Main.rand.NextBool((int)(10f * ((MaxBounces + 1 - _bounces) * 0.75f))))
                {
                    var dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height,
                        DustID.TreasureSparkle, -Projectile.velocity.X, -Projectile.velocity.Y);
                    dust.noGravity = true;
                    dust.velocity *= 0.75f;
                }

                break;
            }
            case (float)ActionState.Catch:
            {
                if (AITimer <
                    45 * _animSpeedMultiplier) //Stay still (no velocity) if 45 frames havent passed yet (60fps)
                {
                    Projectile.frame = (int)ActionState.Catch;
                    Projectile.rotation = _rotation;
                    Projectile.velocity.X = 0;
                    Projectile.velocity.Y = 0;
                }
                else
                {
                    Projectile.frame = (int)ActionState.Capture;
                    Projectile.rotation = 0;
                    Projectile.velocity.Y +=
                        0.25f; //Add to Y velocity so projectile moves downwards (i subtracted this in testing - the pokeball flew into the sky and disappeared)
                }

                break;
            }
            case (float)ActionState.Capture:
            {
                const float shakeIntensity = 0.15f;
                var shakeAtTick = 75 * _animSpeedMultiplier;
                Projectile.rotation += _rotationVelocity;
                if (AITimer >= shakeAtTick)
                {
                    //Main.NewText(catchTries, Color.CornflowerBlue);
                    if (_catchTries == 0 || ModContent.GetInstance<GameplayConfig>().FastAnimations)
                    {
                        if (_isCaught)
                        {
                            Projectile.frame = (int)ActionState.CaptureComplete;
                            SoundEngine.PlaySound(new SoundStyle("Terramon/Sounds/ls_catch_click"),
                                Projectile.position);
                            AIState = (float)ActionState.CaptureComplete;
                            AITimer = 0;
                        }
                        else
                        {
                            ReleasePokemon();
                            Projectile.alpha = 255;
                        }
                    }
                    else
                    {
                        _catchTries -= 1;
                        _rotationDirection = !_rotationDirection;
                        _rotationVelocity = _rotationDirection ? shakeIntensity : -shakeIntensity;
                        SoundEngine.PlaySound(new SoundStyle(_wobbleSoundPaths[2 - _catchTries]),
                            Projectile.position);
                    }

                    AITimer = 0;
                    Projectile.rotation = 0;
                }
                else if (AITimer > shakeAtTick * 0.2 && _rotationVelocity != 0)
                {
                    _rotationVelocity = 0;
                    Projectile.rotation = 0;
                }
                else if (AITimer > shakeAtTick * 0.15 && _rotationVelocity != 0)
                {
                    _rotationVelocity = _rotationDirection ? shakeIntensity : -shakeIntensity;
                }
                else if (AITimer > shakeAtTick * 0.05 && _rotationVelocity != 0)
                {
                    _rotationVelocity = _rotationDirection ? -shakeIntensity : shakeIntensity;
                }

                break;
            }
            case (float)ActionState.CaptureComplete:
            {
                var catchSuccessAtTick = 90f * _animSpeedMultiplier;
                if (AITimer == 1)
                    for (var i = 0; i < 3; i++)
                        Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.YellowStarDust);
                if (AITimer > catchSuccessAtTick / 2)
                {
                    Projectile.alpha += 18;
                    Projectile.light -= 0.025f;
                }

                if (AITimer >= catchSuccessAtTick)
                    PokemonCatchSuccess();
                break;
            }
        }
    }

    private void ShimmerBehaviour()
    {
        if (Type != ModContent.ProjectileType<AetherBallProjectile>() && Projectile.owner == Main.myPlayer)
        {
            var shimmer = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position,
                new Vector2(Projectile.velocity.X, Projectile.velocity.Y * -0.8f),
                ModContent.ProjectileType<AetherBallProjectile>(), 0, 0, Projectile.owner);
            if (Main.projectile[shimmer].ModProjectile is BasePkballProjectile shimmerProj)
                shimmerProj.CatchModifier = CatchModifier;
            Projectile.Kill();
        }
        else if (_bounces > 0 && Projectile.velocity.Y < 0)
        {
            Projectile.shimmerWet = false;
            Projectile.velocity.Y *= -0.8f;
            SoundEngine.PlaySound(new SoundStyle("Terramon/Sounds/pkball_bounce") { Volume = 0.75f },
                Projectile.position);
            _bounces -= 1;
        }
    }

    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        HitPkmn(target);
    }

    protected virtual bool CatchPokemonChances(PokemonNPC target, float random)
    {
        CatchModifier = ChangeCatchModifier(target); //Change modifier (can take into account values like pokemon type)

        var catchChance =
            _capture.Data.Schema.CatchRate / 255f *
            0.85f; //would / 3 to match game but we can't damage pokemon so that would be too hard
        //Main.NewText($"chance {catchChance * catchModifier}, random {random}");
        if (_catchRandom < catchChance * CatchModifier)
            return true;

        var split = (1 - catchChance) /
                    4; //Determine amount of times pokeball will rock (based on closeness to successful catch)

        if (random < catchChance + split * 1)
            _catchTries = 3;
        else if (random < catchChance + split * 2)
            _catchTries = 2;
        else if (random < catchChance + split * 3)
            _catchTries = 1;
        else
            _catchTries = 0;

        return false;
    }

    protected virtual float ChangeCatchModifier(PokemonNPC target)
    {
        return CatchModifier;
    }

    private void PokemonCatchSuccess()
    {
        // Don't run this code on other clients
        if (Projectile.owner != Main.myPlayer) return;

        TerramonWorld.PlaySoundOverBGM(new SoundStyle("Terramon/Sounds/pkball_catch_pla"));
        
        Projectile.Kill();
        var schema = _capture.Data.Schema;
        var ballName = GetType().Name.Split("Projectile")[0];
        _capture.Data.Ball = Enum.Parse<BallID>(ballName);
        var player = TerramonPlayer.LocalPlayer;
        var addSuccess = player.AddPartyPokemon(_capture.Data, out var justRegistered);
        if (addSuccess)
        {
            Main.NewText(Language.GetTextValue("Mods.Terramon.Misc.CatchSuccess",
                schema.Types[0].GetHexColor(), _capture.DisplayName));
        }
        else
        {
            var box = player.TransferPokemonToPC(_capture.Data);
            Main.NewText(box != null
                ? Language.GetTextValue("Mods.Terramon.Misc.CatchSuccessPC",
                    schema.Types[0].GetHexColor(),
                    _capture.DisplayName,
                    box.GivenName ?? player.GetDefaultNameForPCBox(box), player.Player.name)
                : Language.GetTextValue("Mods.Terramon.Misc.CatchSuccessPCNoRoom",
                    schema.Types[0].GetHexColor(),
                    _capture.DisplayName,
                    player.Player.name));
        }

        if (!justRegistered ||
            !ModContent.GetInstance<ClientConfig>().ShowPokedexRegistrationMessages) return;
        Main.NewText(Language.GetTextValue("Mods.Terramon.Misc.PokedexRegistered", _capture.DisplayName),
            new Color(159, 162, 173));
    }

    private void HitPkmn(NPC target)
    {
        _hasContainedLocal = true;
        _capture = (PokemonNPC)target.ModNPC;

        // Play sound effect
        var s = new SoundStyle
        {
            SoundPath = "Terramon/Sounds/pkmn_recall",
            Volume = 0.375f
        };
        SoundEngine.PlaySound(s);

        // Register as seen in the player's Pokedex
        var ownerPlayer = Main.player[Projectile.owner].GetModPlayer<TerramonPlayer>();
        ownerPlayer.UpdatePokedex(_capture.ID, PokedexEntryStatus.Seen, shiny: _capture.Data?.IsShiny ?? false);

        AIState = (float)ActionState.Catch;
        AITimer = 0;
        if (!ModContent.GetInstance<GameplayConfig>().FastAnimations)
            _bounces = 5; //If fast animation disabled, reset bounces to 5 (so the animation isn't shorter if it's already hit the ground)
        else if (_bounces > 2)
            _bounces = 2;

        // Calculate rotation to face the target (Pokemon)
        _rotation = (target.Center - Projectile.Center).ToRotation();

        // Ensure the Pokeball sprite doesn't appear upside down or reversed
        if (Math.Abs(_rotation) > MathHelper.PiOver2)
        {
            _rotation = _rotation > 0 ? _rotation - MathHelper.Pi : _rotation + MathHelper.Pi;
            Projectile.spriteDirection = 1;
        }
        else
        {
            Projectile.spriteDirection = -1;
        }
        
        _capture.Encapsulate(Projectile.position); // Destroy Pokémon NPC

        // Queue resync for the proojectile in multiplayer
        Projectile.netUpdate = true;
    }

    private void ReleasePokemon()
    {
        if (_capture == null) return;
        SoundEngine.PlaySound(new SoundStyle("Terramon/Sounds/ls_catch_fail"), Projectile.position);

        // Release (respawn) the Pokémon on the server. It will be synced to all clients.
        if (Main.netMode != NetmodeID.MultiplayerClient)
        {
            var source = Entity.GetSource_FromThis("PokemonRelease");
            var newNPC =
                NPC.NewNPC(source, (int)Projectile.Center.X, (int)Projectile.Center.Y,
                    _capture.Type); // spawn a new NPC at the new position
            var newPoke = (PokemonNPC)Main.npc[newNPC].ModNPC;
            newPoke.Data = _capture.Data;
            newPoke.NPC.spriteDirection = _capture.NPC.spriteDirection;
            newPoke.NPC.FindFrame();
            //newPoke.isShimmer = capture.isShimmer;
            //newPoke.level = capture.level;
            //newPoke.catchAttempts = capture.catchAttempts + 1;
            //Main.NewText($"Catch attempts: {newPoke.catchAttempts}", Color.Firebrick);
        }

        _capture = null;
        _hasReleasedLocal = true;
    }

    private enum ActionState
    {
        Throw,
        Catch,
        Capture,
        CaptureComplete
    }
}