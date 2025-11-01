using Microsoft.Xna.Framework.Input;
using ReLogic.Content;
using Terramon.Content.Items;
using Terramon.Core.Loaders.UILoading;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.UI;
using Terraria.UI.Chat;

namespace Terramon.Content.GUI;

public class TooltipOverlay : SmartUIState, ILoadable
{
    public enum HeldPokemonSource
    {
        Unspecified,
        Party,
        PC
    }

    private static string _text = string.Empty;
    private static Color _color = new(232, 232, 244);
    private static string _tooltip = string.Empty;
    private static Asset<Texture2D> _icon;
    private static PokemonData _heldPokemon;
    private static HeldPokemonSource _heldPokemonSource;
    private static bool _hoveringTrash;
    private static bool _hoveringAnyPCTile;

    private static Action<PokemonData> _onReturn;
    private static Action<PokemonData, HeldPokemonSource> _onPlace;

    public override bool Visible => true;

    private static readonly Asset<Texture2D> ReleaseTexture;
    private static readonly Asset<Texture2D> TrashTexture;
    
    static TooltipOverlay()
    {
        if (Main.dedServ) return;
        
        ReleaseTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/Miscellaneous/Release");
        TrashTexture = ModContent.Request<Texture2D>("Terraria/Images/Trash");
    }

    public void Load(Mod mod)
    {
        if (Main.dedServ) return;
        
        On_Main.DrawInterface += static (orig, self, gameTime) =>
        {
            orig(self, gameTime);
            Reset();
        };

        On_Main.DrawMap += static (orig, self, gameTime) =>
        {
            orig(self, gameTime);
            Reset();
        };

        On_Main.DrawInterface_40_InteractItemIcon += (orig, self) =>
        {
            if (_heldPokemon == null) orig(self);
        };

        On_Main.DrawPendingMouseText += orig =>
        {
            if (_heldPokemon != null)
            {
                if (Main.HoverItem.type > ItemID.None)
                {
                    Main.instance._mouseTextCache = default;
                    if (_hoveringTrash)
                        Main.instance.MouseText(
                            $"{(Main.keyState.IsKeyDown(Keys.LeftShift) || Main.keyState.IsKeyDown(Keys.RightShift) ? "Left" : "Shift")} click to release {_heldPokemon.DisplayName}",
                            _heldPokemon.IsShiny ? ModContent.RarityType<KeyItemRarity>() : ItemRarityID.White);
                }

                var mouseItem = Main.mouseItem;
                Main.mouseItem = new Item
                {
                    type = ItemID.IronPickaxe,
                    stack = 1
                };
                Main.HoverItem.type = ItemID.None;
                orig();
                Main.mouseItem = mouseItem;
            }
            else
            {
                orig();
            }
        };

        On_Main.TryAllowingToCraftRecipe += (On_Main.orig_TryAllowingToCraftRecipe orig, Recipe recipe, bool crafting,
            out bool allowCrafting) =>
        {
            if (_heldPokemon != null) return allowCrafting = false;

            return orig(recipe, crafting, out allowCrafting);
        };

        On_ItemSlot.LeftClick_refItem_int += (On_ItemSlot.orig_LeftClick_refItem_int orig, ref Item inv, int context) =>
        {
            if (_heldPokemon == null)
            {
                orig(ref inv, context);
            }
            else
            {
                if (inv != Main.LocalPlayer.trashItem || !InventoryParty.InPCMode) return;

                _hoveringTrash = true;

                if (Main.mouseLeft && Main.mouseLeftRelease)
                {
                    if (Main.keyState.IsKeyDown(Keys.LeftShift) || Main.keyState.IsKeyDown(Keys.RightShift))
                    {
                        SoundEngine.PlaySound(SoundID.Grab);
                        ClearHeldPokemon();
                    }
                    else
                    {
                        SoundEngine.PlaySound(new SoundStyle("Terramon/Sounds/button_locked")
                        {
                            Volume = 0.25f
                        });
                    }
                }

                if (_heldPokemon != null && Main.HoverItem.IsAir)
                    Main.instance.MouseText(
                        $"{(Main.keyState.IsKeyDown(Keys.LeftShift) || Main.keyState.IsKeyDown(Keys.RightShift) ? "Left" : "Shift")} click to release {_heldPokemon.DisplayName}",
                        _heldPokemon.IsShiny ? ModContent.RarityType<KeyItemRarity>() : ItemRarityID.White);
            }
        };

        On_ItemSlot.LeftClick_ItemArray_int_int += (orig, inv, context, slot)
            =>
        {
            if (_heldPokemon == null) orig(inv, context, slot);
        };
    }

    public override int InsertionIndex(List<GameInterfaceLayer> layers)
    {
        return layers.Count - 1;
    }

    public static void SetName(string newName)
    {
        _text = newName;
    }

    public static void SetColor(Color newColor)
    {
        _color = newColor;
    }

    public static void SetTooltip(string newTooltip)
    {
        _tooltip = newTooltip;
    }

