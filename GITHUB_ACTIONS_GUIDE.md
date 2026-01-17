# GitHub Actions 自动构建和发布说明

## 功能概述

此 GitHub Actions workflow 会自动完成以下任务：

1. **编译解决方案** - 使用 .NET 8.0 编译整个解决方案
2. **打包单个 DLL** - 将每个项目的 DLL 单独压缩成 zip 文件
3. **创建完整包** - 创建包含所有 BepInEx 和 MelonLoader DLL 的完整包
4. **发布到 Release** - 自动创建 GitHub Release 并上传所有包

## 触发方式

### 方式一：通过 Tag 触发（推荐）

推送一个以 `v` 开头的 tag 到仓库：

```bash
git tag v1.0.0
git push origin v1.0.0
```

### 方式二：手动触发

1. 进入 GitHub 仓库的 Actions 页面
2. 选择 "Build and Release" workflow
3. 点击 "Run workflow" 按钮
4. 选择分支并点击运行

## 输出结构

Release 中会包含以下文件：

### BepInEx 插件（单个包）
```
BepInEx/
├── CustomizeLib.BepInEx.zip
├── ElectricPeaReborn.BepInEx.zip
├── HypnoSquash.BepInEx.zip
└── ... (其他 BepInEx 插件)
```

### MelonLoader 插件（单个包）
```
MelonLoader/
├── CustomizeLib.MelonLoader.zip
├── ElectricPeaReborn.MelonLoader.zip
└── ... (其他 MelonLoader 插件)
```

### 完整包
```
BepInEx-All.zip      - 包含所有 BepInEx 插件
MelonLoader-All.zip  - 包含所有 MelonLoader 插件
```

## Release 说明

每次发布会自动生成 Release Notes，包含：

- 版本号
- BepInEx 插件列表
- MelonLoader 插件列表
- 完整包下载链接

## 本地测试

在推送之前，可以在本地测试构建流程：

```powershell
# 恢复依赖
dotnet restore PVZRHCustomization.sln

# 编译解决方案
dotnet build PVZRHCustomization.sln --configuration Release

# 检查输出
Get-ChildItem -Path ./res/BepInEx -Filter *.dll
Get-ChildItem -Path ./res/MelonLoader/Mods -Filter *.dll
```

## 注意事项

1. **确保 res 目录存在** - workflow 假设编译后的 DLL 位于 `./res/BepInEx` 和 `./res/MelonLoader/Mods` 目录
2. **解决方案路径** - 确保主解决方案文件名为 `PVZRHCustomization.sln`
3. **.NET 版本** - 当前配置使用 .NET 8.0，如需修改请更新 workflow 中的 `DOTNET_VERSION` 环境变量
4. **GITHUB_TOKEN** - workflow 自动使用 GitHub 提供的 `GITHUB_TOKEN`，无需额外配置

## 自定义配置

### 修改 .NET 版本

编辑 `.github/workflows/build-and-release.yml`：

```yaml
env:
  DOTNET_VERSION: '8.0.x'  # 修改为所需版本
```

### 修改输出目录

编辑 workflow 中的 `Create output directories` 步骤：

```yaml
- name: Create output directories
  run: |
    New-Item -ItemType Directory -Force -Path ./your/custom/path
```

### 添加额外的构建步骤

在 `Build solution` 步骤之后添加：

```yaml
- name: Run tests
  run: dotnet test PVZRHCustomization.sln --configuration Release
```

## 故障排查

### 编译失败

检查解决方案是否能正常编译：

```bash
dotnet build PVZRHCustomization.sln --configuration Release
```

### DLL 未找到

确保编译后的 DLL 位于正确的目录：
- BepInEx: `./res/BepInEx/*.dll`
- MelonLoader: `./res/MelonLoader/Mods/*.dll`

### Release 创建失败

确保：
1. Tag 格式正确（以 `v` 开头，如 `v1.0.0`）
2. 仓库有创建 Release 的权限
3. `GITHUB_TOKEN` 有正确的权限（默认应该足够）

## 示例：发布新版本

```bash
# 1. 提交代码
git add .
git commit -m "Update plugins"
git push

# 2. 创建并推送 tag
git tag v3.0.1
git push origin v3.0.1

# 3. 等待 workflow 完成后，访问 GitHub Releases 页面下载
```

## 工作流程图

```
Push Tag / Manual Trigger
    ↓
Checkout Code
    ↓
Setup .NET 8.0
    ↓
Restore Dependencies
    ↓
Build Solution
    ↓
Create Output Directories
    ↓
Copy & Zip Individual DLLs
    ↓
Create Combined Packages
    ↓
Generate Release Notes
    ↓
Upload Artifacts
    ↓
Create GitHub Release
    ↓
Done! 🎉
```
