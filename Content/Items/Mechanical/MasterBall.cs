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
using Terramon.Content.NPCs.Pokemon;


namespace Terramon.Content.Items.Mechanical
{
    class MasterBallProjectile : BasePkballProjectile
    {
        public override int pokeballCapture => ModContent.ItemType<MasterBallItem>();
        public override float catchModifier => 2f;
        public override bool CatchPokemonChances(PokemonNPC capture, float random) => true;
    }

    class MasterBallItem : BasePkballItem
    {
        protected override int UseRarity => ModContent.RarityType<MasterBallRarity>();
        public override int pokeballThrow => ModContent.ProjectileType<MasterBallProjectile>();
        public override void SetStaticDefaults()
        {
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = igPrice / 2; //Amount needed to duplicate them in Journey Mode
        }
    }
    public class MasterBallRarity : ModRarity
    {
        public override Color RarityColor => new(164, 96, 178);
    }
}
