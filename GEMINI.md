# GEMINI.md - Project Context: Yugo

## Project Overview
**Yugo** is a grid-based puzzle game engine built with **C# (.NET 10.0)** and **MonoGame**. The core gameplay revolves around moving and merging colored blocks ("Movable" entities) driven by gravity and adjacency rules.

### Key Technologies
- **Framework:** MonoGame (DesktopGL)
- **UI System:** Dear ImGui (via `ImGui.NET`)
- **Game Logic:** Grid-based simulation with recursive push propagation and gravity stability loops.
- **Serialization:** XML-based level loading/saving and snapshot-based Undo/Redo system.
- **Testing:** xUnit for core logic verification.

## Architecture
The project is divided into three main components:

1.  **`src/Yugo.Core`**: Standalone logic library.
    - `Grid`: 2D coordinate container.
    - `Level`: Simulation controller (Gravity, History, Stability).
    - `LevelState`: Encapsulates logic for capturing and applying level snapshots via `SnapshotFactory`.
    - `Entities`: `Movable` (colored blocks) and `Wall` (static obstacles).
    - `Rules`: `IMergeRule` (adjacency-based merging) and `IWinRule` (victory conditions).

2.  **`src/Yugo.Game`**: MonoGame application.
    - `ImGuiRenderer`: Custom manual integration for ImGui rendering using unsafe vertex buffer synchronization.
    - `GridView`: Handles coordinate translation between screen space and grid space, supporting viewport offsets.
    - `Screens`: Implements `IScreen` for navigation (`MenuScreen`, `EditorScreen`, `Scene`).
    - `Renderers`: Dedicated `IRenderer` implementations for different entity types.

3.  **`tests/Yugo.Core.Tests`**: Logic and serialization unit tests.

## Building and Running

### Prerequisites
- .NET 10.0 SDK
- MonoGame dependencies (SDL2, etc.)

### Key Commands
- **Build All:** `dotnet build`
- **Run Game:** `dotnet run --project src/Yugo.Game`
- **Run Tests:** `dotnet test`
- **Format Code:** `dotnet format` (uses Csharpier via pre-commit)

## Development Conventions

### Coding Style
- **Nullable & Implicit Usings:** Enabled and strictly enforced.
- **Unsafe Code:** Allowed in `Yugo.Game` for high-performance ImGui data copying.
- **Formatting:** Code MUST be formatted using `csharpier` (enforced via pre-commit hooks).

### UI Development (ImGui)
- **Immediate Mode:** UI logic resides in `IScreen.DrawGui()`.
- **Layout Safety:** Use `io.WantCaptureMouse` to prevent clicking through UI elements onto the game grid.
- **Theme:** Currently uses `ImGui.StyleColorsLight()` to match the game's aesthetic.

### Logic vs. Rendering
- **Decoupling:** All gameplay mechanics MUST reside in `Yugo.Core`. 
- **Grid Offset:** When adding UI panels (like the Editor Sidebar), update the `GridView` with a restricted rectangle to avoid overlapping game elements.
- **Stability:** After any interaction, the level must call `RunUntilStable()` to process physics and rules.

### Serialization
- Use `[TypeId("id", "Name")]` for all serializable entities and rules.
- State management should use `LevelState.Capture(level)` and `state.Apply(level)`.

## Roadmap
- [x] Migrate UI from Gum to ImGui.
- [x] Implement professional level editor with resizing and confirmation safety.
- [ ] Add Undo/Redo visual buttons in `Scene` (Gameplay).
- [ ] Implement more complex entity types (e.g., rotators).
- [ ] Sound system integration.