# GEMINI.md - Project Context: Yugo

## Project Overview
**Yugo** is a grid-based puzzle game engine and ecosystem built with **C# (.NET 10.0)**, **MonoGame**, and **React**. It features a high-fidelity simulation system driven by gravity and a web-based community for level sharing.

### Key Technologies
- **Framework:** MonoGame (DesktopGL)
- **Backend:** ASP.NET Core (REST API + TCP Bridge)
- **Frontend:** React + Vite + TailwindCSS v4
- **UI System:** Dear ImGui (Game)
- **Logic:** Decoupled core library with `ClusterEntity` inheritance.
- **Persistence:** SQLite (Server) & JSON (App).
- **DevOps:** GitHub Actions for automated releases.

## Architecture
The project is divided into four main components:

1.  **`Yugo.Core`**: The simulation heart.
    - `Level`: Main simulation controller.
    - `ClusterEntity`: Base class for `Movable` and `Piston`.
    - `ClusterMergeRule`: Unified adjacency merging with priority.
    - `LevelState`: Snapshot-based state management for Undo/Redo.

2.  **`Yugo.Game`**: The visual front-end (App).
    - `ImGuiRenderer`: High-performance custom UI renderer.
    - `RemoteService`: Background TCP listener (port 9090) for web-to-app commands.
    - `Screens`: Editor (Object Mode), Menu, and Scene.

3.  **`Yugo.Server`**: ASP.NET Core Backend.
    - `AuthController`: Simple user authentication (admin-created accounts).
    - `LevelsController`: Level sharing, downloading, and remote control triggering.
    - `RemoteAppService`: TCP client for sending commands to the desktop App.

4.  **`web`**: Vite + React Frontend.
    - Community dashboard for playing and editing shared levels directly in the App.

## Building and Running

### Prerequisites
- .NET 10.0 SDK
- Node.js (for `web` directory)
- Inno Setup 6 (Windows)

### Key Commands
- **Run Game:** `dotnet run --project Yugo.Game`
- **Run Server:** `dotnet run --project Yugo.Server`
- **Run Web:** `cd web && npm run dev`
- **Run Tests:** `dotnet test`

## Development Conventions

### Editor "Object Mode"
- **Interaction**: Centered around the **Selected Entity**. 
- **Left Click**: Add cell / **Right Click**: Remove cell.

### Communication (Web -> App)
- The Web frontend calls Server APIs, which then send TCP JSON commands to the App on port 9090.
- Commands: `play_level`, `edit_level`.

### Logic Standards
- **Strict Decoupling**: Physics MUST stay in `Yugo.Core`.
- **Formatting**: `csharpier` for C#, `prettier` for TSX (enforced via pre-commit).

## Roadmap
- [x] Migrate UI to ImGui.
- [x] Implement 'Object Mode' editor.
- [x] Cross-platform persistence and DMG/Setup automation.
- [x] Web & Server basic architecture + TCP Bridge.
- [ ] Implement Level sharing upload in App.
- [ ] Add Undo/Redo UI buttons in Gameplay.
- [ ] Advanced entities (Teleporters).
