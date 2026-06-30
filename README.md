# CustomWinConsole

Custom Windows console replacing cmd.exe with **1000 built-in commands**, themes, aliases, scripts, and a text editor.

## Features

- **1000 commands** — math, strings, files, networking, crypto, git, colors, ASCII art, and more
- **6 themes** — matrix, ocean, sunset, cyberpunk, minimal, fire
- **Aliases** — create custom shortcuts, saved to `~/.customwinconsole_aliases.json`
- **Todo list** — built-in task manager
- **Scripting** — run `.cwc` scripts with `#` comments
- **Text editor** — `edit <file>` for quick edits
- **Clipboard** — `clip` and `paste` via PowerShell
- **Customizable prompt** — `{dir}`, `{user}`, `{time}`, `{date}`
- **Pipe & redirect** — `|`, `>`, `>>` pass through to cmd.exe natively
- **Russian text** — full encoding 866 support

## Quick Start

```bash
dotnet build
CustomWinConsole.exe
```

## Usage

```bash
help              # show all commands
help-all          # show extended help
theme matrix      # switch theme
alias ll ls -la   # create alias
todo add "Task"   # add todo
edit myfile.txt   # open text editor
script run.cwc    # run script
```

## Themes

| Theme | Style |
|-------|-------|
| `matrix` | Green on black, terminal feel |
| `ocean` | Blue tones, calm |
| `sunset` | Warm oranges and reds |
| `cyberpunk` | Neon pink/cyan, futuristic |
| `minimal` | Clean white/gray |
| `fire` | Red/orange, intense |

## Examples

```bash
# Math
calc 2+2
sqrt 144
fib 10

# Strings
upper hello
reverse world
count abcabc a

# Files
cat myfile.txt
grep "error" logfile.txt
find *.cs

# System
sysinfo
memused
drives

# Network
myip
ping google.com
httpheaders https://example.com

# Fun
cowsay "Moo!"
fortune
matrix 50
```

## Building

Requires .NET 10.0+

```bash
dotnet build -c Release
```

## License

MIT
