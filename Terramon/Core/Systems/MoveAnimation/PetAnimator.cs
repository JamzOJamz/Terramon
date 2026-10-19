using Terramon.Content.Projectiles;

namespace Terramon.Core.Systems.MoveAnimation;

public sealed class PetAnimator : IAnimator
{
    public PokemonPet pet;
    public Entity other;
    public int animContextMax;
    public static readonly PetAnimator Instance = new();
    public void QueueContextfulAction(Action<WorldContext> action, EffectContext context, EffectTime delay = default, EffectOrder order = EffectOrder.WorldUpdate, ScaleParams scaleParams = default)
    {
        (order switch
        {
            EffectOrder.PreDrawAnimated => pet.PreDrawAnimActions,
            EffectOrder.PostDrawAnimated => pet.PostDrawAnimActions,
            _ => AnimationEffectsSystem.QueuedActions[(int)order]
        }).Add(new GenericAction(action, context, pet.Projectile.Hitbox, other.Hitbox, delay.ToFrames(animContextMax), scaleParams with { PreSelf = pet.Projectile.scale }));
    }
    public void ModifyDraw(EffectPosition? position = null, EffectRotation? rotation = null, EffectScale? scaleX = null, EffectScale? scaleY = null, EffectDirection direction = 0)
    {
        pet.AnimPosition = position;
        pet.AnimRot = rotation;
        pet.AnimScaleX = scaleX;
        pet.AnimScaleY = scaleY;
        pet.AnimDir = direction;
    }
}
