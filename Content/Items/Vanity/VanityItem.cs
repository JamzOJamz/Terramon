using Terramon.Content.Items.PokeBalls;
using Terramon.Core.Loaders;

namespace Terramon.Content.Items;

[LoadAfter(typeof(BasePkballMiniItem))]
public abstract class VanityItem : TerramonItem
{
    public override string Texture => "Terramon/Assets/Items/Vanity/" + GetType().Name;
}