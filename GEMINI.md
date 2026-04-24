# Hvergi Toolkit

## Project Overview
Hvergi Toolkit is a utility application for Wurm Online players, built with **Godot 4.6** and **C# (.NET 8.0)**. It provides a suite of tools for managing player data, tracking skills, and simulating game mechanics.

### Key Technologies
- **Game Engine:** Godot 4.6 (Forward Plus renderer)
- **Language:** C# (.NET 8.0 SDK)
- **Physics:** Jolt Physics
- **Rendering:** D3D12 (Windows), Vulkan/Forward+ (General)

## Architecture
The application uses a modular architecture where the main toolkit manages a set of independent "Apps" that run in their own windows.

- **Main Scene:** `scenes/hvergi_toolkit/hvergi_toolkit.tscn`
- **Autoloads:**
  - `Terminal`: A global logging and output system for feedback.
- **App System:**
  - Each utility is located in `scenes/apps/{app_name}/`.
  - Apps are typically `Window` nodes that are instantiated dynamically by the main toolkit.

## Utility Apps
| App Name | Description |
| :--- | :--- |
| **Player Editor** | Manages Wurm Online installation paths and player data. Includes auto-discovery for standalone and Steam versions. |
| **Moi Tracker** | (Base implemented) Tracks Meditating/Moi progress. |
| **Skill Tracker** | (Base implemented) Monitors skill gain logs. |
| **Affinity Food Planner** | (Base implemented) Plans food for affinity gains. |
| **STP Calculator** | (Base implemented) Skill Tick Probability/Power calculator. |
| **Log Alert/Search** | (Base implemented) Utilities for processing Wurm log files. |

## Building and Running
### Prerequisites
- Godot 4.6 (with .NET support)
- .NET 8.0 SDK

### Key Commands
- **Build Project:** `dotnet build`
- **Run Project:** Open in Godot Editor and press `F5`, or run `godot --path .` via CLI.
- **Exporting:** Presets are configured in `export_presets.cfg` for Windows Desktop.

## Development Conventions
- **Scene Organization:** Each app has its own directory in `scenes/apps/` containing its `.tscn` and `.cs` files.
- **Node Referencing:** Use `%UniqueName` in scenes and `GetNode("%Name")` in C# for robust referencing.
- **Cross-Platform:** Directory discovery logic in `PlayerEditor.cs` supports Windows (Registry), Linux (XML), and macOS (XML/Plist).
- **Styling:** The UI uses standard Godot Theme constants and `PanelContainer`/`MarginContainer` for responsive layouts.
