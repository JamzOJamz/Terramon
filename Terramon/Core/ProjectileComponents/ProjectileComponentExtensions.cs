#nullable enable
using System.Reflection;

namespace Terramon.Core.ProjectileComponents;

public static class ProjectileComponentExtensions
{
    public static MethodInfo EnableComponentMethod { get; } =
        typeof(ProjectileComponentExtensions).GetMethod(nameof(EnableComponent))!;
    
    // ReSharper disable once UnusedMember.Global
    public static T EnableComponent<T>(this Projectile projectile, Action<T>? initializer = null) where T : ProjectileComponent
    {
        var component = projectile.GetGlobalProjectile<T>();

        component.SetEnabled(projectile, true);

        initializer?.Invoke(component);

        return component;
    }
}