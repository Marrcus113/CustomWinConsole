using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CustomWinConsole;

class Program
{
    static string currentDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    public static string CurrentDir => currentDir;
    static List<string> history = new();
    static int historyIndex = -1;
    static bool running = true;
    public static Stopwatch? TimerSw;
    static string promptTemplate = "{dir}> ";
    static ConsoleColor themePrimary = ConsoleColor.White;
    static ConsoleColor themeAccent = ConsoleColor.Cyan;
    static ConsoleColor themePrompt = ConsoleColor.White;
    static ConsoleColor themeDir = ConsoleColor.Cyan;
    static ConsoleColor themeError = ConsoleColor.Red;
    static ConsoleColor themeFile = ConsoleColor.Gray;
    static ConsoleColor themeDirColor = ConsoleColor.Cyan;
    static ConsoleColor themeExe = ConsoleColor.Green;
    static Dictionary<string, string> aliases = new();
    static Dictionary<string, Action<string[]>> builtins = new();
    static List<string> todoList = new();
    static string todoFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".customwinconsole_todo.json");
    static Stopwatch? activeStopwatch;
    static PluginManager? pluginManager;

    static void Main()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Console.Title = "CustomWinConsole";        EnableVirtualTerminal();
        Console.OutputEncoding = Encoding.UTF8;
        InitBuiltins();
        pluginManager = new PluginManager(builtins);
        pluginManager.LoadAll();
        LoadAliases();

        PrintBanner();

        while (running)
        {
            PrintPrompt();
            var input = ReadLine();
            if (string.IsNullOrWhiteSpace(input)) continue;

            history.Add(input);
            historyIndex = history.Count;

            // Resolve aliases
            var resolved = ResolveAliases(input);
            if (!TryRunBuiltin(resolved))
                RunSystemCommand(resolved);
        }

        SaveAliases();
    }

    static void PrintBanner()
    {
        Console.ForegroundColor = themeAccent;
        Console.WriteLine(@"
  ╔══════════════════════════════════════╗
  ║       CustomWinConsole              ║
  ║  Type 'help' for commands list      ║
  ╚══════════════════════════════════════╝");
        Console.ResetColor();
        Console.WriteLine();
    }

    static void InitBuiltins()
    {
        builtins["help"] = _ => Console.WriteLine(@"
  System commands:  dir, cd, echo, ipconfig, whoami, tasklist, systeminfo, etc.
  Supports: pipes (|), redirects (> >>), aliases

  Custom commands:
    help              This help
    clear/cls         Clear screen
    exit/quit         Exit
    ls [dir]          Colored directory listing
    theme <name>      Switch theme (matrix/ocean/sunset/cyberpunk/minimal/fire)
    prompt <template> Set prompt ({dir} {user} {host} {time} {date})
    alias <n>=<cmd>   Create alias (alias ll=ls)
    unalias <n>       Remove alias
    aliases           List aliases
    colors            Show color palette
    neofetch          System info with art
    ascii             ASCII art banner
    calc <expr>       Calculator
    tree [dir]        Directory tree
    history           Command history
    curl <url>        Fetch URL
    open <path>       Open file/folder with default app
    cat <file>        Print file contents
    grep <pat> <file> Search pattern in file
    head <file> [n]   Show first lines
    wc <file>         Count lines/words/chars
    touch <file>      Create empty file
    mkdir <dir>       Create directory
    rm <path>         Delete file/directory
    cp <src> <dst>    Copy file
    mv <src> <dst>    Move/rename file
    unzip <file>      Extract ZIP
    download <url>    Download file
    size <path>       Show file/dir size
    find <name>       Search files by name
    edit <file>       Simple text editor
    script <file.cwc> Run script file
    todo add/list/done/clear  Task list
    fortune           Random quote
    cowsay <text>     Cow says text
    timer <sec>       Countdown timer
    stopwatch [stop]  Stopwatch
    clip <file>       Copy file to clipboard
    paste             Paste from clipboard
    gstat             git status
    glog              git log --oneline -15
    gbranch           git branch -v
    gdiff             git diff
    gadd <files>      git add
    gcommit <msg>     git commit -m");

        builtins["clear"] = _ => Console.Clear();
        builtins["cls"] = _ => Console.Clear();
        builtins["exit"] = _ => running = false;
        builtins["quit"] = _ => running = false;

        builtins["theme"] = args =>
        {
            if (args.Length == 0)
            {
                Console.WriteLine("  Available themes: matrix, ocean, sunset, cyberpunk, minimal, fire");
                Console.Write("  Current: ");
                Console.ForegroundColor = themeAccent;
                Console.WriteLine(GetCurrentThemeName());
                Console.ResetColor();
                return;
            }
            ApplyTheme(args[0].ToLower());
            Console.ForegroundColor = themeAccent;
            Console.WriteLine($"  Theme: {args[0]}");
            Console.ResetColor();
        };

        builtins["prompt"] = args =>
        {
            if (args.Length == 0)
            {
                Console.WriteLine("  Current prompt: " + promptTemplate);
                Console.WriteLine("  Variables: {dir} {user} {host} {time} {date}");
                Console.WriteLine("  Example: prompt {user}@{host}:{dir}$ ");
                return;
            }
            promptTemplate = string.Join(' ', args);
            Console.ForegroundColor = themeAccent;
            Console.WriteLine($"  Prompt: {promptTemplate}");
            Console.ResetColor();
        };

        builtins["alias"] = args =>
        {
            if (args.Length == 0)
            {
                ShowAliases();
                return;
            }
            var parts = string.Join(' ', args).Split('=', 2);
            if (parts.Length == 2)
            {
                aliases[parts[0].Trim()] = parts[1].Trim();
                SaveAliases();
                Console.ForegroundColor = themeAccent;
                Console.WriteLine($"  Alias: {parts[0]} = {parts[1]}");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = themeError;
                Console.WriteLine("  Usage: alias name=command");
                Console.ResetColor();
            }
        };

        builtins["unalias"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: unalias <name>"); return; }
            if (aliases.Remove(args[0]))
            {
                SaveAliases();
                Console.ForegroundColor = themeAccent;
                Console.WriteLine($"  Removed alias: {args[0]}");
                Console.ResetColor();
            }
        };

        builtins["aliases"] = _ => ShowAliases();

        builtins["neofetch"] = _ =>
        {
            var ramMB = GetTotalRamMB();
            var uptime = TimeSpan.FromMilliseconds(Environment.TickCount64);
            var now = DateTime.Now;
            Console.ForegroundColor = themeAccent;
            Console.WriteLine($@"
     ╔═══════════════════════════════╗
     ║  OS:       {RuntimeInformation.OSDescription}
     ║  Arch:     {RuntimeInformation.OSArchitecture}
     ║  .NET:     {Environment.Version}
     ║  CPU:      {Environment.ProcessorCount} cores
     ║  RAM:      {ramMB} MB
     ║  Uptime:   {uptime.Days}d {uptime.Hours}h {uptime.Minutes}m
     ║  Time:     {now:HH:mm:ss}
     ║  Date:     {now:yyyy-MM-dd}
     ║  User:     {Environment.UserName}
     ║  Host:     {Environment.MachineName}
     ║  Dir:      {currentDir}
     ║  Theme:    {GetCurrentThemeName()}
     ╚═══════════════════════════════╝");
            Console.ResetColor();
        };

        builtins["ascii"] = _ =>
        {
            Console.ForegroundColor = themeAccent;
            Console.WriteLine(@"
   ╔═╗╔═╗╔╗ ╦╔═╗╦ ╦
   ║  ║ ║╠╩╗║╣ ╚╦╝
   ╚═╝╚═╝╚═╝╩  ╩
  Custom Windows Console");
            Console.ResetColor();
        };

        builtins["calc"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: calc 2+2*3"); return; }
            try
            {
                var result = new System.Data.DataTable().Compute(string.Join(' ', args), "");
                Console.ForegroundColor = themeAccent;
                Console.WriteLine($"  = {result}");
                Console.ResetColor();
            }
            catch (Exception ex) { PrintError(ex.Message); }
        };

        builtins["tree"] = args =>
        {
            var target = args.Length > 0 ? Path.Combine(currentDir, args[0]) : currentDir;
            if (!Directory.Exists(target)) { PrintError($"Not found: {target}"); return; }
            Console.ForegroundColor = themeAccent;
            Console.WriteLine($"  {Path.GetFileName(target)}/");
            Console.ResetColor();
            PrintTree(target, "");
        };

        builtins["history"] = _ =>
        {
            for (int i = 0; i < history.Count; i++)
                Console.WriteLine($"  {i + 1,4}  {history[i]}");
        };

        builtins["colors"] = _ =>
        {
            foreach (ConsoleColor c in Enum.GetValues(typeof(ConsoleColor)))
            {
                Console.ForegroundColor = c;
                Console.Write($"  {c,-16}");
            }
            Console.ResetColor();
            Console.WriteLine();
        };

        builtins["curl"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: curl <url>"); return; }
            try
            {
                var url = args[0];
                if (!url.StartsWith("http")) url = "https://" + url;
                var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
                var response = http.GetStringAsync(url).Result;
                Console.WriteLine(response);
            }
            catch (Exception ex) { PrintError(ex.Message); }
        };

        builtins["open"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: open <path>"); return; }
            var path = args[0];
            if (path == ".") path = currentDir;
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true,
                    WorkingDirectory = currentDir
                });
                Console.ForegroundColor = themeAccent;
                Console.WriteLine($"  Opened: {path}");
                Console.ResetColor();
            }
            catch (Exception ex) { PrintError(ex.Message); }
        };

        builtins["cat"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: cat <file>"); return; }
            var path = Path.Combine(currentDir, args[0]);
            if (!File.Exists(path)) { PrintError($"Not found: {args[0]}"); return; }
            Console.WriteLine(File.ReadAllText(path));
        };

        builtins["grep"] = args =>
        {
            if (args.Length < 2) { Console.WriteLine("  Usage: grep <pattern> <file>"); return; }
            var path = Path.Combine(currentDir, args[1]);
            if (!File.Exists(path)) { PrintError($"Not found: {args[1]}"); return; }
            var lines = File.ReadAllLines(path);
            foreach (var line in lines)
            {
                if (line.Contains(args[0], StringComparison.OrdinalIgnoreCase))
                {
                    var idx = line.IndexOf(args[0], StringComparison.OrdinalIgnoreCase);
                    Console.Write("  ");
                    Console.Write(line[..idx]);
                    Console.ForegroundColor = themeAccent;
                    Console.Write(args[0]);
                    Console.ResetColor();
                    Console.WriteLine(line[(idx + args[0].Length)..]);
                }
            }
        };

        builtins["head"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: head <file> [lines]"); return; }
            var path = Path.Combine(currentDir, args[0]);
            if (!File.Exists(path)) { PrintError($"Not found: {args[0]}"); return; }
            var n = args.Length > 1 && int.TryParse(args[1], out var v) ? v : 10;
            foreach (var line in File.ReadAllLines(path).Take(n)) Console.WriteLine(line);
        };

        builtins["wc"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: wc <file>"); return; }
            var path = Path.Combine(currentDir, args[0]);
            if (!File.Exists(path)) { PrintError($"Not found: {args[0]}"); return; }
            var text = File.ReadAllText(path);
            Console.WriteLine($"  {text.Split('\n').Length} lines, {text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length} words, {text.Length} chars");
        };

        builtins["touch"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: touch <file>"); return; }
            File.WriteAllText(Path.Combine(currentDir, args[0]), "");
            Console.ForegroundColor = themeAccent; Console.WriteLine($"  Created: {args[0]}"); Console.ResetColor();
        };

        builtins["mkdir"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: mkdir <dir>"); return; }
            Directory.CreateDirectory(Path.Combine(currentDir, args[0]));
            Console.ForegroundColor = themeAccent; Console.WriteLine($"  Created: {args[0]}"); Console.ResetColor();
        };

        builtins["rm"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: rm <path>"); return; }
            var path = Path.Combine(currentDir, args[0]);
            if (File.Exists(path)) { File.Delete(path); Console.WriteLine($"  Deleted: {args[0]}"); }
            else if (Directory.Exists(path)) { Directory.Delete(path, true); Console.WriteLine($"  Deleted: {args[0]}"); }
            else PrintError($"Not found: {args[0]}");
        };

        builtins["cp"] = args =>
        {
            if (args.Length < 2) { Console.WriteLine("  Usage: cp <src> <dst>"); return; }
            var src = Path.Combine(currentDir, args[0]);
            var dst = Path.Combine(currentDir, args[1]);
            if (File.Exists(src)) { File.Copy(src, dst, true); Console.WriteLine($"  Copied: {args[0]} -> {args[1]}"); }
            else PrintError($"Not found: {args[0]}");
        };

        builtins["mv"] = args =>
        {
            if (args.Length < 2) { Console.WriteLine("  Usage: mv <src> <dst>"); return; }
            var src = Path.Combine(currentDir, args[0]);
            var dst = Path.Combine(currentDir, args[1]);
            if (File.Exists(src)) { File.Move(src, dst); Console.WriteLine($"  Moved: {args[0]} -> {args[1]}"); }
            else PrintError($"Not found: {args[0]}");
        };

        builtins["unzip"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: unzip <file> [dest]"); return; }
            var path = Path.Combine(currentDir, args[0]);
            if (!File.Exists(path)) { PrintError($"Not found: {args[0]}"); return; }
            var dest = args.Length > 1 ? Path.Combine(currentDir, args[1]) : currentDir;
            try
            {
                System.IO.Compression.ZipFile.ExtractToDirectory(path, dest, true);
                Console.ForegroundColor = themeAccent; Console.WriteLine($"  Extracted to: {dest}"); Console.ResetColor();
            }
            catch (Exception ex) { PrintError(ex.Message); }
        };

        builtins["download"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: download <url> [filename]"); return; }
            try
            {
                var url = args[0];
                if (!url.StartsWith("http")) url = "https://" + url;
                var filename = args.Length > 1 ? args[1] : Path.GetFileName(new Uri(url).AbsolutePath);
                if (string.IsNullOrEmpty(filename)) filename = "download";
                var dest = Path.Combine(currentDir, filename);
                Console.WriteLine($"  Downloading {url}...");
                var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
                var bytes = http.GetByteArrayAsync(url).Result;
                File.WriteAllBytes(dest, bytes);
                Console.ForegroundColor = themeAccent;
                Console.WriteLine($"  Saved: {filename} ({bytes.Length} bytes)");
                Console.ResetColor();
            }
            catch (Exception ex) { PrintError(ex.Message); }
        };

        builtins["size"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: size <path>"); return; }
            var path = Path.Combine(currentDir, args[0]);
            if (File.Exists(path))
            {
                var fi = new FileInfo(path);
                Console.WriteLine($"  {fi.Length} bytes ({fi.Length / 1024.0:F1} KB)");
            }
            else if (Directory.Exists(path))
            {
                var di = new DirectoryInfo(path);
                var size = di.EnumerateFiles("*", new EnumerationOptions { RecurseSubdirectories = true }).Sum(f => f.Length);
                Console.WriteLine($"  {size} bytes ({size / 1024.0:F1} KB)");
            }
            else PrintError($"Not found: {args[0]}");
        };

        builtins["find"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: find <name>"); return; }
            var pattern = "*" + args[0] + "*";
            var files = Directory.EnumerateFiles(currentDir, pattern, new EnumerationOptions { RecurseSubdirectories = true }).Take(50);
            foreach (var f in files)
            {
                var shortPath = f[(currentDir.Length + 1)..];
                Console.WriteLine($"  {shortPath}");
            }
        };

        // === NEW COMMANDS ===

        // ls — colored directory listing
        builtins["ls"] = args =>
        {
            var target = args.Length > 0 ? Path.Combine(currentDir, args[0]) : currentDir;
            if (!Directory.Exists(target)) { PrintError($"Not found: {target}"); return; }
            Console.WriteLine();
            var entries = Directory.GetFileSystemEntries(target);
            int dirs = 0, files = 0;
            foreach (var entry in entries)
            {
                var name = Path.GetFileName(entry);
                if (Directory.Exists(entry))
                {
                    Console.ForegroundColor = themeDirColor;
                    Console.Write("  📁 ");
                    Console.WriteLine(name + "/");
                    dirs++;
                }
                else
                {
                    var icon = GetFileIcon(name);
                    Console.ForegroundColor = GetFileColor(entry);
                    Console.Write($"  {icon} ");
                    Console.WriteLine(name);
                    files++;
                }
            }
            Console.ResetColor();
            Console.WriteLine($"\n  {dirs} dirs, {files} files");
        };

        // todo commands
        builtins["todo"] = args =>
        {
            LoadTodo();
            if (args.Length == 0)
            {
                Console.WriteLine("  Usage: todo add/list/done/clear");
                return;
            }
            switch (args[0].ToLower())
            {
                case "add":
                    if (args.Length < 2) { Console.WriteLine("  Usage: todo add <task>"); return; }
                    var task = string.Join(' ', args[1..]);
                    todoList.Add($"[ ] {task}");
                    SaveTodo();
                    Console.ForegroundColor = themeAccent;
                    Console.WriteLine($"  Added: {task}");
                    Console.ResetColor();
                    break;
                case "list":
                    if (todoList.Count == 0) { Console.WriteLine("  No tasks."); return; }
                    for (int i = 0; i < todoList.Count; i++)
                    {
                        Console.ForegroundColor = todoList[i].StartsWith("[x]") ? ConsoleColor.DarkGray : themeAccent;
                        Console.WriteLine($"  {i + 1}. {todoList[i]}");
                    }
                    Console.ResetColor();
                    break;
                case "done":
                    if (args.Length < 2 || !int.TryParse(args[1], out var idx) || idx < 1 || idx > todoList.Count)
                    { Console.WriteLine("  Usage: todo done <number>"); return; }
                    todoList[idx - 1] = "[x]" + todoList[idx - 1][3..];
                    SaveTodo();
                    Console.ForegroundColor = themeAccent;
                    Console.WriteLine($"  Done: {todoList[idx - 1]}");
                    Console.ResetColor();
                    break;
                case "clear":
                    todoList.Clear();
                    SaveTodo();
                    Console.ForegroundColor = themeAccent;
                    Console.WriteLine("  Todo cleared.");
                    Console.ResetColor();
                    break;
                default:
                    Console.WriteLine("  Usage: todo add/list/done/clear");
                    break;
            }
        };

        // fortune
        builtins["fortune"] = _ =>
        {
            var fortunes = new[]
            {
                "The best way to predict the future is to invent it.",
                "Code is like humor. When you have to explain it, it's bad.",
                "First, solve the problem. Then, write the code.",
                "Any fool can write code that a computer can understand.",
                "Programs must be written for people to read.",
                "Simplicity is the soul of efficiency.",
                "Talk is cheap. Show me the code. — Linus Torvalds",
                "It works on my machine.",
                "There are only two hard things: cache invalidation and naming things.",
                "Deleted code is debugged code.",
                "Measuring programming progress by lines of code is like measuring aircraft building progress by weight.",
                "Perfection is achieved not when there is nothing more to add, but when there is nothing left to take away.",
                "The most dangerous phrase: We've always done it this way.",
                "Make it work, make it right, make it fast.",
                "Debugging is twice as hard as writing the code in the first place.",
                "Unix is simple. It just takes a genius to understand its simplicity.",
                "The only way to go fast is to go well. — Robert C. Martin",
                "Clean code always looks like it was written by someone who cares.",
                "Code never lies, comments sometimes do.",
                "Programming today is a race between software engineers striving to build bigger programs and the universe trying to produce bigger idiots."
            };
            var rng = new Random();
            Console.ForegroundColor = themeAccent;
            Console.WriteLine($"\n  \"{fortunes[rng.Next(fortunes.Length)]}\"\n");
            Console.ResetColor();
        };

        // cowsay
        builtins["cowsay"] = args =>
        {
            var text = args.Length > 0 ? string.Join(' ', args) : "Moo!";
            var border = new string('─', text.Length + 2);
            Console.WriteLine($"  ┌{border}┐");
            Console.WriteLine($"  │ {text} │");
            Console.WriteLine($"  └{border}┘");
            Console.WriteLine("         \\   ^__^");
            Console.WriteLine("          \\  (oo)\\_______");
            Console.WriteLine("             (__)\\       )\\/\\");
            Console.WriteLine("                 ||----w |");
            Console.WriteLine("                 ||     ||");
        };

        // timer
        builtins["timer"] = args =>
        {
            if (args.Length == 0 || !int.TryParse(args[0], out var seconds) || seconds <= 0)
            { Console.WriteLine("  Usage: timer <seconds>"); return; }
            Console.ForegroundColor = themeAccent;
            Console.WriteLine($"  Timer: {seconds}s");
            Console.ResetColor();
            for (int i = seconds; i > 0; i--)
            {
                Console.Write($"\r  {i}s remaining... ");
                System.Threading.Thread.Sleep(1000);
            }
            Console.WriteLine("\r  Time's up!                    ");
            try { Console.Beep(800, 500); } catch { }
        };

        // stopwatch
        builtins["stopwatch"] = args =>
        {
            if (args.Length > 0 && args[0].ToLower() == "stop" && activeStopwatch != null)
            {
                activeStopwatch.Stop();
                Console.ForegroundColor = themeAccent;
                Console.WriteLine($"  Stopwatch: {activeStopwatch.Elapsed}");
                Console.ResetColor();
                activeStopwatch = null;
                return;
            }
            activeStopwatch = Stopwatch.StartNew();
            Console.ForegroundColor = themeAccent;
            Console.WriteLine("  Stopwatch started. Type 'stopwatch stop' to stop.");
            Console.ResetColor();
        };

        // edit — simple text editor
        builtins["edit"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: edit <file>"); return; }
            var path = Path.Combine(currentDir, args[0]);
            var lines = new List<string>();
            if (File.Exists(path))
                lines.AddRange(File.ReadAllLines(path));

            Console.Clear();
            Console.ForegroundColor = themeAccent;
            Console.WriteLine($"  === Editing: {args[0]} ===");
            Console.WriteLine("  Type lines. Empty line + Enter to save. Ctrl+C to cancel.");
            Console.ResetColor();
            Console.WriteLine();

            var editLines = new List<string>();
            while (true)
            {
                Console.ForegroundColor = themeDir;
                Console.Write($"  {editLines.Count + 1,3} > ");
                Console.ResetColor();
                var line = Console.ReadLine();
                if (line == null) break;
                if (string.IsNullOrEmpty(line) && editLines.Count > 0) break;
                editLines.Add(line);
            }

            File.WriteAllLines(path, editLines);
            Console.ForegroundColor = themeAccent;
            Console.WriteLine($"  Saved: {args[0]} ({editLines.Count} lines)");
            Console.ResetColor();
        };

        // script — execute .cwc files
        builtins["script"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: script <file.cwc>"); return; }
            var scriptPath = Path.Combine(currentDir, args[0]);
            if (!File.Exists(scriptPath)) { PrintError($"Not found: {args[0]}"); return; }
            var scriptLines = File.ReadAllLines(scriptPath);
            int lineNum = 0;
            foreach (var line in scriptLines)
            {
                lineNum++;
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#")) continue;
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"  [{lineNum}] {trimmed}");
                Console.ResetColor();
                if (!TryRunBuiltin(trimmed))
                    RunSystemCommand(trimmed);
            }
            Console.ForegroundColor = themeAccent;
            Console.WriteLine($"  Script done: {args[0]} ({lineNum} lines)");
            Console.ResetColor();
        };

        // clipboard
        builtins["clip"] = args =>
        {
            if (args.Length > 0)
            {
                var path = Path.Combine(currentDir, args[0]);
                if (!File.Exists(path)) { PrintError($"Not found: {args[0]}"); return; }
                var text = File.ReadAllText(path);
                SetClipboard(text);
                Console.ForegroundColor = themeAccent;
                Console.WriteLine($"  Copied to clipboard: {args[0]} ({text.Length} chars)");
                Console.ResetColor();
            }
            else
            {
                Console.WriteLine("  Reading from stdin (type text, Ctrl+Z to finish):");
                var lines = new List<string>();
                string? line;
                while ((line = Console.ReadLine()) != null) lines.Add(line);
                SetClipboard(string.Join('\n', lines));
                Console.ForegroundColor = themeAccent;
                Console.WriteLine($"  Copied to clipboard ({lines.Count} lines)");
                Console.ResetColor();
            }
        };

        builtins["paste"] = _ =>
        {
            var text = GetClipboard();
            if (!string.IsNullOrEmpty(text))
            {
                Console.ForegroundColor = themeAccent;
                Console.Write(text);
                Console.ResetColor();
            }
            else
                Console.WriteLine("  Clipboard is empty.");
        };

        // git shortcuts
        builtins["gstat"] = _ => RunSystemCommand("git status");
        builtins["glog"] = _ => RunSystemCommand("git log --oneline -15");
        builtins["gbranch"] = _ => RunSystemCommand("git branch -v");
        builtins["gdiff"] = _ => RunSystemCommand("git diff");
        builtins["gadd"] = args => RunSystemCommand($"git add {string.Join(' ', args)}");
        builtins["gcommit"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: gcommit <message>"); return; }
            RunSystemCommand($"git commit -m \"{string.Join(' ', args)}\"");
        };

        // === BATCH 2: 45 more commands ===

        // tail — last N lines of file
        builtins["tail"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: tail <file> [lines]"); return; }
            var path = Path.Combine(currentDir, args[0]);
            if (!File.Exists(path)) { PrintError($"Not found: {args[0]}"); return; }
            var n = args.Length > 1 && int.TryParse(args[1], out var v) ? v : 10;
            foreach (var line in File.ReadAllLines(path).TakeLast(n)) Console.WriteLine(line);
        };

        // uniq — remove duplicate lines
        builtins["uniq"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: uniq <file>"); return; }
            var path = Path.Combine(currentDir, args[0]);
            if (!File.Exists(path)) { PrintError($"Not found: {args[0]}"); return; }
            var lines = File.ReadAllLines(path).Distinct();
            foreach (var l in lines) Console.WriteLine(l);
        };

        // sort — sort lines
        builtins["sort"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: sort <file>"); return; }
            var path = Path.Combine(currentDir, args[0]);
            if (!File.Exists(path)) { PrintError($"Not found: {args[0]}"); return; }
            var lines = File.ReadAllLines(path).OrderBy(x => x);
            foreach (var l in lines) Console.WriteLine(l);
        };

        // rev — reverse text
        builtins["rev"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: rev <text>"); return; }
            Console.WriteLine(string.Join(' ', args).Reverse().ToArray());
        };

        // upper / lower
        builtins["upper"] = args => Console.WriteLine(string.Join(' ', args).ToUpper());
        builtins["lower"] = args => Console.WriteLine(string.Join(' ', args).ToLower());

        // repeat — repeat text N times
        builtins["repeat"] = args =>
        {
            if (args.Length < 2) { Console.WriteLine("  Usage: repeat <count> <text>"); return; }
            if (!int.TryParse(args[0], out var n)) { Console.WriteLine("  Invalid count"); return; }
            for (int i = 0; i < n; i++) Console.WriteLine(string.Join(' ', args[1..]));
        };

        // len — string length
        builtins["len"] = args => Console.WriteLine($"  {string.Join(' ', args).Length} chars");

        // md5 / sha256 — file hash
        builtins["md5"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: md5 <file>"); return; }
            var path = Path.Combine(currentDir, args[0]);
            if (!File.Exists(path)) { PrintError($"Not found: {args[0]}"); return; }
            var hash = System.Security.Cryptography.MD5.HashData(File.ReadAllBytes(path));
            Console.WriteLine($"  MD5: {Convert.ToHexString(hash).ToLower()}");
        };

        builtins["sha256"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: sha256 <file>"); return; }
            var path = Path.Combine(currentDir, args[0]);
            if (!File.Exists(path)) { PrintError($"Not found: {args[0]}"); return; }
            var hash = System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(path));
            Console.WriteLine($"  SHA256: {Convert.ToHexString(hash).ToLower()}");
        };

        // date / time (standalone)
        builtins["date"] = _ => Console.WriteLine($"  {DateTime.Now:yyyy-MM-dd}");
        builtins["time"] = _ => Console.WriteLine($"  {DateTime.Now:HH:mm:ss}");
        builtins["datetime"] = _ => Console.WriteLine($"  {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

        // uptime
        builtins["uptime"] = _ =>
        {
            var uptime = TimeSpan.FromMilliseconds(Environment.TickCount64);
            Console.WriteLine($"  {uptime.Days}d {uptime.Hours}h {uptime.Minutes}m {uptime.Seconds}s");
        };

        // whoami / hostname
        builtins["whoami"] = _ => Console.WriteLine($"  {Environment.UserName}@{Environment.MachineName}");
        builtins["hostname"] = _ => Console.WriteLine($"  {Environment.MachineName}");

        // echo — with color support
        builtins["echo"] = args => Console.WriteLine(string.Join(' ', args));

        // base64 encode/decode
        builtins["base64"] = args =>
        {
            if (args.Length < 2) { Console.WriteLine("  Usage: base64 encode/decode <text>"); return; }
            if (args[0].ToLower() == "encode")
                Console.WriteLine(Convert.ToBase64String(Encoding.UTF8.GetBytes(string.Join(' ', args[1..]))));
            else if (args[0].ToLower() == "decode")
                Console.WriteLine(Encoding.UTF8.GetString(Convert.FromBase64String(args[1])));
            else
                Console.WriteLine("  Usage: base64 encode/decode <text>");
        };

        // hex — text to hex
        builtins["hex"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: hex <text>"); return; }
            Console.WriteLine(BitConverter.ToString(Encoding.UTF8.GetBytes(string.Join(' ', args))).Replace("-", " "));
        };

        // uuid
        builtins["uuid"] = _ => Console.WriteLine($"  {Guid.NewGuid()}");

        // random — random number
        builtins["random"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine(new Random().Next()); return; }
            if (args.Length == 1 && int.TryParse(args[0], out var max))
                Console.WriteLine(new Random().Next(max));
            else if (args.Length == 2 && int.TryParse(args[0], out var min) && int.TryParse(args[1], out var max2))
                Console.WriteLine(new Random().Next(min, max2 + 1));
            else
                Console.WriteLine("  Usage: random [max] or random <min> <max>");
        };

        // choice — random from list
        builtins["choice"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: choice <a> <b> [c]..."); return; }
            Console.ForegroundColor = themeAccent;
            Console.WriteLine($"  {args[new Random().Next(args.Length)]}");
            Console.ResetColor();
        };

        // countdown — visual countdown
        builtins["countdown"] = args =>
        {
            if (args.Length == 0 || !int.TryParse(args[0], out var n)) { Console.WriteLine("  Usage: countdown <seconds>"); return; }
            for (int i = n; i > 0; i--)
            {
                var bar = new string('█', i * 20 / n) + new string('░', 20 - i * 20 / n);
                Console.Write($"\r  [{bar}] {i}s ");
                System.Threading.Thread.Sleep(1000);
            }
            Console.WriteLine("\r  [████████████████████] Done!       ");
        };

        // math functions
        builtins["sqrt"] = args =>
        {
            if (args.Length == 0 || !double.TryParse(args[0], out var n)) { Console.WriteLine("  Usage: sqrt <number>"); return; }
            Console.WriteLine($"  = {Math.Sqrt(n)}");
        };

        builtins["pow"] = args =>
        {
            if (args.Length < 2 || !double.TryParse(args[0], out var b) || !double.TryParse(args[1], out var e))
            { Console.WriteLine("  Usage: pow <base> <exp>"); return; }
            Console.WriteLine($"  = {Math.Pow(b, e)}");
        };

        builtins["pi"] = _ => Console.WriteLine($"  π = {Math.PI}");
        builtins["e"] = _ => Console.WriteLine($"  e = {Math.E}");

        // min / max
        builtins["min"] = args =>
        {
            var nums = args.Where(a => double.TryParse(a, out _)).Select(double.Parse).ToList();
            if (nums.Count == 0) { Console.WriteLine("  Usage: min <numbers...>"); return; }
            Console.WriteLine($"  = {nums.Min()}");
        };

        builtins["max"] = args =>
        {
            var nums = args.Where(a => double.TryParse(a, out _)).Select(double.Parse).ToList();
            if (nums.Count == 0) { Console.WriteLine("  Usage: max <numbers...>"); return; }
            Console.WriteLine($"  = {nums.Max()}");
        };

        // sum / avg
        builtins["sum"] = args =>
        {
            var nums = args.Where(a => double.TryParse(a, out _)).Select(double.Parse).ToList();
            if (nums.Count == 0) { Console.WriteLine("  Usage: sum <numbers...>"); return; }
            Console.WriteLine($"  = {nums.Sum()}");
        };

        builtins["avg"] = args =>
        {
            var nums = args.Where(a => double.TryParse(a, out _)).Select(double.Parse).ToList();
            if (nums.Count == 0) { Console.WriteLine("  Usage: avg <numbers...>"); return; }
            Console.WriteLine($"  = {nums.Average()}");
        };

        // factorial
        builtins["factorial"] = args =>
        {
            if (args.Length == 0 || !long.TryParse(args[0], out var n) || n < 0)
            { Console.WriteLine("  Usage: factorial <number>"); return; }
            long result = 1;
            for (long i = 2; i <= n; i++) result *= i;
            Console.WriteLine($"  {n}! = {result}");
        };

        // fib — fibonacci
        builtins["fib"] = args =>
        {
            if (args.Length == 0 || !int.TryParse(args[0], out var n) || n < 0)
            { Console.WriteLine("  Usage: fib <count>"); return; }
            long a = 0, b = 1;
            for (int i = 0; i < n; i++)
            {
                Console.Write($"{a} ");
                (a, b) = (b, a + b);
            }
            Console.WriteLine();
        };

        // prime — check if prime
        builtins["prime"] = args =>
        {
            if (args.Length == 0 || !long.TryParse(args[0], out var n))
            { Console.WriteLine("  Usage: prime <number>"); return; }
            if (n < 2) { Console.WriteLine("  Not prime"); return; }
            for (long i = 2; i * i <= n; i++)
            {
                if (n % i == 0) { Console.WriteLine($"  {n} is not prime (divisible by {i})"); return; }
            }
            Console.ForegroundColor = themeAccent;
            Console.WriteLine($"  {n} is prime!");
            Console.ResetColor();
        };

        // ascii table
        builtins["ascii"] = args =>
        {
            if (args.Length > 0 && args[0] == "table")
            {
                Console.WriteLine("  ASCII Table:");
                for (int i = 32; i < 127; i++)
                    Console.Write($"  {i,3} '{(char)i}'{(i % 8 == 7 ? "\n" : "")}");
                Console.WriteLine();
                return;
            }
            Console.ForegroundColor = themeAccent;
            Console.WriteLine(@"
   ╔═╗╔═╗╔╗ ╦╔═╗╦ ╦
   ║  ║ ║╠╩╗║╣ ╚╦╝
   ╚═╝╚═╝╚═╝╩  ╩
  Custom Windows Console");
            Console.ResetColor();
        };

        // bench — benchmark a command
        builtins["bench"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: bench <command>"); return; }
            var cmd = string.Join(' ', args);
            var sw = Stopwatch.StartNew();
            RunSystemCommand(cmd);
            sw.Stop();
            Console.ForegroundColor = themeAccent;
            Console.WriteLine($"  Time: {sw.ElapsedMilliseconds}ms");
            Console.ResetColor();
        };

        // which — find command path
        builtins["which"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: which <command>"); return; }
            RunSystemCommand($"where {args[0]}");
        };

        // env list
        builtins["env"] = args =>
        {
            if (args.Length == 0)
            {
                foreach (System.Collections.DictionaryEntry kv in Environment.GetEnvironmentVariables())
                    Console.WriteLine($"  {kv.Key}={kv.Value}");
            }
            else
            {
                var val = Environment.GetEnvironmentVariable(args[0]);
                Console.WriteLine(val ?? $"  {args[0]} not set");
            }
        };

        // export — set env var
        builtins["export"] = args =>
        {
            if (args.Length < 2) { Console.WriteLine("  Usage: export VAR=value"); return; }
            var parts = string.Join(' ', args).Split('=', 2);
            if (parts.Length == 2)
            {
                Environment.SetEnvironmentVariable(parts[0], parts[1]);
                Console.ForegroundColor = themeAccent;
                Console.WriteLine($"  {parts[0]}={parts[1]}");
                Console.ResetColor();
            }
        };

        // disk usage
        builtins["disk"] = _ =>
        {
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (!drive.IsReady) continue;
                var total = drive.TotalSize / 1024 / 1024 / 1024;
                var free = drive.AvailableFreeSpace / 1024 / 1024 / 1024;
                var used = total - free;
                var pct = total > 0 ? used * 100 / total : 0;
                Console.WriteLine($"  {drive.Name} {drive.VolumeLabel,-10} {used,4}GB / {total,4}GB ({pct}%)");
            }
        };

        // processes top
        builtins["top"] = _ =>
        {
            var procs = Process.GetProcesses()
                .OrderByDescending(p => p.WorkingSet64)
                .Take(15);
            Console.WriteLine($"  {"PID",-8} {"Memory",-12} {"Name"}");
            foreach (var p in procs)
            {
                try
                {
                    var mem = p.WorkingSet64 / 1024 / 1024;
                    Console.WriteLine($"  {p.Id,-8} {mem,-10} MB  {p.ProcessName}");
                }
                catch { }
            }
        };

        // kill process
        builtins["kill"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: kill <pid>"); return; }
            if (int.TryParse(args[0], out var pid))
            {
                try
                {
                    var proc = Process.GetProcessById(pid);
                    proc.Kill();
                    Console.ForegroundColor = themeAccent;
                    Console.WriteLine($"  Killed: {proc.ProcessName} (PID {pid})");
                    Console.ResetColor();
                }
                catch (Exception ex) { PrintError(ex.Message); }
            }
        };

        // pidof — find process by name
        builtins["pidof"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: pidof <name>"); return; }
            var procs = Process.GetProcessesByName(args[0]);
            foreach (var p in procs)
                Console.WriteLine($"  {p.Id}");
        };

        // memory info
        builtins["mem"] = _ =>
        {
            var memStatus = new MEMORYSTATUSEX();
            memStatus.dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>();
            if (GlobalMemoryStatusEx(ref memStatus))
            {
                var total = memStatus.ullTotalPhys / 1024 / 1024;
                var avail = memStatus.ullAvailPhys / 1024 / 1024;
                var used = total - avail;
                Console.WriteLine($"  Total:     {total} MB");
                Console.WriteLine($"  Used:      {used} MB ({memStatus.dwMemoryLoad}%)");
                Console.WriteLine($"  Available: {avail} MB");
            }
        };

        // cpu info
        builtins["cpu"] = _ =>
        {
            Console.WriteLine($"  Cores:     {Environment.ProcessorCount}");
            Console.WriteLine($"  64-bit:    {Environment.Is64BitOperatingSystem}");
        };

        // ping improved
        builtins["ping"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: ping <host>"); return; }
            RunSystemCommand($"ping -n 4 {args[0]}");
        };

        // dns
        builtins["dns"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: dns <host>"); return; }
            RunSystemCommand($"nslookup {args[0]}");
        };

        // curl improved
        builtins["curl"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: curl <url>"); return; }
            RunSystemCommand($"curl -s {args[0]}");
        };

        // === BATCH 3: 225 MORE COMMANDS ===

        // Text processing
        builtins["replace"] = args =>
        {
            if (args.Length < 3) { Console.WriteLine("  Usage: replace <file> <old> <new>"); return; }
            var path = Path.Combine(currentDir, args[0]);
            if (!File.Exists(path)) { PrintError($"Not found: {args[0]}"); return; }
            var text = File.ReadAllText(path);
            var result = text.Replace(args[1], args[2]);
            File.WriteAllText(path, result);
            Console.ForegroundColor = themeAccent;
            Console.WriteLine($"  Replaced '{args[1]}' -> '{args[2]}'");
            Console.ResetColor();
        };

        builtins["count"] = args =>
        {
            if (args.Length < 2) { Console.WriteLine("  Usage: count <file> <word>"); return; }
            var path = Path.Combine(currentDir, args[0]);
            if (!File.Exists(path)) { PrintError($"Not found: {args[0]}"); return; }
            var text = File.ReadAllText(path);
            var count = Regex.Matches(text, Regex.Escape(args[1]), RegexOptions.IgnoreCase).Count;
            Console.WriteLine($"  '{args[1]}' found {count} times");
        };

        builtins["words"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: words <file>"); return; }
            var path = Path.Combine(currentDir, args[0]);
            if (!File.Exists(path)) { PrintError($"Not found: {args[0]}"); return; }
            var words = File.ReadAllText(path).Split(' ', StringSplitOptions.RemoveEmptyEntries);
            Console.WriteLine($"  {words.Length} words");
            foreach (var w in words.GroupBy(x => x.ToLower()).OrderByDescending(x => x.Count()).Take(10))
                Console.WriteLine($"    {w.Key}: {w.Count()}");
        };

        builtins["lines"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: lines <file>"); return; }
            var path = Path.Combine(currentDir, args[0]);
            if (!File.Exists(path)) { PrintError($"Not found: {args[0]}"); return; }
            Console.WriteLine($"  {File.ReadAllLines(path).Length} lines");
        };

        builtins["trim"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: trim <text>"); return; }
            Console.WriteLine(string.Join(' ', args).Trim());
        };

        builtins["substr"] = args =>
        {
            if (args.Length < 3) { Console.WriteLine("  Usage: substr <text> <start> <length>"); return; }
            if (int.TryParse(args[1], out var start) && int.TryParse(args[2], out var len))
            {
                var text = args[0];
                if (start + len <= text.Length)
                    Console.WriteLine(text.Substring(start, len));
                else
                    Console.WriteLine("  Substring out of range");
            }
        };

        builtins["split"] = args =>
        {
            if (args.Length < 2) { Console.WriteLine("  Usage: split <text> <delimiter>"); return; }
            var parts = args[0].Split(args[1]);
            for (int i = 0; i < parts.Length; i++)
                Console.WriteLine($"  [{i}] {parts[i]}");
        };

        builtins["join"] = args =>
        {
            if (args.Length < 2) { Console.WriteLine("  Usage: join <delimiter> <words...>"); return; }
            Console.WriteLine(string.Join(args[0], args[1..]));
        };

        builtins["contains"] = args =>
        {
            if (args.Length < 2) { Console.WriteLine("  Usage: contains <text> <search>"); return; }
            Console.WriteLine(args[0].Contains(args[1], StringComparison.OrdinalIgnoreCase) ? "  true" : "  false");
        };

        builtins["startswith"] = args =>
        {
            if (args.Length < 2) { Console.WriteLine("  Usage: startswith <text> <search>"); return; }
            Console.WriteLine(args[0].StartsWith(args[1], StringComparison.OrdinalIgnoreCase) ? "  true" : "  false");
        };

        builtins["endswith"] = args =>
        {
            if (args.Length < 2) { Console.WriteLine("  Usage: endswith <text> <search>"); return; }
            Console.WriteLine(args[0].EndsWith(args[1], StringComparison.OrdinalIgnoreCase) ? "  true" : " false");
        };

        builtins["reverse"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: reverse <text>"); return; }
            Console.WriteLine(new string(args[0].Reverse().ToArray()));
        };

        builtins["duplicate"] = args =>
        {
            if (args.Length < 2) { Console.WriteLine("  Usage: duplicate <text> <count>"); return; }
            if (int.TryParse(args[1], out var n))
                Console.WriteLine(string.Concat(Enumerable.Repeat(args[0], n)));
        };

        builtins["pad"] = args =>
        {
            if (args.Length < 3) { Console.WriteLine("  Usage: pad <text> <width> <char>"); return; }
            if (int.TryParse(args[1], out var w) && args[2].Length > 0)
                Console.WriteLine(args[0].PadRight(w, args[2][0]));
        };

        builtins["center"] = args =>
        {
            if (args.Length < 2) { Console.WriteLine("  Usage: center <text> <width>"); return; }
            if (int.TryParse(args[1], out var w))
                Console.WriteLine(args[0].PadLeft((w + args[0].Length) / 2).PadRight(w));
        };

        builtins["wrap"] = args =>
        {
            if (args.Length < 2) { Console.WriteLine("  Usage: wrap <text> <width>"); return; }
            if (int.TryParse(args[1], out var w))
            {
                var text = args[0];
                for (int i = 0; i < text.Length; i += w)
                    Console.WriteLine(text.Substring(i, Math.Min(w, text.Length - i)));
            }
        };

        builtins["truncate"] = args =>
        {
            if (args.Length < 2) { Console.WriteLine("  Usage: truncate <text> <max>"); return; }
            if (int.TryParse(args[1], out var max))
                Console.WriteLine(args[0].Length > max ? args[0][..max] + "..." : args[0]);
        };

        // File operations
        builtins["exists"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: exists <path>"); return; }
            var path = Path.Combine(currentDir, args[0]);
            Console.WriteLine(File.Exists(path) || Directory.Exists(path) ? "  true" : "  false");
        };

        builtins["isdir"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: isdir <path>"); return; }
            Console.WriteLine(Directory.Exists(Path.Combine(currentDir, args[0])) ? "  true" : "  false");
        };

        builtins["isfile"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: isfile <path>"); return; }
            Console.WriteLine(File.Exists(Path.Combine(currentDir, args[0])) ? "  true" : "  false");
        };

        builtins["ext"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: ext <file>"); return; }
            Console.WriteLine(Path.GetExtension(args[0]));
        };

        builtins["name"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: name <path>"); return; }
            Console.WriteLine(Path.GetFileName(args[0]));
        };

        builtins["dir"] = args =>
        {
            var target = args.Length > 0 ? Path.Combine(currentDir, args[0]) : currentDir;
            if (!Directory.Exists(target)) { PrintError($"Not found: {target}"); return; }
            foreach (var d in Directory.GetDirectories(target))
                Console.WriteLine($"  📁 {Path.GetFileName(d)}/");
            foreach (var f in Directory.GetFiles(target))
                Console.WriteLine($"  📄 {Path.GetFileName(f)}");
        };

        builtins["extensions"] = args =>
        {
            var target = args.Length > 0 ? Path.Combine(currentDir, args[0]) : currentDir;
            if (!Directory.Exists(target)) { PrintError($"Not found: {target}"); return; }
            var exts = Directory.GetFiles(target)
                .GroupBy(f => Path.GetExtension(f).ToLower())
                .OrderByDescending(g => g.Count());
            foreach (var g in exts)
                Console.WriteLine($"  {g.Key ?? "(none)",-10} {g.Count()} files");
        };

        builtins["biggest"] = args =>
        {
            var target = args.Length > 0 ? Path.Combine(currentDir, args[0]) : currentDir;
            if (!Directory.Exists(target)) { PrintError($"Not found: {target}"); return; }
            var files = Directory.GetFiles(target, "*", new EnumerationOptions { RecurseSubdirectories = true })
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.Length)
                .Take(10);
            foreach (var f in files)
                Console.WriteLine($"  {f.Length / 1024.0,10:F1} KB  {f.FullName[(target.Length + 1)..]}");
        };

        builtins["newest"] = args =>
        {
            var target = args.Length > 0 ? Path.Combine(currentDir, args[0]) : currentDir;
            if (!Directory.Exists(target)) { PrintError($"Not found: {target}"); return; }
            var files = Directory.GetFiles(target, "*", new EnumerationOptions { RecurseSubdirectories = true })
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.LastWriteTime)
                .Take(10);
            foreach (var f in files)
                Console.WriteLine($"  {f.LastWriteTime:yyyy-MM-dd HH:mm}  {f.FullName[(target.Length + 1)..]}");
        };

        builtins["oldest"] = args =>
        {
            var target = args.Length > 0 ? Path.Combine(currentDir, args[0]) : currentDir;
            if (!Directory.Exists(target)) { PrintError($"Not found: {target}"); return; }
            var files = Directory.GetFiles(target, "*", new EnumerationOptions { RecurseSubdirectories = true })
                .Select(f => new FileInfo(f))
                .OrderBy(f => f.LastWriteTime)
                .Take(10);
            foreach (var f in files)
                Console.WriteLine($"  {f.LastWriteTime:yyyy-MM-dd HH:mm}  {f.FullName[(target.Length + 1)..]}");
        };

        // System info
        builtins["sysinfo"] = _ =>
        {
            Console.WriteLine($"  OS:         {RuntimeInformation.OSDescription}");
            Console.WriteLine($"  Arch:       {RuntimeInformation.OSArchitecture}");
            Console.WriteLine($"  .NET:       {Environment.Version}");
            Console.WriteLine($"  CPU cores:  {Environment.ProcessorCount}");
            Console.WriteLine($"  64-bit:     {Environment.Is64BitOperatingSystem}");
            Console.WriteLine($"  User:       {Environment.UserName}");
            Console.WriteLine($"  Domain:     {Environment.UserDomainName}");
            Console.WriteLine($"  Dir:        {currentDir}");
            Console.WriteLine($"  Clipboard:  {Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}");
        };

        builtins["drives"] = _ =>
        {
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (!drive.IsReady) continue;
                Console.WriteLine($"  {drive.Name} {drive.DriveFormat,-8} {drive.DriveType} {drive.TotalSize / 1024 / 1024 / 1024}GB");
            }
        };

        builtins["processes"] = _ =>
        {
            var count = Process.GetProcesses().Length;
            Console.WriteLine($"  {count} processes running");
        };

        builtins["services"] = _ => RunSystemCommand("sc query state= all | findstr SERVICE_NAME");

        builtins["tasks"] = _ => RunSystemCommand("tasklist /fo table | head -20");

        builtins["netstat"] = _ => RunSystemCommand("netstat -an | head -20");

        builtins["ipconfig"] = _ => RunSystemCommand("ipconfig");

        builtins["ifconfig"] = _ => RunSystemCommand("ipconfig");

        builtins["tracert"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: tracert <host>"); return; }
            RunSystemCommand($"tracert {args[0]}");
        };

        builtins["pathping"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: pathping <host>"); return; }
            RunSystemCommand($"pathping {args[0]}");
        };

        builtins["nslookup"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: nslookup <host>"); return; }
            RunSystemCommand($"nslookup {args[0]}");
        };

        builtins["whois"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: whois <domain>"); return; }
            RunSystemCommand($"nslookup {args[0]}");
        };

        // Encoding
        builtins["url-encode"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: url-encode <text>"); return; }
            Console.WriteLine(Uri.EscapeDataString(string.Join(' ', args)));
        };

        builtins["url-decode"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: url-decode <text>"); return; }
            Console.WriteLine(Uri.UnescapeDataString(args[0]));
        };

        builtins["html-encode"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: html-encode <text>"); return; }
            Console.WriteLine(System.Net.WebUtility.HtmlEncode(string.Join(' ', args)));
        };

        builtins["html-decode"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: html-decode <text>"); return; }
            Console.WriteLine(System.Net.WebUtility.HtmlDecode(args[0]));
        };

        builtins["xml-escape"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: xml-escape <text>"); return; }
            Console.WriteLine(System.Security.SecurityElement.Escape(string.Join(' ', args)));
        };

        // Conversion
        builtins["dec"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: dec <number>"); return; }
            if (int.TryParse(args[0], out var n))
                Console.WriteLine($"  {n} = 0x{n:X} = 0b{Convert.ToString(n, 2)} = 0{oct(n)}");
        };

        static string oct(int n) => Convert.ToString(n, 8);

        builtins["hex2dec"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: hex2dec <hex>"); return; }
            if (int.TryParse(args[0].Replace("0x", ""), System.Globalization.NumberStyles.HexNumber, null, out var n))
                Console.WriteLine($"  {n}");
        };

        builtins["bin2dec"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: bin2dec <binary>"); return; }
            Console.WriteLine($"  {Convert.ToInt32(args[0], 2)}");
        };

        builtins["dec2bin"] = args =>
        {
            if (args.Length == 0 || !int.TryParse(args[0], out var n)) { Console.WriteLine("  Usage: dec2bin <number>"); return; }
            Console.WriteLine($"  {Convert.ToString(n, 2)}");
        };

        builtins["dec2hex"] = args =>
        {
            if (args.Length == 0 || !int.TryParse(args[0], out var n)) { Console.WriteLine("  Usage: dec2hex <number>"); return; }
            Console.WriteLine($"  0x{n:X}");
        };

        builtins["dec2oct"] = args =>
        {
            if (args.Length == 0 || !int.TryParse(args[0], out var n)) { Console.WriteLine("  Usage: dec2oct <number>"); return; }
            Console.WriteLine($"  0{Convert.ToString(n, 8)}");
        };

        builtins["celsius"] = args =>
        {
            if (args.Length == 0 || !double.TryParse(args[0], out var f)) { Console.WriteLine("  Usage: celsius <fahrenheit>"); return; }
            Console.WriteLine($"  {(f - 32) * 5 / 9:F1} °C");
        };

        builtins["fahrenheit"] = args =>
        {
            if (args.Length == 0 || !double.TryParse(args[0], out var c)) { Console.WriteLine("  Usage: fahrenheit <celsius>"); return; }
            Console.WriteLine($"  {c * 9 / 5 + 32:F1} °F");
        };

        builtins["km"] = args =>
        {
            if (args.Length == 0 || !double.TryParse(args[0], out var miles)) { Console.WriteLine("  Usage: km <miles>"); return; }
            Console.WriteLine($"  {miles * 1.60934:F2} km");
        };

        builtins["miles"] = args =>
        {
            if (args.Length == 0 || !double.TryParse(args[0], out var km)) { Console.WriteLine("  Usage: miles <km>"); return; }
            Console.WriteLine($"  {km / 1.60934:F2} miles");
        };

        builtins["kg"] = args =>
        {
            if (args.Length == 0 || !double.TryParse(args[0], out var lbs)) { Console.WriteLine("  Usage: kg <lbs>"); return; }
            Console.WriteLine($"  {lbs * 0.453592:F2} kg");
        };

        builtins["lbs"] = args =>
        {
            if (args.Length == 0 || !double.TryParse(args[0], out var kg)) { Console.WriteLine("  Usage: lbs <kg>"); return; }
            Console.WriteLine($"  {kg / 0.453592:F2} lbs");
        };

        builtins["inches"] = args =>
        {
            if (args.Length == 0 || !double.TryParse(args[0], out var cm)) { Console.WriteLine("  Usage: inches <cm>"); return; }
            Console.WriteLine($"  {cm / 2.54:F2} inches");
        };

        builtins["cm"] = args =>
        {
            if (args.Length == 0 || !double.TryParse(args[0], out var inches)) { Console.WriteLine("  Usage: cm <inches>"); return; }
            Console.WriteLine($"  {inches * 2.54:F2} cm");
        };

        // Fun
        builtins["8ball"] = _ =>
        {
            var answers = new[] { "Yes!", "No.", "Maybe.", "Definitely!", "Absolutely not.", "Ask again later.", "I think so.", "Doubtful.", "For sure!", "No way!" };
            Console.ForegroundColor = themeAccent;
            Console.WriteLine($"  🎱 {answers[new Random().Next(answers.Length)]}");
            Console.ResetColor();
        };

        builtins["dice"] = args =>
        {
            var count = args.Length > 0 && int.TryParse(args[0], out var c) ? c : 1;
            var rng = new Random();
            for (int i = 0; i < count; i++)
                Console.Write($"  {rng.Next(1, 7)}");
            Console.WriteLine();
        };

        builtins["coin"] = _ =>
        {
            Console.ForegroundColor = themeAccent;
            Console.WriteLine($"  🪙 {(new Random().Next(2) == 0 ? "Heads" : "Tails")}");
            Console.ResetColor();
        };

        builtins["quote"] = _ =>
        {
            var quotes = new[] { "\"Stay hungry, stay foolish.\" — Steve Jobs", "\"The only limit is your imagination.\" — Someone", "\"Code never lies, comments sometimes do.\" — Jim Weirich", "\"First, solve the problem. Then, write the code.\" — John Johnson", "\"Programs must be written for people to read.\" — Hal Abelson" };
            Console.ForegroundColor = themeAccent;
            Console.WriteLine($"\n  {quotes[new Random().Next(quotes.Length)]}\n");
            Console.ResetColor();
        };

        builtins["joke"] = _ =>
        {
            var jokes = new[] {
                "Why do programmers prefer dark mode? Because light attracts bugs!",
                "A SQL query walks into a bar, sees two tables and asks: 'Can I JOIN you?'",
                "Why do Java developers wear glasses? Because they can't C#!",
                "What's a programmer's favorite hangout place? Foo Bar!",
                "Why was the computer cold? It left its Windows open!",
                "!false — It's funny because it's true.",
                "There are only 10 types of people: those who understand binary and those who don't."
            };
            Console.ForegroundColor = themeAccent;
            Console.WriteLine($"  {jokes[new Random().Next(jokes.Length)]}");
            Console.ResetColor();
        };

        builtins["trivia"] = _ =>
        {
            var facts = new[] {
                "The first computer bug was an actual bug (moth) found in 1947.",
                "The first programmer was Ada Lovelace, in 1843.",
                "The average programmer types 50-70 words per minute.",
                "There are approximately 700 programming languages.",
                "The first computer weighed 27 tons.",
                "About 70% of coding jobs don't require a CS degree.",
                "The first computer mouse was made of wood."
            };
            Console.ForegroundColor = themeAccent;
            Console.WriteLine($"  🧠 {facts[new Random().Next(facts.Length)]}");
            Console.ResetColor();
        };

        builtins["colors"] = _ =>
        {
            foreach (ConsoleColor c in Enum.GetValues(typeof(ConsoleColor)))
            {
                Console.ForegroundColor = c;
                Console.Write($"  {c,-16}");
            }
            Console.ResetColor();
            Console.WriteLine();
        };

        builtins["emoji"] = _ =>
        {
            var emojis = "😀😃😄😁😆😅🤣😂🙂🙃😉😊😇🥰😍🤩😘😗😚😋😛😜🤪😝🤑🤗🤭🤫🤔🤐🤨😐😑😶😏😒🙄😬🤥😌😔😪🤤😴😷🤒🤕🤢🤮🤧🥵🥶🥴😵🤯🤠🥳🥸😎🤓🧐😕😟🙁😮😯😲😳🥺😦😧😨😰😥😢😭😱😖😣😞😓😩😫🥱😤😡😠🤬😈👿💀☠️💩🤡👹👺👻👽🤖😺😸😹😻😼😽🙀😿😾";
            var rng = new Random();
            for (int i = 0; i < 20; i++)
            {
                var idx = rng.Next(emojis.Length / 2) * 2;
                Console.Write($"  {emojis.Substring(idx, 2)}");
            }
            Console.WriteLine();
        };

        // Git extended
        builtins["gst"] = _ => RunSystemCommand("git status --short");
        builtins["glg"] = _ => RunSystemCommand("git log --oneline --graph -20");
        builtins["gco"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: gco <branch>"); return; }
            RunSystemCommand($"git checkout {args[0]}");
        };

        builtins["gcb"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: gcb <branch>"); return; }
            RunSystemCommand($"git checkout -b {args[0]}");
        };

        builtins["gp"] = _ => RunSystemCommand("git push");
        builtins["gpl"] = _ => RunSystemCommand("git pull");
        builtins["gf"] = _ => RunSystemCommand("git fetch");
        builtins["grs"] = _ => RunSystemCommand("git reset --soft HEAD~1");
        builtins["gc"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: gc <message>"); return; }
            RunSystemCommand($"git commit -m \"{string.Join(' ', args)}\"");
        };

        builtins["gca"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: gca <message>"); return; }
            RunSystemCommand($"git add -A && git commit -m \"{string.Join(' ', args)}\"");
        };

        builtins["gsh"] = _ => RunSystemCommand("git stash");
        builtins["gsp"] = _ => RunSystemCommand("git stash pop");
        builtins["gsl"] = _ => RunSystemCommand("git stash list");
        builtins["glast"] = _ => RunSystemCommand("git log -1 --stat");
        builtins["gwho"] = _ => RunSystemCommand("git shortlog -sn");

        // Dev tools
        builtins["json"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: json <file>"); return; }
            var path = Path.Combine(currentDir, args[0]);
            if (!File.Exists(path)) { PrintError($"Not found: {args[0]}"); return; }
            try
            {
                var doc = JsonDocument.Parse(File.ReadAllText(path));
                var opts = new JsonSerializerOptions { WriteIndented = true };
                Console.WriteLine(JsonSerializer.Serialize(doc.RootElement, opts));
            }
            catch (Exception ex) { PrintError(ex.Message); }
        };

        builtins["yaml2json"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: yaml2json <file>"); return; }
            PrintError("YAML parsing requires additional package");
        };

        builtins["jq"] = args =>
        {
            if (args.Length < 2) { Console.WriteLine("  Usage: jq <file> <path>"); return; }
            var path = Path.Combine(currentDir, args[0]);
            if (!File.Exists(path)) { PrintError($"Not found: {args[0]}"); return; }
            try
            {
                var doc = JsonDocument.Parse(File.ReadAllText(path));
                Console.WriteLine(doc.RootElement);
            }
            catch (Exception ex) { PrintError(ex.Message); }
        };

        builtins["validate-json"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: validate-json <file>"); return; }
            var path = Path.Combine(currentDir, args[0]);
            if (!File.Exists(path)) { PrintError($"Not found: {args[0]}"); return; }
            try
            {
                JsonDocument.Parse(File.ReadAllText(path));
                Console.ForegroundColor = themeAccent;
                Console.WriteLine("  Valid JSON ✓");
                Console.ResetColor();
            }
            catch (Exception ex) { PrintError($"Invalid JSON: {ex.Message}"); }
        };

        builtins["xml"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: xml <file>"); return; }
            var path = Path.Combine(currentDir, args[0]);
            if (!File.Exists(path)) { PrintError($"Not found: {args[0]}"); return; }
            try
            {
                var doc = System.Xml.Linq.XDocument.Load(path);
                Console.WriteLine(doc);
            }
            catch (Exception ex) { PrintError(ex.Message); }
        };

        builtins["validate-xml"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: validate-xml <file>"); return; }
            var path = Path.Combine(currentDir, args[0]);
            if (!File.Exists(path)) { PrintError($"Not found: {args[0]}"); return; }
            try
            {
                System.Xml.Linq.XDocument.Load(path);
                Console.ForegroundColor = themeAccent;
                Console.WriteLine("  Valid XML ✓");
                Console.ResetColor();
            }
            catch (Exception ex) { PrintError($"Invalid XML: {ex.Message}"); }
        };

        builtins["csv"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: csv <file>"); return; }
            var path = Path.Combine(currentDir, args[0]);
            if (!File.Exists(path)) { PrintError($"Not found: {args[0]}"); return; }
            var lines = File.ReadAllLines(path);
            foreach (var line in lines)
            {
                var cols = line.Split(',');
                Console.WriteLine($"  {string.Join(" | ", cols)}");
            }
        };

        builtins["csv-head"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: csv-head <file> [rows]"); return; }
            var path = Path.Combine(currentDir, args[0]);
            if (!File.Exists(path)) { PrintError($"Not found: {args[0]}"); return; }
            var n = args.Length > 1 && int.TryParse(args[1], out var v) ? v : 5;
            var lines = File.ReadAllLines(path).Take(n);
            foreach (var line in lines)
            {
                var cols = line.Split(',');
                Console.WriteLine($"  {string.Join(" | ", cols)}");
            }
        };

        builtins["csv-count"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: csv-count <file>"); return; }
            var path = Path.Combine(currentDir, args[0]);
            if (!File.Exists(path)) { PrintError($"Not found: {args[0]}"); return; }
            Console.WriteLine($"  {File.ReadAllLines(path).Length} rows");
        };

        // Archive
        builtins["zip"] = args =>
        {
            if (args.Length < 2) { Console.WriteLine("  Usage: zip <source> <dest.zip>"); return; }
            var src = Path.Combine(currentDir, args[0]);
            var dst = Path.Combine(currentDir, args[1]);
            try
            {
                System.IO.Compression.ZipFile.CreateFromDirectory(src, dst);
                Console.ForegroundColor = themeAccent;
                Console.WriteLine($"  Created: {args[1]}");
                Console.ResetColor();
            }
            catch (Exception ex) { PrintError(ex.Message); }
        };

        builtins["unzip"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: unzip <file> [dest]"); return; }
            var path = Path.Combine(currentDir, args[0]);
            if (!File.Exists(path)) { PrintError($"Not found: {args[0]}"); return; }
            var dest = args.Length > 1 ? Path.Combine(currentDir, args[1]) : currentDir;
            try
            {
                System.IO.Compression.ZipFile.ExtractToDirectory(path, dest, true);
                Console.ForegroundColor = themeAccent;
                Console.WriteLine($"  Extracted to: {dest}");
                Console.ResetColor();
            }
            catch (Exception ex) { PrintError(ex.Message); }
        };

        builtins["tar"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: tar <args>"); return; }
            RunSystemCommand($"tar {string.Join(' ', args)}");
        };

        // Process
        builtins["pids"] = _ =>
        {
            var procs = Process.GetProcesses().OrderBy(p => p.Id).Take(20);
            foreach (var p in procs)
            {
                try { Console.WriteLine($"  {p.Id,6} {p.ProcessName}"); }
                catch { }
            }
        };

        builtins["pname"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: pname <pid>"); return; }
            if (int.TryParse(args[0], out var pid))
            {
                try { Console.WriteLine($"  {Process.GetProcessById(pid).ProcessName}"); }
                catch { Console.WriteLine("  Not found"); }
            }
        };

        builtins["memof"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: memof <pid>"); return; }
            if (int.TryParse(args[0], out var pid))
            {
                try
                {
                    var p = Process.GetProcessById(pid);
                    Console.WriteLine($"  {p.ProcessName}: {p.WorkingSet64 / 1024 / 1024} MB");
                }
                catch { Console.WriteLine("  Not found"); }
            }
        };

        builtins["start"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: start <program>"); return; }
            try
            {
                Process.Start(new ProcessStartInfo { FileName = args[0], UseShellExecute = true });
                Console.ForegroundColor = themeAccent;
                Console.WriteLine($"  Started: {args[0]}");
                Console.ResetColor();
            }
            catch (Exception ex) { PrintError(ex.Message); }
        };

        builtins["wait"] = args =>
        {
            if (args.Length == 0 || !int.TryParse(args[0], out var ms)) { Console.WriteLine("  Usage: wait <ms>"); return; }
            System.Threading.Thread.Sleep(ms);
        };

        builtins["sleep"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: sleep <seconds>"); return; }
            if (double.TryParse(args[0], out var sec))
                System.Threading.Thread.Sleep((int)(sec * 1000));
        };

        // Misc
        builtins["history"] = _ =>
        {
            for (int i = 0; i < history.Count; i++)
                Console.WriteLine($"  {i + 1,4}  {history[i]}");
        };

        builtins["history-search"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: history-search <query>"); return; }
            var query = string.Join(' ', args);
            for (int i = 0; i < history.Count; i++)
            {
                if (history[i].Contains(query, StringComparison.OrdinalIgnoreCase))
                    Console.WriteLine($"  {i + 1,4}  {history[i]}");
            }
        };

        builtins["history-clear"] = _ =>
        {
            history.Clear();
            Console.ForegroundColor = themeAccent;
            Console.WriteLine("  History cleared.");
            Console.ResetColor();
        };

        builtins["clear"] = _ => Console.Clear();
        builtins["cls"] = _ => Console.Clear();
        builtins["reset"] = _ => { Console.ResetColor(); Console.Clear(); };

        builtins["banner"] = _ => PrintBanner();

        builtins["cowsay"] = args =>
        {
            var text = args.Length > 0 ? string.Join(' ', args) : "Moo!";
            var border = new string('─', text.Length + 2);
            Console.WriteLine($"  ┌{border}┐");
            Console.WriteLine($"  │ {text} │");
            Console.WriteLine($"  └{border}┘");
            Console.WriteLine("         \\   ^__^");
            Console.WriteLine("          \\  (oo)\\_______");
            Console.WriteLine("             (__)\\       )\\/\\");
            Console.WriteLine("                 ||----w |");
            Console.WriteLine("                 ||     ||");
        };

        builtins["thought"] = args =>
        {
            var text = args.Length > 0 ? string.Join(' ', args) : "...";
            Console.WriteLine("         ( )");
            Console.WriteLine("         (_)");
            Console.WriteLine("        (   )");
            Console.WriteLine("         ( )");
            Console.WriteLine($"         {text}");
        };

        builtins["shrug"] = _ => Console.WriteLine("  ¯\\_(ツ)_/¯");

        builtins["tableflip"] = _ => Console.WriteLine("  (╯°□°)╯︵ ┻━┻");

        builtins["unflip"] = _ => Console.WriteLine("  ┬─┬ノ( º _ ºノ)");

        builtins["lenny"] = _ => Console.WriteLine("  ( ͡° ͜ʖ ͡°)");

        builtins["debug"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: debug <command>"); return; }
            var sw = Stopwatch.StartNew();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"  [DEBUG] Running: {string.Join(' ', args)}");
            Console.ResetColor();
            if (!TryRunBuiltin(string.Join(' ', args)))
                RunSystemCommand(string.Join(' ', args));
            sw.Stop();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"  [DEBUG] Completed in {sw.ElapsedMilliseconds}ms");
            Console.ResetColor();
        };

        builtins["help-all"] = _ =>
        {
            builtins["help"](Array.Empty<string>());
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(@"
  Batch 1-5: ~205 commands (see above)
  Batch 6: ~350 commands (text, math, time, file, system, network, git, encoding, games, dev)
  Run 'help-all' for full list. Total: ~555 commands");
            Console.ResetColor();
        };

        CommandsExtra.Register(builtins);
        CommandsExtra2.Register(builtins);
        CommandsExtra3.Register(builtins);
        CommandsExtra4.Register(builtins);
        CommandsExtra5.Register(builtins);

        builtins["plugin"] = args =>
        {
            if (args.Length == 0) { Console.WriteLine("  Usage: plugin <list|install|remove|repo>"); return; }
            switch (args[0].ToLower())
            {
                case "list":
                    pluginManager?.ListPlugins();
                    break;
                case "install":
                    if (args.Length < 2) { Console.WriteLine("  Usage: plugin install <name> [url]"); return; }
                    _ = (pluginManager?.Install(args[1], args.Length > 2 ? args[2] : null));
                    break;
                case "remove":
                    if (args.Length < 2) { Console.WriteLine("  Usage: plugin remove <name>"); return; }
                    if (pluginManager?.Remove(args[1]) == true)
                        Console.WriteLine($"  Плагин '{args[1]}' удалён.");
                    else
                        Console.WriteLine($"  Плагин '{args[1]}' не найден.");
                    break;
                case "repo":
                    Console.WriteLine("  Репозиторий плагинов:");
                    Console.WriteLine("  https://github.com/Marrcus113/CustomWinConsolePlugins");
                    break;
                default:
                    Console.WriteLine("  Неизвестная команда. Используй: list, install, remove, repo");
                    break;
            }
        };
    }

    static void ShowAliases()
    {
        if (aliases.Count == 0) { Console.WriteLine("  No aliases defined."); return; }
        foreach (var kv in aliases)
            Console.WriteLine($"  {kv.Key} = {kv.Value}");
    }

    static string ResolveAliases(string input)
    {
        var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return input;
        if (aliases.TryGetValue(parts[0], out var cmd))
        {
            parts[0] = cmd;
            return string.Join(' ', parts);
        }
        return input;
    }

    static void ApplyTheme(string name)
    {
        switch (name)
        {
            case "matrix":
                themePrimary = ConsoleColor.Green; themeAccent = ConsoleColor.DarkGreen;
                themePrompt = ConsoleColor.Green; themeDir = ConsoleColor.Green;
                themeError = ConsoleColor.DarkGreen; themeFile = ConsoleColor.Green;
                themeDirColor = ConsoleColor.DarkGreen; themeExe = ConsoleColor.White;
                break;
            case "ocean":
                themePrimary = ConsoleColor.Cyan; themeAccent = ConsoleColor.DarkCyan;
                themePrompt = ConsoleColor.Cyan; themeDir = ConsoleColor.Blue;
                themeError = ConsoleColor.DarkRed; themeFile = ConsoleColor.White;
                themeDirColor = ConsoleColor.Blue; themeExe = ConsoleColor.Cyan;
                break;
            case "sunset":
                themePrimary = ConsoleColor.Yellow; themeAccent = ConsoleColor.DarkYellow;
                themePrompt = ConsoleColor.Yellow; themeDir = ConsoleColor.Magenta;
                themeError = ConsoleColor.Red; themeFile = ConsoleColor.White;
                themeDirColor = ConsoleColor.DarkRed; themeExe = ConsoleColor.Yellow;
                break;
            case "cyberpunk":
                themePrimary = ConsoleColor.Magenta; themeAccent = ConsoleColor.DarkYellow;
                themePrompt = ConsoleColor.Magenta; themeDir = ConsoleColor.Cyan;
                themeError = ConsoleColor.Red; themeFile = ConsoleColor.White;
                themeDirColor = ConsoleColor.DarkMagenta; themeExe = ConsoleColor.Magenta;
                break;
            case "minimal":
                themePrimary = ConsoleColor.White; themeAccent = ConsoleColor.Gray;
                themePrompt = ConsoleColor.White; themeDir = ConsoleColor.DarkGray;
                themeError = ConsoleColor.Red; themeFile = ConsoleColor.White;
                themeDirColor = ConsoleColor.DarkGray; themeExe = ConsoleColor.White;
                break;
            case "fire":
                themePrimary = ConsoleColor.Red; themeAccent = ConsoleColor.Yellow;
                themePrompt = ConsoleColor.Red; themeDir = ConsoleColor.DarkRed;
                themeError = ConsoleColor.Magenta; themeFile = ConsoleColor.White;
                themeDirColor = ConsoleColor.DarkRed; themeExe = ConsoleColor.Yellow;
                break;
            default:
                Console.WriteLine($"  Unknown theme: {name}. Available: matrix, ocean, sunset, cyberpunk, minimal, fire");
                return;
        }
        Console.ForegroundColor = themePrimary;
        Console.Title = $"CustomWinConsole [{name}]";
    }

    static string GetCurrentThemeName()
    {
        if (themePrimary == ConsoleColor.Green) return "matrix";
        if (themePrimary == ConsoleColor.Cyan) return "ocean";
        if (themePrimary == ConsoleColor.Yellow) return "sunset";
        if (themePrimary == ConsoleColor.Magenta) return "cyberpunk";
        if (themePrimary == ConsoleColor.White) return "minimal";
        if (themePrimary == ConsoleColor.Red) return "fire";
        return "custom";
    }

    static void PrintError(string msg)
    {
        Console.ForegroundColor = themeError;
        Console.WriteLine($"  Error: {msg}");
        Console.ResetColor();
    }

    static void PrintTree(string dir, string prefix)
    {
        try
        {
            var dirs = Directory.GetDirectories(dir);
            var files = Directory.GetFiles(dir);

            for (int i = 0; i < dirs.Length; i++)
            {
                var isLast = i == dirs.Length - 1 && files.Length == 0;
                Console.Write($"  {prefix}{(isLast ? "└── " : "├── ")}");
                Console.ForegroundColor = themeDirColor;
                Console.WriteLine(Path.GetFileName(dirs[i]) + "/");
                Console.ResetColor();
                PrintTree(dirs[i], prefix + (isLast ? "    " : "│   "));
            }

            for (int i = 0; i < files.Length; i++)
            {
                var isLast = i == files.Length - 1;
                Console.Write($"  {prefix}{(isLast ? "└── " : "├── ")}");
                Console.ForegroundColor = GetFileColor(files[i]);
                Console.WriteLine(Path.GetFileName(files[i]));
                Console.ResetColor();
            }
        }
        catch { }
    }

    static ConsoleColor GetFileColor(string path)
    {
        return Path.GetExtension(path).ToLower() switch
        {
            ".cs" or ".py" or ".js" or ".ts" or ".rs" or ".go" or ".java" => themeAccent,
            ".exe" or ".msi" or ".bat" or ".cmd" or ".ps1" => themeExe,
            ".dll" or ".so" or ".dylib" => themeFile,
            ".json" or ".xml" or ".yaml" or ".yml" => ConsoleColor.Yellow,
            ".md" or ".txt" or ".log" => ConsoleColor.Gray,
            ".png" or ".jpg" or ".gif" or ".bmp" or ".svg" => ConsoleColor.Magenta,
            ".zip" or ".rar" or ".7z" or ".tar" or ".gz" => ConsoleColor.DarkYellow,
            _ => themeFile,
        };
    }

    static bool TryRunBuiltin(string input)
    {
        var parts = ParseArgs(input);
        if (parts.Length == 0) return false;

        var cmd = parts[0].ToLower();
        var args = parts.Length > 1 ? parts[1..] : Array.Empty<string>();

        if (builtins.TryGetValue(cmd, out var action))
        {
            action(args);
            return true;
        }
        return false;
    }

    static void RunSystemCommand(string input)
    {
        var trimmed = input.TrimStart();

        // cd is special
        if (trimmed.StartsWith("cd ", StringComparison.OrdinalIgnoreCase) || trimmed == "cd")
        {
            var arg = trimmed.Length > 2 ? trimmed[3..].Trim() : "";
            if (string.IsNullOrEmpty(arg))
            {
                Console.WriteLine(currentDir);
                return;
            }
            var target = arg == "~" ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                : Path.IsPathRooted(arg) ? arg : Path.Combine(currentDir, arg);

            if (Directory.Exists(target))
                currentDir = Path.GetFullPath(target);
            else
                PrintError($"Directory not found: {arg}");
            return;
        }

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c chcp 65001 >nul && {input}",
                WorkingDirectory = currentDir,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            using var proc = Process.Start(psi);
            if (proc == null) return;

            var stdout = proc.StandardOutput.ReadToEnd();
            var stderr = proc.StandardError.ReadToEnd();
            proc.WaitForExit();

            if (!string.IsNullOrEmpty(stdout)) Console.Write(stdout);
            if (!string.IsNullOrEmpty(stderr))
            {
                Console.ForegroundColor = themeError;
                Console.Write(stderr);
                Console.ResetColor();
            }
        }
        catch (Exception ex) { PrintError(ex.Message); }
    }

    static string[] ParseArgs(string input)
    {
        var parts = new List<string>();
        var current = new StringBuilder();
        bool inQuote = false;

        foreach (var c in input)
        {
            if (c == '"') { inQuote = !inQuote; continue; }
            if (c == ' ' && !inQuote)
            {
                if (current.Length > 0) { parts.Add(current.ToString()); current.Clear(); }
            }
            else
                current.Append(c);
        }
        if (current.Length > 0) parts.Add(current.ToString());
        return parts.ToArray();
    }

    static void PrintPrompt()
    {
        var text = promptTemplate
            .Replace("{dir}", ShortPath(currentDir))
            .Replace("{user}", Environment.UserName)
            .Replace("{host}", Environment.MachineName)
            .Replace("{time}", DateTime.Now.ToString("HH:mm"))
            .Replace("{date}", DateTime.Now.ToString("yyyy-MM-dd"));

        Console.ForegroundColor = themeDir;
        Console.Write($" {text}");
        Console.ForegroundColor = themePrompt;
        Console.ResetColor();
    }

    static string ShortPath(string path)
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (path.StartsWith(home)) return "~" + path[home.Length..];
        return path;
    }

    static string ReadLine()
    {
        var sb = new StringBuilder();
        int cursorPos = 0;
        int promptLen = GetPromptLength();

        while (true)
        {
            var key = Console.ReadKey(true);

            switch (key.Key)
            {
                case ConsoleKey.Enter:
                    Console.WriteLine();
                    return sb.ToString();

                case ConsoleKey.Backspace when cursorPos > 0:
                    sb.Remove(cursorPos - 1, 1);
                    cursorPos--;
                    Redraw(sb.ToString(), cursorPos, promptLen);
                    break;

                case ConsoleKey.Delete when cursorPos < sb.Length:
                    sb.Remove(cursorPos, 1);
                    Redraw(sb.ToString(), cursorPos, promptLen);
                    break;

                case ConsoleKey.LeftArrow when cursorPos > 0:
                    cursorPos--;
                    Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                    break;

                case ConsoleKey.RightArrow when cursorPos < sb.Length:
                    cursorPos++;
                    Console.SetCursorPosition(Console.CursorLeft + 1, Console.CursorTop);
                    break;

                case ConsoleKey.UpArrow when history.Count > 0 && historyIndex > 0:
                    historyIndex--;
                    sb.Clear(); sb.Append(history[historyIndex]); cursorPos = sb.Length;
                    Redraw(sb.ToString(), cursorPos, promptLen);
                    break;

                case ConsoleKey.DownArrow:
                    if (historyIndex < history.Count - 1)
                    { historyIndex++; sb.Clear(); sb.Append(history[historyIndex]); cursorPos = sb.Length; }
                    else
                    { historyIndex = history.Count; sb.Clear(); cursorPos = 0; }
                    Redraw(sb.ToString(), cursorPos, promptLen);
                    break;

                case ConsoleKey.Home:
                    cursorPos = 0;
                    Console.SetCursorPosition(promptLen, Console.CursorTop);
                    break;

                case ConsoleKey.End:
                    cursorPos = sb.Length;
                    Console.SetCursorPosition(promptLen + sb.Length, Console.CursorTop);
                    break;

                case ConsoleKey.Tab:
                    var partial = sb.ToString()[..cursorPos];
                    var lastSpace = partial.LastIndexOf(' ');
                    var toComplete = lastSpace >= 0 ? partial[(lastSpace + 1)..] : partial;
                    try
                    {
                        var matches = Directory.GetFileSystemEntries(currentDir, toComplete + "*");
                        if (matches.Length == 1)
                        {
                            var name = Path.GetFileName(matches[0]);
                            var before = lastSpace >= 0 ? partial[..(lastSpace + 1)] : "";
                            sb.Clear();
                            sb.Append(before + name + (Directory.Exists(matches[0]) ? "\\" : " "));
                            cursorPos = sb.Length;
                            Redraw(sb.ToString(), cursorPos, promptLen);
                        }
                    }
                    catch { }
                    break;

                default:
                    if (key.KeyChar >= 32)
                    {
                        sb.Insert(cursorPos, key.KeyChar);
                        cursorPos++;
                        Redraw(sb.ToString(), cursorPos, promptLen);
                    }
                    break;
            }
        }
    }

    static int GetPromptLength()
    {
        var text = promptTemplate
            .Replace("{dir}", ShortPath(currentDir))
            .Replace("{user}", Environment.UserName)
            .Replace("{host}", Environment.MachineName)
            .Replace("{time}", DateTime.Now.ToString("HH:mm"))
            .Replace("{date}", DateTime.Now.ToString("yyyy-MM-dd"));
        return text.Length + 2; // space before + space after
    }

    static void Redraw(string text, int cursorPos, int promptLen)
    {
        var (left, top) = Console.GetCursorPosition();
        Console.SetCursorPosition(promptLen, top);
        Console.Write(new string(' ', Console.WindowWidth - promptLen));
        Console.SetCursorPosition(promptLen, top);
        Console.Write(text);
        Console.SetCursorPosition(promptLen + cursorPos, top);
    }

    static long GetTotalRamMB()
    {
        try
        {
            var memStatus = new MEMORYSTATUSEX();
            memStatus.dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>();
            if (GlobalMemoryStatusEx(ref memStatus))
                return (long)(memStatus.ullTotalPhys / 1024 / 1024);
        }
        catch { }
        return 0;
    }

    static void EnableVirtualTerminal()
    {
        try
        {
            var handle = GetStdHandle(-11);
            GetConsoleMode(handle, out uint mode);
            SetConsoleMode(handle, mode | 0x0004);
        }
        catch { }
    }

    static void LoadAliases()
    {
        try
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".customwinconsole_aliases.json");
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                aliases = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
            }
        }
        catch { }
    }

    static void SaveAliases()
    {
        try
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".customwinconsole_aliases.json");
            var json = JsonSerializer.Serialize(aliases, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
        catch { }
    }

    static string GetFileIcon(string name) => Path.GetExtension(name).ToLower() switch
    {
        ".cs" or ".py" or ".js" or ".ts" or ".rs" or ".go" or ".java" => "📜",
        ".txt" or ".md" or ".log" => "📄",
        ".json" or ".xml" or ".yaml" or ".yml" => "📋",
        ".png" or ".jpg" or ".gif" or ".bmp" or ".svg" => "🖼️",
        ".zip" or ".rar" or ".7z" or ".tar" or ".gz" => "📦",
        ".exe" or ".msi" or ".bat" or ".cmd" or ".ps1" => "⚙️",
        ".dll" or ".so" or ".dylib" => "🔧",
        _ => "📄"
    };

    static void LoadTodo()
    {
        try
        {
            if (File.Exists(todoFile))
            {
                var json = File.ReadAllText(todoFile);
                todoList = JsonSerializer.Deserialize<List<string>>(json) ?? new();
            }
        }
        catch { todoList = new(); }
    }

    static void SaveTodo()
    {
        try
        {
            var json = JsonSerializer.Serialize(todoList, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(todoFile, json);
        }
        catch { }
    }

    static void SetClipboard(string text)
    {
        try
        {
            var psi = new ProcessStartInfo("clip") { RedirectStandardInput = true, UseShellExecute = false, CreateNoWindow = true };
            using var proc = Process.Start(psi);
            if (proc != null) { proc.StandardInput.Write(text); proc.StandardInput.Close(); proc.WaitForExit(); }
        }
        catch { }
    }

    static string GetClipboard()
    {
        try
        {
            var psi = new ProcessStartInfo("powershell", "-command \"Get-Clipboard\"")
            { RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true };
            using var proc = Process.Start(psi);
            if (proc != null) return proc.StandardOutput.ReadToEnd().TrimEnd();
        }
        catch { }
        return "";
    }

    [StructLayout(LayoutKind.Sequential)]
    struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    [DllImport("kernel32.dll")] static extern IntPtr GetStdHandle(int h);
    [DllImport("kernel32.dll")] static extern bool GetConsoleMode(IntPtr h, out uint m);
    [DllImport("kernel32.dll")] static extern bool SetConsoleMode(IntPtr h, uint m);
}
