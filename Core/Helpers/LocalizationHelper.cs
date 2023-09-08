using System.Collections.Generic;
using System.Reflection;
using Terraria.Localization;

namespace Terramon.Core.Helpers;

public static class LocalizationHelper
{
    private static readonly MethodInfo LocalizationLoader_LoadTranslations =
        typeof(LocalizationLoader).GetMethod("LoadTranslations", BindingFlags.NonPublic | BindingFlags.Static);

    private static readonly MethodInfo LocalizedText_SetValue =
        typeof(LocalizedText).GetMethod("SetValue", BindingFlags.NonPublic | BindingFlags.Instance);

    public static void ForceLoadModHJsonLocalization(Mod mod)
    {
        var lang = LanguageManager.Instance;
        foreach (var (key, value) in (LocalizationLoader_LoadTranslations.Invoke(null,
                     new object[] { mod, Language.ActiveCulture }) as List<(string key, string value)>)!)
        {
            var text = lang.GetText(key);
            LocalizedText_SetValue.Invoke(text,
                new object[] { value }); // can only set the value of existing keys. Cannot register new keys.
        }
    }
}