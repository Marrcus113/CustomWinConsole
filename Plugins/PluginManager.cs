using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;

namespace CustomWinConsole;

public class PluginManager
{
    private readonly string _pluginsDir;
    private readonly string _hostDir;
    private readonly Dictionary<string, ICwcPlugin> _loaded = new();
    private readonly Dictionary<string, Action<string[]>> _builtins;
    private readonly PluginHost _host;
    private readonly PluginLoadContext _loadContext;

    public IReadOnlyDictionary<string, ICwcPlugin> Loaded => _loaded;
    public string PluginsDir => _pluginsDir;

    public PluginManager(Dictionary<string, Action<string[]>> builtins)
    {
        _builtins = builtins;
        _host = new PluginHost(builtins);
        _hostDir = AppContext.BaseDirectory;
        _pluginsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".customwinconsole", "plugins");
        Directory.CreateDirectory(_pluginsDir);
        _loadContext = new PluginLoadContext(_hostDir);
    }

    public void LoadAll()
    {
        if (!Directory.Exists(_pluginsDir)) return;
        foreach (var dll in Directory.GetFiles(_pluginsDir, "*.dll"))
            LoadPlugin(dll);
    }

    public bool LoadPlugin(string dllPath)
    {
        try
        {
            var asm = _loadContext.LoadFromAssemblyPath(dllPath);
            foreach (var type in asm.GetExportedTypes())
            {
                if (typeof(ICwcPlugin).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                {
                    if (Activator.CreateInstance(type) is ICwcPlugin plugin)
                    {
                        _loaded[plugin.Name] = plugin;
                        plugin.Register(_host);
                        return true;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  Ошибка загрузки плагина {Path.GetFileName(dllPath)}: {ex.Message}");
            Console.ResetColor();
        }
        return false;
    }

    public bool Remove(string name)
    {
        if (!_loaded.ContainsKey(name)) return false;
        _loaded.Remove(name);
        return true;
    }

    public async Task<bool> Install(string name, string? url = null)
    {
        url ??= $"https://github.com/Marrcus113/CustomWinConsolePlugins/releases/latest/download/{name}.dll";
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            var bytes = await http.GetByteArrayAsync(url);
            var path = Path.Combine(_pluginsDir, $"{name}.dll");
            File.WriteAllBytes(path, bytes);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  Плагин '{name}' установлен!");
            Console.ResetColor();
            LoadPlugin(path);
            return true;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  Ошибка установки плагина '{name}': {ex.Message}");
            Console.ResetColor();
            return false;
        }
    }

    public void ListPlugins()
    {
        if (_loaded.Count == 0)
        {
            Console.WriteLine("  Плагинов не установлено.");
            return;
        }
        Console.WriteLine($"  Установлено плагинов: {_loaded.Count}");
        Console.WriteLine();
        foreach (var (name, plugin) in _loaded)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"  {name}");
            Console.ResetColor();
            Console.Write($"  v{plugin.Version}");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"  — {plugin.Description}");
            Console.ResetColor();
            Console.WriteLine();
        }
    }
}

public class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    public PluginLoadContext(string hostDir)
    {
        _resolver = new AssemblyDependencyResolver(hostDir);
    }

    protected override Assembly? Load(AssemblyName name)
    {
        var path = _resolver.ResolveAssemblyToPath(name);
        if (path != null)
            return LoadFromAssemblyPath(path);

        try
        {
            return Assembly.Load(name);
        }
        catch
        {
            return null;
        }
    }
}
