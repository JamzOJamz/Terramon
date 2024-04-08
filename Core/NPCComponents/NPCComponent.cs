using System.Collections.Generic;

namespace Terramon.Core.NPCComponents;

/// <summary>
///     A <see cref="GlobalNPC" /> that can be enabled and disabled at will.
/// </summary>
public abstract class NPCComponent : GlobalNPC
{
    private protected static Dictionary<int, NPCComponent> Instances;

    public bool Enabled { get; private set; }

    public override bool InstancePerEntity => true;

    protected virtual bool CacheInstances => false;

    public virtual void OnEnabled(NPC npc)
    {
    }

    public virtual void OnDisabled(NPC npc)
    {
    }

    public void SetEnabled(NPC npc, bool value)
    {
        if (Enabled == value) return;

        Enabled = value;

        if (value)
            OnEnabled(npc);
        else
            OnDisabled(npc);
    }

    public override void SetDefaults(NPC npc)
    {
        if (!CacheInstances || !Enabled || Instances.ContainsKey(npc.type)) return;
        Instances.Add(npc.type, this);
    }

    public override void Load()
    {
        if (!CacheInstances) return;
        Instances = new Dictionary<int, NPCComponent>();
    }

    public override void Unload()
    {
        if (!CacheInstances) return;
        Instances = null;
    }
}