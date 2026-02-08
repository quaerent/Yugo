# GEMINI.md - Project Context: Yugo

## Project Overview
**Yugo** is a grid-based puzzle game engine built with **C# (.NET 10.0)** and **MonoGame**. It features a high-fidelity simulation system driven by gravity, recursive push propagation, and adjacency-based merging.

### Key Technologies
- **Framework:** MonoGame (DesktopGL)
- **UI System:** Dear ImGui (via `ImGui.NET`) with a custom 32-bit transparent BMP icon and a high-performance renderer.
- **Logic:** Decoupled core library with `ClusterEntity` inheritance.
- **Persistence:** JSON-based cross-platform settings (`PersistentSettings`).
- **DevOps:** GitHub Actions for automated `.dmg` (macOS) and Inno Setup `.exe` (Windows) releases.

## Architecture
The project is divided into three main components:

1.  **`src/Yugo.Core`**: The simulation heart.
    - `ClusterEntity`: Base class managing `ClusterId` for `Movable` and `Piston`.
    - `Piston`: Specialized entity with axis-locked movement and boundary "growth" logic.
    - `ClusterMergeRule`: Unified adjacency merging with priority (Piston > Movable).
    - `LevelState`: Snapshot-based state management for Undo/Redo.

2.  **`src/Yugo.Game`**: The visual front-end.
    - `ImGuiRenderer`: Optimized for sharp text (LinearClamp + Pixel Snapping) and 32-bit transparent BMP support.
    - `GridView`: Coordinate translation between screen and grid space with dual-sidebar offset support.
    - `Screens`: `EditorScreen` (Object-based Inspector), `MenuScreen` (Clean minimal UI), `Scene` (Gameplay).

3.  **`tests/Yugo.Core.Tests`**: Logic verification.

## Building and Running

### Prerequisites
- .NET 10.0 SDK
- Inno Setup 6 (for Windows setup builds)
- macOS: `brew install create-dmg` (for DMG builds)

### Key Commands
- **Run Game:** `dotnet run --project src/Yugo.Game`
- **Publish (Win):** `./scripts/publish_win.sh` (Generates raw exe and Setup)
- **Publish (Mac):** `./scripts/publish_mac.sh` (Generates signed .app bundle)

## Development Conventions

### Editor "Object Mode"
- **Interaction**: Centered around the **Selected Entity**. 
- **Left Click**: Add cell to the selected entity.
- **Right Click**: Remove cell from the selected entity.
- **Properties**: Live conversion between Wall/Movable/Piston and Cluster ID / Axis editing.

### Visual Standards
- **Sharpness**: No `SetWindowFontScale` (to avoid blur). Use integer positions for windows.
- **Highlighting**: Use `RenderUtil.GetHighlightedColor()` (Lerp with white) for interactive targets.
- **Iconography**: 256x256 SVG source for all platform icons.

## Roadmap
- [x] Migrate UI to ImGui.
- [x] Implement 'Object Mode' editor with Inspector.
- [x] Implement Piston mechanism.
- [x] Professional Setup (.exe) and DMG (.dmg) automation.
- [x] 32-bit transparent BMP window icon fix.
- [ ] Add Undo/Redo UI buttons in Gameplay.
- [ ] Implement Teleporters & Rotators.
- [ ] Level pack progression system.