using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Baum2
{
    /// <summary>
    /// PS图层命名(Baum2 snake_case) 到 Unity GUI命名(PascalCase) 的归一化工具
    ///
    /// 转换规则：
    ///   PS层名格式: ui_[type]_[system/module]_[semantic]_[state]@[layout]
    ///   归一化结果: {GUIPrefix}_{SystemSemantic}
    ///
    /// 示例：
    ///   "ui_btn_task_claim_normal@center"  → "Btn_TaskClaim"
    ///   "ui_panel_reward_content@stretchxy" → "Panel_RewardContent"
    ///   "ui_txt_title@center"               → "Txt_Title"
    ///   "ui_glg_bag_item"                   → "Grid_BagItem"
    ///   "ui_cell_reward_item"               → "Cell_RewardItem"
    ///   "ui_slot_equip_weapon"              → "Slot_EquipWeapon"
    ///   "ui_avatar_chat_sender"             → "Avatar_ChatSender"
    ///
    /// 规则来源：GUI命名规范_Baum2_PS转Unity结合规范_v1.0 / gui_naming_standard_html5_v2_9
    /// </summary>
    public static class BaumNameNormalizer
    {
        /// <summary>
        /// Baum2前缀(不含ui_) 到 GUI PascalCase前缀 的映射
        /// Key使用小写便于查找
        /// </summary>
        private static readonly Dictionary<string, string> PrefixMap = new Dictionary<string, string>
        {
            // 控件类
            { "btn",  "Btn" },
            { "txt",  "Txt" },
            { "img",  "Img" },
            { "icon", "Icon" },
            { "input","Input" },
            { "slider","Slider" },
            { "tog",  "Toggle" },
            { "drop", "Dropdown" },
            { "scroll","Scroll" },
            { "tab",  "Tab" },
            { "badge","Badge" },
            { "progress","Bar" },
            { "check","Toggle" },
            { "radio","Toggle" },

            // 面板容器类
            { "panel",  "Panel" },
            { "bg",     "Img" },
            { "mask",   "Mask" },
            { "divider","Divider" },
            { "frame",  "Frame" },
            { "card",   "Panel" },
            { "popup",  "Popup" },
            { "toast",  "Toast" },
            { "loading","Loading" },
            { "item",   "Item" },
            { "header", "Panel" },
            { "footer", "Panel" },
            { "arrow",  "Img" },
            { "close",  "Btn" },
            { "handle", "Img" },
            { "thumb",  "Img" },

            // 布局组件类
            { "glg", "Grid" },
            { "vlg", "Group" },
            { "hlg", "Group" },
            { "cg",  "Group" },

            // 扩展控件（v2.9 新增）
            { "cell",   "Cell" },
            { "slot",   "Slot" },
            { "reddot", "RedDot" },
            { "state",  "State" },
            { "effect", "Effect" },
            { "anim",   "Anim" },
            { "avatar", "Avatar" },
        };

        /// <summary>
        /// 已知状态后缀集合。用于剥离状态后缀。
        /// </summary>
        private static readonly HashSet<string> KnownStates = new HashSet<string>
        {
            "normal", "hover", "pressed", "disabled", "selected",
            "highlighted", "inactive", "empty", "filled", "locked",
            "completed", "claimed", "new", "claimable", "unread",
            "insufficient", "equipped", "loading"
        };

        /// <summary>
        /// 将 PS 图层名归一化为 Unity GUI 节点名。
        /// 剥离 @layout 参数、状态后缀，转换 PascalCase。
        /// </summary>
        /// <param name="rawName">PS 图层原始名称，如 "ui_btn_task_claim_normal@center"</param>
        /// <returns>归一化后的 GUI 名称，如 "Btn_TaskClaim"</returns>
        public static string Normalize(string rawName)
        {
            if (string.IsNullOrEmpty(rawName))
                return rawName;

            // 1. 剥离 @layout 参数（@center / @stretchxy 等）
            string withoutLayout = rawName.Split('@')[0].Trim();

            // 2. 剥离 ui_ 前缀
            if (!withoutLayout.StartsWith("ui_"))
                return rawName;

            string rest = withoutLayout.Substring(3); // 去掉 "ui_"

            // 3. 按 _ 切分
            string[] parts = rest.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return rawName;

            // 4. 第一部分是控件类型
            string controlType = parts[0].ToLowerInvariant();
            string guiPrefix;
            if (!PrefixMap.TryGetValue(controlType, out guiPrefix))
            {
                // 未知前缀：保留原始名作为兜底
                return rawName;
            }

            // 5. 检查最后一部分是否为已知状态后缀
            int semanticEndIdx = parts.Length;
            if (parts.Length > 1)
            {
                string lastPart = parts[parts.Length - 1].ToLowerInvariant();
                if (KnownStates.Contains(lastPart))
                {
                    semanticEndIdx = parts.Length - 1; // 排除状态后缀
                }
            }

            // 6. 收集中间部分（system/module + semantic），转换为 PascalCase
            var semanticParts = new List<string>();
            for (int i = 1; i < semanticEndIdx; i++)
            {
                string word = parts[i];
                if (!string.IsNullOrEmpty(word))
                {
                    semanticParts.Add(CapitalizeFirst(word));
                }
            }

            string pascalSemantic = string.Join("", semanticParts);

            // 7. 组合最终名称
            if (string.IsNullOrEmpty(pascalSemantic))
            {
                // 仅有前缀没有语义（如 "ui_btn_" → "Btn"），保留原始名
                return rawName;
            }

            return guiPrefix + "_" + pascalSemantic;
        }

        /// <summary>
        /// 批量归一化：将一段包含路径的完整名称也尝试归一化。
        /// 保持路径分隔符不变，仅对最后一个路径段执行 Normalize。
        /// </summary>
        public static string NormalizePath(string rawPath)
        {
            if (string.IsNullOrEmpty(rawPath))
                return rawPath;

            // 按常见分隔符切分最后一段
            char[] separators = { '/', '\\' };
            int lastSep = rawPath.LastIndexOfAny(separators);
            if (lastSep < 0)
                return Normalize(rawPath);

            string prefix = rawPath.Substring(0, lastSep + 1);
            string lastName = rawPath.Substring(lastSep + 1);
            return prefix + Normalize(lastName);
        }

        private static string CapitalizeFirst(string word)
        {
            if (string.IsNullOrEmpty(word))
                return word;
            if (word.Length == 1)
                return word.ToUpperInvariant();
            return char.ToUpperInvariant(word[0]) + word.Substring(1).ToLowerInvariant();
        }
    }
}
