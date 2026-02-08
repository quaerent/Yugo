# GEMINI.md - Project Context: Yugo

## Project Overview
**Yugo** is a grid-based puzzle game engine and ecosystem built with **C# (.NET 10.0)**, **MonoGame**, and **React**. It features a high-fidelity simulation system and a Docker-ready community platform for level sharing.

### Key Technologies
- **Framework:** MonoGame (DesktopGL)
- **Backend:** ASP.NET Core (REST API + WebSocket Bridge)
- **Frontend:** React + Vite + TailwindCSS v4
- **Database:** PostgreSQL (Production) / SQLite (Dev)
- **Deployment:** Docker & GitHub Actions (GHCR)

## Architecture
The project is divided into four main components:

1.  **`Yugo.Core`**: The simulation heart.
    - `LevelIdentity`: Unified object for identifying local/cloud sources.
    - `LevelState`: Deep value-based state comparison for optimized Undo history.

2.  **`Yugo.Game`**: The visual front-end (App).
    - `RemoteService`: WebSocket server supporting "Unlink on Disconnect" auth sync.
    - `Identity Awareness`: Automatic Read-Only mode for non-owned cloud levels.

3.  **`Yugo.Server`**: ASP.NET Core Backend.
    - Supports dynamic DB switching (Postgres/SQLite).
    - Externalized admin configuration via `admin_config.json`.

4.  **`web`**: Vite + React Frontend.
    - Acts as the central communication hub.
    - Proxies App sync requests to the Server via WebSocket.

## DevOps & Deployment

### GitHub Actions
- **`docker-publish.yml`**: Builds and pushes `yugo-backend` and `yugo-frontend` to GitHub Packages on every push to `main`.

### Docker Compose
- Orchesrates PostgreSQL, Backend, and Nginx (Frontend).
- Handles reverse proxying for `/api` requests.

## Key Commands
- **Dev Start**: `Cmd+Shift+B` (VS Code START FULL SYSTEM task)
- **Prod Start**: `docker compose up -d`
- **Format**: `./scripts/format.sh`

## Roadmap
- [x] Full-stack architecture with WebSocket sync.
- [x] Dockerization & CI/CD pipeline.
- [x] Identity-aware permissions and cloud-first saving.
- [ ] Implement thumbnail generation for cloud levels.
- [ ] Advanced entities (Teleporters).