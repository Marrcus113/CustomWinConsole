namespace CustomWinConsole;

public interface ICwcPlugin
{
    string Name { get; }
    string Version { get; }
    string Description { get; }
    void Register(IPluginHost host);
}
