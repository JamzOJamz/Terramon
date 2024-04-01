using System.IO;

namespace Terramon.Content.AI;

public abstract class AIController
{
    protected readonly NPC NPC;
    protected const int FrameSpeed = 10;

    protected AIController(NPC npc)
    {
        NPC = npc;
    }

    public abstract void AI();

    public abstract void FindFrame(int frameHeight);

    public virtual void SendExtraAI(BinaryWriter writer)
    {
    }

    public virtual void ReceiveExtraAI(BinaryReader reader)
    {
    }
}