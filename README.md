# Yugo

Yugo 是一款基于 **C# (.NET 10.0)** 和 **MonoGame** 开发的现代网格解谜游戏引擎。它结合了重力驱动物理、递归推挤传播以及基于颜色簇（Cluster）的实体系统。

## 核心特性

- **确定性模拟**：逻辑与渲染完全分离，确保在 Windows 和 macOS 上表现高度一致。
- **对象化编辑器 (Object Mode)**：
  - **双栏布局**：左侧属性检查器 (Inspector)，右侧实体列表 (Entity List)。
  - **动态编辑**：选中实体后，可自由增删其方块，支持非连通分支。
  - **类型转换**：实时将实体转换为 Wall, Movable 或 Piston。
- **活塞 (Piston) 机制**：支持轴向锁定，且被推挤时能自动从边界“生长”出方块。
- **高品质视觉**：
  - 自定义 ImGui 渲染器，支持像素对齐的高清晰度 UI。
  - 灵敏的高亮反馈：交互目标颜色实时变浅。
  - 原生图标支持：包含 32-bit 透明窗口图标。

## 快速开始

### 安装环境
- 安装 [.NET 10.0 SDK](https://dotnet.microsoft.com/)
- Windows: 推荐安装 [Inno Setup 6](https://jrsoftware.org/isdl.php) 以支持安装包构建。
- macOS: 推荐安装 `brew install create-dmg`。

### 运行
- **启动游戏**: `dotnet run --project src/Yugo.Game`
- **运行测试**: `dotnet test`

## 编辑器操作

- **选中对象**: 在右侧列表点击，或在网格上直接点击实体。
- **编辑方块**: 选中对象后，**左键**添加，**右键**删除。
- **属性调整**: 在左侧面板修改 Cluster ID (1-5) 或活塞轴向。
- **保存**: 点击 `SAVE*` 按钮或按 `S`。

## 发布与分发

项目通过 GitHub Actions 实现全自动发布：
- **Windows**: 生成标准的 `Yugo-Setup.exe` 安装程序。
- **macOS**: 生成带签名的 `Yugo-MacOS.dmg` 磁盘镜像。

## 常见问题 (FAQ)

### macOS 运行提示“已损坏”？
这是由于应用未进行商业签名。请执行：
```bash
xattr -cr /path/to/Yugo.app
```
或在“系统设置 -> 隐私与安全性”中点击“仍要打开”。

### 为什么文字有些模糊？
请确保窗口处于 1:1 分辨率且没有应用系统级的非整数倍缩放。项目已实现像素对齐以最大程度保证清晰度。