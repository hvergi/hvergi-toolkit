# Hvergi Toolkit

## Project Overview
Hvergi Toolkit is a Godot-based utility application designed for players of Wurm Online. It serves as a centralized hub for various specialized tools such as trackers, planners, and editors to enhance the gaming experience.

- **Engine:** Godot 4.6 (Forward Plus renderer)
- **Language:** C# (.NET 8.0 / .NET 9.0 for Android)
- **Physics Engine:** Jolt Physics
- **Rendering Driver:** D3D12 (Windows)

## Architecture
The application is structured around a main scene (`scenes/hvergi_toolkit/hvergi_toolkit.tscn`) which provides a unified UI with the following sections:
- **News:** Project updates and changelogs.
- **Apps:** A grid of utility applications (Trackers, Planners, Search tools, etc.).
- **Settings:** Application configuration.
- **Terminal:** A log display for system messages and status updates.

## Building and Running
### Prerequisites
- Godot Engine 4.6 (with .NET support).
- .NET 8.0 SDK (and .NET 9.0 for Android targets).

### Key Commands
- **Build Project:** `dotnet build`
- **Run Project:** Open `project.godot` in Godot Editor and press F5, or use `godot --path .` from the CLI.
- **Test Project:** (TODO: Add testing framework if implemented)

## Development Conventions
- **Scene Organization:** Scenes are located in the `scenes/` directory, typically grouped with their corresponding C# scripts.
- **C# Scripting:** 
    - Uses the `partial class` pattern common in Godot 4.
    - Uses `[Export]` for inspector-visible variables.
    - Adheres to standard C# naming conventions (PascalCase for classes/methods).
- **UI System:** Built using Godot's Control nodes, leveraging `VBoxContainer`, `HBoxContainer`, and `TabContainer` for layout management.
- **Node Referencing:** Utilizes `%UniqueName` and `unique_id` features for robust node access within scenes.
