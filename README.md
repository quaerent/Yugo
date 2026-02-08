# Yugo

Yugo 是一个现代网格解谜游戏生态系统，包含受重力驱动的游戏引擎、跨平台桌面 App、以及基于 Web 的关卡分享社区。

## 核心组件

- **Yugo.Core**: 逻辑核心，处理受重力驱动的物理模拟与实体合并。
- **Yugo.Game**: 基于 MonoGame 和 Dear ImGui 的跨平台桌面 App（支持 Win/Mac）。
- **Yugo.Server**: ASP.NET Core 后端，提供用户鉴权、关卡存储及 App 远程控制桥接。
- **web**: 基于 React + TailwindCSS v4 的社区前端，可直接在网页端遥控桌面 App 游玩。

## 快速开始 (本地开发)

1. **启动后端服务**: `dotnet run --project Yugo.Server` (默认使用 SQLite)
2. **启动 Web 前端**: `cd web && npm install && npm run dev`
3. **启动游戏 App**: `dotnet run --project Yugo.Game`

## 生产环境部署 (Docker)

项目已完全 Docker 化，支持一键部署至云服务器：

1. **准备配置**: 
   - 编辑 `admin_config.json` 设置您的管理员用户名和密码。
2. **一键启动**:
   ```bash
   docker compose up -d
   ```
   该指令将自动从 GitHub Container Registry 拉取最新镜像，并启动 PostgreSQL 数据库、后端 API 和 Nginx 前端。

- **前端地址**: `http://localhost:8080`
- **后端 API**: `http://localhost:5057`

## 自动化流水线
每当代码推送到 `main` 分支，GitHub Actions 会自动构建并发布最新的 Docker 镜像至 `ghcr.io/quaerent/yugo`。

## 编辑器交互 (Object Mode)
- **选择**: 在网格或侧边栏列表选中实体。
- **绘图**: 选中后，**左键**增加方块，**右键**修剪。
- **属性**: 实时调整 Cluster ID 或活塞轴向。
- **云端同步**: 登录后，编辑云端关卡点击 SAVE 即可瞬间同步。

## 开发规范
- C# 代码遵循 `csharpier` 格式。
- TypeScript 代码遵循 `prettier` 格式。
- 提交前必须通过 pre-commit 钩子检查。