# Baum2 - PS 到 Unity UI 工作流

## 版本
- Baum2.unitypackage: v2.0 (适配 ui_ 前缀体系)
- Photoshop Script: v0.7.0

## 功能
- 将 Photoshop PSD 文件转换为 Unity UI 预制体
- 支持完整的 UI 命名规范
- 自动识别控件类型和状态
- 生成优化后的图片资源

## 安装
1. **Photoshop 脚本**：将 `PhotoshopScript/Baum.js` 复制到 Photoshop 脚本目录
2. **Unity 插件**：导入 `Baum2.unitypackage`

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
