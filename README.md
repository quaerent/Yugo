# Yugo

Yugo is a modern grid-based puzzle game engine built with **C# (.NET 10.0)** and **MonoGame**. The game features a unique mechanics system driven by gravity, recursive push propagation, and adjacency-based merging.

## Core Features
- **Deterministic Simulation**: A standalone core library handles all physics and rules, ensuring consistent behavior across platforms.
- **Professional Editor**: A built-in level editor using **Dear ImGui**, supporting:
  - Real-time grid resizing with safety confirmation.
  - Multi-cluster entity placement.
  - Snapshot-based saving/loading.
- **Modern UI**: Professional dark-themed UI integration via a custom ImGui renderer.
- **Stability Engine**: Recursive push and gravity simulation that runs until the level reaches a stable state.

## Project Structure
- `src/Yugo.Core`: The logical heart of the game. Contains entity behavior, gravity logic, and merge rules.
- `src/Yugo.Game`: The visual front-end built with MonoGame.
  - `ImGuiRenderer.cs`: Custom high-performance UI integration.
  - `GridView.cs`: Advanced screen-to-grid coordinate management.
- `tests/Yugo.Core.Tests`: Automated unit tests for physics and logic validation.

## Tooling & Conventions
- **Code Style**: Enforced via .NET Analyzers and `Directory.Build.props`.
- **Formatting**: Strictly follows **Csharpier** (enforced by pre-commit hooks).
- **Quality Control**: All logic changes should be verified with `dotnet test`.

## Getting Started

### Prerequisites
- .NET 10.0 SDK
- MonoGame DesktopGL dependencies (SDL2, etc.)

### Setup
1. **Restore Tools**: `dotnet tool restore`
2. **Install Pre-commit**: `pre-commit install`
3. **Restore Packages**: `dotnet restore Yugo.slnx`

### Running
- **Launch Game**: `dotnet run --project src/Yugo.Game`
- **Run Tests**: `dotnet test` or `./scripts/test.sh`

## Editor Controls
- **S**: Save level
- **Esc**: Exit to Menu
- **Left Click**: Place current tool
- **Right Click**: Erase cell
- **Toolbox**: Select tools and cluster IDs via the ImGui sidebar on the left.