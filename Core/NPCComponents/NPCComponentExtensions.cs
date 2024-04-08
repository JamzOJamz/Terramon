#nullable enable
using System;

namespace Terramon.Core.NPCComponents;

public static class NPCComponentExtensions
{
    // ReSharper disable once UnusedMember.Global
    public static T EnableComponent<T>(this NPC npc, Action<T>? initializer = null) where T : NPCComponent
    {
        var component = npc.GetGlobalNPC<T>();

        component.SetEnabled(npc, true);

        initializer?.Invoke(component);

        return component;
    }
}