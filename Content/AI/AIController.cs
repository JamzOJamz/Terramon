using System.IO;

namespace Terramon.Content.AI;

public abstract class AIController(NPC npc)
{
    protected readonly NPC NPC = npc;
    protected const int FrameSpeed = 10;

    public abstract void AI();

    public abstract void FindFrame(int frameHeight);

    public virtual void SendExtraAI(BinaryWriter writer)
    {
    }

    public virtual void ReceiveExtraAI(BinaryReader reader)
    {
    }
}