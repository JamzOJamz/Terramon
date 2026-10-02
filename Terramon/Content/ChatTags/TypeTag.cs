using ReLogic.Content;
using ReLogic.Graphics;
using Terramon.ID;

namespace Terramon.Content.ChatTags;

public sealed class TypeTag : LoadableChatTag
{
    public static readonly Asset<Texture2D> TypeIcons = ModContent.Request<Texture2D>("Terramon/Assets/Misc/TypeIcons");
    public PokemonType Type;
    public override string[] Aliases => ["ptype"];
    public override void Initialize(string[] args)
    {
        if (!Enum.TryParse(Text, true, out PokemonType type))
            return;
        Type = type;
    }
    public override void Draw(SpriteBatch sb, ref Vector2 size, Vector2 position, Color color, float scale)
    {
        var tex = TypeIcons.Value;
        var frame = tex.Frame(19, 1, (int)Type, 0, -2);
        size.X += frame.Width + 4;
        if (sb is null)
            return;
        position.Y -= 3;
        sb.Draw(tex, position, frame, Type.GetColor().MultiplyRGB(color), 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }
    public override float GetStringLength(DynamicSpriteFont font) => TypeIcons.Width() / 19 * Scale;
    public static string GenerateTag(PokemonType type) => $"[{ModContent.GetInstance<TypeTag>().Aliases[0]}:{type}]";
}
