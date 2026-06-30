using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace CustomWinConsole;

static class CommandsExtra2
{
    public static void Register(Dictionary<string, Action<string[]>> builtins)
    {
        // Extra text
        builtins["charcount"] = a => Console.WriteLine($"  {string.Join(" ", a).Length} chars");
        builtins["wordcount"] = a => Console.WriteLine($"  {a.Length} words");
        builtins["linecount"] = a => Console.WriteLine($"  {string.Join(" ", a).Count(c => c == '\n') + 1} lines");
        builtins["bytecount"] = a => Console.WriteLine($"  {System.Text.Encoding.UTF8.GetByteCount(string.Join(" ", a))} bytes");
        builtins["uppercase"] = a => Console.WriteLine(string.Join(" ", a).ToUpper());
        builtins["lowercase"] = a => Console.WriteLine(string.Join(" ", a).ToLower());
        builtins["camelcase"] = a => { var s = string.Join(" ", a); Console.WriteLine(char.ToLower(s[0]) + s[1..]); };
        builtins["pascalcase"] = a => Console.WriteLine(string.Concat(string.Join(" ", a).Split(' ').Select(w => char.ToUpper(w[0]) + w[1..])));
        builtins["kebabcase"] = a => Console.WriteLine(string.Join("-", a.Select(w => w.ToLower())));
        builtins["snakecase"] = a => Console.WriteLine(string.Join("_", a.Select(w => w.ToLower())));
        builtins["titlecase2"] = a => Console.WriteLine(string.Join(" ", a.Select(w => char.ToUpper(w[0]) + w[1..])));
        builtins["sentencecase"] = a => { var s = string.Join(" ", a).ToLower(); Console.WriteLine(char.ToUpper(s[0]) + s[1..]); };
        builtins["alternating"] = a => Console.WriteLine(string.Concat(string.Join(" ", a).Select((c, i) => i % 2 == 0 ? char.ToUpper(c) : char.ToLower(c))));
        builtins["strrepeat"] = a => { if (a.Length >= 2 && int.TryParse(a[1], out var n)) Console.WriteLine(string.Concat(Enumerable.Repeat(a[0], n))); };
        builtins["strreverse"] = a => Console.WriteLine(new string(string.Join(" ", a).Reverse().ToArray()));
        builtins["palindrome"] = a => { var s = string.Join(" ", a).ToLower().Replace(" ", ""); Console.WriteLine(s == new string(s.Reverse().ToArray()) ? "  Palindrome!" : "  Not palindrome"); };
        builtins["ispalindrome"] = a => { var s = a.FirstOrDefault()?.ToLower().Replace(" ", "") ?? ""; Console.WriteLine(s == new string(s.Reverse().ToArray()) ? "  true" : "  false"); };
        builtins["anagram"] = a => { if (a.Length >= 2) { var s1 = string.Join("", a[0].ToLower().OrderBy(c => c)); var s2 = string.Join("", a[1].ToLower().OrderBy(c => c)); Console.WriteLine(s1 == s2 ? "  Anagrams!" : "  Not anagrams"); } };
        builtins["levenshtein"] = a => { if (a.Length >= 2) { var s = a[0]; var t = a[1]; var d = new int[s.Length + 1, t.Length + 1]; for (int i = 0; i <= s.Length; i++) d[i, 0] = i; for (int j = 0; j <= t.Length; j++) d[0, j] = j; for (int i = 1; i <= s.Length; i++) for (int j = 1; j <= t.Length; j++) d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + (s[i - 1] == t[j - 1] ? 0 : 1)); Console.WriteLine($"  Distance: {d[s.Length, t.Length]}"); } };
        builtins["cosine"] = a => { if (a.Length >= 2) { var v1 = a[0].ToCharArray(); var v2 = a[1].ToCharArray(); Console.WriteLine($"  Similarity: {v1.Intersect(v2).Count() / (double)Math.Max(v1.Length, 1):F2}"); } };
        builtins["jaccard"] = a => { if (a.Length >= 2) { var s1 = new HashSet<char>(a[0]); var s2 = new HashSet<char>(a[1]); Console.WriteLine($"  Jaccard: {(double)s1.Intersect(s2).Count() / s1.Union(s2).Count():F2}"); } };
        builtins["similarity"] = a => { if (a.Length >= 2) { var s1 = a[0]; var s2 = a[1]; var common = s1.Intersect(s2).Count(); Console.WriteLine($"  Similarity: {common * 200.0 / (s1.Length + s2.Length):F1}%"); } };

