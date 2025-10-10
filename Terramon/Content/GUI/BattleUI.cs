using Terramon.Core.Loaders.UILoading;
using Terraria.GameContent;
using Terraria.UI;

namespace Terramon.Content.GUI;

public class BattleUI : SmartUIState
{
    public override bool Visible => TerramonPlayer.LocalPlayer.Battle != null;

    public override int InsertionIndex(List<GameInterfaceLayer> layers)
    {
        return layers.Count;
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        var battle = TerramonPlayer.LocalPlayer.Battle;
        if (battle == null)
            return;

        var ticks = battle.TickCount;
        var opacity = GetCutsceneOpacity(ticks);

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