    public static void SetIcon(Asset<Texture2D> newIcon)
    {
        _icon = newIcon;
    }

    public static void SetHeldPokemon(PokemonData pokemon, HeldPokemonSource source = HeldPokemonSource.Unspecified,
        Action<PokemonData> onReturn = null,
        Action<PokemonData, HeldPokemonSource> onPlace = null)
    {
        _onPlace?.Invoke(pokemon, source);
        _heldPokemon = pokemon;
        if (_heldPokemonSource != source)
            _onReturn = onReturn;
        _heldPokemonSource = source;
        _onPlace = onPlace;
    }

    public static PokemonData GetHeldPokemon(out HeldPokemonSource source)
    {
        source = _heldPokemonSource;
        return _heldPokemon;
    }

    public static void ClearHeldPokemon(bool ret = false, bool place = true)
    {
        if (ret) _onReturn?.Invoke(_heldPokemon);
        else if (place) _onPlace?.Invoke(null, _heldPokemonSource);
        _heldPokemon = null;
        _heldPokemonSource = HeldPokemonSource.Unspecified;
        _onReturn = null;
        _onPlace = null;

        TextureAssets.Trash = TrashTexture;
    }

    public static bool IsHoldingPokemon()
    {
        return _heldPokemon != null;
    }
    
    public static void SetHoveringAnyPCTile(bool hovering)
    {
        _hoveringAnyPCTile = hovering;
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (_heldPokemon != null)
        {
            TextureAssets.Trash = InventoryParty.InPCMode ? ReleaseTexture : TrashTexture;
            
            if (!_hoveringAnyPCTile && Main.mouseRight && Main.mouseRightRelease)
            {
                SoundEngine.PlaySound(SoundID.Grab);
                ClearHeldPokemon(true);
                return;
            }

            DrawHeldPokemon(spriteBatch);
            return;
        }

        if (_text == string.Empty)
            return;

        var font = FontAssets.MouseText.Value;

        var nameWidth = ChatManager.GetStringSize(font, _text, Vector2.One).X;
        if (_icon != null)
            nameWidth += 22;
        var tipWidth = ChatManager.GetStringSize(font, _tooltip, Vector2.One).X * 0.9f;

        var width = Math.Max(nameWidth, tipWidth);
        float height = -4;
        Vector2 pos;

        if (Main.MouseScreen.X > Main.screenWidth - width)
            pos = Main.MouseScreen - new Vector2(width + 33, 33);
        else
            pos = Main.MouseScreen + new Vector2(33, 33);

        height += ChatManager.GetStringSize(font, "{Dummy}", Vector2.One).Y;
        height += ChatManager.GetStringSize(font, _tooltip, Vector2.One * 0.9f).Y;

        if (pos.Y + height > Main.screenHeight)
            pos.Y -= height;

        Utils.DrawInvBG(Main.spriteBatch,
            new Rectangle((int)pos.X - 13, (int)pos.Y - 10, (int)width + 26, (int)height + 20),
            new Color(36, 33, 96) * 0.925f);

        if (_icon != null)
        {
            spriteBatch.Draw(_icon.Value, pos + new Vector2(-1, 3),
                null, Color.White,
                0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
            pos.X += 22;
        }

        Utils.DrawBorderString(Main.spriteBatch, _text, pos, _color);
        pos.Y += ChatManager.GetStringSize(font, _text, Vector2.One).Y + 4;
        if (_icon != null)
            pos.X -= 22;

        Utils.DrawBorderString(Main.spriteBatch, _tooltip, pos, new Color(218, 218, 242), 0.9f);
    }

    private static void DrawHeldPokemon(SpriteBatch spriteBatch)
    {
        var sprite = _heldPokemon.GetMiniSprite();
        spriteBatch.Draw(sprite.Value, Main.MouseScreen - new Vector2(7, 2), null, Color.White, 0f, Vector2.Zero,
            Main.cursorScale * 0.85f, SpriteEffects.None, 0f);
    }

    public override void SafeUpdate(GameTime gameTime)
    {
        if (!Main.mouseItem.IsAir && _heldPokemon != null)
            ClearHeldPokemon(true);
    }

    private static void Reset()
    {
        _text = string.Empty;
        _color = new Color(232, 232, 244);
        _tooltip = string.Empty;
        _icon = null;
        _hoveringTrash = false;
        _hoveringAnyPCTile = false;
        
        var needsClear = false;
        if (_heldPokemon != null)
            switch (_heldPokemonSource)
            {
                case HeldPokemonSource.Party:
                    if (!Main.playerInventory && !HubUI.Active)
                        needsClear = true;
                    break;
                case HeldPokemonSource.PC:
                    if (TerramonPlayer.LocalPlayer.ActivePCTileEntityID == -1)
                        needsClear = true;
                    break;
                case HeldPokemonSource.Unspecified:
                default:
                    break;
            }

        if (!needsClear) return;
        SoundEngine.PlaySound(SoundID.Grab);
        ClearHeldPokemon(true);
    }
}