        // More math
        builtins["fibonacci"] = a => { if (int.TryParse(a.FirstOrDefault(), out var n)) { long a2 = 0, b = 1; for (int i = 0; i < n; i++) { Console.Write($"{a2} "); (a2, b) = (b, a2 + b); } Console.WriteLine(); } };
        builtins["primes"] = a => { if (int.TryParse(a.FirstOrDefault(), out var n)) { var primes = new List<int>(); for (int i = 2; primes.Count < n; i++) { if (primes.All(p => i % p != 0)) primes.Add(i); } Console.WriteLine($"  {string.Join(", ", primes)}"); } };
        builtins["collatz"] = a => { if (int.TryParse(a.FirstOrDefault(), out var n)) { while (n != 1) { Console.Write($"{n} "); n = n % 2 == 0 ? n / 2 : 3 * n + 1; } Console.WriteLine("1"); } };
        builtins["digitsum"] = a => Console.WriteLine($"  {a.FirstOrDefault()?.Where(char.IsDigit).Sum(c => c - '0') ?? 0}");
        builtins["digitproduct"] = a => { var digits = a.FirstOrDefault()?.Where(char.IsDigit).Select(c => c - '0') ?? Enumerable.Empty<int>(); Console.WriteLine($"  {digits.Aggregate(1, (acc, x) => acc * x)}"); };
        builtins["triangular"] = a => { if (int.TryParse(a.FirstOrDefault(), out var n)) Console.WriteLine($"  {n * (n + 1) / 2}"); };
        builtins["perfect"] = a => { if (int.TryParse(a.FirstOrDefault(), out var n)) { var sum = Enumerable.Range(1, n / 2).Where(i => n % i == 0).Sum(); Console.WriteLine(sum == n ? "  Perfect number!" : $"  Sum of divisors: {sum}"); } };
        builtins["amicable"] = a => { if (int.TryParse(a.FirstOrDefault(), out var n)) { var sum1 = Enumerable.Range(1, n / 2).Where(i => n % i == 0).Sum(); var sum2 = Enumerable.Range(1, sum1 / 2).Where(i => sum1 % i == 0).Sum(); Console.WriteLine(sum2 == n ? $"  Amicable pair: ({n}, {sum1})" : $"  Not amicable"); } };
        builtins["harshad"] = a => { if (int.TryParse(a.FirstOrDefault(), out var n)) { var digitSum = n.ToString().Sum(c => c - '0'); Console.WriteLine(n % digitSum == 0 ? $"  Harshad number (div by {digitSum})" : "  Not Harshad"); } };
        builtins["kaprekar"] = a => { if (int.TryParse(a.FirstOrDefault(), out var n)) { var sq = (n * n).ToString(); Console.WriteLine($"  {n}² = {sq}"); } };
        builtins["automorphic"] = a => { if (int.TryParse(a.FirstOrDefault(), out var n)) { var sq = n * n; Console.WriteLine(sq.ToString().EndsWith(n.ToString()) ? "  Automorphic!" : "  Not automorphic"); } };
        builtins["armstrong"] = a => { if (int.TryParse(a.FirstOrDefault(), out var n)) { var digits = n.ToString().Select(c => c - '0').ToArray(); var sum = digits.Select(d => (int)Math.Pow(d, digits.Length)).Sum(); Console.WriteLine(sum == n ? "  Armstrong number!" : $"  Sum: {sum}"); } };
        builtins["happy"] = a => { if (int.TryParse(a.FirstOrDefault(), out var n)) { var seen = new HashSet<int>(); while (n != 1 && seen.Add(n)) n = n.ToString().Sum(c => (c - '0') * (c - '0')); Console.WriteLine(n == 1 ? "  Happy number!" : "  Not happy"); } };
        builtins["sad"] = a => { if (int.TryParse(a.FirstOrDefault(), out var n)) { var seen = new HashSet<int>(); while (n != 1 && seen.Add(n)) n = n.ToString().Sum(c => (c - '0') * (c - '0')); Console.WriteLine(n != 1 ? "  Sad number!" : "  Not sad"); } };
        builtins["fizzbuzz"] = a => { if (int.TryParse(a.FirstOrDefault(), out var n)) { for (int i = 1; i <= n; i++) { if (i % 15 == 0) Console.Write("FizzBuzz "); else if (i % 3 == 0) Console.Write("Fizz "); else if (i % 5 == 0) Console.Write("Buzz "); else Console.Write($"{i} "); } Console.WriteLine(); } };
        builtins["table"] = a => { if (int.TryParse(a.FirstOrDefault(), out var n)) { for (int i = 1; i <= 10; i++) Console.WriteLine($"  {n} x {i} = {n * i}"); } };
        builtins["multiples"] = a => { if (a.Length >= 2 && int.TryParse(a[0], out var n) && int.TryParse(a[1], out var count)) { for (int i = 1; i <= count; i++) Console.Write($"{n * i} "); Console.WriteLine(); } };
        builtins["divisors"] = a => { if (int.TryParse(a.FirstOrDefault(), out var n)) { var divs = Enumerable.Range(1, n).Where(i => n % i == 0).ToList(); Console.WriteLine($"  {string.Join(", ", divs)} ({divs.Count} divisors)"); } };
        builtins["primefactors"] = a => { if (int.TryParse(a.FirstOrDefault(), out var n)) { var factors = new List<int>(); for (int i = 2; i * i <= n; i++) { while (n % i == 0) { factors.Add(i); n /= i; } } if (n > 1) factors.Add(n); Console.WriteLine($"  {string.Join(" × ", factors)}"); } };

