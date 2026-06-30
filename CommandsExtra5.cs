using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace CustomWinConsole;

public static class CommandsExtra5
{
    public static void Register(Dictionary<string, Action<string[]>> builtins)
    {
        builtins["boxdraw"] = a => { if (a.Length > 0) { string text = string.Join(" ", a); int w = text.Length + 4; Console.WriteLine("┌" + new string('─', w) + "┐"); Console.WriteLine("│ " + text + " │"); Console.WriteLine("└" + new string('─', w) + "┘"); } };
        builtins["doublebox"] = a => { if (a.Length > 0) { string text = string.Join(" ", a); int w = text.Length + 4; Console.WriteLine("╔" + new string('═', w) + "╗"); Console.WriteLine("║ " + text + " ║"); Console.WriteLine("╚" + new string('═', w) + "╝"); } };
        builtins["roundbox"] = a => { if (a.Length > 0) { string text = string.Join(" ", a); int w = text.Length + 4; Console.WriteLine("╭" + new string('─', w) + "╮"); Console.WriteLine("│ " + text + " │"); Console.WriteLine("╰" + new string('─', w) + "╯"); } };
        builtins["shadowbox"] = a => { if (a.Length > 0) { string text = string.Join(" ", a); int w = text.Length + 2; Console.WriteLine(" ┌" + new string('─', w) + "┐"); Console.WriteLine(" │" + text + " │"); Console.WriteLine(" └" + new string('─', w) + "┘▓"); Console.WriteLine("  " + new string('▓', w + 3)); } };
        builtins["thickbox"] = a => { if (a.Length > 0) { string text = string.Join(" ", a); int w = text.Length + 4; Console.WriteLine("┏" + new string('━', w) + "┓"); Console.WriteLine("┃ " + text + " ┃"); Console.WriteLine("┗" + new string('━', w) + "┛"); } };
        builtins["dotbox"] = a => { if (a.Length > 0) { string text = string.Join(" ", a); int w = text.Length + 4; Console.WriteLine("." + new string('-', w) + "."); Console.WriteLine("| " + text + " |"); Console.WriteLine("'" + new string('-', w) + "'"); } };

        builtins["table"] = a => { if (a.Length > 0) { string[] rows = a[0].Split(';'); int maxLen = rows.Max(r => r.Length); string border = "+" + new string('-', maxLen + 2) + "+"; Console.WriteLine(border); foreach (var r in rows) Console.WriteLine($"| {r.PadRight(maxLen)} |"); Console.WriteLine(border); } };
        builtins["csvtable"] = a => { if (a.Length > 0) { var rows = a[0].Split('\n').Select(r => r.Split(',').Select(c => c.Trim()).ToArray()).ToArray(); int[] widths = rows[0].Select((_, i) => rows.Max(r => i < r.Length ? r[i].Length : 0)).ToArray(); string border = "+" + string.Join("+", widths.Select(w => new string('-', w + 2))) + "+"; Console.WriteLine(border); foreach (var r in rows) { Console.WriteLine("| " + string.Join(" | ", r.Select((c, i) => c.PadRight(widths[i]))) + " |"); Console.WriteLine(border); } } };
        builtins["keyval"] = a => { if (a.Length > 0) { string[] pairs = a[0].Split(';'); int maxKey = pairs.Max(p => p.Split('=')[0].Length); foreach (var p in pairs) { var parts = p.Split('=', 2); string val = parts.Length > 1 ? parts[1] : ""; Console.WriteLine($"  {parts[0].PadRight(maxKey)} = {val}"); } } };

        builtins["spinner"] = a => { char[] chars = ['|', '/', '-', '\\']; int count = a.Length > 0 && int.TryParse(a[0], out int n) ? n : 20; for (int i = 0; i < count; i++) { Console.Write($"\r  {chars[i % 4]}"); Thread.Sleep(100); } Console.WriteLine("\r  ✓"); };
        builtins["dots"] = a => { int count = a.Length > 0 && int.TryParse(a[0], out int n) ? n : 30; for (int i = 0; i < count; i++) { Console.Write("."); Thread.Sleep(50); } Console.WriteLine(); };
        builtins["progressfill"] = a => { if (a.Length >= 2 && int.TryParse(a[0], out int cur) && int.TryParse(a[1], out int total)) { int pct = (int)((double)cur / total * 100); int filled = (int)((double)cur / total * 30); Console.Write($"\r  [{new string('#', filled)}{new string('-', 30 - filled)}] {pct}%"); if (cur == total) Console.WriteLine(); } };
        builtins["countdown"] = a => { if (a.Length > 0 && int.TryParse(a[0], out int n)) { for (int i = n; i > 0; i--) { Console.Write($"\r  {i} "); Thread.Sleep(1000); } Console.WriteLine("\r  GO! "); } };
        builtins["typewriter"] = a => { if (a.Length > 0) { foreach (char c in string.Join(" ", a)) { Console.Write(c); Thread.Sleep(30); } Console.WriteLine(); } };
        builtins["matrix"] = a => { int cols = Console.WindowWidth; var rand = new Random(); Console.ForegroundColor = ConsoleColor.Green; int count = a.Length > 0 && int.TryParse(a[0], out int n) ? n : 30; for (int i = 0; i < count; i++) { string line = new string(Enumerable.Range(0, cols).Select(_ => (char)rand.Next(33, 126)).ToArray()); Console.WriteLine(line); Thread.Sleep(50); } Console.ResetColor(); };
        builtins["fireworks"] = _ => { var rand = new Random(); string[] colors = ["\u001b[31m", "\u001b[33m", "\u001b[32m", "\u001b[36m", "\u001b[35m"]; for (int i = 0; i < 20; i++) { Console.ForegroundColor = (ConsoleColor)rand.Next(1, 16); Console.WriteLine($"{colors[rand.Next(colors.Length)]}{new string('*', rand.Next(3, 10))}"); } Console.ResetColor(); };
        builtins["pulse"] = a => { int count = a.Length > 0 && int.TryParse(a[0], out int n) ? n : 10; for (int i = 0; i < count; i++) { Console.Write("\r  ●"); Thread.Sleep(200); Console.Write("\r  ○"); Thread.Sleep(200); } Console.WriteLine(); };
        builtins["loading"] = a => { string msg = a.Length > 0 ? string.Join(" ", a) : "Loading"; int count = a.Length > 1 && int.TryParse(a[^1], out int n) ? n : 20; for (int i = 0; i <= count; i++) { int pct = i * 100 / count; Console.Write($"\r  {msg} [{new string('#', i * 20 / count)}{new string('-', 20 - i * 20 / count)}] {pct}%"); Thread.Sleep(100); } Console.WriteLine(); };
        builtins["wave"] = a => { int count = a.Length > 0 && int.TryParse(a[0], out int n) ? n : 30; for (int i = 0; i < count; i++) { string wave = new string(' ', (int)(Math.Sin(i * 0.5) * 10 + 10)) + "~"; Console.WriteLine(wave); Thread.Sleep(100); } };
        builtins["snake"] = _ => { for (int i = 0; i < 30; i++) { Console.Write(new string(' ', i) + "█"); Thread.Sleep(50); Console.Clear(); } };

        builtins["color"] = a => { if (a.Length > 0) { if (Enum.TryParse<ConsoleColor>(a[0], true, out var c)) Console.ForegroundColor = c; if (a.Length > 1 && Enum.TryParse<ConsoleColor>(a[1], true, out var bg)) Console.BackgroundColor = bg; } };
        builtins["resetcolor"] = _ => Console.ResetColor();
        builtins["red"] = a => { Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine(string.Join(" ", a)); Console.ResetColor(); };
        builtins["green"] = a => { Console.ForegroundColor = ConsoleColor.Green; Console.WriteLine(string.Join(" ", a)); Console.ResetColor(); };
        builtins["blue"] = a => { Console.ForegroundColor = ConsoleColor.Blue; Console.WriteLine(string.Join(" ", a)); Console.ResetColor(); };
        builtins["yellow"] = a => { Console.ForegroundColor = ConsoleColor.Yellow; Console.WriteLine(string.Join(" ", a)); Console.ResetColor(); };
        builtins["cyan"] = a => { Console.ForegroundColor = ConsoleColor.Cyan; Console.WriteLine(string.Join(" ", a)); Console.ResetColor(); };
        builtins["magenta"] = a => { Console.ForegroundColor = ConsoleColor.Magenta; Console.WriteLine(string.Join(" ", a)); Console.ResetColor(); };
        builtins["white"] = a => { Console.ForegroundColor = ConsoleColor.White; Console.WriteLine(string.Join(" ", a)); Console.ResetColor(); };
        builtins["gray"] = a => { Console.ForegroundColor = ConsoleColor.Gray; Console.WriteLine(string.Join(" ", a)); Console.ResetColor(); };
        builtins["bold"] = a => { Console.Write("\u001b[1m"); Console.WriteLine(string.Join(" ", a)); Console.Write("\u001b[0m"); };
        builtins["dim"] = a => { Console.Write("\u001b[2m"); Console.WriteLine(string.Join(" ", a)); Console.Write("\u001b[0m"); };
        builtins["italic"] = a => { Console.Write("\u001b[3m"); Console.WriteLine(string.Join(" ", a)); Console.Write("\u001b[0m"); };
        builtins["underline"] = a => { Console.Write("\u001b[4m"); Console.WriteLine(string.Join(" ", a)); Console.Write("\u001b[0m"); };
        builtins["strikethrough"] = a => { Console.Write("\u001b[9m"); Console.WriteLine(string.Join(" ", a)); Console.Write("\u001b[0m"); };
        builtins["blink"] = a => { Console.Write("\u001b[5m"); Console.WriteLine(string.Join(" ", a)); Console.Write("\u001b[0m"); };
        builtins["inverse"] = a => { Console.Write("\u001b[7m"); Console.WriteLine(string.Join(" ", a)); Console.Write("\u001b[0m"); };
        builtins["rainbow"] = a => { ConsoleColor[] colors = [ConsoleColor.Red, ConsoleColor.Yellow, ConsoleColor.Green, ConsoleColor.Cyan, ConsoleColor.Blue, ConsoleColor.Magenta]; foreach (var (c, i) in string.Join(" ", a).Select((ch, idx) => (ch, idx))) { Console.ForegroundColor = colors[i % colors.Length]; Console.Write(c); } Console.WriteLine(); Console.ResetColor(); };
        builtins["gradient"] = a => { if (a.Length > 0) { string text = string.Join(" ", a); ConsoleColor[] grad = [ConsoleColor.DarkRed, ConsoleColor.Red, ConsoleColor.DarkYellow, ConsoleColor.Yellow, ConsoleColor.Green, ConsoleColor.DarkGreen, ConsoleColor.Cyan]; foreach (var (c, i) in text.Select((ch, idx) => (ch, idx))) { Console.ForegroundColor = grad[i * grad.Length / text.Length]; Console.Write(c); } Console.WriteLine(); Console.ResetColor(); } };

        builtins["asciiart"] = a => { if (a.Length > 0) { string name = a[0].ToLower(); if (name == "cat") Console.WriteLine(" /\\_/\\\n( o.o )\n > ^ <"); else if (name == "heart") Console.WriteLine("  ♥♥♥  \n ♥   ♥ \n♥     ♥\n ♥   ♥ \n  ♥♥♥  "); else if (name == "star") Console.WriteLine("    *    \n   * *   \n  *   *  \n * * * * \n*       *"); else if (name == "house") Console.WriteLine("   /\\   \n  /  \\  \n /    \\ \n|      |\n|  []  |\n|  []  |"); else if (name == "tree") Console.WriteLine("    🌲    \n   /|\\   \n  / | \\  \n /  |  \\ \n    |    \n    |    "); else if (name == "skull") Console.WriteLine("  _____ \n /     \\\n| () () |\n|  ___  |\n \\_____/"); else if (name == "robot") Console.WriteLine("  [___] \n =o-o=\n /| |\\\n (_|_)"); else if (name == "coffee") Console.WriteLine("   (  \n   ) \n  __|\n /__|\\n \\___/"); else if (name == "mushroom") Console.WriteLine("  _____ \n /     \\\n|  @ @  |\n|  ___  |\n \\_____/"); else if (name == "sword") Console.WriteLine("      /\n  ___/\n /   \\\n|  |  |\n \\   /\n  \\ /"); else Console.WriteLine($"  No art for '{name}'"); } };

        builtins["password"] = a => { int len = a.Length > 0 && int.TryParse(a[0], out int n) ? n : 16; string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*"; Console.WriteLine("  " + new string(Enumerable.Range(0, len).Select(_ => chars[Random.Shared.Next(chars.Length)]).ToArray())); };
        builtins["pin"] = _ => Console.WriteLine($"  {Random.Shared.Next(1000, 9999)}");
        builtins["roll"] = a => { int count = a.Length > 0 && int.TryParse(a[0], out int n) ? n : 1; for (int i = 0; i < count; i++) Console.Write($"  {Random.Shared.Next(1, 7)}"); Console.WriteLine(); };
        builtins["coinflip"] = _ => Console.WriteLine(Random.Shared.Next(2) == 0 ? "  Heads" : "  Tails");
        builtins["lottery"] = _ => { var nums = Enumerable.Range(1, 49).OrderBy(_ => Random.Shared.Next()).Take(6).OrderBy(x => x).ToList(); Console.WriteLine("  " + string.Join(" ", nums)); };
        builtins["dice"] = _ => Console.WriteLine($"  {Random.Shared.Next(1, 7)}");
        builtins["pick"] = a => { if (a.Length > 0) Console.WriteLine($"  {a[Random.Shared.Next(a.Length)]}"); };
        builtins["choice"] = a => { if (a.Length > 0) Console.WriteLine($"  {a[Random.Shared.Next(a.Length)]}"); };
        builtins["shuffle"] = a => { var list = a.ToList(); for (int i = list.Count - 1; i > 0; i--) { int j = Random.Shared.Next(i + 1); (list[i], list[j]) = (list[j], list[i]); } Console.WriteLine("  " + string.Join(" ", list)); };
        builtins["sample"] = a => { if (a.Length > 0) { int count = a.Length > 1 && int.TryParse(a[^1], out int n) ? n : 1; var items = a.Skip(a.Length > 1 && int.TryParse(a[^1], out _) ? 0 : 1).ToList(); Console.WriteLine("  " + string.Join(" ", items.OrderBy(_ => Random.Shared.Next()).Take(Math.Min(count, items.Count)))); } };
        builtins["weighted"] = a => { if (a.Length >= 2 && double.TryParse(a[^1], out double w)) { var items = a.Take(a.Length - 1).ToList(); Console.WriteLine($"  {items[Random.Shared.Next(items.Count)]} (p={w:F2})"); } };

        builtins["lorem"] = a => { int words = a.Length > 0 && int.TryParse(a[0], out int n) ? n : 50; string[] pool = ["lorem", "ipsum", "dolor", "sit", "amet", "consectetur", "adipiscing", "elit", "sed", "do", "eiusmod", "tempor", "incididunt", "ut", "labore", "et", "dolore", "magna", "aliqua", "enim", "ad", "minim", "veniam", "quis", "nostrud", "exercitation", "ullamco", "laboris", "nisi", "aliquip", "ex", "ea", "commodo", "consequat", "duis", "aute", "irure", "in", "reprehenderit", "voluptate", "velit", "esse", "cillum", "fugiat", "nulla", "pariatur", "excepteur", "sint", "occaecat", "cupidatat", "non", "proident", "sunt", "culpa", "qui", "officia", "deserunt", "mollit", "anim", "id", "est", "laborum"]; Console.WriteLine("  " + string.Join(" ", Enumerable.Range(0, words).Select(_ => pool[Random.Shared.Next(pool.Length)]))); };
        builtins["uuidgen"] = _ => Console.WriteLine($"  {Guid.NewGuid()}");
        builtins["randomcolor"] = _ => Console.WriteLine($"  #{Random.Shared.Next(0, 16777215):X6}");
        builtins["randomascii"] = a => { int len = a.Length > 0 && int.TryParse(a[0], out int n) ? n : 10; Console.WriteLine("  " + new string(Enumerable.Range(0, len).Select(_ => (char)Random.Shared.Next(32, 127)).ToArray())); };
        builtins["randomname"] = _ => { string[] first = ["Alex", "Jordan", "Taylor", "Morgan", "Casey", "Riley", "Quinn", "Avery", "Cameron", "Drew"]; string[] last = ["Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis", "Rodriguez", "Martinez"]; Console.WriteLine($"  {first[Random.Shared.Next(first.Length)]} {last[Random.Shared.Next(last.Length)]}"); };
        builtins["randomip"] = _ => Console.WriteLine($"  {Random.Shared.Next(1, 255)}.{Random.Shared.Next(0, 255)}.{Random.Shared.Next(0, 255)}.{Random.Shared.Next(1, 254)}");
        builtins["randomdate"] = _ => { var start = new DateTime(2000, 1, 1); var end = DateTime.Today; Console.WriteLine($"  {start.AddDays(Random.Shared.Next((end - start).Days)):yyyy-MM-dd}"); };
        builtins["randombool"] = _ => Console.WriteLine(Random.Shared.Next(2) == 0 ? "  true" : "  false");
        builtins["randomletter"] = _ => Console.WriteLine($"  {(char)Random.Shared.Next('A', 'Z' + 1)}");
        builtins["randomemoji"] = _ => { string[] emojis = ["😀", "🎉", "🔥", "⭐", "🚀", "💡", "🎯", "🏆", "💎", "🌈", "🎲", "🎨", "🎭", "🎪", "🎬", "🎸", "🎹", "🎺", "🎻", "🥁"]; Console.WriteLine($"  {emojis[Random.Shared.Next(emojis.Length)]}"); };

        builtins["calcadd"] = a => { if (a.Length > 0) Console.WriteLine($"  {a.Select(double.Parse).Sum():F4}"); };
        builtins["calcmul"] = a => { if (a.Length > 0) Console.WriteLine($"  {a.Select(double.Parse).Aggregate(1.0, (x, y) => x * y):F4}"); };
        builtins["calcsin"] = a => { if (a.Length > 0 && double.TryParse(a[0], out double v)) Console.WriteLine($"  {Math.Sin(v):F6}"); };
        builtins["calccos"] = a => { if (a.Length > 0 && double.TryParse(a[0], out double v)) Console.WriteLine($"  {Math.Cos(v):F6}"); };
        builtins["calctan"] = a => { if (a.Length > 0 && double.TryParse(a[0], out double v)) Console.WriteLine($"  {Math.Tan(v):F6}"); };
        builtins["calcsqrt"] = a => { if (a.Length > 0 && double.TryParse(a[0], out double v)) Console.WriteLine($"  {Math.Sqrt(v):F6}"); };
        builtins["calcpow"] = a => { if (a.Length >= 2 && double.TryParse(a[0], out double b) && double.TryParse(a[1], out double e)) Console.WriteLine($"  {Math.Pow(b, e):F4}"); };
        builtins["calcp"] = _ => Console.WriteLine($"  {Math.PI}");
        builtins["calce"] = _ => Console.WriteLine($"  {Math.E}");
        builtins["calcln"] = a => { if (a.Length > 0 && double.TryParse(a[0], out double v)) Console.WriteLine($"  {Math.Log(v):F6}"); };
        builtins["calclog2"] = a => { if (a.Length > 0 && double.TryParse(a[0], out double v)) Console.WriteLine($"  {Math.Log2(v):F6}"); };
        builtins["calclog10"] = a => { if (a.Length > 0 && double.TryParse(a[0], out double v)) Console.WriteLine($"  {Math.Log10(v):F6}"); };
        builtins["calcfact"] = a => { if (a.Length > 0 && int.TryParse(a[0], out int n) && n >= 0) { long r = 1; for (int i = 2; i <= n; i++) r *= i; Console.WriteLine($"  {r}"); } };
        builtins["calcfib"] = a => { if (a.Length > 0 && int.TryParse(a[0], out int n)) { long a2 = 0, b2 = 1; for (int i = 0; i < n; i++) { long t = a2; a2 = b2; b2 = t + b2; } Console.WriteLine($"  {a2}"); } };
        builtins["calcprime"] = a => { if (a.Length > 0 && int.TryParse(a[0], out int n)) { bool isPrime = n > 1; for (int i = 2; i * i <= n; i++) if (n % i == 0) { isPrime = false; break; } Console.WriteLine(isPrime ? "  true" : "  false"); } };
        builtins["calcgcd"] = a => { if (a.Length >= 2 && int.TryParse(a[0], out int x) && int.TryParse(a[1], out int y)) { while (y != 0) { int t = y; y = x % y; x = t; } Console.WriteLine($"  {x}"); } };
        builtins["calclcm"] = a => { if (a.Length >= 2 && int.TryParse(a[0], out int x) && int.TryParse(a[1], out int y)) Console.WriteLine($"  {x / (x > 0 && y > 0 ? gcd(x, y) : 1) * y}"); };

        builtins["processlist"] = _ => { foreach (var p in Process.GetProcesses().OrderBy(p => p.ProcessName).Take(30)) Console.WriteLine($"  {p.Id,6} {p.ProcessName}"); };
        builtins["proccount"] = _ => Console.WriteLine($"  {Process.GetProcesses().Length} processes");
        builtins["findproc"] = a => { if (a.Length > 0) { foreach (var p in Process.GetProcesses().Where(p => p.ProcessName.Contains(a[0], StringComparison.OrdinalIgnoreCase))) Console.WriteLine($"  {p.Id} {p.ProcessName}"); } };
        builtins["killproc"] = a => { if (a.Length > 0) { try { if (int.TryParse(a[0], out int pid)) Process.GetProcessById(pid).Kill(); else foreach (var p in Process.GetProcesses().Where(p => p.ProcessName.Contains(a[0], StringComparison.OrdinalIgnoreCase))) p.Kill(); Console.WriteLine("  Killed"); } catch (Exception ex) { Console.WriteLine($"  Error: {ex.Message}"); } } };
        builtins["startproc"] = a => { if (a.Length > 0) { try { Process.Start(new ProcessStartInfo { FileName = a[0], Arguments = a.Length > 1 ? string.Join(" ", a.Skip(1)) : "", UseShellExecute = true }); Console.WriteLine("  Started"); } catch (Exception ex) { Console.WriteLine($"  Error: {ex.Message}"); } } };
        builtins["procinfo"] = a => { if (a.Length > 0) { try { var p = int.TryParse(a[0], out int pid) ? Process.GetProcessById(pid) : Process.GetProcessesByName(a[0]).FirstOrDefault(); if (p != null) { Console.WriteLine($"  Name: {p.ProcessName}"); Console.WriteLine($"  PID: {p.Id}"); Console.WriteLine($"  Memory: {p.WorkingSet64 / 1048576.0:F1}MB"); Console.WriteLine($"  CPU: {p.TotalProcessorTime.TotalSeconds:F1}s"); Console.WriteLine($"  Start: {p.StartTime:yyyy-MM-dd HH:mm:ss}"); } } catch { } } };

        builtins["netstat"] = _ => { try { var props = IPGlobalProperties.GetIPGlobalProperties(); foreach (var conn in props.GetActiveTcpConnections().Where(c => c.State == TcpState.Established).Take(20)) Console.WriteLine($"  {conn.LocalEndPoint} -> {conn.RemoteEndPoint}"); } catch { } };
        builtins["listening"] = _ => { try { var props = IPGlobalProperties.GetIPGlobalProperties(); foreach (var ep in props.GetActiveTcpListeners().Take(20)) Console.WriteLine($"  {ep}"); } catch { } };
        builtins["connstat"] = _ => { try { var props = IPGlobalProperties.GetIPGlobalProperties(); Console.WriteLine($"  Established: {props.GetActiveTcpConnections().Count(c => c.State == TcpState.Established)}"); Console.WriteLine($"  Listening: {props.GetActiveTcpListeners().Length}"); } catch { } };

        builtins["env"] = a => { if (a.Length > 0) { foreach (DictionaryEntry e in Environment.GetEnvironmentVariables()) if (e.Key?.ToString()?.Contains(a[0], StringComparison.OrdinalIgnoreCase) == true) Console.WriteLine($"  {e.Key}={e.Value}"); } else foreach (DictionaryEntry e in Environment.GetEnvironmentVariables()) Console.WriteLine($"  {e.Key}={e.Value}"); };
        builtins["home"] = _ => Console.WriteLine($"  {Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}");
        builtins["whoami"] = _ => Console.WriteLine($"  {Environment.UserDomainName}\\{Environment.UserName}");
        builtins["hostname"] = _ => Console.WriteLine($"  {Environment.MachineName}");
        builtins["cwd"] = _ => Console.WriteLine($"  {Directory.GetCurrentDirectory()}");
        builtins["settitle"] = a => { if (a.Length > 0) Console.Title = string.Join(" ", a); };
        builtins["beep"] = a => { if (a.Length >= 2 && int.TryParse(a[0], out int f) && int.TryParse(a[1], out int d)) try { Console.Beep(f, d); } catch { } };
        builtins["bell"] = _ => Console.Write("\a");
        builtins["clearscreen"] = _ => Console.Clear();
        builtins["reset"] = _ => { Console.ResetColor(); Console.Clear(); };
        builtins["consize"] = _ => Console.WriteLine($"  {Console.WindowWidth}x{Console.WindowHeight}");
        builtins["buffer"] = _ => Console.WriteLine($"  Buffer: {Console.BufferWidth}x{Console.BufferHeight}");
        builtins["resize"] = a => { if (a.Length >= 2 && int.TryParse(a[0], out int w) && int.TryParse(a[1], out int h)) { try { Console.SetWindowSize(w, h); Console.SetBufferSize(w, h); } catch { } } };
        builtins["cursorpos"] = _ => Console.WriteLine($"  ({Console.CursorLeft}, {Console.CursorTop})");
        builtins["setcursor"] = a => { if (a.Length >= 2 && int.TryParse(a[0], out int x) && int.TryParse(a[1], out int y)) try { Console.SetCursorPosition(x, y); } catch { } };
        builtins["hidecursor"] = _ => Console.CursorVisible = false;
        builtins["showcursor"] = _ => Console.CursorVisible = true;
        builtins["cursorstyle"] = a => { if (a.Length > 0) { if (a[0] == "block") Console.Write("\u001b[2 q"); else if (a[0] == "underline") Console.Write("\u001b[4 q"); else if (a[0] == "bar") Console.Write("\u001b[6 q"); } };

        builtins["isalpha"] = a => { if (a.Length > 0) Console.WriteLine(a[0].All(char.IsLetter) ? "  true" : "  false"); };
        builtins["isdigit"] = a => { if (a.Length > 0) Console.WriteLine(a[0].All(char.IsDigit) ? "  true" : "  false"); };
        builtins["isalphanum"] = a => { if (a.Length > 0) Console.WriteLine(a[0].All(char.IsLetterOrDigit) ? "  true" : "  false"); };
        builtins["countvowels"] = a => { if (a.Length > 0) Console.WriteLine($"  {a[0].Count(c => "aeiouAEIOU".Contains(c))}"); };
        builtins["countconsonants"] = a => { if (a.Length > 0) Console.WriteLine($"  {a[0].Count(c => char.IsLetter(c) && !"aeiouAEIOU".Contains(c))}"); };
        builtins["countspaces"] = a => { if (a.Length > 0) Console.WriteLine($"  {a[0].Count(char.IsWhiteSpace)}"); };
        builtins["initials"] = a => Console.WriteLine("  " + string.Join(".", a.Select(x => x[0])));
        builtins["acronym"] = a => Console.WriteLine("  " + string.Concat(a.Select(x => x[0])).ToUpper());
        builtins["capwords"] = a => Console.WriteLine("  " + string.Join(" ", a.Select(x => char.ToUpper(x[0]) + x[1..])));
        builtins["swapcase"] = a => Console.WriteLine("  " + string.Concat(a[0].Select(c => char.IsUpper(c) ? char.ToLower(c) : char.ToUpper(c))));
        builtins["titlecase"] = a => Console.WriteLine("  " + string.Join(" ", a.Select(x => char.ToUpper(x[0]) + x[1..].ToLower())));
        builtins["capitalize"] = a => { if (a.Length > 0) Console.WriteLine("  " + char.ToUpper(a[0][0]) + a[0][1..]); };
        builtins["strpadleft"] = a => { if (a.Length >= 2 && int.TryParse(a[1], out int w)) Console.WriteLine($"  {a[0].PadLeft(w)}"); };
        builtins["strpadright"] = a => { if (a.Length >= 2 && int.TryParse(a[1], out int w)) Console.WriteLine($"  {a[0].PadRight(w)}"); };
        builtins["strcenter"] = a => { if (a.Length >= 2 && int.TryParse(a[1], out int w)) Console.WriteLine($"  {a[0].PadLeft((a[0].Length + w) / 2).PadRight(w)}"); };
        builtins["strtrim"] = a => { if (a.Length > 0) Console.WriteLine($"  {a[0].Trim()}"); };
        builtins["strtrimstart"] = a => { if (a.Length > 0) Console.WriteLine($"  {a[0].TrimStart()}"); };
        builtins["strtrimend"] = a => { if (a.Length > 0) Console.WriteLine($"  {a[0].TrimEnd()}"); };
        builtins["strrev"] = a => { if (a.Length > 0) Console.WriteLine($"  {new string(a[0].Reverse().ToArray())}"); };
        builtins["strrepeat"] = a => { if (a.Length >= 2 && int.TryParse(a[1], out int n)) Console.WriteLine($"  {string.Concat(Enumerable.Repeat(a[0], n))}"); };
        builtins["strexplode"] = a => { if (a.Length >= 2) { foreach (var p in a[0].Split(a[1])) Console.WriteLine($"  {p}"); } };
        builtins["strimplode"] = a => { if (a.Length >= 2) Console.WriteLine($"  {string.Join(a[0], a.Skip(1))}"); };
        builtins["strcontains"] = a => { if (a.Length >= 2) Console.WriteLine(a[0].Contains(a[1]) ? "  true" : "  false"); };
        builtins["streq"] = a => { if (a.Length >= 2) Console.WriteLine(string.Equals(a[0], a[1], StringComparison.OrdinalIgnoreCase) ? "  true" : "  false"); };
        builtins["strcmpi"] = a => { if (a.Length >= 2) Console.WriteLine($"  {string.Compare(a[0], a[1], StringComparison.OrdinalIgnoreCase)}"); };
        builtins["strstartswith"] = a => { if (a.Length >= 2) Console.WriteLine(a[0].StartsWith(a[1]) ? "  true" : "  false"); };
        builtins["strendswith"] = a => { if (a.Length >= 2) Console.WriteLine(a[0].EndsWith(a[1]) ? "  true" : "  false"); };
        builtins["strindex"] = a => { if (a.Length >= 2) Console.WriteLine($"  {a[0].IndexOf(a[1])}"); };
        builtins["strrindex"] = a => { if (a.Length >= 2) Console.WriteLine($"  {a[0].LastIndexOf(a[1])}"); };
        builtins["strsubstr"] = a => { if (a.Length >= 3 && int.TryParse(a[1], out int s) && int.TryParse(a[2], out int l)) { int s0 = Math.Max(0, Math.Min(s, a[0].Length)); Console.WriteLine($"  {a[0].Substring(s0, Math.Min(l, a[0].Length - s0))}"); } };
        builtins["strchar"] = a => { if (a.Length >= 2 && int.TryParse(a[1], out int i) && i >= 0 && i < a[0].Length) Console.WriteLine($"  {a[0][i]}"); };
        builtins["strchars"] = a => { if (a.Length > 0) Console.WriteLine($"  {a[0].Length}"); };
        builtins["strcount"] = a => { if (a.Length >= 2) Console.WriteLine($"  {(a[0].Length - a[0].Replace(a[1], "").Length) / a[1].Length}"); };
        builtins["strfill"] = a => { if (a.Length >= 2 && int.TryParse(a[1], out int n)) Console.WriteLine($"  {new string(a[0][0], n)}"); };
        builtins["strempty"] = a => Console.WriteLine(string.IsNullOrEmpty(a[0] ?? "") ? "  true" : "  false");
        builtins["strisnull"] = a => Console.WriteLine(a[0] == null ? "  true" : "  false");
        builtins["strdefault"] = a => { if (a.Length >= 2) Console.WriteLine(string.IsNullOrEmpty(a[0]) ? $"  {a[1]}" : $"  {a[0]}"); };
        builtins["strconcat"] = a => Console.WriteLine($"  {string.Concat(a)}");
        builtins["strjoin"] = a => { if (a.Length >= 2) Console.WriteLine($"  {string.Join(a[0], a.Skip(1))}"); };
        builtins["strslice"] = a => { if (a.Length >= 2 && int.TryParse(a[1], out int s)) { int e = a.Length >= 3 && int.TryParse(a[2], out int end) ? end : a[0].Length; int s0 = Math.Max(0, Math.Min(s, a[0].Length)); int e0 = Math.Max(s0, Math.Min(e, a[0].Length)); Console.WriteLine($"  {a[0][s0..e0]}"); } };
        builtins["strlines"] = a => { if (a.Length > 0) { var lines = a[0].Split('\n'); Console.WriteLine($"  {lines.Length} lines"); } };
        builtins["strwords"] = a => { if (a.Length > 0) { var words = a[0].Split(' ', StringSplitOptions.RemoveEmptyEntries); Console.WriteLine($"  {words.Length} words"); } };
    }

    private static int gcd(int a, int b) { while (b != 0) { int t = b; b = a % b; a = t; } return a; }
}
