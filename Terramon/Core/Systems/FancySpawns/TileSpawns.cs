using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.Achievements;
using Terraria.Enums;
using Terraria.GameContent.Achievements;

namespace Terramon.Core.Systems.FancySpawns;

public sealed class TileSpawns : GlobalTile
{
    // replace with OnTileKilled when hook exists
    public override void KillTile(int i, int j, int type, ref bool fail, ref bool effectOnly, ref bool noItem)
    {
        if (fail)
            return;
    }
    public override void NearbyEffects(int i, int j, int type, bool closer)
    {
        if (closer)
            return;

    }
}
