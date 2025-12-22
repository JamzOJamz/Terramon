namespace Terramon.Content.Scenes;

public class BattleScene : ModSceneEffect
{
    public override int Music => MusicLoader.GetMusicSlot(Mod, "Assets/Music/BattleWild");
    
    public override SceneEffectPriority Priority => SceneEffectPriority.BossHigh;

    public override bool IsSceneEffectActive(Player player)
    {
        var modPlayer = player.Terramon();
        return modPlayer.BattleClient is { BattleOngoing: true };
    }

    public override void SpecialVisuals(Player player, bool isActive)
    {
        if (isActive)
            Main.musicFade[Music] = 1f;
    }
}