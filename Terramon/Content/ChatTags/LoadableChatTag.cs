using System.Reflection;
using Terraria.UI.Chat;

namespace Terramon.Content.ChatTags;

public abstract class LoadableChatTag : TextSnippet, ILoadable, ITagHandler
{
    private static readonly MethodInfo _registerMethod = typeof(ChatManager).GetMethod(nameof(ChatManager.Register));
    public abstract string[] Aliases { get; }
    public void Load(Mod mod) => _registerMethod.MakeGenericMethod(GetType()).Invoke(null, [Aliases]);
    public TextSnippet Parse(string text, Color baseColor = default, string options = null)
    {
        var snippet = (LoadableChatTag)Activator.CreateInstance(GetType());
        snippet.Text = text;
        snippet.Color = baseColor;
        snippet.Initialize(options.Split('/'));
        // alternatively i guess Initialize could return bool. if false then return new TextSnippet(text) here like vanilla chat tags do
        return snippet;
    }
    public virtual void Initialize(string[] args) { }
    public sealed override bool UniqueDraw(bool justCheckingString, out Vector2 size, SpriteBatch spriteBatch, Vector2 position = default, Color color = default, float scale = 1)
    {
        size = default;
        Draw(spriteBatch, ref size, position, color, scale);
        return true;
    }
    /// <summary>
    ///     Draw the text as affected by the tag
    /// </summary>
    /// <param name="sb">Can be null. If null, the string is simply being measured (so size must still be modified)</param>
    /// <param name="size">Current string size. Must be modified for correct spacing when drawing and measuring</param>
    /// <param name="position">The drawing position</param>
    /// <param name="color">The color of the string being drawn</param>
    /// <param name="scale">The scale of the string being drawn</param>
    public abstract void Draw(SpriteBatch sb, ref Vector2 size, Vector2 position, Color color, float scale);
    public void Unload() { }
}
