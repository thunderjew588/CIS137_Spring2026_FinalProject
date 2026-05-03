# ChucK Listener Control Utility

A command-line utility for managing ChucK audio processes with advanced score buffer capabilities. Built for CIS137 Spring 2026 Final Project.

## 🎵 Overview

This utility provides an interactive shell for controlling ChucK listener processes, staging audio files, and building complex musical scores that can be executed as batches. It bridges .NET applications with ChucK's real-time audio synthesis through OSC (Open Sound Control) messaging.

## ✨ Features

### ChucK Process Management
- **Start/Stop Control** - Launch and terminate ChucK listener processes
- **Status Monitoring** - View process status, PID, CPU time, and uptime
- **Automatic Cleanup** - Gracefully stops ChucK when exiting the utility

### File Staging System
- **Add Files** - Stage `.ck` files with custom parameters (frequency, gain, duration, etc.)
- **Parameter Support** - Attach key-value parameters to each file
- **List & Remove** - View and manage staged files
- **Quick Clear** - Clear all staged files at once

### Score Buffer Operations
The score buffer allows you to compose musical sequences before sending them to ChucK:

- **addfile** - Add ChucK files with unique keys and parameters
- **addplay** - Schedule play commands with duration
- **remfile** - Queue file removal commands
- **remove** - Remove entries by index or range (supports `1`, `1-3`, `1 3 5`)
- **move** - Reorder entries (single or range moves)
- **copy** - Copy staged files to buffer with auto-generated keys
- **list** - View buffer contents with clear command type indicators
- **clear** - Empty the entire buffer
- **save/load** - Persist buffers to `.mgs` files (JSON format)
- **show** - List available `.mgs` score files
- **flush** - Send entire buffer to ChucK as a single batch operation

## 🚀 Getting Started

### Prerequisites
- .NET 8 SDK
- ChucK (with `chuck` in your PATH)
- Visual Studio 2022+ (for development)

### Installation

1. Clone the repository:
```bash
git clone https://github.com/thunderjew588/CIS137_Spring2026_FinalProject
cd CIS137_Spring2026_FinalProject
```

2. Build the solution:
```bash
dotnet build
```

3. Run the utility:
```bash
dotnet run --project MustangGongShow.TestConsole -- <path-to-OscListener.ck>
```

Example:
```bash
dotnet run --project MustangGongShow.TestConsole -- Chuck/OscListener.ck
```

## 📖 Usage Examples

### Basic Workflow
```bash
# Start the ChucK listener
> start

# View available .ck files
> show

# Stage some files with parameters
> add mysound.ck freq=440 gain=0.8
> add bass.ck freq=110
> list

# Play for 10 seconds
> play 10

# Clean up
> clear
> stop
```

### Score Buffer Workflow
```bash
# Build a score
> buffer addfile lead.ck lead1 freq=440 gain=1.0
> buffer addfile bass.ck bass1 freq=110 gain=0.8
> buffer addplay 8 key=section1
> buffer addfile lead.ck lead2 freq=880 gain=0.7
> buffer addplay 4
> buffer remfile lead1

# Review and edit
> buffer list
> buffer move 2 4
> buffer remove 3

# Save for later
> buffer save myscore

# Send to ChucK
> buffer flush

# Load and replay
> buffer clear
> buffer load myscore
> buffer flush
```

### Advanced Buffer Operations
```bash
# Copy staged files to buffer with auto-keys
> add sound1.ck freq=440
> add sound2.ck freq=880
> buffer copy

# Remove multiple entries
> buffer remove 1 3 5        # Remove specific indices
> buffer remove 2-4          # Remove range
> buffer remove 1-3 7 9-11   # Mixed ranges and indices

# Move ranges
> buffer move 2-4 8          # Move entries 2-4 to position 8
```

## 📋 Command Reference

### Main Commands
| Command | Description |
|---------|-------------|
| `start` | Start ChucK listener process |
| `stop` | Stop ChucK listener process |
| `status` | Get listener status |
| `show` | List available .ck files |
| `add <file> [params...]` | Stage a file with parameters |
| `list` | List staged files |
| `remove <index>` | Remove staged file |
| `clear` | Clear staged files |
| `play <seconds>` | Play staged files |
| `buffer <cmd>` | Execute buffer operation |
| `help` | Show detailed help |
| `exit` | Exit utility |

### Buffer Commands
Use `buffer help` in the utility for detailed buffer command documentation.

Quick reference:
- `addfile <file.ck> <key> [params...]` - Add file with unique key
- `addplay <duration> [params...]` - Add play command
- `remfile <key>` - Add remove command
- `list` - View buffer contents
- `save <name>` - Save buffer to .mgs file
- `load <name>` - Load buffer from .mgs file
- `flush` - Send buffer to ChucK server

## 📁 File Formats

### .mgs Score Files
Score buffers are saved as JSON files with `.mgs` extension:

```json
{
  "scoreBuffer": [
    {
      "index": 1,
      "command": "add",
      "parameters": {
        "filename": "mysound.ck",
        "key": "sound1",
        "freq": "440"
      }
    },
    {
      "index": 2,
      "command": "play",
      "parameters": {
        "duration": "5",
        "key": "play1"
      }
    }
  ]
}
```

## 🏗️ Project Structure

```
CIS137_Spring2026_FinalProject/
├── Chuck/                          # ChucK scripts directory
│   ├── OscListener.ck             # OSC listener script
│   └── *.ck                       # Your ChucK files
├── MustangGongShow.TestConsole/   # Main console application
│   └── Program.cs                 # Core implementation
├── MustangGongShow/               # WPF application (if used)
├── MustangGongShow.IOC/           # Dependency injection
├── MustangGongShow.Shell/         # Shell components
└── MustangGongShow.ViewModel/     # View models
```

## 🔧 Technical Details

### OSC Communication
- **Protocol**: UDP over OSC (Open Sound Control)
- **Default Port**: 6449
- **Address**: `127.0.0.1` (localhost)
- **Message Path**: `/chuck-daemon/cmd`

### Batch Flush Format
When flushing the buffer, commands are sent as:
```
Arg 0: "score"
Arg 1: <count> (integer)
Arg 2+: Command strings:
  - "add <filename.ck> <key> param1=value1 ..."
  - "play <duration> param1=value1 ..."
  - "remove <key>"
```

## 🎓 Course Context

**Course**: CIS137 - Spring 2026  
**Project**: Final Project  
**Focus**: Real-time audio control, process management, and command-line interface design

## 🤝 Contributing

This is a course project, but suggestions and feedback are welcome through issues.

## 📝 License

Educational project for CIS137 Spring 2026.

## 🙏 Acknowledgments

- ChucK programming language and community
- Vizcon OSC library for .NET
- CIS137 course instructors and materials

---

**Note**: This utility requires ChucK to be installed and accessible via the `chuck` command in your system PATH.
