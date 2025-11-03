using Terraria.Localization;

// ReSharper disable InconsistentNaming

namespace Terramon.Helpers;

/*
 * MIT License
 *
 * Copyright (c) 2025 John Baglio
 *
 * https://github.com/absoluteAquarian/SerousCommonLib/blob/master/src/API/Helpers/LocalizationHelper.cs
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

/// <summary>
///     A helper class for using localized text.
/// </summary>
public static class LocalizationHelper
{
    /// <summary>
    ///     Forces the localization for the given mod to be loaded for use with <see cref="Language" />.
    /// </summary>
    /// <param name="mod">The mod instance</param>
    public static void ForceLoadModHJsonLocalization(Mod mod)
    {
        var lang = LanguageManager.Instance;
        foreach (var (key, value) in LocalizationLoader.LoadTranslations(mod, Language.ActiveCulture))
        {
            var text = lang.GetText(key);
            text.SetValue(value); // can only set the value of existing keys. Cannot register new keys.
        }
    }
}