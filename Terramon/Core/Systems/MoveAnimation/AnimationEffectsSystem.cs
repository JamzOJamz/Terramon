using System.Runtime.InteropServices;

namespace Terramon.Core.Systems.MoveAnimation;

public record struct GenericAction(Action<WorldContext> Action, EffectContext Context, Rectangle Animated, Rectangle Target, int Delay = 0, ScaleParams ScaleParameters = default);

public sealed class AnimationEffectsSystem : ModSystem
{
    public static List<GenericAction>[] QueuedActions;
    public override bool IsLoadingEnabled(Mod mod) => !Main.dedServ;
    public override void Load()
    {
        QueuedActions = new List<GenericAction>[(int)EffectOrder.MAX_GENERIC];
        for (var i = 0; i < QueuedActions.Length; i++)
            QueuedActions[i] = [];

        On_Main.DrawDust += static (orig, self) =>
        {
            Handle(EffectOrder.PreDrawDust);
            orig(self);
            Handle(EffectOrder.PostDrawDust);
        };

        On_Main.DrawProjectiles += static (orig, self) =>
        {
            Handle(EffectOrder.PreDrawProjectiles);
            orig(self);
            Handle(EffectOrder.PostDrawProjectiles);
        };
    }
    public override void PreUpdateTime() => Handle(EffectOrder.WorldUpdate);
    public static void Handle(EffectOrder order) => Handle(QueuedActions[(int)order]);
    public static void Handle(List<GenericAction> list)
    {
        for (var i = 0; i < list.Count; i++)
        {
            ref var action = ref CollectionsMarshal.AsSpan(list)[i];
            if (action.Delay-- == 0)
            {
                action.Action(action.Context.ToWorld(action.Animated, action.Target, action.ScaleParameters));
                list.RemoveAt(i);
                i--;
            }
        }
    }
}
