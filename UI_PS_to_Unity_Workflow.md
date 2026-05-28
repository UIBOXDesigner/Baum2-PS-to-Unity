# UI 资源从 PS 到 Unity 完整工作流

## 1. Photoshop 设计阶段

### 1.1 图层命名规范
- **控件前缀**：`ui_btn_`, `ui_txt_`, `ui_img_`, `ui_icon_`, `ui_input_`, `ui_slider_`, `ui_tog_`, `ui_drop_`, `ui_scroll_`, `ui_tab_`, `ui_badge_`, `ui_progress_`, `ui_check_`, `ui_radio_`
- **面板容器**：`ui_panel_`, `ui_bg_`, `ui_mask_`, `ui_divider_`, `ui_frame_`, `ui_card_`, `ui_popup_`, `ui_toast_`, `ui_loading_`, `ui_item_`, `ui_header_`, `ui_footer_`, `ui_arrow_`, `ui_close_`, `ui_handle_`, `ui_thumb_`
- **状态后缀**：`_normal`, `_hover`, `_pressed`, `_disabled`, `_selected`, `_highlighted`, `_inactive`, `_empty`, `_filled`, `_locked`, `_completed`, `_claimed`, `_new`
- **布局组件**：`ui_glg_`, `ui_vlg_`, `ui_hlg_`, `ui_cg_`

### 1.2 参数语法
在图层名后添加 `@` 符号指定参数：
- `@center` - 锚点居中
- `@stretchx` - X轴拉伸
- `@stretchy` - Y轴拉伸
- `@stretchxy` - XY轴拉伸
- `@rot90` - 旋转90度
- `@flipx` - X轴翻转

示例：
- `ui_btn_ok_normal@center` → 按钮，锚点居中，普通状态
- `ui_img_bg@stretchxy` → 背景图，XY轴拉伸
- `ui_panel_popup@bottom` → 弹窗面板，锚点底部

### 1.3 特殊图层
- `#Canvas` - 定义画布区域
- `#` 开头 - 注释图层（不导出）
- `*` 开头 - 需要栅格化的图层

## 2. Baum 插件导出

### 2.1 安装与使用
1. 将 `Baum.js` 放入 Photoshop 脚本目录
2. 在 Photoshop 中：文件 → 脚本 → 浏览 → 选择 Baum.js
3. 选择保存目录

### 2.2 导出内容
插件会生成：
- `{文件名}.layout.txt` - UI 层级和属性 JSON
- `{文件名}/` 目录 - 所有图片资源 PNG

## 3. Unity 导入

### 3.1 Baum2.unitypackage 导入
1. 在 Unity 中：Assets → Import Package → Custom Package
2. 选择 `Baum2.unitypackage`
3. 导入所有文件

### 3.2 使用 Baum2 导入器
1. 将导出的 `{文件名}.layout.txt` 和 `{文件名}/` 目录复制到 Unity Assets 目录
2. 在 Unity 中：右键点击 `.layout.txt` → Baum2 → Generate UI
3. 自动生成：
   - Prefab：完整的 UI 预制体
   - Sprites：图片资源
   - Canvas：UI 画布

### 3.3 生成结构
```
UI_{模块名}_Panel (Prefab)
├── Canvas
│   ├── ui_panel_main
│   │   ├── ui_bg_main
│   │   ├── ui_header
│   │   │   ├── ui_txt_title
│   │   │   └── ui_btn_close
│   │   └── ui_content
│   └── ...
```

## 4. 命名映射表

| PS 图层前缀 | Unity 组件 | 说明 |
|------------|------------|------|
| `ui_btn_` | Button | 按钮 |
| `ui_txt_` | Text | 文本 |
| `ui_img_` | Image | 图片 |
| `ui_icon_` | Image | 图标 |
| `ui_input_` | InputField | 输入框 |
| `ui_slider_` | Slider | 滑动条 |
| `ui_tog_` | Toggle | 开关 |
| `ui_drop_` | Dropdown | 下拉框 |
| `ui_scroll_` | ScrollRect | 滚动区域 |
| `ui_tab_` | ToggleGroup | 标签页 |
| `ui_panel_` | Panel | 面板 |
| `ui_bg_` | Image | 背景图 |
| `ui_mask_` | Mask | 遮罩 |
| `ui_glg_` | GridLayoutGroup | 网格布局 |
| `ui_vlg_` | VerticalLayoutGroup | 垂直布局 |
| `ui_hlg_` | HorizontalLayoutGroup | 水平布局 |
| `ui_cg_` | CanvasGroup | 画布组 |

## 5. 状态管理

### 5.1 状态后缀识别
- `_normal` → state: "normal"
- `_hover` → state: "hover"
- `_pressed` → state: "pressed"
- `_disabled` → state: "disabled"
- `_selected` → state: "selected"

### 5.2 Unity 中状态切换
Baum2 会为每个状态生成对应的 Sprite，可通过代码控制：
```csharp
// 获取按钮组件
Button btn = GetComponent<Button>();
Image btnImage = btn.GetComponent<Image>();

// 切换状态
btnImage.sprite = Resources.Load<Sprite>("ui_btn_ok_normal");
btnImage.sprite = Resources.Load<Sprite>("ui_btn_ok_pressed");
```

## 6. 最佳实践

### 6.1 PS 设计
- 使用智能对象保持矢量
- 按功能模块分组
- 命名清晰规范
- 使用状态后缀区分不同状态

### 6.2 Unity 优化
- 合并图集减少 DrawCall
- 使用九宫格拉伸减少资源大小
- 合理使用 Canvas 层级
- 状态切换使用 Sprite 替换而非颜色变化

### 6.3 版本控制
- PS 源文件：`.psd`
- 导出资源：`.layout.txt` + `.png`
- Unity 预制体：`.prefab`
