using Terramon.Core.NPCComponents;

// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ConvertToConstant.Global

namespace Terramon.Content.NPCs;

/// <summary>
///     A <see cref="NPCComponent" /> that sets the width and height of an NPC.
/// </summary>
public class NPCTransform : NPCComponent
{
    public int Height = 20;
    public int Width = 20;

    public override void SetDefaults(NPC npc)
    {
        base.SetDefaults(npc);
        if (!Enabled) return;
        npc.width = Width;
        npc.height = Height;
    }
}