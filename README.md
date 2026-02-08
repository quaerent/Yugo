# Yugo

Yugo 是一个现代网格解谜游戏生态系统，包含受重力驱动的游戏引擎、跨平台桌面 App、以及基于 Web 的关卡分享社区。

## 核心组件

- **Yugo.Core**: 逻辑核心，处理受重力驱动的物理模拟与实体合并。
- **Yugo.Game**: 基于 MonoGame 和 Dear ImGui 的跨平台桌面 App（支持 Win/Mac）。
- **Yugo.Server**: ASP.NET Core 后端，提供用户鉴权、关卡存储及 App 远程控制桥接。
- **web**: 基于 React + TailwindCSS v4 的社区前端，可直接在网页端遥控桌面 App 游玩。

## 快速开始

### 准备环境
- 安装 [.NET 10.0 SDK](https://dotnet.microsoft.com/)
- 安装 [Node.js](https://nodejs.org/)

### 启动项目
1. **启动后端服务**: `dotnet run --project Yugo.Server` (默认运行在 http://localhost:5123)
2. **启动 Web 前端**: `cd web && npm install && npm run dev`
3. **启动游戏 App**: `dotnet run --project Yugo.Game`

### 自动化发布
推送 `v*` 格式的 Git Tag 将触发 GitHub Actions 自动构建：
- **Windows**: 生成 Inno Setup 安装程序。
- **macOS**: 生成带签名的 `.dmg` 磁盘镜像。

## 编辑器交互 (Object Mode)
- **选择**: 在网格或侧边栏列表选中实体。
- **绘图**: 选中后，**左键**增加方块，**右键**修剪。
- **属性**: 实时调整 Cluster ID 或活塞轴向。

## 开发规范
- C# 代码遵循 `csharpier` 格式。
- TypeScript 代码遵循 `prettier` 格式。
- 提交前必须通过 pre-commit 钩子检查（`pre-commit install`）。
