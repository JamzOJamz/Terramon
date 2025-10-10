namespace Terramon.Content.Scenes;

public class BattleScene : ModSceneEffect
{
    public override int Music => MusicLoader.GetMusicSlot("Terramon/Sounds/Music/BattleWild");
    
    public override SceneEffectPriority Priority => SceneEffectPriority.BossHigh;

    public override bool IsSceneEffectActive(Player player) => player.Terramon().Battle != null;

    public override void SpecialVisuals(Player player, bool isActive)
    {
        if (isActive)
            Main.musicFade[Music] = 1f;
    }
}