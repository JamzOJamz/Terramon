using Microsoft.Xna.Framework.Audio;
using Terramon.Content.Projectiles;
using Terramon.Content.Scenes;
using Terramon.Core.Loaders.UILoading;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.CameraModifiers;
using Terraria.UI;

namespace Terramon.Content.GUI.TurnBased;

public sealed class SubjectModifier : ICameraModifier
{
    private int _frames;
    private Vector2 _latestTargetPosition;
    private float _progress;
    private bool _returning;
    public float CameraSpeed;
    public Func<Vector2?> WantedSubject;

    public SubjectModifier(Func<Vector2?> subject, float cameraSpeed = 0.1f)
    {
        WantedSubject = subject;
        CameraSpeed = cameraSpeed;
    }

    public SubjectModifier(Func<Vector2> subject, int frames, float cameraSpeed = 0.1f)
    {
        _frames = frames;
        WantedSubject = () => _frames <= 0 ? null : subject();
        CameraSpeed = cameraSpeed;
    }

    public string UniqueIdentity => nameof(Terramon) + nameof(SubjectModifier);
    public bool Finished => _progress <= 0.00005f && _returning;

    public void Update(ref CameraInfo cameraPosition)
    {
        // Main.NewText($"{_returning}:{_progress}:{_latestTargetPosition}");

        if (!_returning)
        {
            var check = WantedSubject();
            if (check.HasValue)
                _latestTargetPosition = check.Value - new Vector2(Main.screenWidth * 0.5f, Main.screenHeight * 0.5f);
            else
                _returning = true;
        }

        cameraPosition.CameraPosition = Vector2.Lerp(cameraPosition.CameraPosition, _latestTargetPosition, _progress);

        if (Main.gameInactive || Main.gamePaused)
            return;

        _progress = float.Lerp(_progress, _returning ? 0f : 1f, CameraSpeed);
        _frames--;
    }

    public void Reset()
    {
        _returning = false;
        _progress = 0f;
    }
}

public sealed class BattleUI : SmartUIState
{
    public static readonly SubjectModifier FocusBetween = new(GetBetweenPosition);

    private static bool OldSidebarToggleState;
    private static float OldGameZoomTarget;

    public override bool Visible => TerramonPlayer.LocalPlayer.Battle != null;

    public override int InsertionIndex(List<GameInterfaceLayer> layers)
    {
        return layers.Count;
    }

    public static void ApplyStartEffects()
    {
        OldGameZoomTarget = Main.GameZoomTarget;

        var partySidebar = PartyDisplay.Sidebar;
        OldSidebarToggleState = partySidebar.IsToggled;
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
        TestBattleUI.Close();
        
        var partySidebar = PartyDisplay.Sidebar;
        if (!partySidebar.IsToggled && OldSidebarToggleState)
            partySidebar.SetToggleState(true);
        
        if (Math.Abs(Main.GameZoomTarget - OldGameZoomTarget) > 0.001f)
            Tween.To(() => Main.GameZoomTarget, OldGameZoomTarget, 0.5f).SetEase(Ease.OutExpo);
        
        lastExtremePosition = null;
    }

    private static float? lastExtremePosition;
    private static Vector2? GetBetweenPosition()
    {
        var terramon = TerramonPlayer.LocalPlayer;
        var battle = terramon.Battle;
        if (battle is null)
            return null;

        Projectile myPet = terramon.ActivePetProjectile?.Projectile;
        if (myPet is null)
            return null;

        Entity other = (Entity)battle.WildNPC?.NPC ?? battle.Player2.ActivePetProjectile.Projectile;
        Vector2 otherCenter = other.Center;
        float target = float.Lerp(myPet.Center.X, otherCenter.X, 0.5f);

        bool correctDirection = Math.Sign(target - otherCenter.X) == other.direction;
        if (!lastExtremePosition.HasValue ||
            (Math.Abs(target) < Math.Abs(lastExtremePosition.Value) && correctDirection))
            lastExtremePosition = target;

        return new(lastExtremePosition.Value, otherCenter.Y);
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        var battle = TerramonPlayer.LocalPlayer.Battle;
        if (battle == null)
            return;

        var ticks = battle.TickCount;
        var opacity = GetCutsceneOpacity(ticks);
        TestBattleUI.PlayerPanel?.Animate(ticks);
        TestBattleUI.FoePanel?.Animate(ticks);

        if (ticks == 160)
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