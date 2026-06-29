# UI 资源从 PS 到 Unity 完整工作流

## 1. Photoshop 设计阶段

### 1.1 图层命名规范
- **控件前缀**：`ui_btn_`, `ui_txt_`, `ui_img_`, `ui_icon_`, `ui_input_`, `ui_slider_`, `ui_tog_`, `ui_drop_`, `ui_scroll_`, `ui_tab_`, `ui_badge_`, `ui_progress_`, `ui_check_`, `ui_radio_`, `ui_cell_`, `ui_slot_`, `ui_reddot_`, `ui_state_`, `ui_effect_`, `ui_anim_`, `ui_avatar_`
- **面板容器**：`ui_panel_`, `ui_bg_`, `ui_mask_`, `ui_divider_`, `ui_frame_`, `ui_card_`, `ui_popup_`, `ui_toast_`, `ui_loading_`, `ui_item_`, `ui_header_`, `ui_footer_`, `ui_arrow_`, `ui_close_`, `ui_handle_`, `ui_thumb_`
- **状态后缀**：`_normal`, `_hover`, `_pressed`, `_disabled`, `_selected`, `_highlighted`, `_inactive`, `_empty`, `_filled`, `_locked`, `_completed`, `_claimed`, `_new`, `_claimable`, `_unread`, `_insufficient`, `_equipped`, `_loading`
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

### 3.3 生成结构（自动归一化后）
```
UI_{模块名}_Panel (Prefab)
├── Canvas
│   ├── Panel_Main
│   │   ├── Img_BgMain
│   │   ├── Panel_Header
│   │   │   ├── Txt_Title
│   │   │   └── Btn_Close
│   │   └── Panel_Content
│   └── ...
```

> **v1.0 自动命名归一化**：Baum2 导入器会自动将 PS 图层名转换为 GUI 命名规范。
> - `ui_btn_task_claim_normal@center` → `Btn_TaskClaim`
> - `ui_panel_reward_content@stretchxy` → `Panel_RewardContent`
> - `ui_cell_reward_item` → `Cell_RewardItem`

## 4. 命名映射表

### 4.1 PS 前缀到 Unity 组件

| PS 图层前缀 | Unity 组件 | 说明 |
|------------|------------|------|
| `ui_btn_` | Button | 按钮 |
| `ui_txt_` | Text / TMP | 文本 |
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
| `ui_cell_` | Image (Cell) | 网格格子 |
| `ui_slot_` | Image (Slot) | 固定槽位 |
| `ui_reddot_` | Image (RedDot) | 红点角标 |
| `ui_state_` | Image (State) | 状态指示 |
| `ui_effect_` | Image (Effect) | 特效占位 |
| `ui_anim_` | Image (Anim) | 动画占位 |
| `ui_avatar_` | Image (Avatar) | 头像 |

### 4.2 Baum2前缀到GUI节点名前缀（自动归一化）

| PS 前缀 | GUI 前缀 | PS 前缀 | GUI 前缀 |
|---------|---------|---------|---------|
| `ui_btn_` | `Btn_` | `ui_badge_` | `Badge_` |
| `ui_txt_` | `Txt_` | `ui_progress_` | `Bar_` |
| `ui_img_` | `Img_` | `ui_panel_` | `Panel_` |
| `ui_icon_` | `Icon_` | `ui_popup_` | `Popup_` |
| `ui_input_` | `Input_` | `ui_toast_` | `Toast_` |
| `ui_slider_` | `Slider_` | `ui_mask_` | `Mask_` |
| `ui_tog_` | `Toggle_` | `ui_frame_` | `Frame_` |
| `ui_drop_` | `Dropdown_` | `ui_glg_` | `Grid_` |
| `ui_scroll_` | `Scroll_` | `ui_vlg_` | `Group_` |
| `ui_tab_` | `Tab_` | `ui_hlg_` | `Group_` |
| `ui_cell_` | `Cell_` | `ui_cg_` | `Group_` |
| `ui_slot_` | `Slot_` | `ui_reddot_` | `RedDot_` |
| `ui_state_` | `State_` | `ui_effect_` | `Effect_` |
| `ui_anim_` | `Anim_` | `ui_avatar_` | `Avatar_` |

## 5. 状态管理

### 5.1 状态后缀识别
| PS后缀 | Unity状态 | PS后缀 | Unity状态 |
|--------|----------|--------|----------|
| `_normal` | normal | `_disabled` | disabled |
| `_hover` | hover | `_selected` | selected |
| `_pressed` | pressed | `_highlighted` | highlighted |
| `_locked` | locked | `_loading` | loading |
| `_empty` | empty | `_filled` | filled |
| `_completed` | completed | `_claimed` | claimed |
| `_claimable` | claimable | `_unread` | unread |
| `_insufficient` | insufficient | `_equipped` | equipped |

### 5.2 状态后缀在归一化中的处理
- PS 图层名中的状态后缀（如 `_normal`、`_selected`）在归一化时被剥离，不进入最终 Unity 节点名
- 状态信息通过 Baum2 的 `state` 字段传递给 Unity，用于代码中状态切换

## 6. 代码变更摘要

### 6.1 BaumNameNormalizer.cs（新增）
- 核心归一化工具类，负责将 PS 图层命名（`ui_btn_task_claim_normal@center`）转换为 GUI 命名（`Btn_TaskClaim`）
- 包含前缀映射表（34个Baum2前缀→31个GUI前缀）和状态后缀集合（18个）
- 提供 `Normalize(string)` 和 `NormalizePath(string)` 两类接口

### 6.2 BaumElements.cs（修改）
- Element 基类新增 `nodeName` 字段，存储归一化后的 GameObject 名称
- `CreateUIGameObject` 改用 `nodeName` 作为 GameObject 名称
- 保留原始 `name` 字段用于图片资源查找

### 6.3 BaumPrefabCreator.cs（修改）
- 新增 `NormalizeHierarchyNames` 递归方法，作为兜底确保所有节点名被归一化

### 6.4 Baum.js（修改）
- `groupToHash` 新增识别 `ui_cell_` / `ui_slot_` / `ui_reddot_` / `ui_state_` / `ui_effect_` / `ui_anim_` / `ui_avatar_` 前缀
- 状态后缀列表扩展 `_claimable` / `_unread` / `_insufficient` / `_equipped` / `_loading`
- `layerToImageNameLoop` 状态后缀同步扩展

### 6.5 BaumMapping.json（修改）
- 新增 7 个前缀映射（cell/slot/reddot/state/effect/anim/avatar）
- 新增 5 个状态后缀（claimable/unread/insufficient/equipped/loading）
- 新增 `gui_prefix_map` 节，定义 PS 前缀到 GUI 前缀的映射

## 7. 最佳实践

### 7.1 PS 设计
- 使用智能对象保持矢量
- 按功能模块分组
- 命名清晰规范，使用 `ui_[type]_[system]_[semantic]_[state]@[layout]` 格式
- 使用状态后缀区分不同状态

### 7.2 Unity 优化
- 合并图集减少 DrawCall
- 使用九宫格拉伸减少资源大小
- 合理使用 Canvas 层级
- 生成后检查所有节点名是否符合 GUI 命名规范

### 7.3 版本控制
- PS 源文件：`.psd`
- 导出资源：`.layout.txt` + `.png`
- Unity 预制体：`.prefab`