        // More conversions
        builtins["money"] = a => { if (a.Length >= 2 && double.TryParse(a[0], out var amt) && double.TryParse(a[1], out var rate)) Console.WriteLine($"  {(amt * rate):C2}"); };
        builtins["timeconv"] = a => { if (a.Length >= 2 && double.TryParse(a[0], out var val)) { var from = a[1].ToLower(); if (from == "h" || from == "hours" || from == "hrs") Console.WriteLine($"  {val * 60} min, {val * 3600} sec"); else if (from == "m" || from == "min") Console.WriteLine($"  {val / 60:F2} hrs, {val * 60} sec"); else if (from == "s" || from == "sec") Console.WriteLine($"  {val / 3600:F4} hrs, {val / 60:F2} min"); } };
        builtins["radix"] = a => { if (a.Length >= 2 && int.TryParse(a[0], out var n) && int.TryParse(a[1], out var r)) Console.WriteLine($"  Base {r}: {Convert.ToString(n, r)}"); };
        builtins["fromradix"] = a => { if (a.Length >= 2 && int.TryParse(a[1], out var r)) Console.WriteLine($"  {Convert.ToInt32(a[0], r)}"); };

        // More system
        builtins["envcount"] = _ => Console.WriteLine($"  {Environment.GetEnvironmentVariables().Count} variables");
        builtins["envget"] = a => { if (a.Length > 0) { var v = Environment.GetEnvironmentVariable(a[0]); Console.WriteLine(v ?? $"  {a[0]} not set"); } };
        builtins["envset"] = a => { if (a.Length >= 2) { Environment.SetEnvironmentVariable(a[0], a[1]); Console.WriteLine($"  Set {a[0]}={a[1]}"); } };
        builtins["envdel"] = a => { if (a.Length > 0) { Environment.SetEnvironmentVariable(a[0], null); Console.WriteLine($"  Deleted {a[0]}"); } };
        builtins["envsearch"] = a => { if (a.Length > 0) { foreach (System.Collections.DictionaryEntry kv in Environment.GetEnvironmentVariables()) { if (kv.Key?.ToString()?.Contains(a[0], StringComparison.OrdinalIgnoreCase) == true) Console.WriteLine($"  {kv.Key}={kv.Value}"); } } };
        builtins["threadcount"] = _ => Console.WriteLine($"  {System.Diagnostics.Process.GetCurrentProcess().Threads.Count} threads");
        builtins["gcinfo"] = _ => { for (int i = 0; i <= GC.MaxGeneration; i++) Console.WriteLine($"  Gen {i}: {GC.CollectionCount(i)} collections"); };
        builtins["gcmemory"] = _ => { GC.Collect(); Console.WriteLine($"  Total: {GC.GetTotalMemory(true) / 1024 / 1024} MB"); };
        builtins["processinfo"] = _ => { var p = Process.GetCurrentProcess(); Console.WriteLine($"  PID: {p.Id}"); Console.WriteLine($"  Threads: {p.Threads.Count}"); Console.WriteLine($"  Memory: {p.WorkingSet64 / 1024 / 1024} MB"); Console.WriteLine($"  CPU: {p.TotalProcessorTime}"); };
        builtins["handlecount"] = _ => Console.WriteLine($"  Handles: {Process.GetCurrentProcess().HandleCount}");
        builtins["gccount"] = _ => Console.WriteLine($"  GC: {GC.CollectionCount(0)} Gen0, {GC.CollectionCount(1)} Gen1, {GC.CollectionCount(2)} Gen2");
        builtins["meminfo"] = _ => { Console.WriteLine($"  Working Set: {Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024} MB"); Console.WriteLine($"  Private Memory: {Process.GetCurrentProcess().PrivateMemorySize64 / 1024 / 1024} MB"); Console.WriteLine($"  Virtual Memory: {Process.GetCurrentProcess().VirtualMemorySize64 / 1024 / 1024} MB"); };
        builtins["starttime"] = _ => Console.WriteLine($"  {Process.GetCurrentProcess().StartTime:yyyy-MM-dd HH:mm:ss}");
        builtins["elapsed"] = _ => Console.WriteLine($"  {Process.GetCurrentProcess().TotalProcessorTime}");
        builtins["id"] = _ => Console.WriteLine($"  PID: {Process.GetCurrentProcess().Id}");
        builtins["parentpid"] = _ => Console.WriteLine("  N/A (not supported on .NET)");

