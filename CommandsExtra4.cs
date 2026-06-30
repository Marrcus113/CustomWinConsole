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

public static class CommandsExtra4
{
    public static void Register(Dictionary<string, Action<string[]>> builtins)
    {
        builtins["binary"] = a => { if (a.Length > 0 && int.TryParse(a[0], out int n)) Console.WriteLine($"  {Convert.ToString(n, 2)}"); };
        builtins["octal"] = a => { if (a.Length > 0 && int.TryParse(a[0], out int n)) Console.WriteLine($"  {Convert.ToString(n, 8)}"); };
        builtins["frombinary"] = a => { if (a.Length > 0) Console.WriteLine($"  {Convert.ToInt32(a[0], 2)}"); };
        builtins["fromoctal"] = a => { if (a.Length > 0) Console.WriteLine($"  {Convert.ToInt32(a[0], 8)}"); };
        builtins["fromhexnum"] = a => { if (a.Length > 0) Console.WriteLine($"  {Convert.ToInt32(a[0].Replace("0x", ""), 16)}"); };
        builtins["tohexnum"] = a => { if (a.Length > 0 && int.TryParse(a[0], out int n)) Console.WriteLine($"  0x{(a[0] == "0" ? "0" : Convert.ToString(n, 16).ToUpper())}"); };
        builtins["scientific"] = a => { if (a.Length > 0 && double.TryParse(a[0], out double v)) Console.WriteLine($"  {v:E6}"); };
        builtins["fixedpoint"] = a => { if (a.Length > 0 && double.TryParse(a[0], out double v)) Console.WriteLine($"  {v:F6}"); };
        builtins["numberformat"] = a => { if (a.Length > 0 && double.TryParse(a[0], out double v)) Console.WriteLine($"  {v:N2}"); };
        builtins["bytesize"] = a => { if (a.Length > 0 && double.TryParse(a[0], out double v)) { string[] u = ["B", "KB", "MB", "GB", "TB"]; int i = 0; while (v >= 1024 && i < u.Length - 1) { v /= 1024; i++; } Console.WriteLine($"  {v:F2} {u[i]}"); } };
        builtins["ordinal"] = a => { if (a.Length > 0 && int.TryParse(a[0], out int n)) { string suffix = (n % 100) switch { 11 or 12 or 13 => "th", _ => (n % 10) switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" } }; Console.WriteLine($"  {n}{suffix}"); } };
        builtins["compact"] = a => { if (a.Length > 0 && double.TryParse(a[0], out double v)) Console.WriteLine(v >= 1e9 ? $"  {v / 1e9:F1}B" : v >= 1e6 ? $"  {v / 1e6:F1}M" : v >= 1e3 ? $"  {v / 1e3:F1}K" : $"  {v:F0}"); };
        builtins["currency"] = a => { if (a.Length > 0 && double.TryParse(a[0], out double v)) Console.WriteLine($"  ${v:N2}"); };
        builtins["scientificprefix"] = a => { if (a.Length > 0 && double.TryParse(a[0], out double v)) { string[] p = ["", "k", "M", "G", "T", "P"]; int i = 0; while (v >= 1000 && i < p.Length - 1) { v /= 1000; i++; } Console.WriteLine($"  {v:F2}{p[i]}"); } };

        builtins["envget"] = a => { if (a.Length > 0) Console.WriteLine($"  {Environment.GetEnvironmentVariable(a[0]) ?? "null"}"); };
        builtins["envset"] = a => { if (a.Length >= 2) { Environment.SetEnvironmentVariable(a[0], a[1]); Console.WriteLine("  Set"); } };
        builtins["envdel"] = a => { if (a.Length > 0) { Environment.SetEnvironmentVariable(a[0], null); Console.WriteLine("  Deleted"); } };
        builtins["envlist"] = _ => { foreach (DictionaryEntry e in Environment.GetEnvironmentVariables()) Console.WriteLine($"  {e.Key}={e.Value}"); };
        builtins["envcount"] = _ => Console.WriteLine($"  {Environment.GetEnvironmentVariables().Count} vars");
        builtins["envsearch"] = a => { if (a.Length > 0) foreach (DictionaryEntry e in Environment.GetEnvironmentVariables()) if (e.Key?.ToString()?.Contains(a[0], StringComparison.OrdinalIgnoreCase) == true) Console.WriteLine($"  {e.Key}={e.Value}"); };
        builtins["machine"] = _ => Console.WriteLine($"  {Environment.MachineName}");
        builtins["username"] = _ => Console.WriteLine($"  {Environment.UserName}");
        builtins["domain"] = _ => Console.WriteLine($"  {Environment.UserDomainName}");
        builtins["osversion"] = _ => Console.WriteLine($"  {Environment.OSVersion}");
        builtins["64bit"] = _ => Console.WriteLine(Environment.Is64BitOperatingSystem ? "  64-bit" : "  32-bit");
        builtins["clrversion"] = _ => Console.WriteLine($"  {Environment.Version}");
        builtins["processorcount"] = _ => Console.WriteLine($"  {Environment.ProcessorCount}");
        builtins["tickcount"] = _ => Console.WriteLine($"  {Environment.TickCount64}ms");
        builtins["runtime"] = _ => Console.WriteLine($"  {TimeSpan.FromMilliseconds(Environment.TickCount64).TotalHours:F2}h");

        builtins["ginit"] = _ => { try { Process.Start("git", "init").WaitForExit(); Console.WriteLine("  Done"); } catch { Console.WriteLine("  git not found"); } };
        builtins["gclone"] = a => { if (a.Length > 0) { try { Process.Start("git", $"clone {a[0]}").WaitForExit(); Console.WriteLine("  Cloned"); } catch { Console.WriteLine("  Error"); } } };
        builtins["gstatus"] = _ => { try { Process.Start("git", "status").WaitForExit(); } catch { Console.WriteLine("  git not found"); } };
        builtins["glog"] = a => { try { string count = a.Length > 0 ? a[0] : "10"; Process.Start("git", $"log --oneline -{count}").WaitForExit(); } catch { Console.WriteLine("  git not found"); } };
        builtins["gbranch"] = _ => { try { Process.Start("git", "branch -v").WaitForExit(); } catch { Console.WriteLine("  git not found"); } };
        builtins["gadd"] = a => { try { Process.Start("git", $"add {string.Join(" ", a)}").WaitForExit(); Console.WriteLine("  Added"); } catch { Console.WriteLine("  Error"); } };
        builtins["gcommit"] = a => { try { Process.Start("git", $"commit -m \"{string.Join(" ", a)}\"").WaitForExit(); Console.WriteLine("  Committed"); } catch { Console.WriteLine("  Error"); } };
        builtins["gdiff"] = _ => { try { Process.Start("git", "diff").WaitForExit(); } catch { Console.WriteLine("  git not found"); } };
        builtins["gremote"] = _ => { try { Process.Start("git", "remote -v").WaitForExit(); } catch { Console.WriteLine("  git not found"); } };
        builtins["gpull"] = _ => { try { Process.Start("git", "pull").WaitForExit(); } catch { Console.WriteLine("  Error"); } };
        builtins["gpush"] = _ => { try { Process.Start("git", "push").WaitForExit(); } catch { Console.WriteLine("  Error"); } };

        builtins["isemail"] = a => { if (a.Length > 0) Console.WriteLine(Regex.IsMatch(a[0], @"^[^@\s]+@[^@\s]+\.[^@\s]+$") ? "  true" : "  false"); };
        builtins["isurl"] = a => { if (a.Length > 0) Console.WriteLine(Uri.TryCreate(a[0], UriKind.Absolute, out _) ? "  true" : "  false"); };
        builtins["isip"] = a => { if (a.Length > 0) Console.WriteLine(IPAddress.TryParse(a[0], out _) ? "  true" : "  false"); };
        builtins["isphone"] = a => { if (a.Length > 0) Console.WriteLine(Regex.IsMatch(a[0], @"^[\+]?[(]?[0-9]{3}[)]?[-\s\.]?[0-9]{3}[-\s\.]?[0-9]{4,6}$") ? "  true" : "  false"); };
        builtins["ishex"] = a => { if (a.Length > 0) Console.WriteLine(Regex.IsMatch(a[0].Replace("0x", ""), @"^[0-9a-fA-F]+$") ? "  true" : "  false"); };
        builtins["isbase64"] = a => { if (a.Length > 0) { try { Convert.FromBase64String(a[0]); Console.WriteLine("  true"); } catch { Console.WriteLine("  false"); } } };
        builtins["isdate"] = a => { if (a.Length > 0) Console.WriteLine(DateTime.TryParse(a[0], out _) ? "  true" : "  false"); };
        builtins["isint"] = a => { if (a.Length > 0) Console.WriteLine(int.TryParse(a[0], out _) ? "  true" : "  false"); };
        builtins["isfloat"] = a => { if (a.Length > 0) Console.WriteLine(double.TryParse(a[0], out _) ? "  true" : "  false"); };
        builtins["isbool"] = a => { if (a.Length > 0) Console.WriteLine(bool.TryParse(a[0], out _) ? "  true" : "  false"); };
        builtins["hasupper"] = a => { if (a.Length > 0) Console.WriteLine(a[0].Any(char.IsUpper) ? "  true" : "  false"); };
        builtins["haslower"] = a => { if (a.Length > 0) Console.WriteLine(a[0].Any(char.IsLower) ? "  true" : "  false"); };
        builtins["hasdigit"] = a => { if (a.Length > 0) Console.WriteLine(a[0].Any(char.IsDigit) ? "  true" : "  false"); };
        builtins["hasspace"] = a => { if (a.Length > 0) Console.WriteLine(a[0].Any(char.IsWhiteSpace) ? "  true" : "  false"); };
        builtins["allupper"] = a => { if (a.Length > 0) Console.WriteLine(a[0].All(char.IsUpper) ? "  true" : "  false"); };
        builtins["alllower"] = a => { if (a.Length > 0) Console.WriteLine(a[0].All(char.IsLower) ? "  true" : "  false"); };
        builtins["alldigit"] = a => { if (a.Length > 0) Console.WriteLine(a[0].All(char.IsDigit) ? "  true" : "  false"); };
        builtins["allalpha"] = a => { if (a.Length > 0) Console.WriteLine(a[0].All(char.IsLetter) ? "  true" : "  false"); };
        builtins["isalphanum"] = a => { if (a.Length > 0) Console.WriteLine(a[0].All(char.IsLetterOrDigit) ? "  true" : "  false"); };
        builtins["isprintable"] = a => { if (a.Length > 0) Console.WriteLine(a[0].All(c => !char.IsControl(c) || c == '\n' || c == '\r') ? "  true" : "  false"); };
        builtins["isascii"] = a => { if (a.Length > 0) Console.WriteLine(a[0].All(c => c < 128) ? "  true" : "  false"); };
        builtins["isunicode"] = a => { if (a.Length > 0) Console.WriteLine(a[0].Any(c => c > 127) ? "  true" : "  false"); };
        builtins["issymmetric"] = a => { if (a.Length > 0) Console.WriteLine(a[0] == new string(a[0].Reverse().ToArray()) ? "  true" : "  false"); };
        builtins["issorted"] = a => { if (a.Length > 0) { var sorted = a.OrderBy(x => x).ToArray(); Console.WriteLine(a.SequenceEqual(sorted) ? "  true" : "  false"); } };

        builtins["mean"] = a => { if (a.Length > 0) { var nums = a.Select(double.Parse).ToList(); Console.WriteLine($"  {nums.Average():F4}"); } };
        builtins["median"] = a => { if (a.Length > 0) { var nums = a.Select(double.Parse).OrderBy(x => x).ToList(); double mid = nums.Count % 2 == 0 ? (nums[nums.Count / 2 - 1] + nums[nums.Count / 2]) / 2 : nums[nums.Count / 2]; Console.WriteLine($"  {mid:F4}"); } };
        builtins["mode"] = a => { if (a.Length > 0) { var groups = a.GroupBy(x => x).OrderByDescending(g => g.Count()); Console.WriteLine($"  {groups.First().Key} ({groups.First().Count()}x)"); } };
        builtins["range"] = a => { if (a.Length > 0) { var nums = a.Select(double.Parse).ToList(); Console.WriteLine($"  {nums.Max() - nums.Min():F4}"); } };
        builtins["stdev"] = a => { if (a.Length > 1) { var nums = a.Select(double.Parse).ToList(); double avg = nums.Average(); double variance = nums.Select(x => (x - avg) * (x - avg)).Sum(); Console.WriteLine($"  {Math.Sqrt(variance / nums.Count):F4}"); } };
        builtins["variance"] = a => { if (a.Length > 1) { var nums = a.Select(double.Parse).ToList(); double avg = nums.Average(); Console.WriteLine($"  {nums.Select(x => (x - avg) * (x - avg)).Average():F4}"); } };
        builtins["quartile"] = a => { if (a.Length > 3) { var nums = a.Select(double.Parse).OrderBy(x => x).ToList(); int q1 = nums.Count / 4; int q3 = nums.Count * 3 / 4; Console.WriteLine($"  Q1={nums[q1]:F4} Q3={nums[q3]:F4}"); } };
        builtins["iqr"] = a => { if (a.Length > 3) { var nums = a.Select(double.Parse).OrderBy(x => x).ToList(); int q1 = nums.Count / 4; int q3 = nums.Count * 3 / 4; Console.WriteLine($"  {nums[q3] - nums[q1]:F4}"); } };
        builtins["zscore"] = a => { if (a.Length == 2 && double.TryParse(a[0], out double val)) { var nums = a.Skip(1).Select(double.Parse).ToList(); double avg = nums.Average(); double sd = Math.Sqrt(nums.Select(x => (x - avg) * (x - avg)).Average()); Console.WriteLine($"  {(val - avg) / sd:F4}"); } };
        builtins["correlation"] = a => { if (a.Length >= 4 && a.Length % 2 == 0) { var x = new List<double>(); var y = new List<double>(); for (int i = 0; i < a.Length; i += 2) { x.Add(double.Parse(a[i])); y.Add(double.Parse(a[i + 1])); } double mx = x.Average(), my = y.Average(); double num = x.Zip(y, (xi, yi) => (xi - mx) * (yi - my)).Sum(); double dx = Math.Sqrt(x.Select(xi => (xi - mx) * (xi - mx)).Sum()); double dy = Math.Sqrt(y.Select(yi => (yi - my) * (yi - my)).Sum()); Console.WriteLine($"  {num / (dx * dy):F4}"); } };
        builtins["percent"] = a => { if (a.Length >= 2 && double.TryParse(a[0], out double part) && double.TryParse(a[1], out double whole) && whole != 0) Console.WriteLine($"  {part / whole * 100:F2}%"); };
        builtins["increase"] = a => { if (a.Length >= 2 && double.TryParse(a[0], out double old) && double.TryParse(a[1], out double @new) && old != 0) Console.WriteLine($"  {(@new - old) / Math.Abs(old) * 100:F2}%"); };
        builtins["roi"] = a => { if (a.Length >= 2 && double.TryParse(a[0], out double cost) && double.TryParse(a[1], out double profit) && cost != 0) Console.WriteLine($"  {(profit - cost) / cost * 100:F2}%"); };

        builtins["timerstart"] = _ => Program.TimerSw = Stopwatch.StartNew();
        builtins["timerstop"] = _ => { if (Program.TimerSw != null) { Program.TimerSw.Stop(); Console.WriteLine($"  {Program.TimerSw.Elapsed.TotalSeconds:F3}s"); } };
        builtins["timerlap"] = _ => { if (Program.TimerSw != null) Console.WriteLine($"  Lap: {Program.TimerSw.Elapsed.TotalSeconds:F3}s"); };
        builtins["timerreset"] = _ => { Program.TimerSw = null; Console.WriteLine("  Reset"); };
        builtins["timerstatus"] = _ => { if (Program.TimerSw != null && Program.TimerSw.IsRunning) Console.WriteLine($"  Running: {Program.TimerSw.Elapsed.TotalSeconds:F3}s"); else Console.WriteLine("  Stopped"); };

        builtins["sleepms"] = a => { if (a.Length > 0 && int.TryParse(a[0], out int ms)) Thread.Sleep(ms); };
        builtins["timestamp"] = _ => Console.WriteLine($"  {DateTimeOffset.UtcNow.ToUnixTimeSeconds()}");
        builtins["timestampms"] = _ => Console.WriteLine($"  {DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");
        builtins["fromtimestamp"] = a => { if (a.Length > 0 && long.TryParse(a[0], out long ts)) Console.WriteLine($"  {DateTimeOffset.FromUnixTimeSeconds(ts):yyyy-MM-dd HH:mm:ss}"); };
        builtins["nowiso"] = _ => Console.WriteLine($"  {DateTime.UtcNow:O}");
        builtins["todayiso"] = _ => Console.WriteLine($"  {DateTime.Today:yyyy-MM-dd}");
        builtins["year"] = _ => Console.WriteLine($"  {DateTime.Now.Year}");
        builtins["month"] = _ => Console.WriteLine($"  {DateTime.Now.Month}");
        builtins["day"] = _ => Console.WriteLine($"  {DateTime.Now.Day}");
        builtins["hour"] = _ => Console.WriteLine($"  {DateTime.Now.Hour}");
        builtins["minute"] = _ => Console.WriteLine($"  {DateTime.Now.Minute}");
        builtins["second"] = _ => Console.WriteLine($"  {DateTime.Now.Second}");
        builtins["dayofweek"] = _ => Console.WriteLine($"  {DateTime.Now.DayOfWeek}");
        builtins["dayofyear"] = _ => Console.WriteLine($"  {DateTime.Now.DayOfYear}");
        builtins["weeknumber"] = _ => Console.WriteLine($"  {System.Globalization.CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(DateTime.Now, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday)}");
        builtins["isleapyear"] = a => { if (a.Length > 0 && int.TryParse(a[0], out int y)) Console.WriteLine(DateTime.IsLeapYear(y) ? "  true" : "  false"); };
        builtins["daysinmonth"] = a => { if (a.Length >= 2 && int.TryParse(a[0], out int y) && int.TryParse(a[1], out int m)) Console.WriteLine($"  {DateTime.DaysInMonth(y, m)}"); };
        builtins["addtime"] = a => { if (a.Length >= 2 && int.TryParse(a[1], out int days)) Console.WriteLine($"  {DateTime.Now.AddDays(days):yyyy-MM-dd HH:mm:ss}"); };
        builtins["difftime"] = a => { if (a.Length >= 2 && DateTime.TryParse(a[0], out DateTime d1) && DateTime.TryParse(a[1], out DateTime d2)) Console.WriteLine($"  {(d2 - d1).TotalDays:F1} days"); };
        builtins["age"] = a => { if (a.Length > 0 && DateTime.TryParse(a[0], out DateTime bday)) Console.WriteLine($"  {((DateTime.Now - bday).TotalDays / 365.25):F1} years"); };
        builtins["nextfriday"] = _ => { var d = DateTime.Today; while (d.DayOfWeek != DayOfWeek.Friday) d = d.AddDays(1); Console.WriteLine($"  {d:yyyy-MM-dd}"); };
        builtins["nextmonday"] = _ => { var d = DateTime.Today; while (d.DayOfWeek != DayOfWeek.Monday) d = d.AddDays(1); Console.WriteLine($"  {d:yyyy-MM-dd}"); };
        builtins["isweekend"] = _ => Console.WriteLine(DateTime.Now.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ? "  true" : "  false");
        builtins["isworkday"] = _ => Console.WriteLine(DateTime.Now.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ? "  false" : "  true");
    }
}
