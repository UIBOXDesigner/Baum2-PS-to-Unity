using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using DG.Tweening;
using System.Collections.Generic;
using System.Linq;

namespace UIJuice.Editor
{
    public class UIJuiceToolWindow : EditorWindow
    {
        private enum ToolMode { ButtonControl, CallAnimFile, CommonEffect }
        private ToolMode _toolMode = ToolMode.CommonEffect;

        private const string DefaultSettingsAssetPath = "Assets/Editor/UIJuiceSettings.asset";
        private const string LastSettingsPathPrefsKey = "UIJuice.Editor.UIJuiceToolWindow.LastSettingsPath";

        private UIJuiceSettings _settings;
        private string _settingsAssetPath = DefaultSettingsAssetPath;
        private string _searchText = "";
        private int _selectedRuleGroupIndex = 0;
        private string _ruleText = "";
        
        private Vector2 _mainScroll;
        private Vector2 _tagScroll;

        private bool _isAddingGroup = false;
        private string _newGroupName = "";
        private int _renamingGroupIndex = -1;
        private string _renameText = "";

        [MenuItem("Tools/Jimmy专用/UI 动效/动画规则工具")]
        public static void ShowWindow()
        {
            var window = GetWindow<UIJuiceToolWindow>("Animation Rule Tool");
            window.minSize = new Vector2(380, 500); 
        }

        private void OnEnable()
        {
            LoadLastOrDefaultSettingsAsset();
            Undo.undoRedoPerformed += Repaint;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= Repaint;
        }

        private void LoadLastOrDefaultSettingsAsset()
        {
            string lastPath = EditorPrefs.GetString(LastSettingsPathPrefsKey, DefaultSettingsAssetPath);
            _settings = AssetDatabase.LoadAssetAtPath<UIJuiceSettings>(lastPath);

            if (_settings == null && lastPath != DefaultSettingsAssetPath)
            {
                _settings = AssetDatabase.LoadAssetAtPath<UIJuiceSettings>(DefaultSettingsAssetPath);
                lastPath = DefaultSettingsAssetPath;
            }

            if (_settings == null)
            {
                _settings = CreateSettingsAsset(DefaultSettingsAssetPath);
                lastPath = DefaultSettingsAssetPath;
            }

            _settingsAssetPath = AssetDatabase.GetAssetPath(_settings);
            if (string.IsNullOrEmpty(_settingsAssetPath)) _settingsAssetPath = lastPath;
            EditorPrefs.SetString(LastSettingsPathPrefsKey, _settingsAssetPath);

            NormalizeSettingsData(true);
        }

        private UIJuiceSettings CreateSettingsAsset(string assetPath)
        {
            EnsureFolderExists(assetPath);

            var asset = ScriptableObject.CreateInstance<UIJuiceSettings>();
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return asset;
        }

        private void EnsureFolderExists(string assetPath)
        {
            string folderPath = System.IO.Path.GetDirectoryName(assetPath);
            if (string.IsNullOrEmpty(folderPath)) return;
            folderPath = folderPath.Replace("\\", "/");

            string current = "Assets";
            string[] parts = folderPath.Split('/');
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }

        private void NormalizeSettingsData(bool saveAfterNormalize)
        {
            if (_settings == null) return;

            bool changed = false;

            if (_settings.tagGroups == null)
            {
                _settings.tagGroups = new List<TagGroup>();
                changed = true;
            }

            if (_settings.buttonTags != null && _settings.buttonTags.Count > 0)
            {
                _settings.tagGroups.Add(new TagGroup { groupName = "按钮动画", tags = new List<AnimationTag>(_settings.buttonTags) });
                _settings.buttonTags.Clear();
                changed = true;
            }
            if (_settings.popupTags != null && _settings.popupTags.Count > 0)
            {
                _settings.tagGroups.Add(new TagGroup { groupName = "弹窗动画", tags = new List<AnimationTag>(_settings.popupTags) });
                _settings.popupTags.Clear();
                changed = true;
            }

            if (_settings.tagGroups.Count == 0)
            {
                _settings.tagGroups.Add(new TagGroup { groupName = "弹窗动画", tags = new List<AnimationTag>() });
                _settings.tagGroups.Add(new TagGroup { groupName = "按钮动画", tags = new List<AnimationTag>() });
                changed = true;
            }

            foreach (var group in _settings.tagGroups)
            {
                if (group.tags == null)
                {
                    group.tags = new List<AnimationTag>();
                    changed = true;
                }
            }

            if (changed && saveAfterNormalize)
            {
                EditorUtility.SetDirty(_settings);
                AssetDatabase.SaveAssets();
            }
        }

