using System.Reflection;
using ReLogic.Content;
using Terramon.Content.Items;
using Terramon.Helpers;
using Terramon.ID;
using Terraria.GameContent.UI.Elements;

namespace Terramon.Content.GUI;

[Autoload(Side = ModSide.Client)]
internal class CharacterSelectPartyIndicator : ILoadable
{
    private static Asset<Texture2D> _emptyPokeBallTexture;
    private static FieldInfo _buttonLabelField;
    private static FieldInfo _deleteButtonLabelField;
    private static FieldInfo _deleteButtonField;

    public void Load(Mod mod)
    {
        _emptyPokeBallTexture = mod.Assets.Request<Texture2D>("Assets/GUI/Miscellaneous/EmptyPokeBall");
        var type = typeof(UICharacterListItem);
        _buttonLabelField = type.GetField("_buttonLabel", BindingFlags.NonPublic | BindingFlags.Instance);
        _deleteButtonLabelField = type.GetField("_deleteButtonLabel", BindingFlags.NonPublic | BindingFlags.Instance);
        _deleteButtonField = type.GetField("_deleteButton", BindingFlags.NonPublic | BindingFlags.Instance);
        On_UICharacterListItem.DrawSelf += UICharacterListItemDrawSelf_Detour;
    }

    public void Unload()
    {
        _emptyPokeBallTexture = null;
        _buttonLabelField = null;
        On_UICharacterListItem.DrawSelf -= UICharacterListItemDrawSelf_Detour;
    }

    private static void UICharacterListItemDrawSelf_Detour(On_UICharacterListItem.orig_DrawSelf orig,
        UICharacterListItem self, SpriteBatch spriteBatch)
    {
        orig(self, spriteBatch);

        if (((UIText)_buttonLabelField.GetValue(self))!.Text != string.Empty)
            return;

        var modPlayer = self.Data.Player.GetModPlayer<TerramonPlayer>();
        var selfPosition = self.GetDimensions().Position();
        if (!modPlayer.HasChosenStarter ||
            new Rectangle((int)selfPosition.X + 11, (int)selfPosition.Y + 69, 92, 20).Contains(
                Main.MouseScreen.ToPoint()))
            return;

        var indicatorDrawPos = selfPosition + new Vector2(110, 71);
        var deleteButtonLabel = (UIText)_deleteButtonLabelField.GetValue(self)!;
        var hoverConsumed = false;
        for (var i = 0; i < modPlayer.Party.Length; i++)
        {
            var poke = modPlayer.Party[i];
            var ballDrawPos = indicatorDrawPos + new Vector2(i * 18, 0);
            spriteBatch.Draw(poke != null ? BallAssets.GetBallIcon(BallID.PokeBall).Value : _emptyPokeBallTexture.Value,
                ballDrawPos, null,
                Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
            if (hoverConsumed || poke == null ||
                !Main.MouseScreen.Between(ballDrawPos, ballDrawPos + new Vector2(16, 16))) continue;
            deleteButtonLabel.SetText(poke.DisplayName);
            deleteButtonLabel.TextColor =
                poke.IsShiny ? ModContent.GetInstance<KeyItemRarity>().RarityColor : Color.White;
            deleteButtonLabel.Recalculate();
            hoverConsumed = true;
        }

        if (hoverConsumed || ((UIImageButton)_deleteButtonField.GetValue(self))!.IsMouseHovering) return;
        deleteButtonLabel.SetText(string.Empty);
        deleteButtonLabel.TextColor = Color.White;
    }
}