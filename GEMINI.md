# GEMINI.md - Project Context: Yugo

## Project Overview
**Yugo** is a grid-based puzzle game engine built with **C# (.NET 10.0)** and **MonoGame**. The game features a unique mechanics system driven by gravity, recursive push propagation, and adjacency-based merging.

### Key Technologies
- **Framework:** MonoGame (DesktopGL)
- **UI System:** Dear ImGui (via `ImGui.NET`) with custom high-performance renderer.
- **Logic:** Grid-based simulation with gravity and recursive push logic.
- **Persistence:** JSON-based cross-platform settings and XML level serialization.
- **CI/CD:** GitHub Actions for automated multi-platform releases.

## Architecture
The project is divided into three main components:

1.  **`src/Yugo.Core`**: The logical heart.
    - `Level`: Main simulation controller.
    - `ClusterEntity`: Base class for colored entities (`Movable`, `Piston`), managing `ClusterId`.
    - `Piston`: Specialized entity with axis-locked movement and boundary growth logic.
    - `ClusterMergeRule`: Unified adjacency merging with priority (Piston > Movable).
    - `LevelState`: Snapshot management for Undo/Redo via `SnapshotFactory`.

2.  **`src/Yugo.Game`**: Visual front-end.
    - `ImGuiRenderer`: Manual integration using `unsafe` memory copying and `LinearClamp` sampling for sharp UI.
    - `GridView`: Handles viewport offsets and coordinate translation for dual-sidebar layouts.
    - `Screens`: `EditorScreen` (Object-based Inspector mode), `MenuScreen` (Clean floating UI), `Scene` (Gameplay).
    - `PersistentSettings`: Handles cross-platform data storage (AppData/Application Support).

3.  **`tests/Yugo.Core.Tests`**: Logic verification.

## Building and Running

### Prerequisites
- .NET 10.0 SDK
- SDL2 (bundled with MonoGame)

### Key Commands
- **Build All:** `dotnet build`
- **Run Game:** `dotnet run --project src/Yugo.Game`
- **Run Tests:** `dotnet test`
- **Publish (Win):** `./scripts/publish_win.sh`
- **Publish (Mac):** `./scripts/publish_mac.sh`

## Development Conventions

### Editor "Object Mode"
- Interaction is based on the **Selected Entity**. 
- **Left Click**: Add cell to selected entity.
- **Right Click**: Remove cell from selected entity.
- **Properties Panel**: Used for entity type conversion and attribute editing (ClusterId, Axis).

### UI Implementation
- **Sharpness**: Always snap window positions to integers and use `LinearClamp` to avoid text blur.
- **Interactive Safety**: Check `io.WantCaptureMouse` before processing grid clicks.
- **Highlighting**: Use `RenderUtil.GetHighlightedColor()` (mix with white) for selected/hovered entities. No outlines.

### Logic Consistency
- **Core-only Physics**: No gameplay logic allowed in `Yugo.Game`.
- **Default Cluster**: All `ClusterEntity` instances must default to `ClusterId = 1`.

## Roadmap
- [x] Migrate UI from Gum to ImGui.
- [x] Implement 'Object Mode' editor with Entity List.
- [x] Implement Piston mechanism.
- [x] Cross-platform persistence and automated publishing.
- [ ] Add Undo/Redo visual buttons in Gameplay.
- [ ] Implement advanced entities (Teleporters, Rotators).
- [ ] Sound & Music integration.
