# GEMINI.md - Project Context: Yugo

## Project Overview
**Yugo** is a grid-based puzzle engine and cloud ecosystem. It features a unique "Web-as-the-Brain" architecture where the browser manages identities and metadata, while the desktop App handles simulation and rendering.

### Key Technologies
- **Backend:** ASP.NET Core (Virtual Admin Auth + WebSocket Bridge).
- **Frontend:** React + Vite (Central Message Hub).
- **Identity:** `LevelIdentity` system with path/id deduplication.
- **Protocol:** WebSocket with `sync_cloud_list` for transient metadata sync.

## Architecture & Security

### 1. Unique System Admin
- **Virtual ID 0**: The `admin` account exists only in memory and config files.
- **Physical Isolation**: Database contains NO admin data. Admin password cannot be reset via web UI.
- **Privilege Scope**: Global registry management + User lifecycle control.

### 2. Transient Metadata Sync
- **Title Resolution**: Game client (App) does NOT persist cloud level titles.
- **Heartbeat Sync**: Web frontend pushes a full {ID -> Title} map every 10s.
- **Result**: Immediate, ecosystem-wide renaming without App-side manual saves.

### 3. Identity-Aware Editor
- **Read-Only Lock**: Automatically triggered if `Identity.AuthorId` != `Engine.CurrentUser.Id`.
- **Cloud-First Persistence**: Save button transparently routes to WebSocket proxy for cloud levels.

## Key Protocols
- `set_user`: Push identity to App on login.
- `share_level`: App requests Web to proxy save data to Server.
- `sync_cloud_list`: Periodic full metadata refresh.
- `update_identity`: Instant title correction signal.

## Deployment
- CI/CD publishes to Docker Hub.
- Volume mapping for `admin_config.json` allows credential management without redeploy.
