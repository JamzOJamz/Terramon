namespace Terramon.Core.Battling.TurnBased;

public interface IShowdownService : IDisposable
{
    void WriteCommand(string type, object[] data);
    event Action<string, string[]> ReceiveMessage;
}