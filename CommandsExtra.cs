using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace CustomWinConsole;

static class CommandsExtra
{
    public static void Register(Dictionary<string, Action<string[]>> builtins)
    {
        // Text tools
        builtins["titlecase"] = a => Console.WriteLine(string.Join(" ", a).ToTitleCase());
        builtins["swapcase"] = a => Console.WriteLine(string.Concat(string.Join(" ", a).Select(c => char.IsUpper(c) ? char.ToLower(c) : char.ToUpper(c))));
        builtins["isnumber"] = a => Console.WriteLine(double.TryParse(a.FirstOrDefault() ?? "", out _) ? "  true" : "  false");
        builtins["isalpha"] = a => Console.WriteLine(string.Join(" ", a).All(char.IsLetter) ? "  true" : "  false");
        builtins["isalnum"] = a => Console.WriteLine(string.Join(" ", a).All(char.IsLetterOrDigit) ? "  true" : "  false");
        builtins["isupper"] = a => Console.WriteLine(string.Join(" ", a).All(char.IsUpper) ? "  true" : "  false");
        builtins["islower"] = a => Console.WriteLine(string.Join(" ", a).All(char.IsLower) ? "  true" : "  false");
        builtins["urlencode"] = a => Console.WriteLine(Uri.EscapeDataString(string.Join(" ", a)));
        builtins["urldecode"] = a => Console.WriteLine(Uri.UnescapeDataString(a.FirstOrDefault() ?? ""));
        builtins["capitalize"] = a => { var s = string.Join(" ", a); Console.WriteLine(s.Length > 0 ? char.ToUpper(s[0]) + s[1..] : ""); };
        builtins["countchars"] = a => Console.WriteLine($"  {string.Join(" ", a).Length} chars, {string.Join(" ", a).Count(c => c == ' ')} spaces, {string.Join(" ", a).Count(c => char.IsDigit(c))} digits");
        builtins["charcode"] = a => { foreach (var c in a[0]) Console.Write($"{(int)c} "); Console.WriteLine(); };
        builtins["fromcharcode"] = a => { try { Console.WriteLine(new string(a.Select(x => (char)int.Parse(x)).ToArray())); } catch { Console.WriteLine("  Invalid input"); } };
        builtins["repeatline"] = a => { if (a.Length >= 2 && int.TryParse(a[0], out var n)) for (int i = 0; i < n; i++) Console.WriteLine(string.Join(" ", a[1..])); };
        builtins["strip"] = a => Console.WriteLine(string.Join(" ", a).Trim());
        builtins["lstrip"] = a => Console.WriteLine(string.Join(" ", a).TrimStart());
        builtins["rstrip"] = a => Console.WriteLine(string.Join(" ", a).TrimEnd());
        builtins["squeeze"] = a => Console.WriteLine(Regex.Replace(string.Join(" ", a), @"\s+", " "));
        builtins["escape"] = a => Console.WriteLine(System.Net.WebUtility.HtmlEncode(string.Join(" ", a)));
        builtins["unescape"] = a => Console.WriteLine(System.Net.WebUtility.HtmlDecode(a.FirstOrDefault() ?? ""));
        builtins["mask"] = a => { if (a.Length > 0) { var s = a[0]; Console.WriteLine(s.Length > 4 ? s[..2] + new string('*', s.Length - 4) + s[^2..] : new string('*', s.Length)); } };

        // Math extras
        builtins["abs"] = a => { if (double.TryParse(a.FirstOrDefault(), out var n)) Console.WriteLine($"  {Math.Abs(n)}"); };
        builtins["ceil"] = a => { if (double.TryParse(a.FirstOrDefault(), out var n)) Console.WriteLine($"  {Math.Ceiling(n)}"); };
        builtins["floor"] = a => { if (double.TryParse(a.FirstOrDefault(), out var n)) Console.WriteLine($"  {Math.Floor(n)}"); };
        builtins["round"] = a => { if (a.Length >= 1 && double.TryParse(a[0], out var n)) { var d = a.Length > 1 && int.TryParse(a[1], out var p) ? p : 0; Console.WriteLine($"  {Math.Round(n, d)}"); } };
        builtins["log"] = a => { if (a.Length >= 1 && double.TryParse(a[0], out var n)) { Console.WriteLine(a.Length > 1 && double.TryParse(a[1], out var b) ? $"  {Math.Log(n, b)}" : $"  {Math.Log(n)}"); } };
        builtins["sin"] = a => { if (double.TryParse(a.FirstOrDefault(), out var n)) Console.WriteLine($"  {Math.Sin(n)}"); };
        builtins["cos"] = a => { if (double.TryParse(a.FirstOrDefault(), out var n)) Console.WriteLine($"  {Math.Cos(n)}"); };
        builtins["tan"] = a => { if (double.TryParse(a.FirstOrDefault(), out var n)) Console.WriteLine($"  {Math.Tan(n)}"); };
        builtins["asin"] = a => { if (double.TryParse(a.FirstOrDefault(), out var n)) Console.WriteLine($"  {Math.Asin(n)}"); };
        builtins["acos"] = a => { if (double.TryParse(a.FirstOrDefault(), out var n)) Console.WriteLine($"  {Math.Acos(n)}"); };
        builtins["atan"] = a => { if (double.TryParse(a.FirstOrDefault(), out var n)) Console.WriteLine($"  {Math.Atan(n)}"); };
        builtins["sinh"] = a => { if (double.TryParse(a.FirstOrDefault(), out var n)) Console.WriteLine($"  {Math.Sinh(n)}"); };
        builtins["cosh"] = a => { if (double.TryParse(a.FirstOrDefault(), out var n)) Console.WriteLine($"  {Math.Cosh(n)}"); };
        builtins["tanh"] = a => { if (double.TryParse(a.FirstOrDefault(), out var n)) Console.WriteLine($"  {Math.Tanh(n)}"); };
        builtins["exp"] = a => { if (double.TryParse(a.FirstOrDefault(), out var n)) Console.WriteLine($"  {Math.Exp(n)}"); };
        builtins["ln"] = a => { if (double.TryParse(a.FirstOrDefault(), out var n)) Console.WriteLine($"  {Math.Log(n)}"); };
        builtins["log2"] = a => { if (double.TryParse(a.FirstOrDefault(), out var n)) Console.WriteLine($"  {Math.Log2(n)}"); };
        builtins["log10"] = a => { if (double.TryParse(a.FirstOrDefault(), out var n)) Console.WriteLine($"  {Math.Log10(n)}"); };
        builtins["sign"] = a => { if (double.TryParse(a.FirstOrDefault(), out var n)) Console.WriteLine($"  {Math.Sign(n)}"); };
        builtins["clamp"] = a => { if (a.Length >= 3 && double.TryParse(a[0], out var v) && double.TryParse(a[1], out var min) && double.TryParse(a[2], out var max)) Console.WriteLine($"  {Math.Clamp(v, min, max)}"); };
        builtins["lerp"] = a => { if (a.Length >= 3 && double.TryParse(a[0], out var a1) && double.TryParse(a[1], out var b1) && double.TryParse(a[2], out var t)) Console.WriteLine($"  {a1 + (b1 - a1) * t}"); };
        builtins["gcd"] = a => { if (a.Length >= 2 && long.TryParse(a[0], out var x) && long.TryParse(a[1], out var y)) { while (y != 0) { (x, y) = (y, x % y); } Console.WriteLine($"  {x}"); } };
        builtins["lcm"] = a => { if (a.Length >= 2 && long.TryParse(a[0], out var x) && long.TryParse(a[1], out var y)) Console.WriteLine($"  {x / GCD(x, y) * y}"); };
        builtins["pythag"] = a => { if (a.Length >= 2 && double.TryParse(a[0], out var aa) && double.TryParse(a[1], out var bb)) Console.WriteLine($"  {Math.Sqrt(aa * aa + bb * bb):F4}"); };
        builtins["degrees"] = a => { if (double.TryParse(a.FirstOrDefault(), out var n)) Console.WriteLine($"  {n * 180 / Math.PI:F4}°"); };
        builtins["radians"] = a => { if (double.TryParse(a.FirstOrDefault(), out var n)) Console.WriteLine($"  {n * Math.PI / 180:F4} rad"); };
        builtins["pct"] = a => { if (a.Length >= 2 && double.TryParse(a[0], out var v) && double.TryParse(a[1], out var t)) Console.WriteLine($"  {v / t * 100:F2}%"); };
        builtins["tip"] = a => { if (a.Length >= 2 && double.TryParse(a[0], out var bill) && double.TryParse(a[1], out var pct)) Console.WriteLine($"  Tip: ${bill * pct / 100:F2}  Total: ${bill + bill * pct / 100:F2}"); };
        builtins["bmi"] = a => { if (a.Length >= 2 && double.TryParse(a[0], out var kg) && double.TryParse(a[1], out var m)) { var bmi = kg / (m * m); var cat = bmi < 18.5 ? "underweight" : bmi < 25 ? "normal" : bmi < 30 ? "overweight" : "obese"; Console.WriteLine($"  BMI: {bmi:F1} ({cat})"); } };

        // Time / date extras
        builtins["now"] = _ => Console.WriteLine($"  {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
        builtins["today"] = _ => Console.WriteLine($"  {DateTime.Now:yyyy-MM-dd}");
        builtins["year"] = _ => Console.WriteLine($"  {DateTime.Now:yyyy}");
        builtins["month"] = _ => Console.WriteLine($"  {DateTime.Now:MMMM}");
        builtins["day"] = _ => Console.WriteLine($"  {DateTime.Now:dddd}");
        builtins["weekday"] = _ => Console.WriteLine($"  {DateTime.Now:dddd}");
        builtins["hour"] = _ => Console.WriteLine($"  {DateTime.Now:HH}");
        builtins["minute"] = _ => Console.WriteLine($"  {DateTime.Now:mm}");
        builtins["second"] = _ => Console.WriteLine($"  {DateTime.Now:ss}");
        builtins["epoch"] = _ => Console.WriteLine($"  { DateTimeOffset.UtcNow.ToUnixTimeSeconds()}");
        builtins["epochms"] = _ => Console.WriteLine($"  { DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");
        builtins["fromepoch"] = a => { if (long.TryParse(a.FirstOrDefault(), out var s)) Console.WriteLine($"  {DateTimeOffset.FromUnixTimeSeconds(s):yyyy-MM-dd HH:mm:ss}"); };
        builtins["daysinmonth"] = a => { if (a.Length >= 2 && int.TryParse(a[0], out var y) && int.TryParse(a[1], out var m)) Console.WriteLine($"  {DateTime.DaysInMonth(y, m)} days"); };
        builtins["isleap"] = a => { if (int.TryParse(a.FirstOrDefault(), out var y)) Console.WriteLine(DateTime.IsLeapYear(y) ? "  Leap year" : "  Not leap year"); };
        builtins["age"] = a => { if (DateTime.TryParse(a.FirstOrDefault(), out var bd)) Console.WriteLine($"  {DateTime.Now.Year - bd.Year - (DateTime.Now.DayOfYear < bd.DayOfYear ? 1 : 0)} years"); };
        builtins["daysbetween"] = a => { if (a.Length >= 2 && DateTime.TryParse(a[0], out var d1) && DateTime.TryParse(a[1], out var d2)) Console.WriteLine($"  {(d2 - d1).TotalDays:F0} days"); };
        builtins["addtime"] = a => { if (a.Length >= 2 && int.TryParse(a[1], out var h)) Console.WriteLine(DateTime.Now.AddHours(h).ToString("yyyy-MM-dd HH:mm:ss")); };

        // File extras
        builtins["dupefile"] = a => { if (a.Length >= 2) { File.Copy(Path.Combine(Directory.GetCurrentDirectory(), a[0]), Path.Combine(Directory.GetCurrentDirectory(), a[1]), true); Console.WriteLine($"  Copied: {a[0]} -> {a[1]}"); } };
        builtins["rename"] = a => { if (a.Length >= 2) { File.Move(Path.Combine(Directory.GetCurrentDirectory(), a[0]), Path.Combine(Directory.GetCurrentDirectory(), a[1])); Console.WriteLine($"  Renamed: {a[0]} -> {a[1]}"); } };
        builtins["move"] = a => { if (a.Length >= 2) { File.Move(Path.Combine(Directory.GetCurrentDirectory(), a[0]), Path.Combine(Directory.GetCurrentDirectory(), a[1]), true); Console.WriteLine($"  Moved: {a[0]} -> {a[1]}"); } };
        builtins["type"] = a => { if (a.Length > 0) { var p = Path.Combine(Directory.GetCurrentDirectory(), a[0]); if (File.Exists(p)) Console.Write(File.ReadAllText(p)); else Console.WriteLine($"  Not found: {a[0]}"); } };
        builtins["read"] = a => { if (a.Length > 0) { var p = Path.Combine(Directory.GetCurrentDirectory(), a[0]); if (File.Exists(p)) Console.Write(File.ReadAllText(p)); else Console.WriteLine($"  Not found: {a[0]}"); } };
        builtins["write"] = a => { if (a.Length >= 2) { File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), a[0]), string.Join(" ", a[1..])); Console.WriteLine($"  Written: {a[0]}"); } };
        builtins["append"] = a => { if (a.Length >= 2) { File.AppendAllText(Path.Combine(Directory.GetCurrentDirectory(), a[0]), string.Join(" ", a[1..]) + "\n"); Console.WriteLine($"  Appended: {a[0]}"); } };
        builtins["insert"] = a => { if (a.Length >= 3 && int.TryParse(a[1], out var line)) { var p = Path.Combine(Directory.GetCurrentDirectory(), a[0]); var lines = File.Exists(p) ? File.ReadAllLines(p).ToList() : new List<string>(); while (lines.Count < line) lines.Add(""); lines.Insert(line - 1, string.Join(" ", a[2..])); File.WriteAllLines(p, lines); Console.WriteLine($"  Inserted at line {line}"); } };
        builtins["deleteline"] = a => { if (a.Length >= 2 && int.TryParse(a[1], out var line)) { var p = Path.Combine(Directory.GetCurrentDirectory(), a[0]); var lines = File.ReadAllLines(p).ToList(); if (line > 0 && line <= lines.Count) { lines.RemoveAt(line - 1); File.WriteAllLines(p, lines); Console.WriteLine($"  Deleted line {line}"); } } };
        builtins["readline"] = a => { if (a.Length >= 2 && int.TryParse(a[1], out var line)) { var p = Path.Combine(Directory.GetCurrentDirectory(), a[0]); var lines = File.ReadAllLines(p); if (line > 0 && line <= lines.Length) Console.WriteLine(lines[line - 1]); } };
        builtins["filetime"] = a => { if (a.Length > 0) { var p = Path.Combine(Directory.GetCurrentDirectory(), a[0]); if (File.Exists(p)) { var fi = new FileInfo(p); Console.WriteLine($"  Created:  {fi.CreationTime}"); Console.WriteLine($"  Modified: {fi.LastWriteTime}"); Console.WriteLine($"  Accessed: {fi.LastAccessTime}"); Console.WriteLine($"  Size:     {fi.Length} bytes"); } } };
        builtins["mkdirs"] = a => { foreach (var d in a) Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), d)); Console.WriteLine($"  Created {a.Length} dirs"); };
        builtins["rmdir"] = a => { if (a.Length > 0) { var p = Path.Combine(Directory.GetCurrentDirectory(), a[0]); if (Directory.Exists(p)) { Directory.Delete(p, true); Console.WriteLine($"  Deleted: {a[0]}"); } } };
        builtins["copydir"] = a => { if (a.Length >= 2) { var src = Path.Combine(Directory.GetCurrentDirectory(), a[0]); var dst = Path.Combine(Directory.GetCurrentDirectory(), a[1]); CopyDir(src, dst); Console.WriteLine($"  Copied: {a[0]} -> {a[1]}"); } };
        builtins["countfiles"] = a => { var p = a.Length > 0 ? Path.Combine(Directory.GetCurrentDirectory(), a[0]) : Directory.GetCurrentDirectory(); Console.WriteLine($"  {Directory.GetFiles(p, "*", new EnumerationOptions { RecurseSubdirectories = true }).Length} files"); };
        builtins["countdirs"] = a => { var p = a.Length > 0 ? Path.Combine(Directory.GetCurrentDirectory(), a[0]) : Directory.GetCurrentDirectory(); Console.WriteLine($"  {Directory.GetDirectories(p, "*", new EnumerationOptions { RecurseSubdirectories = true }).Length} dirs"); };
        builtins["total-size"] = a => { var p = a.Length > 0 ? Path.Combine(Directory.GetCurrentDirectory(), a[0]) : Directory.GetCurrentDirectory(); var s = Directory.GetFiles(p, "*", new EnumerationOptions { RecurseSubdirectories = true }).Sum(f => new FileInfo(f).Length); Console.WriteLine($"  {s / 1024.0 / 1024.0:F2} MB"); };
        builtins["empty"] = a => { if (a.Length > 0) { File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), a[0]), ""); Console.WriteLine($"  Emptied: {a[0]}"); } };
        builtins["backup"] = a => { if (a.Length > 0) { var p = Path.Combine(Directory.GetCurrentDirectory(), a[0]); if (File.Exists(p)) { File.Copy(p, p + ".bak", true); Console.WriteLine($"  Backed up: {a[0]}"); } } };
        builtins["restore"] = a => { if (a.Length > 0) { var p = Path.Combine(Directory.GetCurrentDirectory(), a[0]); if (File.Exists(p)) { File.Copy(p, p[^4..] == ".bak" ? p[..^4] : p + ".orig", true); Console.WriteLine($"  Restored: {a[0]}"); } } };
        builtins["hashfile"] = a => { if (a.Length > 0) { var p = Path.Combine(Directory.GetCurrentDirectory(), a[0]); if (File.Exists(p)) { var h = System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(p)); Console.WriteLine($"  SHA256: {Convert.ToHexString(h).ToLower()}"); } } };
        builtins["filestat"] = a => { if (a.Length > 0) { var p = Path.Combine(Directory.GetCurrentDirectory(), a[0]); if (File.Exists(p)) { var fi = new FileInfo(p); Console.WriteLine($"  Name:     {fi.Name}"); Console.WriteLine($"  Size:     {fi.Length}"); Console.WriteLine($"  Created:  {fi.CreationTime}"); Console.WriteLine($"  Modified: {fi.LastWriteTime}"); Console.WriteLine($"  ReadOnly: {fi.IsReadOnly}"); Console.WriteLine($"  Hidden:   {(fi.Attributes & FileAttributes.Hidden) != 0}"); } } };

        // System extras
        builtins["cpus"] = _ => Console.WriteLine($"  {Environment.ProcessorCount} cores");
        builtins["arch"] = _ => Console.WriteLine($"  {RuntimeInformation.OSArchitecture}");
        builtins["runtime"] = _ => Console.WriteLine($"  {RuntimeInformation.RuntimeIdentifier}");
        builtins["framework"] = _ => Console.WriteLine($"  {RuntimeInformation.FrameworkDescription}");
        builtins["os"] = _ => Console.WriteLine($"  {RuntimeInformation.OSDescription}");
        builtins["user"] = _ => Console.WriteLine($"  {Environment.UserName}");
        builtins["domain"] = _ => Console.WriteLine($"  {Environment.UserDomainName}");
        builtins["homedir"] = _ => Console.WriteLine($"  {Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}");
        builtins["tempdir"] = _ => Console.WriteLine($"  {Path.GetTempPath()}");
        builtins["sysdir"] = _ => Console.WriteLine($"  {Environment.GetFolderPath(Environment.SpecialFolder.System)}");
        builtins["windir"] = _ => Console.WriteLine($"  {Environment.GetFolderPath(Environment.SpecialFolder.Windows)}");
        builtins["desktop"] = _ => Console.WriteLine($"  {Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}");
        builtins["downloads"] = _ => Console.WriteLine($"  {Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}/Downloads");
        builtins["documents"] = _ => Console.WriteLine($"  {Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}");
        builtins["pictures"] = _ => Console.WriteLine($"  {Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)}");
        builtins["music"] = _ => Console.WriteLine($"  {Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)}");
        builtins["videos"] = _ => Console.WriteLine($"  {Environment.GetFolderPath(Environment.SpecialFolder.MyVideos)}");
        builtins["appdata"] = _ => Console.WriteLine($"  {Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}");
        builtins["localappdata"] = _ => Console.WriteLine($"  {Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}");
        builtins["startup"] = _ => Console.WriteLine($"  {Environment.GetFolderPath(Environment.SpecialFolder.Startup)}");
        builtins["recent"] = _ => Console.WriteLine($"  {Environment.GetFolderPath(Environment.SpecialFolder.Recent)}");
        builtins["favorites"] = _ => Console.WriteLine($"  {Environment.GetFolderPath(Environment.SpecialFolder.Favorites)}");
        builtins["programs"] = _ => Console.WriteLine($"  {Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}");
        builtins["x86"] = _ => Console.WriteLine($"  {Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)}");
        builtins["common"] = _ => Console.WriteLine($"  {Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles)}");
        builtins["fonts"] = _ => Console.WriteLine($"  {Environment.GetFolderPath(Environment.SpecialFolder.Fonts)}");
        builtins["templates"] = _ => Console.WriteLine($"  {Environment.GetFolderPath(Environment.SpecialFolder.Templates)}");
        builtins["cookies"] = _ => Console.WriteLine($"  {Environment.GetFolderPath(Environment.SpecialFolder.Cookies)}");
        builtins["history-dir"] = _ => Console.WriteLine($"  {Environment.GetFolderPath(Environment.SpecialFolder.History)}");
        builtins["internet"] = _ => Console.WriteLine($"  {Environment.GetFolderPath(Environment.SpecialFolder.InternetCache)}");
        builtins["printers"] = _ => Console.WriteLine($"  {Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/Microsoft/Windows/Printer Shortcuts");
        builtins["admin"] = _ => Console.WriteLine($"  {Environment.GetFolderPath(Environment.SpecialFolder.Programs)}");
        builtins["cdh"] = _ => Console.WriteLine($"  {Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}");
        builtins["userprofile"] = _ => Console.WriteLine($"  {Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}");
        builtins["machinename"] = _ => Console.WriteLine($"  {Environment.MachineName}");
        builtins["tickcount"] = _ => Console.WriteLine($"  {Environment.TickCount64} ms");
        builtins["processor-info"] = _ => { Console.WriteLine($"  Cores: {Environment.ProcessorCount}"); Console.WriteLine($"  64-bit: {Environment.Is64BitOperatingSystem}"); Console.WriteLine($"  .NET: {Environment.Version}"); };

        // Network extras
        builtins["myip"] = _ => { try { Console.WriteLine($"  {new HttpClient().GetStringAsync("https://api.ipify.org").Result}"); } catch { Console.WriteLine("  Could not determine IP"); } };
        builtins["headers"] = a => { if (a.Length > 0) { try { var resp = new HttpClient().SendAsync(new HttpRequestMessage(HttpMethod.Head, a[0])).Result; foreach (var h in resp.Headers) Console.WriteLine($"  {h.Key}: {string.Join(", ", h.Value)}"); } catch (Exception ex) { Console.WriteLine($"  Error: {ex.Message}"); } } };
        builtins["status"] = a => { if (a.Length > 0) { try { var resp = new HttpClient().SendAsync(new HttpRequestMessage(HttpMethod.Head, a[0])).Result; Console.WriteLine($"  {(int)resp.StatusCode} {resp.ReasonPhrase}"); } catch (Exception ex) { Console.WriteLine($"  Error: {ex.Message}"); } } };
        builtins["httpget"] = a => { if (a.Length > 0) { try { Console.WriteLine(new HttpClient().GetStringAsync(a[0]).Result); } catch (Exception ex) { Console.WriteLine($"  Error: {ex.Message}"); } } };
        builtins["httplen"] = a => { if (a.Length > 0) { try { var resp = new HttpClient().SendAsync(new HttpRequestMessage(HttpMethod.Head, a[0])).Result; Console.WriteLine($"  {resp.Content.Headers.ContentLength ?? 0} bytes"); } catch { Console.WriteLine("  Error"); } } };

        // Git extras
        builtins["git-init"] = _ => RunGit("init");
        builtins["git-clone"] = a => RunGit($"clone {string.Join(" ", a)}");
        builtins["git-tag"] = a => RunGit($"tag {string.Join(" ", a)}");
        builtins["git-tags"] = _ => RunGit("tag -l");
        builtins["git-blame"] = a => RunGit($"blame {string.Join(" ", a)}");
        builtins["git-show"] = a => RunGit($"show {string.Join(" ", a)}");
        builtins["git-stash-list"] = _ => RunGit("stash list");
        builtins["git-stash-push"] = _ => RunGit("stash push");
        builtins["git-stash-pop"] = _ => RunGit("stash pop");
        builtins["git-reset-hard"] = a => RunGit($"reset --hard {string.Join(" ", a)}");
        builtins["git-reset-soft"] = a => RunGit($"reset --soft {string.Join(" ", a)}");
        builtins["git-clean"] = _ => RunGit("clean -fd");
        builtins["git-cherry-pick"] = a => RunGit($"cherry-pick {string.Join(" ", a)}");
        builtins["git-rebase"] = a => RunGit($"rebase {string.Join(" ", a)}");
        builtins["git-merge"] = a => RunGit($"merge {string.Join(" ", a)}");
        builtins["git-pull-rebase"] = _ => RunGit("pull --rebase");
        builtins["git-push-force"] = _ => RunGit("push --force");
        builtins["git-remote"] = _ => RunGit("remote -v");
        builtins["git-add-all"] = _ => RunGit("add -A");
        builtins["git-add-patch"] = _ => RunGit("add -p");
        builtins["git-unstage"] = a => RunGit($"reset HEAD {string.Join(" ", a)}");
        builtins["git-restore"] = a => RunGit($"restore {string.Join(" ", a)}");
        builtins["git-reflog"] = _ => RunGit("reflog");
        builtins["git-archive"] = a => RunGit($"archive HEAD -o {string.Join(" ", a)}");

        // Encoding extras
        builtins["rot13"] = a => Console.WriteLine(string.Concat(string.Join(" ", a).Select(c => char.IsLetter(c) ? (char)((c & ~32) + 13 % 26 | (c & 32)) : c)));
        builtins["caesar"] = a => { if (a.Length >= 2 && int.TryParse(a[1], out var shift)) Console.WriteLine(string.Concat(a[0].Select(c => char.IsLetter(c) ? (char)('A' + (c - 'A' + shift + 26) % 26) : c))); };
        builtins["morse"] = a => { var morse = new Dictionary<char, string> { {'A',".-"}, {'B',"-..."}, {'C',"-.-."}, {'D',"-.."}, {'E',"."}, {'F',"..-."}, {'G',"--."}, {'H',"...."}, {'I',".."}, {'J',".---"}, {'K',"-.-"}, {'L',".-.."}, {'M',"--"}, {'N',"-."}, {'O',"---"}, {'P',".--."}, {'Q',"--.-"}, {'R',".-."}, {'S',"..."}, {'T',"-"}, {'U',"..-"}, {'V',"...-"}, {'W',".--"}, {'X',"-..-"}, {'Y',"-.--"}, {'Z',"--.."} }; foreach (var c in string.Join(" ", a).ToUpper()) Console.Write(morse.TryGetValue(c, out var m) ? m + " " : " "); Console.WriteLine(); };
        builtins["frommorse"] = a => { var morse = new Dictionary<string, char> { {".-",'A'}, {"-...",'B'}, {"-.-.",'C'}, {"-..",'D'}, {".",'E'}, {"..-.",'F'}, {"--.",'G'}, {"....",'H'}, {"..",'I'}, {".---",'J'}, {"-.-",'K'}, {".-..",'L'}, {"--",'M'}, {"-.",'N'}, {"---",'O'}, {".--.",'Q'}, {"--.-",'Q'}, {".-.",'R'}, {"...",'S'}, {"-",'T'}, {"..-",'U'}, {"...-",'V'}, {".--",'W'}, {"-..-",'X'}, {"-.--",'Y'}, {"--..",'Z'} }; foreach (var m in a) Console.Write(morse.TryGetValue(m, out var c) ? c.ToString() : "?"); Console.WriteLine(); };
        builtins["roman"] = a => { if (int.TryParse(a.FirstOrDefault(), out var n)) { var vals = new[] { 1000, 900, 500, 400, 100, 90, 50, 40, 10, 9, 5, 4, 1 }; var syms = new[] { "M", "CM", "D", "CD", "C", "XC", "L", "XL", "X", "IX", "V", "IV", "I" }; var result = ""; for (int i = 0; i < vals.Length; i++) while (n >= vals[i]) { result += syms[i]; n -= vals[i]; } Console.WriteLine($"  {result}"); } };
        builtins["fromroman"] = a => { var roman = new Dictionary<string, int> { {"M",1000},{"CM",900},{"D",500},{"CD",400},{"C",100},{"XC",90},{"L",50},{"XL",40},{"X",10},{"IX",9},{"V",5},{"IV",4},{"I",1} }; var s = string.Join("", a).ToUpper(); var result = 0; while (s.Length > 0) { foreach (var kv in roman.Where(k => s.StartsWith(k.Key))) { result += kv.Value; s = s[kv.Key.Length..]; break; } } Console.WriteLine($"  {result}"); };
        builtins["base"] = a => { if (a.Length >= 2 && int.TryParse(a[0], out var num) && int.TryParse(a[1], out var b)) Console.WriteLine($"  {Convert.ToString(num, b)}"); };
        builtins["frombase"] = a => { if (a.Length >= 2 && int.TryParse(a[1], out var b)) Console.WriteLine($"  {Convert.ToInt32(a[0], b)}"); };
        builtins["tobase"] = a => { if (a.Length >= 2 && int.TryParse(a[0], out var num) && int.TryParse(a[1], out var b)) Console.WriteLine(Convert.ToString(num, b)); };

        // Games / Fun extras
        builtins["rps"] = a => { if (a.Length == 0) { Console.WriteLine("  Usage: rps <rock/paper/scissors>"); return; } var choices = new[] { "rock", "paper", "scissors" }; var bot = choices[new Random().Next(3)]; var player = a[0].ToLower(); Console.WriteLine($"  You: {player}  Bot: {bot}"); if (player == bot) Console.WriteLine("  Draw!"); else if ((player == "rock" && bot == "scissors") || (player == "scissors" && bot == "paper") || (player == "paper" && bot == "rock")) Console.WriteLine("  You win!"); else Console.WriteLine("  Bot wins!"); };
        builtins["rps101"] = a => { Console.WriteLine("  Rock-Paper-Scissors-Lizard-Spock!"); Console.WriteLine("  Use: rps101 <choice>"); };
        builtins["hangman-word"] = _ => { var words = new[] { "apple", "banana", "cherry", "dragon", "elephant", "frog", "guitar", "harbor", "igloo", "jungle" }; Console.WriteLine($"  Word: {words[new Random().Next(words.Length)]}"); };
        builtins["riddle"] = _ => { var riddles = new[] { "What has keys but no locks? A keyboard!", "What has a head and a tail but no body? A coin!", "What gets wetter the more it dries? A towel!", "What can travel around the world while staying in a corner? A stamp!" }; Console.WriteLine($"  {riddles[new Random().Next(riddles.Length)]}"); };
        builtins["magic8"] = _ => { var answers = new[] { "Yes", "No", "Maybe", "Ask again", "Definitely", "No way", "Probably", "Unlikely", "For sure", "Doubtful" }; Console.WriteLine($"  🎱 {answers[new Random().Next(answers.Length)]}"); };
        builtins["roll"] = a => { var count = a.Length > 0 && int.TryParse(a[0], out var c) ? c : 1; var total = 0; var rng = new Random(); var results = new List<int>(); for (int i = 0; i < count; i++) { var r = rng.Next(1, 7); results.Add(r); total += r; } Console.WriteLine($"  [{string.Join(", ", results)}] Total: {total}"); };
        builtins["lottery"] = _ => { var rng = new Random(); var nums = Enumerable.Range(1, 49).OrderBy(_ => rng.Next()).Take(6).OrderBy(x => x).ToList(); Console.WriteLine($"  🎰 {string.Join(" ", nums)}"); };
        builtins["flip"] = _ => Console.WriteLine($"  {(new Random().Next(2) == 0 ? "Heads" : "Tails")}");
        builtins["spinner"] = _ => { var frames = new[] { "|", "/", "-", "\\" }; for (int i = 0; i < 20; i++) { Console.Write($"\r  {frames[i % 4]}"); Thread.Sleep(100); } Console.WriteLine("\r  ✓ Done!"); };
        builtins["matrix-rain"] = _ => { var rng = new Random(); var width = Console.WindowWidth; Console.ForegroundColor = ConsoleColor.Green; for (int frame = 0; frame < 30; frame++) { Console.Clear(); for (int y = 0; y < Console.WindowHeight; y++) { for (int x = 0; x < width; x++) { if (rng.Next(100) < 5) { Console.ForegroundColor = rng.Next(3) == 0 ? ConsoleColor.White : ConsoleColor.Green; Console.Write((char)rng.Next(33, 127)); } else Console.Write(' '); } } Thread.Sleep(50); } Console.ResetColor(); Console.Clear(); };
        builtins["colorbar"] = _ => { foreach (ConsoleColor c in Enum.GetValues(typeof(ConsoleColor))) { Console.BackgroundColor = c; Console.Write(new string(' ', 8)); } Console.ResetColor(); Console.WriteLine(); };
        builtins["starfield"] = _ => { var rng = new Random(); for (int i = 0; i < 50; i++) Console.WriteLine(new string(' ', rng.Next(Console.WindowWidth - 1)) + "*"); };
        builtins["box"] = a => { var text = a.Length > 0 ? string.Join(" ", a) : "Hello!"; var w = text.Length + 4; Console.WriteLine("╔" + new string('═', w) + "╗"); Console.WriteLine("║  " + text + "  ║"); Console.WriteLine("╚" + new string('═', w) + "╝"); };
        builtins["doublebox"] = a => { var text = a.Length > 0 ? string.Join(" ", a) : "Hello!"; var w = text.Length + 4; Console.WriteLine("╔═" + new string('═', w) + "═╗"); Console.WriteLine("║  " + text + "  ║"); Console.WriteLine("╚═" + new string('═', w) + "═╝"); };
        builtins["bubble"] = a => { var text = a.Length > 0 ? string.Join(" ", a) : "..."; Console.WriteLine("    ╭───╮"); Console.WriteLine($"   │ {text} │"); Console.WriteLine("    ╰───╯"); Console.WriteLine("       \\"); Console.WriteLine("        (°)"); };
        builtins["progress"] = a => { if (a.Length >= 2 && int.TryParse(a[0], out var pct) && int.TryParse(a[1], out var w)) { var filled = pct * w / 100; Console.Write("  ["); Console.ForegroundColor = ConsoleColor.Green; Console.Write(new string('█', filled)); Console.ForegroundColor = ConsoleColor.DarkGray; Console.Write(new string('░', w - filled)); Console.ResetColor(); Console.WriteLine($"] {pct}%"); } };
        builtins["loading"] = _ => { for (int i = 0; i <= 100; i += 5) { Console.Write($"\r  Loading... {i}%"); Thread.Sleep(100); } Console.WriteLine("\r  Done!            "); };
        builtins["typewriter"] = a => { foreach (var c in string.Join(" ", a)) { Console.Write(c); Thread.Sleep(50); } Console.WriteLine(); };

        // Dev extras
        builtins["timestamp"] = _ => Console.WriteLine($"  {DateTimeOffset.UtcNow.ToUnixTimeSeconds()}");
        builtins["randomcolor"] = _ => { var rng = new Random(); Console.ForegroundColor = (ConsoleColor)rng.Next(16); Console.WriteLine($"  Color: {Console.ForegroundColor}"); Console.ResetColor(); };
        builtins["randomstring"] = a => { var len = a.Length > 0 && int.TryParse(a[0], out var l) ? l : 8; var rng = new Random(); Console.WriteLine(new string(Enumerable.Range(0, len).Select(_ => (char)rng.Next('a', 'z' + 1)).ToArray())); };
        builtins["randomhex"] = a => { var len = a.Length > 0 && int.TryParse(a[0], out var l) ? l : 8; Console.WriteLine(BitConverter.ToString(RandomNumberGenerator.GetBytes(len / 2 + 1))[..len].ToLower()); };
        builtins["password"] = a => { var len = a.Length > 0 && int.TryParse(a[0], out var l) ? l : 16; const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*"; var rng = new Random(); Console.WriteLine(new string(Enumerable.Range(0, len).Select(_ => chars[rng.Next(chars.Length)]).ToArray())); };
        builtins["lorem"] = a => { var count = a.Length > 0 && int.TryParse(a[0], out var c) ? c : 50; var words = new[] { "lorem", "ipsum", "dolor", "sit", "amet", "consectetur", "adipiscing", "elit", "sed", "do", "eiusmod", "tempor", "incididunt", "ut", "labore", "et", "dolore", "magna", "aliqua" }; var rng = new Random(); Console.WriteLine(string.Join(" ", Enumerable.Range(0, count).Select(_ => words[rng.Next(words.Length)]))); };
        builtins["colorize"] = a => { if (a.Length >= 2) { var colorName = a[0]; var text = string.Join(" ", a[1..]); if (Enum.TryParse<ConsoleColor>(colorName, true, out var color)) { Console.ForegroundColor = color; Console.WriteLine(text); Console.ResetColor(); } else Console.WriteLine(text); } };
        builtins["underline"] = a => Console.WriteLine($"\x1b[4m{string.Join(" ", a)}\x1b[0m");
        builtins["bold"] = a => Console.WriteLine($"\x1b[1m{string.Join(" ", a)}\x1b[0m");
        builtins["dim"] = a => Console.WriteLine($"\x1b[2m{string.Join(" ", a)}\x1b[0m");
        builtins["italic"] = a => Console.WriteLine($"\x1b[3m{string.Join(" ", a)}\x1b[0m");
        builtins["strikethrough"] = a => Console.WriteLine($"\x1b[9m{string.Join(" ", a)}\x1b[0m");
        builtins["blink"] = a => Console.WriteLine($"\x1b[5m{string.Join(" ", a)}\x1b[0m");
        builtins["inverse"] = a => Console.WriteLine($"\x1b[7m{string.Join(" ", a)}\x1b[0m");

        // Misc extras
        builtins["qr"] = a => { if (a.Length > 0) Console.WriteLine($"  QR: https://api.qrserver.com/v1/create-qr-code/?size=200x200&data={Uri.EscapeDataString(a[0])}"); };
        builtins["shorten"] = a => { if (a.Length > 0) Console.WriteLine($"  {a[0][..Math.Min(40, a[0].Length)]}..."); };
        builtins["wordcount"] = a => Console.WriteLine($"  {a.Length} words");
        builtins["sentencecount"] = a => Console.WriteLine($"  {string.Join(" ", a).Split('.', '?', '!').Length} sentences");
        builtins["charfreq"] = a => { foreach (var c in string.Join(" ", a).GroupBy(x => x).OrderByDescending(x => x.Key).Take(10)) Console.WriteLine($"  '{c.Key}': {c.Count()}"); };
        builtins["uniquewords"] = a => Console.WriteLine($"  {string.Join(" ", a).Split(' ', StringSplitOptions.RemoveEmptyEntries).Distinct().Count()} unique words");
        builtins["mostcommon"] = a => { foreach (var w in string.Join(" ", a).Split(' ', StringSplitOptions.RemoveEmptyEntries).GroupBy(x => x.ToLower()).OrderByDescending(x => x.Count()).Take(10)) Console.WriteLine($"  {w.Key}: {w.Count()}"); };
        builtins["leastcommon"] = a => { foreach (var w in string.Join(" ", a).Split(' ', StringSplitOptions.RemoveEmptyEntries).GroupBy(x => x.ToLower()).OrderBy(x => x.Count()).Take(10)) Console.WriteLine($"  {w.Key}: {w.Count()}"); };
        builtins["zalgo"] = a => { var zalgoUp = new[] { "\u0300", "\u0301", "\u0302", "\u0303", "\u0304", "\u0305", "\u0306", "\u0307", "\u0308", "\u0309", "\u030A" }; var rng = new Random(); foreach (var c in string.Join(" ", a)) { Console.Write(c); for (int i = 0; i < 5; i++) Console.Write(zalgoUp[rng.Next(zalgoUp.Length)]); } Console.WriteLine(); };
        builtins["vaporwave"] = a => Console.WriteLine(string.Join(" ", string.Join(" ", a).Select(c => (int)c > 127 ? c.ToString() : ((char)(c + 61440)).ToString())));
        builtins["mock"] = a => Console.WriteLine(string.Concat(string.Join(" ", a).Select((c, i) => i % 2 == 0 ? char.ToUpper(c) : char.ToLower(c))));
        builtins["binary-text"] = a => { foreach (var c in string.Join(" ", a)) Console.Write($"{Convert.ToString(c, 2).PadLeft(8, '0')} "); Console.WriteLine(); };
        builtins["from-binary"] = a => { Console.WriteLine(string.Concat(a.Select(b => (char)Convert.ToInt32(b, 2)))); };
        builtins["justify"] = a => { if (a.Length >= 2 && int.TryParse(a[1], out var w)) Console.WriteLine(string.Join(" ", a[0]).PadLeft(w)); };
        builtins["center-text"] = a => { if (a.Length >= 2 && int.TryParse(a[1], out var w)) { var t = a[0]; Console.WriteLine(t.PadLeft((w + t.Length) / 2).PadRight(w)); } };
        builtins["leftpad"] = a => { if (a.Length >= 2 && int.TryParse(a[1], out var w)) Console.WriteLine(a[0].PadLeft(w, a.Length > 2 ? a[2][0] : ' ')); };
        builtins["rightpad"] = a => { if (a.Length >= 2 && int.TryParse(a[1], out var w)) Console.WriteLine(a[0].PadRight(w, a.Length > 2 ? a[2][0] : ' ')); };
        builtins["spacer"] = a => Console.WriteLine(string.Join(a.Length > 0 ? a[0] : " ", string.Join(" ", a).ToCharArray()));
        builtins["boxdraw"] = a => { var w = 40; Console.WriteLine("┌" + new string('─', w) + "┐"); foreach (var line in a.Length > 0 ? a : new[] { " " }) Console.WriteLine("│ " + line.PadRight(w - 1) + "│"); Console.WriteLine("└" + new string('─', w) + "┘"); };
        builtins["checklist"] = a => { for (int i = 0; i < a.Length; i++) Console.WriteLine($"  ☐ {a[i]}"); };
        builtins["done-list"] = a => { for (int i = 0; i < a.Length; i++) Console.WriteLine($"  ☑ {a[i]}"); };
        builtins["numberlist"] = a => { for (int i = 0; i < a.Length; i++) Console.WriteLine($"  {i + 1}. {a[i]}"); };
        builtins["bulletlist"] = a => { foreach (var item in a) Console.WriteLine($"  • {item}"); };
        builtins["arrowlist"] = a => { foreach (var item in a) Console.WriteLine($"  → {item}"); };
        builtins["starlist"] = a => { foreach (var item in a) Console.WriteLine($"  ★ {item}"); };
        builtins["heartlist"] = a => { foreach (var item in a) Console.WriteLine($"  ♥ {item}"); };
        builtins["diamondlist"] = a => { foreach (var item in a) Console.WriteLine($"  ◆ {item}"); };
        builtins["flowerlist"] = a => { foreach (var item in a) Console.WriteLine($"  ✿ {item}"); };
        builtins["sunlist"] = a => { foreach (var item in a) Console.WriteLine($"  ☀ {item}"); };
        builtins["moonlist"] = a => { foreach (var item in a) Console.WriteLine($"  ☾ {item}"); };
    }

    static void RunGit(string args)
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo("git", args)
            {
                WorkingDirectory = Directory.GetCurrentDirectory(),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            using var proc = System.Diagnostics.Process.Start(psi);
            if (proc != null)
            {
                Console.Write(proc.StandardOutput.ReadToEnd());
                var err = proc.StandardError.ReadToEnd();
                if (!string.IsNullOrEmpty(err)) { Console.ForegroundColor = ConsoleColor.Red; Console.Write(err); Console.ResetColor(); }
                proc.WaitForExit();
            }
        }
        catch (Exception ex) { Console.WriteLine($"  Error: {ex.Message}"); }
    }

    static void CopyDir(string src, string dst)
    {
        Directory.CreateDirectory(dst);
        foreach (var file in Directory.GetFiles(src)) File.Copy(file, Path.Combine(dst, Path.GetFileName(file)), true);
        foreach (var dir in Directory.GetDirectories(src)) CopyDir(dir, Path.Combine(dst, Path.GetFileName(dir)));
    }

    static long GCD(long a, long b) { while (b != 0) { (a, b) = (b, a % b); } return a; }

    static string ToTitleCase(this string s) =>
        System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(s.ToLower());
}