        // More file
        builtins["typefile"] = a => { if (a.Length > 0) { var p = Path.Combine(Directory.GetCurrentDirectory(), a[0]); if (File.Exists(p)) Console.Write(File.ReadAllText(p)); else Console.WriteLine($"  Not found: {a[0]}"); } };
        builtins["writeline"] = a => { if (a.Length >= 2) { var p = Path.Combine(Directory.GetCurrentDirectory(), a[0]); File.AppendAllText(p, string.Join(" ", a[1..]) + Environment.NewLine); Console.WriteLine($"  Written to: {a[0]}"); } };
        builtins["readline2"] = a => { if (a.Length >= 2 && int.TryParse(a[1], out var ln)) { var p = Path.Combine(Directory.GetCurrentDirectory(), a[0]); if (File.Exists(p)) { var lines = File.ReadAllLines(p); if (ln > 0 && ln <= lines.Length) Console.WriteLine(lines[ln - 1]); else Console.WriteLine("  Line out of range"); } } };
        builtins["countlines"] = a => { if (a.Length > 0) { var p = Path.Combine(Directory.GetCurrentDirectory(), a[0]); if (File.Exists(p)) Console.WriteLine($"  {File.ReadAllLines(p).Length} lines"); } };
        builtins["countwords"] = a => { if (a.Length > 0) { var p = Path.Combine(Directory.GetCurrentDirectory(), a[0]); if (File.Exists(p)) Console.WriteLine($"  {File.ReadAllText(p).Split(' ', StringSplitOptions.RemoveEmptyEntries).Length} words"); } };
        builtins["filehash"] = a => { if (a.Length > 0) { var p = Path.Combine(Directory.GetCurrentDirectory(), a[0]); if (File.Exists(p)) { var h = System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(p)); Console.WriteLine($"  SHA256: {Convert.ToHexString(h).ToLower()}"); } } };
        builtins["md5file"] = a => { if (a.Length > 0) { var p = Path.Combine(Directory.GetCurrentDirectory(), a[0]); if (File.Exists(p)) { var h = System.Security.Cryptography.MD5.HashData(File.ReadAllBytes(p)); Console.WriteLine($"  MD5: {Convert.ToHexString(h).ToLower()}"); } } };
        builtins["sha1file"] = a => { if (a.Length > 0) { var p = Path.Combine(Directory.GetCurrentDirectory(), a[0]); if (File.Exists(p)) { var h = System.Security.Cryptography.SHA1.HashData(File.ReadAllBytes(p)); Console.WriteLine($"  SHA1: {Convert.ToHexString(h).ToLower()}"); } } };
        builtins["touchdir"] = a => { if (a.Length > 0) { var p = Path.Combine(Directory.GetCurrentDirectory(), a[0]); Directory.CreateDirectory(p); Console.WriteLine($"  Created dir: {a[0]}"); } };
        builtins["listext"] = a => { var p = a.Length > 0 ? Path.Combine(Directory.GetCurrentDirectory(), a[0]) : Directory.GetCurrentDirectory(); var exts = Directory.GetFiles(p).GroupBy(f => Path.GetExtension(f).ToLower()).OrderByDescending(g => g.Count()); foreach (var g in exts) Console.WriteLine($"  {g.Key ?? "none",-10} {g.Count()}"); };
        builtins["dirsize"] = a => { if (a.Length > 0) { var p = Path.Combine(Directory.GetCurrentDirectory(), a[0]); if (Directory.Exists(p)) { var s = Directory.GetFiles(p, "*", new EnumerationOptions { RecurseSubdirectories = true }).Sum(f => new FileInfo(f).Length); Console.WriteLine($"  {s / 1024.0 / 1024.0:F2} MB"); } } };
        builtins["findext"] = a => { if (a.Length >= 2) { var p = Path.Combine(Directory.GetCurrentDirectory(), a[0]); if (Directory.Exists(p)) { var files = Directory.GetFiles(p, $"*{a[1]}", new EnumerationOptions { RecurseSubdirectories = true }); foreach (var f in files) Console.WriteLine($"  {f[(p.Length + 1)..]}"); } } };
        builtins["findsize"] = a => { if (a.Length >= 3 && long.TryParse(a[1], out var min) && long.TryParse(a[2], out var max)) { var p = Path.Combine(Directory.GetCurrentDirectory(), a[0]); if (Directory.Exists(p)) { var files = Directory.GetFiles(p, "*", new EnumerationOptions { RecurseSubdirectories = true }).Select(f => new FileInfo(f)).Where(f => f.Length >= min && f.Length <= max).OrderByDescending(f => f.Length); foreach (var f in files) Console.WriteLine($"  {f.Length,10} {f.Name}"); } } };
        builtins["findname"] = a => { if (a.Length >= 2) { var p = Path.Combine(Directory.GetCurrentDirectory(), a[0]); if (Directory.Exists(p)) { var files = Directory.GetFiles(p, $"*{a[1]}*", new EnumerationOptions { RecurseSubdirectories = true }); foreach (var f in files) Console.WriteLine($"  {f[(p.Length + 1)..]}"); } } };
        builtins["finddate"] = a => { if (a.Length >= 2 && DateTime.TryParse(a[1], out var d)) { var p = Path.Combine(Directory.GetCurrentDirectory(), a[0]); if (Directory.Exists(p)) { var files = Directory.GetFiles(p, "*", new EnumerationOptions { RecurseSubdirectories = true }).Select(f => new FileInfo(f)).Where(f => f.LastWriteTime >= d).OrderByDescending(f => f.LastWriteTime); foreach (var f in files) Console.WriteLine($"  {f.LastWriteTime:yyyy-MM-dd} {f.Name}"); } } };

        // More network
        builtins["fetch"] = a => { if (a.Length > 0) { try { Console.WriteLine(new HttpClient().GetStringAsync(a[0]).Result); } catch (Exception ex) { Console.WriteLine($"  Error: {ex.Message}"); } } };
        builtins["post"] = a => { if (a.Length >= 2) { try { var resp = new HttpClient().PostAsync(a[0], new StringContent(a.Length > 2 ? a[2] : "", System.Text.Encoding.UTF8, "application/json")).Result; Console.WriteLine(resp.Content.ReadAsStringAsync().Result); } catch (Exception ex) { Console.WriteLine($"  Error: {ex.Message}"); } } };
        builtins["headers2"] = a => { if (a.Length > 0) { try { var resp = new HttpClient().GetAsync(a[0]).Result; Console.WriteLine($"  Status: {(int)resp.StatusCode} {resp.ReasonPhrase}"); foreach (var h in resp.Headers) Console.WriteLine($"  {h.Key}: {string.Join(", ", h.Value)}"); } catch { Console.WriteLine("  Error"); } } };
        builtins["jsonapi"] = a => { if (a.Length > 0) { try { var resp = new HttpClient().GetStringAsync(a[0]).Result; Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(System.Text.Json.JsonDocument.Parse(resp).RootElement, new System.Text.Json.JsonSerializerOptions { WriteIndented = true })); } catch (Exception ex) { Console.WriteLine($"  Error: {ex.Message}"); } } };

        // More dev
        builtins["base64file"] = a => { if (a.Length > 0) { var p = Path.Combine(Directory.GetCurrentDirectory(), a[0]); if (File.Exists(p)) Console.WriteLine(Convert.ToBase64String(File.ReadAllBytes(p))); } };
        builtins["frombase64file"] = a => { if (a.Length > 0) { var p = Path.Combine(Directory.GetCurrentDirectory(), a[0]); if (File.Exists(p)) Console.WriteLine(Convert.FromBase64String(File.ReadAllText(p))); } };
        builtins["hexfile"] = a => { if (a.Length > 0) { var p = Path.Combine(Directory.GetCurrentDirectory(), a[0]); if (File.Exists(p)) Console.WriteLine(BitConverter.ToString(File.ReadAllBytes(p)).Replace("-", " ")); } };
        builtins["binaryfile"] = a => { if (a.Length > 0) { var p = Path.Combine(Directory.GetCurrentDirectory(), a[0]); if (File.Exists(p)) { foreach (var b in File.ReadAllBytes(p)) Console.Write(Convert.ToString(b, 2).PadLeft(8, '0') + " "); Console.WriteLine(); } } };
        builtins["linesize"] = a => { if (a.Length > 0) { var p = Path.Combine(Directory.GetCurrentDirectory(), a[0]); if (File.Exists(p)) { var lines = File.ReadAllLines(p); for (int i = 0; i < lines.Length; i++) Console.WriteLine($"  {i + 1,4} {lines[i].Length,5} {lines[i]}"); } } };

        // Git more
        builtins["ginit"] = _ => RunGit("init");
        builtins["gcl"] = a => RunGit($"clone {string.Join(" ", a)}");
        builtins["gtag"] = a => RunGit($"tag {string.Join(" ", a)}");
        builtins["gtags"] = _ => RunGit("tag -l");
        builtins["gblame"] = a => RunGit($"blame {string.Join(" ", a)}");
        builtins["gshow"] = a => RunGit($"show {string.Join(" ", a)}");
        builtins["greset"] = a => RunGit($"reset --hard {string.Join(" ", a)}");
        builtins["gclean"] = _ => RunGit("clean -fd");
        builtins["gmerge"] = a => RunGit($"merge {string.Join(" ", a)}");
        builtins["grebase"] = a => RunGit($"rebase {string.Join(" ", a)}");
        builtins["gcherry"] = a => RunGit($"cherry-pick {string.Join(" ", a)}");
        builtins["gremote"] = _ => RunGit("remote -v");
        builtins["greflog"] = _ => RunGit("reflog");
        builtins["gunstage"] = a => RunGit($"reset HEAD {string.Join(" ", a)}");
        builtins["grestore"] = a => RunGit($"restore {string.Join(" ", a)}");
        builtins["gpushf"] = _ => RunGit("push --force");
        builtins["gpullr"] = _ => RunGit("pull --rebase");
        builtins["gshort"] = _ => RunGit("log --oneline -10");
        builtins["glong"] = _ => RunGit("log --format='%h %s %an' -10");
        builtins["gauthor"] = _ => RunGit("shortlog -sn");
        builtins["gcount"] = _ => RunGit("rev-list --count HEAD");
        builtins["gls-files"] = _ => RunGit("ls-files");
        builtins["gls-tree"] = _ => RunGit("ls-tree -r HEAD --name-only");

        // Extra encoding
        builtins["binary"] = a => { foreach (var c in string.Join(" ", a)) Console.Write(Convert.ToString(c, 2).PadLeft(8, '0') + " "); Console.WriteLine(); };
        builtins["frombinary"] = a => { Console.WriteLine(string.Concat(a.Select(b => (char)Convert.ToInt32(b, 2)))); };
        builtins["octal"] = a => { if (int.TryParse(a.FirstOrDefault(), out var n)) Console.WriteLine($"  0{Convert.ToString(n, 8)}"); };
        builtins["fromoctal"] = a => { if (a.Length > 0 && int.TryParse(a[0], out var n)) Console.WriteLine($"  {Convert.ToInt32(n.ToString(), 8)}"); };
        builtins["scientific"] = a => { if (double.TryParse(a.FirstOrDefault(), out var n)) Console.WriteLine($"  {n:E6}"); };
        builtins["fixed"] = a => { if (double.TryParse(a.FirstOrDefault(), out var n)) Console.WriteLine($"  {n:F4}"); };
        builtins["currency"] = a => { if (double.TryParse(a.FirstOrDefault(), out var n)) Console.WriteLine($"  {n:C}"); };
        builtins["number"] = a => { if (double.TryParse(a.FirstOrDefault(), out var n)) Console.WriteLine($"  {n:N2}"); };
        builtins["percent"] = a => { if (double.TryParse(a.FirstOrDefault(), out var n)) Console.WriteLine($"  {n:P2}"); };
        builtins["ordinal"] = a => { if (int.TryParse(a.FirstOrDefault(), out var n)) { var s = n.ToString(); var suffix = (n % 100 >= 11 && n % 100 <= 13) ? "th" : (n % 10 == 1) ? "st" : (n % 10 == 2) ? "nd" : (n % 10 == 3) ? "rd" : "th"; Console.WriteLine($"  {n}{suffix}"); } };
        builtins["compact"] = a => { if (long.TryParse(a.FirstOrDefault(), out var n)) { string[] suffixes = ["", "K", "M", "G", "T", "P"]; int order = 0; double size = n; while (size >= 1000 && order < suffixes.Length - 1) { order++; size /= 1000; } Console.WriteLine($"  {size:F1}{suffixes[order]}"); } };
        builtins["bytesize"] = a => { if (long.TryParse(a.FirstOrDefault(), out var b)) { string[] sizes = ["B", "KB", "MB", "GB", "TB"]; int order = 0; double size = b; while (size >= 1024 && order < sizes.Length - 1) { order++; size /= 1024; } Console.WriteLine($"  {size:F2} {sizes[order]}"); } };

        // More fun
        builtins["riddle2"] = _ => { var riddles = new[] { "I have cities, but no houses. I have mountains, but no trees. I have water, but no fish. What am I? A map!", "What has a neck but no head? A bottle!", "What can you break without touching it? A promise!", "I have keys but no locks. I have space but no room. You can enter but can't go inside. What am I? A keyboard!" }; Console.WriteLine($"  {riddles[new Random().Next(riddles.Length)]}"); };
        builtins["funfact"] = _ => { var facts = new[] { "Honey never spoils!", "Octopuses have 3 hearts!", "A day on Venus is longer than a year!", "Bananas are berries but strawberries aren't!", "The Eiffel Tower can grow 6 inches in summer!", "A jiffy is 1/100th of a second!", "There are more stars than grains of sand on Earth!" }; Console.WriteLine($"  🌟 {facts[new Random().Next(facts.Length)]}"); };
        builtins["compliment"] = _ => { var c = new[] { "You're doing great!", "You're a star!", "Keep up the good work!", "You're amazing!", "You make the world better!", "You're brilliant!", "You're one of a kind!" }; Console.WriteLine($"  💫 {c[new Random().Next(c.Length)]}"); };
        builtins["insult"] = _ => { var i = new[] { "You fight like a dairy farmer!", "You smell like a wet dog!", "Your code compiles on the third try!", "You're like a semicolon — tiny but essential!", "You're a living proof that even bugs can be features!" }; Console.WriteLine($"  😈 {i[new Random().Next(i.Length)]}"); };

        // Extra validation
        builtins["isemail"] = a => Console.WriteLine(System.Text.RegularExpressions.Regex.IsMatch(a.FirstOrDefault() ?? "", @"^[^@\s]+@[^@\s]+\.[^@\s]+$") ? "  true" : "  false");
        builtins["isurl"] = a => Console.WriteLine(Uri.TryCreate(a.FirstOrDefault(), UriKind.Absolute, out _) ? "  true" : "  false");
        builtins["isip"] = a => Console.WriteLine(System.Net.IPAddress.TryParse(a.FirstOrDefault(), out _) ? "  true" : "  false");
        builtins["isphone"] = a => Console.WriteLine(System.Text.RegularExpressions.Regex.IsMatch(a.FirstOrDefault() ?? "", @"^\+?[\d\s\-\(\)]{7,15}$") ? "  true" : "  false");
        builtins["ishex"] = a => Console.WriteLine(System.Text.RegularExpressions.Regex.IsMatch(a.FirstOrDefault() ?? "", @"^[0-9a-fA-F]+$") ? "  true" : "  false");
        builtins["isbase64"] = a => { try { Convert.FromBase64String(a.FirstOrDefault() ?? ""); Console.WriteLine("  true"); } catch { Console.WriteLine("  false"); } };
        builtins["isjson2"] = a => { try { System.Text.Json.JsonDocument.Parse(a.FirstOrDefault() ?? ""); Console.WriteLine("  true"); } catch { Console.WriteLine("  false"); } };
        builtins["isipv4"] = a => Console.WriteLine(System.Net.IPAddress.TryParse(a.FirstOrDefault(), out var ip) && ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ? "  true" : "  false");
        builtins["isipv6"] = a => Console.WriteLine(System.Net.IPAddress.TryParse(a.FirstOrDefault(), out var ip) && ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 ? "  true" : "  false");
        builtins["isguid"] = a => Console.WriteLine(Guid.TryParse(a.FirstOrDefault(), out _) ? "  true" : "  false");
        builtins["isdate"] = a => Console.WriteLine(DateTime.TryParse(a.FirstOrDefault(), out _) ? "  true" : "  false");
        builtins["isint"] = a => Console.WriteLine(int.TryParse(a.FirstOrDefault(), out _) ? "  true" : "  false");
        builtins["isfloat"] = a => Console.WriteLine(double.TryParse(a.FirstOrDefault(), out _) ? "  true" : "  false");
        builtins["isempty"] = a => Console.WriteLine(string.IsNullOrWhiteSpace(a.FirstOrDefault()) ? "  true" : "  false");
        builtins["isnull"] = a => Console.WriteLine(a.FirstOrDefault() == null ? "  true" : "  false");
        builtins["lenmin"] = a => { if (a.Length >= 2 && int.TryParse(a[1], out var min) && (a.FirstOrDefault()?.Length ?? 0) < min) Console.WriteLine($"  Too short ({a.FirstOrDefault()?.Length} < {min})"); else Console.WriteLine("  OK"); };
        builtins["lenmax"] = a => { if (a.Length >= 2 && int.TryParse(a[1], out var max) && (a.FirstOrDefault()?.Length ?? 0) > max) Console.WriteLine($"  Too long ({a.FirstOrDefault()?.Length} > {max})"); else Console.WriteLine("  OK"); };
        builtins["minlen"] = a => { if (a.Length >= 2 && int.TryParse(a[1], out var min)) Console.WriteLine(a.FirstOrDefault()?.Length >= min ? "  OK" : $"  Too short"); };
        builtins["maxlen"] = a => { if (a.Length >= 2 && int.TryParse(a[1], out var max)) Console.WriteLine(a.FirstOrDefault()?.Length <= max ? "  OK" : $"  Too long"); };
        builtins["hasupper"] = a => Console.WriteLine(a.Any(c => c.Any(char.IsUpper)) ? "  true" : "  false");
        builtins["haslower"] = a => Console.WriteLine(a.Any(c => c.Any(char.IsLower)) ? "  true" : "  false");
        builtins["hasdigit"] = a => Console.WriteLine(a.Any(c => c.Any(char.IsDigit)) ? "  true" : "  false");
        builtins["hasspecial"] = a => Console.WriteLine(a.Any(c => c.Any(ch => !char.IsLetterOrDigit(ch) && !char.IsWhiteSpace(ch))) ? "  true" : "  false");
        builtins["minage"] = a => { if (a.Length >= 2 && int.TryParse(a[1], out var min) && DateTime.TryParse(a[0], out var bd)) { var age = DateTime.Now.Year - bd.Year - (DateTime.Now.DayOfYear < bd.DayOfYear ? 1 : 0); Console.WriteLine(age >= min ? "  OK" : $"  Too young ({age} < {min})"); } };
        builtins["maxage"] = a => { if (a.Length >= 2 && int.TryParse(a[1], out var max) && DateTime.TryParse(a[0], out var bd)) { var age = DateTime.Now.Year - bd.Year - (DateTime.Now.DayOfYear < bd.DayOfYear ? 1 : 0); Console.WriteLine(age <= max ? "  OK" : $"  Too old ({age} > {max})"); } };
        builtins["match"] = a => { if (a.Length >= 2) Console.WriteLine(System.Text.RegularExpressions.Regex.IsMatch(a[0], a[1]) ? "  Match!" : "  No match"); };
        builtins["regex"] = a => { if (a.Length >= 2) { var matches = System.Text.RegularExpressions.Regex.Matches(a[0], a[1]); foreach (System.Text.RegularExpressions.Match m in matches) Console.WriteLine($"  [{m.Index}] {m.Value}"); } };
        builtins["replace2"] = a => { if (a.Length >= 3) Console.WriteLine(a[0].Replace(a[1], a[2])); };
        builtins["regexreplace"] = a => { if (a.Length >= 3) Console.WriteLine(System.Text.RegularExpressions.Regex.Replace(a[0], a[1], a[2])); };
        builtins["extract"] = a => { if (a.Length >= 2) { var matches = System.Text.RegularExpressions.Regex.Matches(a[0], a[1]); foreach (System.Text.RegularExpressions.Match m in matches) Console.WriteLine($"  {m.Value}"); } };

        // Extra stats
        builtins["mean"] = a => { var nums = a.Where(x => double.TryParse(x, out _)).Select(double.Parse).ToList(); if (nums.Count > 0) Console.WriteLine($"  {nums.Average():F4}"); };
        builtins["median"] = a => { var nums = a.Where(x => double.TryParse(x, out _)).Select(double.Parse).OrderBy(x => x).ToList(); if (nums.Count > 0) Console.WriteLine($"  {(nums.Count % 2 == 0 ? (nums[nums.Count / 2 - 1] + nums[nums.Count / 2]) / 2 : nums[nums.Count / 2]):F4}"); };
        builtins["mode"] = a => { var groups = a.Where(x => double.TryParse(x, out _)).Select(double.Parse).GroupBy(x => x).OrderByDescending(g => g.Count()).ToList(); if (groups.Count > 0) Console.WriteLine($"  {groups[0].Key} (appears {groups[0].Count()} times)"); };
        builtins["range"] = a => { var nums = a.Where(x => double.TryParse(x, out _)).Select(double.Parse).ToList(); if (nums.Count > 0) Console.WriteLine($"  {nums.Max() - nums.Min():F4}"); };
        builtins["stdev"] = a => { var nums = a.Where(x => double.TryParse(x, out _)).Select(double.Parse).ToList(); if (nums.Count > 1) { var avg = nums.Average(); var variance = nums.Select(x => (x - avg) * (x - avg)).Sum(); Console.WriteLine($"  {Math.Sqrt(variance / nums.Count):F4}"); } };
        builtins["variance"] = a => { var nums = a.Where(x => double.TryParse(x, out _)).Select(double.Parse).ToList(); if (nums.Count > 1) { var avg = nums.Average(); Console.WriteLine($"  {nums.Select(x => (x - avg) * (x - avg)).Average():F4}"); } };
        builtins["percentile"] = a => { if (a.Length >= 2 && double.TryParse(a[1], out var p)) { var nums = a.Where(x => double.TryParse(x, out _)).Select(double.Parse).OrderBy(x => x).ToList(); var idx = (int)Math.Ceiling(p / 100 * nums.Count) - 1; Console.WriteLine($"  {nums[Math.Clamp(idx, 0, nums.Count - 1)]:F4}"); } };
        builtins["quartile"] = a => { var nums = a.Where(x => double.TryParse(x, out _)).Select(double.Parse).OrderBy(x => x).ToList(); if (nums.Count >= 4) { var q1 = nums[nums.Count / 4]; var q2 = nums[nums.Count / 2]; var q3 = nums[nums.Count * 3 / 4]; Console.WriteLine($"  Q1: {q1}, Q2: {q2}, Q3: {q3}"); } };
        builtins["iqr"] = a => { var nums = a.Where(x => double.TryParse(x, out _)).Select(double.Parse).OrderBy(x => x).ToList(); if (nums.Count >= 4) { var q1 = nums[nums.Count / 4]; var q3 = nums[nums.Count * 3 / 4]; Console.WriteLine($"  IQR: {q3 - q1:F4}"); } };
        builtins["zscore"] = a => { if (a.Length >= 2 && double.TryParse(a[0], out var val)) { var nums = a.Skip(1).Where(x => double.TryParse(x, out _)).Select(double.Parse).ToList(); if (nums.Count > 0) { var avg = nums.Average(); var stdev = Math.Sqrt(nums.Select(x => (x - avg) * (x - avg)).Average()); Console.WriteLine($"  Z-score: {(val - avg) / stdev:F4}"); } } };
        builtins["correlation"] = a => { if (a.Length >= 4) { var x = new[] { double.Parse(a[0]), double.Parse(a[1]) }; var y = new[] { double.Parse(a[2]), double.Parse(a[3]) }; var avgX = x.Average(); var avgY = y.Average(); var num = x.Zip(y, (a2, b) => (a2 - avgX) * (b - avgY)).Sum(); var denX = x.Select(v => (v - avgX) * (v - avgX)).Sum(); var denY = y.Select(v => (v - avgY) * (v - avgY)).Sum(); Console.WriteLine($"  Correlation: {num / Math.Sqrt(denX * denY):F4}"); } };
    }

    static void RunGit(string args)
    {
        try
        {
            var psi = new ProcessStartInfo("git", args)
            {
                WorkingDirectory = Directory.GetCurrentDirectory(),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            if (proc != null) { Console.Write(proc.StandardOutput.ReadToEnd()); var err = proc.StandardError.ReadToEnd(); if (!string.IsNullOrEmpty(err)) { Console.ForegroundColor = ConsoleColor.Red; Console.Write(err); Console.ResetColor(); } proc.WaitForExit(); }
        }
        catch (Exception ex) { Console.WriteLine($"  Error: {ex.Message}"); }
    }
}
