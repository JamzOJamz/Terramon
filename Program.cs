using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using Terramon.Core.Loaders.UILoading;

[assembly: MetadataUpdateHandler(typeof(UILoader))]

namespace Terramon;

[AutoloadBossHead]
internal class Program
{
    public static void Main(string[] args)
    {
        Environment.CurrentDirectory = @"C:\Program Files (x86)\Steam\steamapps\common\tModLoader";
        Console.WriteLine("H");
        DoLaunch(args);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void DoLaunch(string[] args)
    {
        args = args.Append("-console").ToArray();
        Console.WriteLine("Starting tml");
        typeof(ModLoader).Assembly.GetType("Terraria.MonoLaunch")!.GetMethod("Main",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!.CreateDelegate<Action<string[]>>()
            .Invoke(args);
    }
}