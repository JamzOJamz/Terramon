namespace Terramon.Core.Battling.TurnBased;

public interface IShowdownProvider : IDisposable
{
    void WriteCommand(string type, object[] data);
    event Action<string, string[]> ReceiveMessage;
}