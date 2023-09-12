using Microsoft.Xna.Framework;
using Terraria.GameContent.Creative;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;
using Terraria;
using Terraria.ID;


namespace Terramon.Content.Items.Mechanical
{
    class PremierBallProjectile : BasePkballProjectile
    {
        public override int pokeballCapture => ModContent.ItemType<PremierBallItem>();
        public override float catchModifier => 1;
    }

    class PremierBallItem : BasePkballItem
    {
        public override int pokeballThrow => ModContent.ProjectileType<PremierBallProjectile>();
        public override int igPrice => 200;
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault($"Premier Ball");
            // Tooltip.SetDefault("A somewhat rare Poké Ball that has \nbeen specially made to commemorate an \nevent of some sort.");
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = igPrice / 2; //Amount needed to duplicate them in Journey Mode

        }
    }
}
