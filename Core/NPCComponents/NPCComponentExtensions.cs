#nullable enable
using System.Reflection;

namespace Terramon.Core.NPCComponents;

public static class NPCComponentExtensions
{
    public static MethodInfo EnableComponentMethod { get; } =
        typeof(NPCComponentExtensions).GetMethod(nameof(EnableComponent))!;
    
    // ReSharper disable once UnusedMember.Global
    public static T EnableComponent<T>(this NPC npc, Action<T>? initializer = null) where T : NPCComponent
    {
        var component = npc.GetGlobalNPC<T>();

        component.SetEnabled(npc, true);

        initializer?.Invoke(component);

        return component;
    }
}