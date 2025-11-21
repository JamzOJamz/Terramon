using Microsoft.Xna.Framework.Audio;
using Terramon.Content.Projectiles;
using Terramon.Content.Scenes;
using Terramon.Core.Battling;
using Terramon.Core.Loaders.UILoading;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.CameraModifiers;
using Terraria.UI;

namespace Terramon.Content.GUI.TurnBased;

/// <summary>
///     Handles the client-side battle cinematics and presentation layer for turn-based battles.
/// </summary>
public sealed class BattleUI : SmartUIState
{
    private static readonly SubjectModifier FocusBetween = new(GetBetweenPosition);

    private static int _ticks;
    private static bool _effectsActive;
    private static bool _oldSidebarToggleState;
    private static float _oldGameZoomTarget;
    private static Vector2? _smoothCamPos;

    public override bool Visible => BattleClient.LocalBattleOngoing;

    public override int InsertionIndex(List<GameInterfaceLayer> layers)
    {
        return layers.Count;
    }

    public override void SafeUpdate(GameTime gameTime)
    {
        if (_ticks < int.MaxValue) // TODO: Decide how to handle _ticks reaching int.MaxValue in extremely long battles
            _ticks++;
    }

    public static void ApplyStartEffects()
    {
        if (_effectsActive)
            throw new InvalidOperationException(
                "ApplyStartEffects() was called twice without first calling ApplyEndEffects().");

        _effectsActive = true;

        _ticks = 0;
        _smoothCamPos = null;
        _oldGameZoomTarget = Main.GameZoomTarget;
        var partySidebar = PartyDisplay.Sidebar;
        _oldSidebarToggleState = partySidebar.IsToggled;
        partySidebar.Close();

        if (Main.audioSystem is LegacyAudioSystem audioSystem)
        {
            var curMusic = Main.curMusic;
            var curMusicFade = Main.musicFade[curMusic];
            Tween.To(() => curMusicFade, x => Main.musicFade[curMusic] = x, 0f, 0.57f);
            var bgmTrack = audioSystem.AudioTracks[ModContent.GetInstance<BattleScene>().Music];
            bgmTrack.Stop(AudioStopOptions.Immediate);
            bgmTrack.Reuse();
            bgmTrack.Play();
        }

        Task.Run(async () =>
        {
            await Task.Delay(570);
            Main.QueueMainThreadAction(() =>
            {
                FocusBetween.Reset();
                Main.instance.CameraModifiers.Add(FocusBetween);
                Tween.To(() => Main.GameZoomTarget, 5f, 1.42f)
                    .SetEase(Ease.InBackExpo, EaseParams.Back(1.6f)).OnComplete = () =>
                {
                    TestBattleUI.Open();
                    Main.GameZoomTarget = 1.5f;
                };
            });
        });
    }

    public static void ApplyEndEffects()
    {
        if (!_effectsActive)
            throw new InvalidOperationException(
                "ApplyEndEffects() was called before ApplyStartEffects().");

        _effectsActive = false;

        TestBattleUI.Close();

        var partySidebar = PartyDisplay.Sidebar;
        if (!partySidebar.IsToggled && _oldSidebarToggleState)
            partySidebar.SetToggleState(true);

        if (Math.Abs(Main.GameZoomTarget - _oldGameZoomTarget) > 0.001f)
            Tween.To(() => Main.GameZoomTarget, _oldGameZoomTarget, 0.5f).SetEase(Ease.OutExpo);
    }

    private static Vector2? GetBetweenPosition()
    {
        if (!BattleClient.LocalBattleOngoing)
            return null;

        var myPet = TerramonPlayer.LocalPlayer.ActivePetProjectile?.Projectile;
        if (myPet is null)
            return null;

        var other = BattleClient.LocalClient.Foe.SyncedEntity;
        if (other is Player plr)
        {
            var pet = plr.Terramon().ActivePetProjectile;
            if (pet != null)
                other = pet.Projectile;
        }

        var otherCenter = other.Center;

        var targetX = otherCenter.X + (other.direction * (PokemonPet.DistanceFromFoe * 0.5f));
        var targetY = (myPet.Center.Y + otherCenter.Y) * 0.5f;
        Vector2 target = new(targetX, targetY);

        _smoothCamPos ??= target;

        const float smoothFactor = 0.1f;
        _smoothCamPos = Vector2.Lerp(_smoothCamPos.Value, target, smoothFactor);

        return _smoothCamPos;
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (!BattleClient.LocalBattleOngoing)
            return;

        var opacity = GetCutsceneOpacity(_ticks);
        TestBattleUI.PlayerPanel?.Animate(_ticks);
        TestBattleUI.FoePanel?.Animate(_ticks);

        if (_ticks == 160)
            Main.GameZoomTarget = 1.5f;

        if (opacity > 0f)
        {
            spriteBatch.Draw(
                TextureAssets.MagicPixel.Value,
                new Rectangle(0, 0, Main.screenWidth, Main.screenHeight),
                Color.White * opacity
            );
        }
    }

    private static float GetCutsceneOpacity(int ticks)
    {
        // Two quick flashes
        if (ticks <= 15) return Flash(ticks, 0, 15);
        if (ticks <= 30) return Flash(ticks, 15, 30);

        // Long fade in
        if (ticks <= 117) return FadeIn(ticks, 45, 117);

        // Hold white
        if (ticks <= 165) return 1f;

        // Fade out
        if (ticks <= 177) return FadeOut(ticks, 165, 177);

        return 0f;
    }

    private static float Flash(int ticks, int start, int end)
    {
        var mid = (start + end) / 2f;
        var progress = ticks <= mid
            ? (ticks - start) / (mid - start)
            : 1f - (ticks - mid) / (end - mid);
        return MathHelper.Clamp(progress, 0f, 1f);
    }

    private static float FadeIn(int ticks, int start, int end)
    {
        return MathHelper.Clamp((ticks - start) / (float)(end - start), 0f, 1f);
    }

    private static float FadeOut(int ticks, int start, int end)
    {
        return MathHelper.Clamp(1f - (ticks - start) / (float)(end - start), 0f, 1f);
    }
}

public sealed class SubjectModifier(Func<Vector2?> subject, float cameraSpeed = 0.1f) : ICameraModifier
{
/*
    private int _frames;
*/
    private Vector2 _latestTargetPosition;
    private float _progress;
    private bool _returning;

    /*
    public SubjectModifier(Func<Vector2> subject, int frames, float cameraSpeed = 0.1f)
    {
        _frames = frames;
        _wantedSubject = () => _frames <= 0 ? null : subject();
        _cameraSpeed = cameraSpeed;
    }
*/

    public string UniqueIdentity => nameof(Terramon) + nameof(SubjectModifier);

    public bool Finished => _progress <= 0.00005f && _returning;

    public void Update(ref CameraInfo cameraPosition)
    {
        // Main.NewText($"{_returning}:{_progress}:{_latestTargetPosition}");

        if (!_returning)
        {
            var check = subject();
            if (check.HasValue)
                _latestTargetPosition = check.Value - new Vector2(Main.screenWidth * 0.5f, Main.screenHeight * 0.5f);
            else
                _returning = true;
        }

        cameraPosition.CameraPosition = Vector2.Lerp(cameraPosition.CameraPosition, _latestTargetPosition, _progress);

        if (Main.gameInactive || Main.gamePaused)
            return;

        _progress = float.Lerp(_progress, _returning ? 0f : 1f, cameraSpeed);
        // _frames--;
    }

    public void Reset()
    {
        _returning = false;
        _progress = 0f;
    }
}