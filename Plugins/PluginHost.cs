namespace CustomWinConsole;

public class PluginHost : IPluginHost
{
    private readonly Dictionary<string, Action<string[]>> _builtins;

    public PluginHost(Dictionary<string, Action<string[]>> builtins)
    {
        _builtins = builtins;
    }

    public string CurrentDirectory =>
        Program.CurrentDir;

    public event Action<string>? OnCommandUnregistered;

    public void RegisterCommand(string name, Action<string[]> handler)
    {
        _builtins[name.ToLower()] = handler;
    }

    public void Write(string text, ConsoleColor color = ConsoleColor.Gray)
    {
        Console.ForegroundColor = color;
        Console.Write(text);
        Console.ResetColor();
    }

    public void WriteLine(string text = "")
    {
        Console.WriteLine(text);
    }

    public void WriteError(string text)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"  Ошибка: {text}");
        Console.ResetColor();
    }
}