        private void SwitchSettingsAsset(UIJuiceSettings newSettings)
        {
            if (newSettings == null)
            {
                EditorUtility.DisplayDialog("配置无效", "请选择 UIJuiceSettings 类型的 .asset 配置文件。", "确定");
                return;
            }

            _settings = newSettings;
            _settingsAssetPath = AssetDatabase.GetAssetPath(_settings);
            EditorPrefs.SetString(LastSettingsPathPrefsKey, _settingsAssetPath);

            _selectedRuleGroupIndex = 0;
            _searchText = "";
            _ruleText = "";
            _isAddingGroup = false;
            _renamingGroupIndex = -1;

            NormalizeSettingsData(true);
            Repaint();
        }

        private void CreateNewSettingsAssetByPanel()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "新建 UIJuice 配置",
                "UIJuiceSettings",
                "asset",
                "请选择新配置文件保存位置。建议放在 Assets/Editor 或项目自己的 Editor 配置目录下。"
            );

            if (string.IsNullOrEmpty(path)) return;

            var existed = AssetDatabase.LoadAssetAtPath<UIJuiceSettings>(path);
            if (existed != null)
            {
                SwitchSettingsAsset(existed);
                return;
            }

            var newSettings = CreateSettingsAsset(path);
            SwitchSettingsAsset(newSettings);
        }

        private void LoadSettingsAssetByPanel()
        {
            string absolutePath = EditorUtility.OpenFilePanel("加载 UIJuiceSettings 配置", Application.dataPath, "asset");
            if (string.IsNullOrEmpty(absolutePath)) return;

            string assetPath = AbsolutePathToAssetPath(absolutePath);
            if (string.IsNullOrEmpty(assetPath))
            {
                EditorUtility.DisplayDialog("无法加载配置", "Unity 的 .asset 配置必须位于当前项目的 Assets 目录内。请先把配置文件放进项目 Assets 目录。", "确定");
                return;
            }

            AssetDatabase.ImportAsset(assetPath);
            var loadedSettings = AssetDatabase.LoadAssetAtPath<UIJuiceSettings>(assetPath);
            if (loadedSettings == null)
            {
                EditorUtility.DisplayDialog("配置类型不匹配", "选择的 .asset 不是 UIJuiceSettings 类型，不能作为该工具的配置文件。", "确定");
                return;
            }

            SwitchSettingsAsset(loadedSettings);
        }

        private string AbsolutePathToAssetPath(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath)) return string.Empty;

            absolutePath = absolutePath.Replace("\\", "/");
            string dataPath = Application.dataPath.Replace("\\", "/");
            if (!absolutePath.StartsWith(dataPath)) return string.Empty;

            return "Assets" + absolutePath.Substring(dataPath.Length);
        }

        private void OnGUI()
        {
            if (_settings == null) return;
            EditorGUI.BeginChangeCheck();

            _mainScroll = EditorGUILayout.BeginScrollView(_mainScroll);

            DrawTopBar();
            EditorGUILayout.Space(5);

            DrawConfigAssetSection();
            EditorGUILayout.Space(5);

            DrawTagsSection();
            EditorGUILayout.Space(10);

            DrawSettingsSection();
            EditorGUILayout.Space(10);

            DrawRuleSection();
            EditorGUILayout.Space(15);

            EditorGUILayout.EndScrollView();

            DrawBottomButtons();

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(_settings);
            }
        }

        private void DrawTopBar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label(" 🔍 可用命名规则", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            _searchText = EditorGUILayout.TextField(_searchText, EditorStyles.toolbarSearchField, GUILayout.Width(200));
            if (GUILayout.Button("清空", EditorStyles.toolbarButton, GUILayout.Width(50))) 
            { 
                _searchText = ""; 
                GUI.FocusControl(null); 
            }
            GUILayout.EndHorizontal();
        }

        private void DrawConfigAssetSection()
        {
            EditorGUILayout.BeginVertical("helpbox");
            GUILayout.Label("📦 配置文件 (.asset)", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            var selectedSettings = (UIJuiceSettings)EditorGUILayout.ObjectField("当前配置", _settings, typeof(UIJuiceSettings), false);
            if (EditorGUI.EndChangeCheck())
            {
                if (selectedSettings != null && selectedSettings != _settings)
                {
                    SwitchSettingsAsset(selectedSettings);
                    GUIUtility.ExitGUI();
                }
                else if (selectedSettings == null)
                {
                    EditorUtility.DisplayDialog("不能清空配置", "工具必须绑定一个 UIJuiceSettings .asset 配置文件。可以点击【新建配置】创建新文件。", "确定");
                }
            }

            if (GUILayout.Button("定位", GUILayout.Width(50)) && _settings != null)
            {
                Selection.activeObject = _settings;
                EditorGUIUtility.PingObject(_settings);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("路径", string.IsNullOrEmpty(_settingsAssetPath) ? "未保存的配置" : _settingsAssetPath, EditorStyles.miniLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("加载配置", EditorStyles.miniButtonLeft))
            {
                LoadSettingsAssetByPanel();
                GUIUtility.ExitGUI();
            }
            if (GUILayout.Button("新建配置", EditorStyles.miniButtonMid))
            {
                CreateNewSettingsAssetByPanel();
                GUIUtility.ExitGUI();
            }
            if (GUILayout.Button("保存配置", EditorStyles.miniButtonMid))
            {
                if (_settings != null)
                {
                    EditorUtility.SetDirty(_settings);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
            }
            if (GUILayout.Button("重载配置", EditorStyles.miniButtonRight))
            {
                if (!string.IsNullOrEmpty(_settingsAssetPath))
                {
                    AssetDatabase.ImportAsset(_settingsAssetPath);
                    SwitchSettingsAsset(AssetDatabase.LoadAssetAtPath<UIJuiceSettings>(_settingsAssetPath));
                    GUIUtility.ExitGUI();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox("支持两种加载方式：点击【加载配置】选择项目 Assets 目录内的 .asset，或直接把 UIJuiceSettings 类型 .asset 拖到【当前配置】字段。切换后会记住上次使用的配置路径。", MessageType.Info);
            EditorGUILayout.EndVertical();
        }

        private void DrawTagsSection()
        {
            EditorGUILayout.BeginVertical("helpbox"); 

            _tagScroll = GUILayout.BeginScrollView(_tagScroll, GUILayout.MaxHeight(200));

            for (int i = 0; i < _settings.tagGroups.Count; i++)
            {
                var group = _settings.tagGroups[i];

                EditorGUILayout.BeginHorizontal();
                
                if (_renamingGroupIndex == i)
                {
                    _renameText = EditorGUILayout.TextField(_renameText, GUILayout.ExpandWidth(true));
                    if (GUILayout.Button("确认", EditorStyles.miniButton, GUILayout.Width(40)))
                    {
                        Undo.RecordObject(_settings, "Rename Group");
                        group.groupName = _renameText;
                        _renamingGroupIndex = -1;
                        GUI.FocusControl(null);
                    }
                    if (GUILayout.Button("取消", EditorStyles.miniButton, GUILayout.Width(40)))
                    {
                        _renamingGroupIndex = -1;
                        GUI.FocusControl(null);
                    }
                }
                else
                {
                    GUILayout.Label("📁 " + group.groupName, EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("重命名", EditorStyles.miniButton, GUILayout.Width(50)))
                    {
                        _renamingGroupIndex = i;
                        _renameText = group.groupName;
                    }

                    GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
                    if (GUILayout.Button("删除", EditorStyles.miniButton, GUILayout.Width(40)))
                    {
                        if (EditorUtility.DisplayDialog("删除分组", $"确定要删除分组【{group.groupName}】及其中所有的动画标签吗？\n(此操作不可撤销)", "确认删除", "取消"))
                        {
                            Undo.RecordObject(_settings, "Delete Group");
                            _settings.tagGroups.RemoveAt(i);
                            EditorGUILayout.EndHorizontal();
                            GUILayout.EndScrollView();
                            EditorGUILayout.EndVertical();
                            GUIUtility.ExitGUI(); 
                        }
                    }
                    GUI.backgroundColor = Color.white;
                }
                EditorGUILayout.EndHorizontal();

                if (group.tags == null) group.tags = new List<AnimationTag>();
                var filteredTags = group.tags.Where(t => t != null && (string.IsNullOrEmpty(_searchText) || (!string.IsNullOrEmpty(t.name) && t.name.ToLower().Contains(_searchText.ToLower())))).ToList();
                DrawTagGroupFlowLayout(group, filteredTags);

                EditorGUILayout.Space(8);
            }

            GUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawTagGroupFlowLayout(TagGroup group, List<AnimationTag> tags)
        {
            if (tags.Count == 0)
            {
                GUILayout.Label("   (暂无规则)", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            float windowWidth = EditorGUIUtility.currentViewWidth - 40f; 
            float currentLineWidth = 0f;

            GUILayout.BeginHorizontal();
            foreach (var tag in tags)
            {
                float tagWidth = 145f; 

                if (currentLineWidth + tagWidth > windowWidth && currentLineWidth > 0)
                {
                    GUILayout.FlexibleSpace(); 
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    currentLineWidth = 0f;
                }

                DrawSingleTag(group, tag);
                currentLineWidth += tagWidth;
            }
            GUILayout.FlexibleSpace(); 
            GUILayout.EndHorizontal();
        }

        private void DrawSingleTag(TagGroup group, AnimationTag tag)
        {
            var style = new GUIStyle(EditorStyles.miniButton) 
            { 
                alignment = TextAnchor.MiddleCenter, 
                margin = new RectOffset(2, 2, 2, 2), 
                padding = new RectOffset(5, 5, 2, 2) 
            };
            
            if (tag.isSelected)
            {
                style.normal.textColor = Color.white;
                style.normal.background = (Texture2D)EditorGUIUtility.Load("d_SelectionRect");
            }

            GUILayout.BeginHorizontal(GUILayout.Width(140)); 
            
            string prefix = "";
            if (tag.savedSettings != null)
            {
                switch (tag.savedSettings.toolMode)
                {
                    case 0: prefix = "[控件] "; break;
                    case 1: prefix = "[文件] "; break;
                    case 2: prefix = "[动效] "; break;
                }
            }
            
            string displayName = prefix + (string.IsNullOrEmpty(tag.name) ? "未命名规则" : tag.name);

            if (GUILayout.Toggle(tag.isSelected, displayName, style, GUILayout.ExpandWidth(true)))
            {
                if (!tag.isSelected) 
                {
                    foreach (var g in _settings.tagGroups)
                        foreach (var t in g.tags)
                            t.isSelected = false;

                    tag.isSelected = true;
                    LoadSettingsFromTag(tag);
                    GUI.FocusControl(null); 
                }
            }

            if (GUILayout.Button("x", EditorStyles.miniButton, GUILayout.Width(20)))
            {
                Undo.RecordObject(_settings, "Delete Tag");
                group.tags.Remove(tag);
                GUILayout.EndHorizontal();
                GUIUtility.ExitGUI(); 
            }
            GUILayout.EndHorizontal();
        }

        private void DrawSettingsSection()
        {
            EditorGUILayout.BeginVertical("helpbox");
            
            GUILayout.Label("⚙️ 动画模式与参数", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            GUILayout.BeginHorizontal();
            _toolMode = DrawRadioButton(_toolMode, ToolMode.ButtonControl, "🔘 按钮控件");
            _toolMode = DrawRadioButton(_toolMode, ToolMode.CallAnimFile, "🎬 调用动画文件");
            _toolMode = DrawRadioButton(_toolMode, ToolMode.CommonEffect, "✨ 普通动效");
            GUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            Undo.RecordObject(_settings, "Change UI Juice Settings");
            
            EditorGUI.indentLevel++;
            switch (_toolMode)
            {
                case ToolMode.ButtonControl:
                    GUILayout.Label("🔹 入场动画 (Enter)", EditorStyles.miniBoldLabel);
                    _settings.pro_enterDuration = EditorGUILayout.FloatField("出现耗时", _settings.pro_enterDuration);
                    _settings.pro_enterEase = (DG.Tweening.Ease)EditorGUILayout.EnumPopup("出现曲线", _settings.pro_enterEase);
                    
                    EditorGUILayout.Space(5);
                    GUILayout.Label("🔹 交互反馈 (Hover & Click)", EditorStyles.miniBoldLabel);
                    _settings.pro_hoverScale = EditorGUILayout.FloatField("悬停缩放 (Hover)", _settings.pro_hoverScale);
                    _settings.pro_hoverDuration = EditorGUILayout.FloatField("悬停耗时", _settings.pro_hoverDuration);
                    _settings.pro_pressedScale = EditorGUILayout.FloatField("按下缩放 (Down)", _settings.pro_pressedScale);
                    _settings.pro_clickDuration = EditorGUILayout.FloatField("按下耗时", _settings.pro_clickDuration);
                    _settings.pro_upScale = EditorGUILayout.FloatField("抬起回弹 (Up Scale)", _settings.pro_upScale);
                    _settings.pro_upDuration = EditorGUILayout.FloatField("抬起耗时", _settings.pro_upDuration);

                    EditorGUILayout.Space(5);
                    GUILayout.Label("🔹 禁用点击反馈 (Disabled)", EditorStyles.miniBoldLabel);
                    _settings.pro_disabledShakeAmount = EditorGUILayout.FloatField("左右震动幅度 (位移)", _settings.pro_disabledShakeAmount);
                    _settings.pro_disabledShakeDuration = EditorGUILayout.FloatField("震动耗时", _settings.pro_disabledShakeDuration);
                    break;

                case ToolMode.CommonEffect:
                    // --- 缩放模块 ---
                    _settings.effect_enableScale = EditorGUILayout.ToggleLeft(" 🔹 缩放 (Scale)", _settings.effect_enableScale, EditorStyles.boldLabel);
                    EditorGUI.BeginDisabledGroup(!_settings.effect_enableScale); 
                    _settings.effect_scaleDuration = EditorGUILayout.FloatField("缩放耗时", _settings.effect_scaleDuration);
                    _settings.effect_scaleEase = (DG.Tweening.Ease)EditorGUILayout.EnumPopup("缩放曲线", _settings.effect_scaleEase);
                    _settings.effect_startScale = EditorGUILayout.Vector3Field("开始缩放倍率 (Start)", _settings.effect_startScale); 
                    _settings.effect_endScale = EditorGUILayout.Vector3Field("结束缩放倍率 (End)", _settings.effect_endScale); 
                    EditorGUI.EndDisabledGroup();

                    EditorGUILayout.Space(5);

                    // --- 平移模块 ---
                    _settings.effect_enableMove = EditorGUILayout.ToggleLeft(" 🔹 平移 (Move)", _settings.effect_enableMove, EditorStyles.boldLabel);
                    EditorGUI.BeginDisabledGroup(!_settings.effect_enableMove);
                    _settings.effect_moveDuration = EditorGUILayout.FloatField("平移耗时", _settings.effect_moveDuration);
                    _settings.effect_moveEase = (DG.Tweening.Ease)EditorGUILayout.EnumPopup("平移曲线", _settings.effect_moveEase);
                    _settings.effect_startOffset = EditorGUILayout.Vector2Field("开始偏移量 (Start Offset)", _settings.effect_startOffset);
                    _settings.effect_endOffset = EditorGUILayout.Vector2Field("结束偏移量 (End Offset)", _settings.effect_endOffset);
                    EditorGUI.EndDisabledGroup();

                    EditorGUILayout.Space(5);

                    // --- 透明度模块 (Fade) ---
                    _settings.effect_enableFade = EditorGUILayout.ToggleLeft(" 🔹 透明度渐变 (Fade)", _settings.effect_enableFade, EditorStyles.boldLabel);
                    EditorGUI.BeginDisabledGroup(!_settings.effect_enableFade);
                    _settings.effect_fadeDuration = EditorGUILayout.FloatField("渐变耗时", _settings.effect_fadeDuration);
                    _settings.effect_fadeEase = (DG.Tweening.Ease)EditorGUILayout.EnumPopup("渐变曲线", _settings.effect_fadeEase);
                    // 使用滑动条限制透明度在 0 到 1 之间
                    _settings.effect_startAlpha = EditorGUILayout.Slider("开始透明度", _settings.effect_startAlpha, 0f, 1f);
                    _settings.effect_endAlpha = EditorGUILayout.Slider("结束透明度", _settings.effect_endAlpha, 0f, 1f);
                    EditorGUI.EndDisabledGroup();

                    EditorGUILayout.Space(5);

                    // --- 抖动模块 ---
                    _settings.effect_enablePunch = EditorGUILayout.ToggleLeft(" 🔹 Q弹抖动 (Punch)", _settings.effect_enablePunch, EditorStyles.boldLabel);
                    EditorGUI.BeginDisabledGroup(!_settings.effect_enablePunch);
                    _settings.effect_punchAmount = EditorGUILayout.FloatField("抖动幅度", _settings.effect_punchAmount);
                    _settings.effect_punchVibrato = EditorGUILayout.IntField("抖动频率", _settings.effect_punchVibrato);
                    EditorGUI.EndDisabledGroup();
                    break;

                case ToolMode.CallAnimFile:
                    _settings.file_animController = (RuntimeAnimatorController)EditorGUILayout.ObjectField(
                        "Animator 控制器", _settings.file_animController, typeof(RuntimeAnimatorController), false);
                    EditorGUILayout.Space(5);
                    EditorGUILayout.HelpBox("一键挂载时，将自动为目标物体添加 Animator 组件并指派该动画控制器。", MessageType.Info);
                    break;
            }
            EditorGUI.indentLevel--;

            EditorGUILayout.Space(10);
            DrawSaveButton();

            EditorGUILayout.EndVertical();
        }

        private ToolMode DrawRadioButton(ToolMode current, ToolMode value, string text)
        {
            var isSelected = current == value;
            var style = new GUIStyle(EditorStyles.miniButton) { fontSize = 12, padding = new RectOffset(5, 5, 4, 4) };
            if (isSelected) 
            { 
                style.normal.textColor = Color.white; 
                style.normal.background = (Texture2D)EditorGUIUtility.Load("d_SelectionRect"); 
            }
            
            if (GUILayout.Toggle(isSelected, text, style)) return value;
            return current;
        }

        private bool IsSettingsModified(AnimationTag tag)
        {
            if (tag == null || tag.savedSettings == null) return false;
            var s = tag.savedSettings;

            if (s.toolMode != (int)_toolMode) return true;

            if (s.effect_enableScale != _settings.effect_enableScale) return true;
            if (s.effect_scaleDuration != _settings.effect_scaleDuration) return true;
            if (s.effect_scaleEase != _settings.effect_scaleEase) return true;
            if (s.effect_startScale != _settings.effect_startScale) return true;
            if (s.effect_endScale != _settings.effect_endScale) return true;

            if (s.effect_enableMove != _settings.effect_enableMove) return true;
            if (s.effect_moveDuration != _settings.effect_moveDuration) return true;
            if (s.effect_moveEase != _settings.effect_moveEase) return true;
            if (s.effect_startOffset != _settings.effect_startOffset) return true;
            if (s.effect_endOffset != _settings.effect_endOffset) return true;

            if (s.effect_enableFade != _settings.effect_enableFade) return true;
            if (s.effect_fadeDuration != _settings.effect_fadeDuration) return true;
            if (s.effect_fadeEase != _settings.effect_fadeEase) return true;
            if (s.effect_startAlpha != _settings.effect_startAlpha) return true;
            if (s.effect_endAlpha != _settings.effect_endAlpha) return true;

            if (s.effect_enablePunch != _settings.effect_enablePunch) return true;
            if (s.effect_punchAmount != _settings.effect_punchAmount) return true;
            if (s.effect_punchVibrato != _settings.effect_punchVibrato) return true;

            if (s.pro_enterDuration != _settings.pro_enterDuration) return true;
            if (s.pro_enterEase != _settings.pro_enterEase) return true;
            if (s.pro_hoverScale != _settings.pro_hoverScale) return true;
            if (s.pro_hoverDuration != _settings.pro_hoverDuration) return true;
            if (s.pro_pressedScale != _settings.pro_pressedScale) return true;
            if (s.pro_clickDuration != _settings.pro_clickDuration) return true;
            if (s.pro_upScale != _settings.pro_upScale) return true;
            if (s.pro_upDuration != _settings.pro_upDuration) return true;
            if (s.pro_disabledShakeAmount != _settings.pro_disabledShakeAmount) return true;
            if (s.pro_disabledShakeDuration != _settings.pro_disabledShakeDuration) return true;

            if (s.file_animController != _settings.file_animController) return true;

            return false;
        }

        private void DrawSaveButton()
        {
            AnimationTag activeTag = null;
            foreach (var group in _settings.tagGroups)
            {
                activeTag = group.tags.FirstOrDefault(t => t.isSelected);
                if (activeTag != null) break;
            }
            
            GUI.enabled = activeTag != null;
            
            bool isModified = IsSettingsModified(activeTag);
            
            if (activeTag == null)
            {
                GUI.backgroundColor = new Color(0.4f, 0.4f, 0.4f);
            }
            else if (isModified)
            {
                GUI.backgroundColor = new Color(0.2f, 0.7f, 0.3f); 
            }
            else
            {
                GUI.backgroundColor = new Color(0.5f, 0.5f, 0.5f); 
            }
            
            string btnText = "请先在上方选择一个规则标签";
            if (activeTag != null)
            {
                btnText = isModified 
                    ? $"⚠️ 发现修改，保存参数到【{activeTag.name}】" 
                    : $"✔ 【{activeTag.name}】参数已是最新";
            }
            
            if (GUILayout.Button(btnText, GUILayout.Height(28)))
            {
                if (activeTag != null && isModified)
                {
                    SaveSettingsToSelectedTag(activeTag);
                    GUI.FocusControl(null); 
                }
            }
            
            GUI.backgroundColor = Color.white;
            GUI.enabled = true;
        }

        private void LoadSettingsFromTag(AnimationTag tag)
        {
            Undo.RecordObject(_settings, "Load Tag Settings");
            var s = tag.savedSettings;
            _toolMode = (ToolMode)s.toolMode;

            _settings.effect_enableScale = s.effect_enableScale;
            _settings.effect_scaleDuration = s.effect_scaleDuration;
            _settings.effect_scaleEase = s.effect_scaleEase;
            _settings.effect_startScale = s.effect_startScale;
            _settings.effect_endScale = s.effect_endScale;
            
            _settings.effect_enableMove = s.effect_enableMove;
            _settings.effect_moveDuration = s.effect_moveDuration;
            _settings.effect_moveEase = s.effect_moveEase;
            _settings.effect_startOffset = s.effect_startOffset;
            _settings.effect_endOffset = s.effect_endOffset;

            _settings.effect_enableFade = s.effect_enableFade;
            _settings.effect_fadeDuration = s.effect_fadeDuration;
            _settings.effect_fadeEase = s.effect_fadeEase;
            _settings.effect_startAlpha = s.effect_startAlpha;
            _settings.effect_endAlpha = s.effect_endAlpha;
            
            _settings.effect_enablePunch = s.effect_enablePunch;
            _settings.effect_punchAmount = s.effect_punchAmount;
            _settings.effect_punchVibrato = s.effect_punchVibrato;

            _settings.pro_enterDuration = s.pro_enterDuration;
            _settings.pro_enterEase = s.pro_enterEase;
            _settings.pro_hoverScale = s.pro_hoverScale;
            _settings.pro_hoverDuration = s.pro_hoverDuration;
            _settings.pro_pressedScale = s.pro_pressedScale;
            _settings.pro_clickDuration = s.pro_clickDuration;
            _settings.pro_upScale = s.pro_upScale;
            _settings.pro_upDuration = s.pro_upDuration;
            _settings.pro_disabledShakeAmount = s.pro_disabledShakeAmount;
            _settings.pro_disabledShakeDuration = s.pro_disabledShakeDuration;

            _settings.file_animController = s.file_animController;
        }

        private void SaveSettingsToSelectedTag(AnimationTag activeTag)
        {
            Undo.RecordObject(_settings, "Save Tag Settings");
            var s = activeTag.savedSettings;
            s.toolMode = (int)_toolMode;

            s.effect_enableScale = _settings.effect_enableScale;
            s.effect_scaleDuration = _settings.effect_scaleDuration;
            s.effect_scaleEase = _settings.effect_scaleEase;
            s.effect_startScale = _settings.effect_startScale;
            s.effect_endScale = _settings.effect_endScale;
            
            s.effect_enableMove = _settings.effect_enableMove;
            s.effect_moveDuration = _settings.effect_moveDuration;
            s.effect_moveEase = _settings.effect_moveEase;
            s.effect_startOffset = _settings.effect_startOffset;
            s.effect_endOffset = _settings.effect_endOffset;

            s.effect_enableFade = _settings.effect_enableFade;
            s.effect_fadeDuration = _settings.effect_fadeDuration;
            s.effect_fadeEase = _settings.effect_fadeEase;
            s.effect_startAlpha = _settings.effect_startAlpha;
            s.effect_endAlpha = _settings.effect_endAlpha;
            
            s.effect_enablePunch = _settings.effect_enablePunch;
            s.effect_punchAmount = _settings.effect_punchAmount;
            s.effect_punchVibrato = _settings.effect_punchVibrato;

            s.pro_enterDuration = _settings.pro_enterDuration;
            s.pro_enterEase = _settings.pro_enterEase;
            s.pro_hoverScale = _settings.pro_hoverScale;
            s.pro_hoverDuration = _settings.pro_hoverDuration;
            s.pro_pressedScale = _settings.pro_pressedScale;
            s.pro_clickDuration = _settings.pro_clickDuration;
            s.pro_upScale = _settings.pro_upScale;
            s.pro_upDuration = _settings.pro_upDuration;
            s.pro_disabledShakeAmount = _settings.pro_disabledShakeAmount;
            s.pro_disabledShakeDuration = _settings.pro_disabledShakeDuration;
            
            s.file_animController = _settings.file_animController;
            
            EditorUtility.SetDirty(_settings);
            AssetDatabase.SaveAssets(); 
        }

        private void DrawRuleSection()
        {
            EditorGUILayout.BeginVertical("helpbox");
            GUILayout.Label("➕ 添加新规则", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            GUILayout.BeginHorizontal();
            var groupNames = _settings.tagGroups.Select(g => g.groupName).ToArray();
            if (groupNames.Length > 0)
            {
                if (_selectedRuleGroupIndex >= groupNames.Length) _selectedRuleGroupIndex = 0;
                _selectedRuleGroupIndex = EditorGUILayout.Popup(_selectedRuleGroupIndex, groupNames, GUILayout.ExpandWidth(true));
            }
            else
            {
                GUILayout.Label("请先添加分组", GUILayout.ExpandWidth(true));
            }

            if (GUILayout.Button("+ 新建分组", GUILayout.Width(80)))
            {
                _isAddingGroup = !_isAddingGroup;
                _newGroupName = "";
            }
            GUILayout.EndHorizontal();

            if (_isAddingGroup)
            {
                GUILayout.BeginHorizontal();
                _newGroupName = EditorGUILayout.TextField("新分组名称", _newGroupName, GUILayout.ExpandWidth(true));
                if (GUILayout.Button("确认", EditorStyles.miniButton, GUILayout.Width(60)))
                {
                    if (!string.IsNullOrEmpty(_newGroupName))
                    {
                        Undo.RecordObject(_settings, "Add Group");
                        _settings.tagGroups.Add(new TagGroup { groupName = _newGroupName, tags = new List<AnimationTag>() });
                        _selectedRuleGroupIndex = _settings.tagGroups.Count - 1; 
                        _isAddingGroup = false;
                        GUI.FocusControl(null); 
                    }
                }
                GUILayout.EndHorizontal();
                EditorGUILayout.Space(5);
            }

            GUILayout.BeginHorizontal();
            _ruleText = EditorGUILayout.TextField(_ruleText, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("添加规则", GUILayout.Width(80)))
            {
                if (!string.IsNullOrEmpty(_ruleText) && _settings.tagGroups.Count > 0)
                {
                    Undo.RecordObject(_settings, "Add Tag");
                    var targetGroup = _settings.tagGroups[_selectedRuleGroupIndex];

                    if (targetGroup.tags == null) targetGroup.tags = new List<AnimationTag>();

                    var newTag = new AnimationTag { name = _ruleText };

                    targetGroup.tags.Add(newTag);
                    _ruleText = "";
                    GUI.FocusControl(null);
                }
            }
            GUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawBottomButtons()
        {
            EditorGUILayout.Space(5); 

            GUILayout.BeginHorizontal(); 
            GUILayout.FlexibleSpace();
            
            GUI.backgroundColor = new Color(0.2f, 0.4f, 0.8f);
            if (GUILayout.Button("🚀 一键挂载选中规则到物体", GUILayout.Height(35), GUILayout.ExpandWidth(true))) 
            { 
                if (Selection.activeGameObject != null) 
                    ApplySettingsToGameObject(Selection.activeGameObject); 
                else
                    EditorUtility.DisplayDialog("提示", "请先在层级面板 (Hierarchy) 中选中一个要挂载的 UI 物体。", "确定");
            }
            GUI.backgroundColor = Color.white;
            
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5); 
        }

        private void ApplySettingsToGameObject(GameObject go)
        {
            if (go == null)
            {
                TEngine.Log.Error("[UIJuice] 应用失败，目标 GameObject 为空。");
                return;
            }

            if (Application.isPlaying)
            {
                TEngine.Log.Warning("[UIJuice] Play Mode 下禁止应用 UIJuice 配置。");
                return;
            }

            Undo.RecordObject(go, "Apply UI Juice Settings");
            switch (_toolMode)
            {
                case ToolMode.ButtonControl:
                {
                    var pro = go.GetComponent<UIJuicePro>();
                    if (pro == null) pro = go.AddComponent<UIJuicePro>();
                    if (pro == null)
                    {
                        TEngine.Log.Error("[UIJuice] 添加 UIJuicePro 组件失败。");
                        return;
                    }

                    pro.enterDuration = _settings.pro_enterDuration;
                    pro.enterEase = _settings.pro_enterEase;
                    pro.hoverScale = _settings.pro_hoverScale;
                    pro.hoverDuration = _settings.pro_hoverDuration;
                    pro.pressedScale = _settings.pro_pressedScale;
                    pro.clickDuration = _settings.pro_clickDuration;
                    pro.upScale = _settings.pro_upScale;
                    pro.upDuration = _settings.pro_upDuration;
                    pro.disabledShakeAmount = _settings.pro_disabledShakeAmount;
                    pro.disabledShakeDuration = _settings.pro_disabledShakeDuration;
                    EditorUtility.SetDirty(pro);
                    break;
                }

                case ToolMode.CommonEffect:
                {
                    var effect = go.GetComponent<UIJuiceEffect>();
                    if (effect == null) effect = go.AddComponent<UIJuiceEffect>();
                    if (effect == null)
                    {
                        TEngine.Log.Error("[UIJuice] 添加 UIJuiceEffect 组件失败。");
                        return;
                    }

                    effect.enableScale = _settings.effect_enableScale;
                    effect.scaleDuration = _settings.effect_scaleDuration;
                    effect.scaleEase = _settings.effect_scaleEase;
                    effect.startScale = _settings.effect_startScale;
                    effect.endScale = _settings.effect_endScale;

                    effect.enableMove = _settings.effect_enableMove;
                    effect.moveDuration = _settings.effect_moveDuration;
                    effect.moveEase = _settings.effect_moveEase;
                    effect.startOffset = _settings.effect_startOffset;
                    effect.endOffset = _settings.effect_endOffset;

                    effect.enableFade = _settings.effect_enableFade;
                    effect.fadeDuration = _settings.effect_fadeDuration;
                    effect.fadeEase = _settings.effect_fadeEase;
                    effect.startAlpha = _settings.effect_startAlpha;
                    effect.endAlpha = _settings.effect_endAlpha;

                    effect.enablePunch = _settings.effect_enablePunch;
                    effect.punchAmount = _settings.effect_punchAmount;
                    effect.punchVibrato = _settings.effect_punchVibrato;
                    EditorUtility.SetDirty(effect);
                    break;
                }

                case ToolMode.CallAnimFile:
                {
                    if (_settings.file_animController != null)
                    {
                        var animator = go.GetComponent<Animator>();
                        if (animator == null) animator = go.AddComponent<Animator>();
                        if (animator == null)
                        {
                            TEngine.Log.Error("[UIJuice] 添加 Animator 组件失败。");
                            return;
                        }

                        animator.runtimeAnimatorController = _settings.file_animController;
                        EditorUtility.SetDirty(animator);
                    }
                    else
                    {
                        TEngine.Log.Warning("[UIJuice] Animator Controller 未配置。");
                    }

                    break;
                }
            }

            EditorUtility.SetDirty(go);

            if (PrefabUtility.IsPartOfPrefabInstance(go))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(go);
                var root = PrefabUtility.GetOutermostPrefabInstanceRoot(go);
                if (root != null)
                {
                    PrefabUtility.RecordPrefabInstancePropertyModifications(root);
                }
            }
            else if (PrefabUtility.IsPartOfPrefabAsset(go))
            {
                PrefabUtility.SavePrefabAsset(go);
            }
            else if (go.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(go.scene);
            }

            AssetDatabase.SaveAssets();
            SceneView.RepaintAll();
        }
    }
}
