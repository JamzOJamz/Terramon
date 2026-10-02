using Terramon.Content.ChatTags;
using Terramon.ID;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.UI;
using static Terraria.GameContent.UI.Elements.UIBestiaryEntryInfoPage;

namespace Terramon.Core.Systems.NPCTypingSystem;

public sealed class NPCTypingGlobalNPC : GlobalNPC
{
    public static LocalizedText TypeDisplay;
    public override void SetStaticDefaults()
    {
        TypeDisplay = Terramon.Instance.GetLocalization("GUI.Pokedex.TypeDisplay");
    }
    public sealed class BestiaryTypingElement : IBestiaryInfoElement, IBestiaryPrioritizedElement, ICategorizedBestiaryInfoElement
    {
        public PokemonType Primary, Secondary;
        public float OrderPriority => 1f;
        public BestiaryInfoCategory ElementCategory => BestiaryInfoCategory.FlavorText;
        public BestiaryTypingElement(NPC npc) => (Primary, Secondary) = NPCTyping.Get(npc);
        public UIElement ProvideUIElement(BestiaryUICollectionInfo info)
        {
            // This code mostly adapted from vanilla Bestiary code
            UIPanel backPanel = new(Main.Assets.Request<Texture2D>("Images/UI/Bestiary/Stat_Panel"), null, customBarSize: 7)
            {
                Width = StyleDimension.FromPixelsAndPercent(-11f, 1f),
                Height = StyleDimension.FromPixels(30f),
                BackgroundColor = new Color(43, 56, 101),
                BorderColor = Color.Transparent,
                Left = StyleDimension.FromPixels(-8f),
                HAlign = 1f
            };
            backPanel.OnUpdate += static e =>
            {
                if (e.IsMouseHovering)
                    Main.instance.MouseText(Terramon.Instance.GetLocalization("GUI.Pokedex.Types").Value);
            };
            backPanel.SetPadding(0f);
            var singleType = Secondary is 0;
            var typeDisplay = TypeDisplay.WithFormatArgs(Terramon.Instance.GetLocalization("Types." + Primary), TypeTag.GenerateTag(Primary));
            if (Secondary is not 0)
                typeDisplay = TypeDisplay.WithFormatArgs(typeDisplay, TypeDisplay.Format(Terramon.Instance.GetLocalization("Types." + Secondary), TypeTag.GenerateTag(Secondary)));
            UIText importantFlavorTextElement = new(typeDisplay, 0.8f)
            {
                HAlign = 0.5f,
                VAlign = 0.5f,
            };
            backPanel.Append(importantFlavorTextElement);

            return backPanel;
        }
    }
    public override void SetBestiary(NPC npc, BestiaryDatabase database, BestiaryEntry bestiaryEntry)
    {
        bestiaryEntry.Info.Add(new BestiaryTypingElement(npc));
    }
}
