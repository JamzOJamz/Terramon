using System.IO;
using System.Linq;
using System.Reflection;

using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;
using Terraria.Graphics.Shaders;
using Terraria.Graphics.Effects;

using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using static Terraria.ModLoader.Core.TmodFile;
using Mono.Cecil;

namespace Terramon.Effects
{
    interface OrderedLoadable
    {
        void Load();
        void Unload();
        float Priority { get; }
    }

    class ShaderLoader : OrderedLoadable
    {
        public float Priority => 0.9f;

        public void Load()
        {
            if (Main.dedServ)
                return;

            MethodInfo info = typeof(Mod).GetProperty("File", BindingFlags.NonPublic | BindingFlags.Instance).GetGetMethod(true);
            var file = (TmodFile)info.Invoke(Terramon.Instance, null);

            System.Collections.Generic.IEnumerable<FileEntry> shaders = file.Where(n => n.Name.StartsWith("Effects/") && n.Name.EndsWith(".xnb"));

            foreach (FileEntry entry in shaders)
            {
                string name = entry.Name.Replace(".xnb", "").Replace("Effects/", "");
                string path = entry.Name.Replace(".xnb", "");
                LoadShader(name, path);
            }
        }

        public void Unload()
        {

        }

        public static void LoadShader(string name, string path)
        {
            var screenRef = new Ref<Effect>(Terramon.Instance.Assets.Request<Effect>(path, ReLogic.Content.AssetRequestMode.ImmediateLoad).Value);
            Filters.Scene[name] = new Filter(new ScreenShaderData(screenRef, name + "Pass"), EffectPriority.High);
            Filters.Scene[name].Load();
        }
    }
}