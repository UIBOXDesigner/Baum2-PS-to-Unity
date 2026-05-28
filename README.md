# Baum2 - PS 到 Unity UI 工作流

## 版本
- Baum2.unitypackage: v2.0 (适配 ui_ 前缀体系)
- Photoshop Script: v0.7.0

## 功能
- 将 Photoshop PSD 文件转换为 Unity UI 预制体
- 支持完整的 UI 命名规范
- 自动识别控件类型和状态
- 生成优化后的图片资源

## 项目结构

```
PSD转U3D插件/
├── install.bat                   # 一键安装脚本（双击即用）
├── 安装说明.txt                   # 安装说明（简体中文）
├── PhotoshopPlugin/              # Photoshop 插件安装包
│   ├── Installation_Guide.md     # 详细安装指南
│   ├── manifest.json             # 插件配置文件
│   ├── icon.png                  # 插件图标
│   └── version.txt               # 版本信息
├── PhotoshopScript/              # Photoshop 脚本源码
│   ├── Baum.js                   # 主导出脚本（ES5）
│   ├── Baum.coffee               # 主脚本源文件（CoffeeScript）
│   └── BaumMapping.json          # 控件类型映射配置
├── Baum2.unitypackage            # Unity 侧插件包
├── BaumElements.cs               # Unity UI 元素生成脚本
├── BaumPrefabCreator.cs          # Unity 预制体创建脚本
├── UI_PS_to_Unity_Workflow.md    # PS → Unity 完整工作流文档
└── README.md                     # 本文件
```

## 安装

### 一键安装（最简单）

双击运行项目根目录下的 `install.bat`，脚本将自动检测 Photoshop 安装路径并复制插件文件，无需手动操作。

> 如遇「权限不足」提示，请右键 `install.bat` → 以管理员身份运行。
> 详细安装说明请参阅 `安装说明.txt`。

### Photoshop 插件（手动安装）

将 `PhotoshopPlugin/` 目录下所有文件复制到 Photoshop 脚本目录：

**Windows:**
```
C:\Program Files\Adobe\Adobe Photoshop 202X\Presets\Scripts\
```

**macOS:**
```
/Applications/Adobe Photoshop 202X/Presets/Scripts/
```

重启 Photoshop 后，在 `文件 → 脚本` 菜单中即可找到 Baum.js。

> 详细安装说明请参阅 [`PhotoshopPlugin/Installation_Guide.md`](<PhotoshopPlugin/Installation_Guide.md>)

### 快速运行（无需安装）

1. 打开 PSD 文件
2. `文件 → 脚本 → 浏览`，选择 `PhotoshopScript/Baum.js`
3. 选择导出目录即可

### Unity 插件

1. 打开 Unity 项目
2. `Assets → Import Package → Custom Package`
3. 选择 `Baum2.unitypackage`
4. 点击 `Import`

## 使用流程
1. 在 PS 中设计 UI，按规范命名图层
2. 运行 Baum.js 脚本导出资源
3. 将导出文件复制到 Unity Assets
4. 在 Unity 中右键 .layout.txt → Baum2 → Generate UI

## 命名规范
详见 `UI_PS_to_Unity_Workflow.md`

## 更新日志
### v2.0 (2026-05-28)
- 从 `m_` 前缀迁移到 `ui_` 前缀体系
- 支持 30+ 控件类型
- 支持 15+ 状态后缀
- 完整的 PS → Unity 工作流文档
