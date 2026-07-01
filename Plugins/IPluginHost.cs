namespace CustomWinConsole;

public interface IPluginHost
{
    void RegisterCommand(string name, Action<string[]> handler);
    void Write(string text, ConsoleColor color = ConsoleColor.Gray);
    void WriteLine(string text = "");
    void WriteError(string text);
    string CurrentDirectory { get; }
    event Action<string>? OnCommandUnregistered;
}
