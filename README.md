# Yugo

Yugo 是一款基于 **C# (.NET 10.0)** 和 **MonoGame** 开发的现代网格解谜游戏引擎。它拥有独特的物理模拟系统，结合了重力驱动、递归推挤传播以及基于颜色簇（Cluster）的实体合并机制。

## 核心特性

- **确定性模拟引擎**：核心逻辑独立于渲染层，确保在不同平台上具有完全一致的物理表现。
- **专业级编辑器 (Object Mode)**：内置基于 **Dear ImGui** 的对象化编辑器：
  - **实体列表管理**：支持一键添加 Wall, Movable, Piston 等实体。
  - **属性检查器 (Inspector)**：实时修改选中实体的 Cluster ID, Axis (活塞方向) 等属性。
  - **动态形状编辑**：选中实体后，可自由在其非连通的分支上增删方格。
  - **安全缩放**：支持实时调整关卡尺寸，并具备带警告的清空确认逻辑。
- **活塞 (Piston) 机制**：特殊的机械实体，支持轴向锁定移动，且在被推挤时能自动从边界“生长”出新的单元格。
- **现代 UI 交互**：
  - 高性能自定义 ImGui 渲染器，支持高清晰度文字和像素对齐。
  - 灵敏的颜色高亮反馈：鼠标悬停或拖拽实体时，颜色会即时变浅。
- **跨平台持久化**：自动记录并存储最近打开的关卡，适配 Windows (AppData) 和 macOS (Application Support)。

## 项目结构

- **`src/Yugo.Core`**: 逻辑核心。包含实体行为、重力逻辑和合并规则。
- **`src/Yugo.Game`**: 视觉表现层。包含 ImGui 渲染器、网格转换逻辑及屏幕管理。
- **`tests/Yugo.Core.Tests`**: 自动化逻辑测试，确保物理规则的稳定性。

## 快速开始

### 准备环境
- 安装 [.NET 10.0 SDK](https://dotnet.microsoft.com/)
- 确保系统已安装 MonoGame 依赖（如 SDL2 等）

### 初始化项目
1. **还原工具**: `dotnet tool restore`
2. **安装钩子**: `pre-commit install`
3. **还原包**: `dotnet restore Yugo.slnx`

### 运行与测试
- **启动游戏**: `dotnet run --project src/Yugo.Game`
- **运行测试**: `dotnet test` 或 `./scripts/test.sh`

## 编辑器操作指南

- **选择对象**: 在右侧列表中点击实体，或直接在网格上点击该实体。
- **编辑形状**: 选中对象后，**左键**点击空白处添加方格，**右键**点击已有方格进行修剪。
- **转换类型**: 在左侧 Properties 面板中切换 `Entity Type`，可实时将选中的墙变成色块或活塞。
- **快捷键**:
  - `S`: 快速保存（未保存时 SAVE 按钮会显示星号 `*`）
  - `Esc`: 退出至主菜单

## 发布与部署

项目支持全自动化的跨平台发布：

### 自动化发布 (CI/CD)
每当推送以 `v` 开头的 Git Tag（如 `v1.0.0`）时，GitHub Actions 会自动构建 Windows 和 macOS 的自包含包并发布到 GitHub Release。

### 本地手动发布
- **Windows**: 运行 `./scripts/publish_win.sh`，产物位于 `publish/windows/`。
- **macOS**: 运行 `./scripts/publish_mac.sh`，生成原生的 `Yugo.app`，位于 `publish/mac/`。

## 开发规范
- **代码格式化**: 必须使用 `csharpier`（提交时会自动运行）。
- **逻辑分层**: 所有玩法机制必须写在 `Yugo.Core` 中，严禁在 `Yugo.Game` 中编写物理逻辑。
