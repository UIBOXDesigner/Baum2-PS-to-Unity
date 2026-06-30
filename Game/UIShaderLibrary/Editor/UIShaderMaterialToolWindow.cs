#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UIShaderLibrary.Editor
{
    /// <summary>
    /// UGUI Shader 材质挂载工具。
    /// 匹配 UIShaderPack_v1.0.0 中的 14 个 Shader：UI/Effects/*。
    /// </summary>
    public class UIShaderMaterialToolWindow : EditorWindow
    {
        enum Preset
        {
            DefaultEx,
            Gray,
            BlackToAlpha,
            Additive,
            FlowLight,
            Outline,
            Glow,
            RoundedCorner,
            Dissolve,
            ProgressMask,
            Blur,
            NoiseEdge,
            HueShift,
            GradientOverlay
        }

        const string MaterialRoot = "Assets/UIShaderPack/Materials";
        const string RuntimeMaterialControllerTypeName = "Game.UIShaderLibrary.UIShaderRuntimeMaterial";

        Preset preset = Preset.Gray;
        bool includeChildren;
        bool addRuntimeMaterialController = true;
        Vector2 scroll;

        static readonly Dictionary<Preset, string> MatNames = new Dictionary<Preset, string>
        {
            { Preset.DefaultEx, "M_UI_DefaultEx" },
            { Preset.Gray, "M_UI_Gray" },
            { Preset.BlackToAlpha, "M_UI_BlackToAlpha" },
            { Preset.Additive, "M_UI_Additive" },
            { Preset.FlowLight, "M_UI_FlowLight" },
            { Preset.Outline, "M_UI_Outline" },
            { Preset.Glow, "M_UI_Glow" },
            { Preset.RoundedCorner, "M_UI_RoundedCorner" },
            { Preset.Dissolve, "M_UI_Dissolve" },
            { Preset.ProgressMask, "M_UI_ProgressMask" },
            { Preset.Blur, "M_UI_Blur" },
            { Preset.NoiseEdge, "M_UI_NoiseEdge" },
            { Preset.HueShift, "M_UI_HueShift" },
            { Preset.GradientOverlay, "M_UI_GradientOverlay" }
        };

        static readonly Dictionary<Preset, string> ShaderNames = new Dictionary<Preset, string>
        {
            { Preset.DefaultEx, "UI/Effects/DefaultEx" },
            { Preset.Gray, "UI/Effects/Gray" },
            { Preset.BlackToAlpha, "UI/Effects/BlackToAlpha" },
            { Preset.Additive, "UI/Effects/Additive" },
            { Preset.FlowLight, "UI/Effects/FlowLight" },
            { Preset.Outline, "UI/Effects/Outline" },
            { Preset.Glow, "UI/Effects/Glow" },
            { Preset.RoundedCorner, "UI/Effects/RoundedCorner" },
            { Preset.Dissolve, "UI/Effects/Dissolve" },
            { Preset.ProgressMask, "UI/Effects/ProgressMask" },
            { Preset.Blur, "UI/Effects/Blur" },
            { Preset.NoiseEdge, "UI/Effects/NoiseEdge" },
            { Preset.HueShift, "UI/Effects/HueShift" },
            { Preset.GradientOverlay, "UI/Effects/GradientOverlay" }
        };

        static readonly Dictionary<Preset, string> Descriptions = new Dictionary<Preset, string>
        {
            { Preset.DefaultEx, "基础 UI 扩展：亮度、饱和度、对比度、透明度。" },
            { Preset.Gray, "UI 置灰：按钮禁用、未解锁图标。" },
            { Preset.BlackToAlpha, "黑底转透明：光效、火焰、刀光、扫光素材。" },
            { Preset.Additive, "加法叠加：高亮光效、爆点、发光装饰。" },
            { Preset.FlowLight, "流光扫过：按钮、品质框、Logo 高光。" },
            { Preset.Outline, "图片描边：图标轮廓、头像边缘。" },
            { Preset.Glow, "外发光：可点击提示、稀有品质表现。" },
            { Preset.RoundedCorner, "圆角裁切：头像、卡片、弹窗面板。" },
            { Preset.Dissolve, "溶解：出现、消失、奖励反馈。" },
            { Preset.ProgressMask, "进度遮罩：线性/径向填充、技能 CD。" },
            { Preset.Blur, "模糊：背景虚化、弹窗遮罩底图。" },
            { Preset.NoiseEdge, "噪声边缘：燃烧边、魔法边缘、动态边缘。" },
            { Preset.HueShift, "色相偏移：换色、阵营色、品质色。" },
            { Preset.GradientOverlay, "渐变叠加：面板、按钮、背景层次。" }
        };

        static readonly Preset[] PresetOrder =
        {
            Preset.DefaultEx,
            Preset.Gray,
            Preset.BlackToAlpha,
            Preset.Additive,
            Preset.FlowLight,
            Preset.Outline,
            Preset.Glow,
            Preset.RoundedCorner,
            Preset.Dissolve,
            Preset.ProgressMask,
            Preset.Blur,
            Preset.NoiseEdge,
            Preset.HueShift,
            Preset.GradientOverlay
        };

        static readonly GUIContent[] PresetDisplayOptions =
        {
            new GUIContent("基础扩展（DefaultEx）"),
            new GUIContent("图片置灰（Gray）"),
            new GUIContent("黑底转透明（BlackToAlpha）"),
            new GUIContent("加法发光（Additive）"),
            new GUIContent("流光扫光（FlowLight）"),
            new GUIContent("图片描边（Outline）"),
            new GUIContent("外发光（Glow）"),
            new GUIContent("圆角裁切（RoundedCorner）"),
            new GUIContent("溶解消隐（Dissolve）"),
            new GUIContent("进度遮罩（ProgressMask）"),
            new GUIContent("UI 模糊（Blur）"),
            new GUIContent("噪声边缘（NoiseEdge）"),
            new GUIContent("色相偏移（HueShift）"),
            new GUIContent("渐变叠加（GradientOverlay）")
        };

        static readonly Dictionary<Preset, string> DisplayNames = new Dictionary<Preset, string>
        {
            { Preset.DefaultEx, "基础扩展（DefaultEx）" },
            { Preset.Gray, "图片置灰（Gray）" },
            { Preset.BlackToAlpha, "黑底转透明（BlackToAlpha）" },
            { Preset.Additive, "加法发光（Additive）" },
            { Preset.FlowLight, "流光扫光（FlowLight）" },
            { Preset.Outline, "图片描边（Outline）" },
            { Preset.Glow, "外发光（Glow）" },
            { Preset.RoundedCorner, "圆角裁切（RoundedCorner）" },
            { Preset.Dissolve, "溶解消隐（Dissolve）" },
            { Preset.ProgressMask, "进度遮罩（ProgressMask）" },
            { Preset.Blur, "UI 模糊（Blur）" },
            { Preset.NoiseEdge, "噪声边缘（NoiseEdge）" },
            { Preset.HueShift, "色相偏移（HueShift）" },
            { Preset.GradientOverlay, "渐变叠加（GradientOverlay）" }
        };

        [MenuItem("Tools/UI Shader Pack/挂载 Shader 材质工具")]
        public static void Open()
        {
            GetWindow<UIShaderMaterialToolWindow>("UI Shader Pack").Show();
        }

        [MenuItem("Tools/UI Shader Pack/一键生成或修复材质球")]
        public static void RepairAll()
        {
            EnsureMaterialRoot();

            foreach (Preset p in Enum.GetValues(typeof(Preset)))
            {
                GetOrCreateMaterial(p);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[UI Shader Pack] 14 个材质球已生成或修复完成。路径：" + MaterialRoot);
        }

        void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);

            EditorGUILayout.LabelField("UI Shader Pack 挂载工具", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("选中 Hierarchy 中的 UI 对象，选择 Shader 类型后点击挂载。支持 Image / RawImage / Text / 其他继承自 Graphic 的组件。", MessageType.Info);

            int selectedPresetIndex = GetPresetIndex(preset);
            selectedPresetIndex = EditorGUILayout.Popup(new GUIContent("Shader 类型"), selectedPresetIndex, PresetDisplayOptions);
            if (selectedPresetIndex >= 0 && selectedPresetIndex < PresetOrder.Length)
            {
                preset = PresetOrder[selectedPresetIndex];
            }

            includeChildren = EditorGUILayout.ToggleLeft("包含子节点 Graphic", includeChildren);
            addRuntimeMaterialController = EditorGUILayout.ToggleLeft("添加运行时材质实例控制脚本", addRuntimeMaterialController);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("当前类型", DisplayNames[preset]);
            EditorGUILayout.LabelField("Shader", ShaderNames[preset]);
            EditorGUILayout.LabelField("Material", MatNames[preset]);
            EditorGUILayout.HelpBox(Descriptions[preset], MessageType.None);

            EditorGUILayout.Space(8);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("生成/修复全部材质球", GUILayout.Height(32)))
                {
                    RepairAll();
                }

                if (GUILayout.Button("定位当前材质", GUILayout.Height(32)))
                {
                    Material m = GetOrCreateMaterial(preset);
                    if (m != null)
                    {
                        AssetDatabase.SaveAssets();
                        Selection.activeObject = m;
                        EditorGUIUtility.PingObject(m);
                    }
                }
            }

            if (GUILayout.Button("挂载到选中对象", GUILayout.Height(40)))
            {
                Apply();
            }

            if (GUILayout.Button("清空选中对象材质", GUILayout.Height(28)))
            {
                Clear();
            }

            EditorGUILayout.EndScrollView();
        }

        void Apply()
        {
            if (Selection.gameObjects.Length == 0)
            {
                EditorUtility.DisplayDialog("UI Shader Pack", "请先选中 Hierarchy 中的 UI 对象。", "确定");
                return;
            }

            Material mat = GetOrCreateMaterial(preset);
            if (mat == null)
            {
                return;
            }

            int count = 0;
            foreach (GameObject go in Selection.gameObjects)
            {
                Graphic[] graphics = includeChildren
                    ? go.GetComponentsInChildren<Graphic>(true)
                    : go.GetComponents<Graphic>();

                foreach (Graphic graphic in graphics)
                {
                    Undo.RecordObject(graphic, "Apply UI Shader Material");
                    graphic.material = mat;
                    EditorUtility.SetDirty(graphic);

                    if (addRuntimeMaterialController)
                    {
                        AddOrBindRuntimeController(graphic, mat);
                    }

                    count++;
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[UI Shader Pack] 已挂载 " + mat.name + " 到 " + count + " 个 Graphic。Shader：" + ShaderNames[preset]);
        }

        static void AddOrBindRuntimeController(Graphic graphic, Material mat)
        {
            if (graphic == null || mat == null)
            {
                return;
            }

            Type controllerType = FindType(RuntimeMaterialControllerTypeName);
            if (controllerType == null)
            {
                Debug.LogWarning("[UI Shader Pack] 未找到运行时材质实例控制脚本：" + RuntimeMaterialControllerTypeName + "。已跳过添加控制脚本，不影响材质挂载。");
                return;
            }

            Component controller = graphic.GetComponent(controllerType);
            if (controller == null)
            {
                controller = Undo.AddComponent(graphic.gameObject, controllerType);
            }

            Undo.RecordObject(controller, "Bind UI Shader Runtime Material");

            SerializedObject so = new SerializedObject(controller);
            SerializedProperty targetGraphic = so.FindProperty("targetGraphic");
            if (targetGraphic != null)
            {
                targetGraphic.objectReferenceValue = graphic;
            }

            SerializedProperty sourceMaterial = so.FindProperty("sourceMaterial");
            if (sourceMaterial != null)
            {
                sourceMaterial.objectReferenceValue = mat;
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(controller);
        }

        void Clear()
        {
            int count = 0;
            foreach (GameObject go in Selection.gameObjects)
            {
                Graphic[] graphics = includeChildren
                    ? go.GetComponentsInChildren<Graphic>(true)
                    : go.GetComponents<Graphic>();

                foreach (Graphic graphic in graphics)
                {
                    Undo.RecordObject(graphic, "Clear UI Material");
                    graphic.material = null;
                    EditorUtility.SetDirty(graphic);
                    count++;
                }
            }

            Debug.Log("[UI Shader Pack] 已清空 " + count + " 个 Graphic 材质。");
        }

        static int GetPresetIndex(Preset value)
        {
            for (int i = 0; i < PresetOrder.Length; i++)
            {
                if (PresetOrder[i] == value)
                {
                    return i;
                }
            }

            return 0;
        }

        static Material GetOrCreateMaterial(Preset p)
        {
            EnsureMaterialRoot();

            string matPath = MaterialRoot + "/" + MatNames[p] + ".mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);

            if (mat == null)
            {
                mat = FindMaterialByName(MatNames[p]);
            }

            Shader shader = Shader.Find(ShaderNames[p]);
            if (shader == null)
            {
                Debug.LogError("[UI Shader Pack] 找不到 Shader：" + ShaderNames[p] + "。请先导入 UIShaderPack_v1.0.0 中的 Shaders 目录。材质：" + MatNames[p]);
                return mat;
            }

            if (mat == null)
            {
                mat = new Material(shader)
                {
                    name = MatNames[p]
                };
                AssetDatabase.CreateAsset(mat, matPath);
            }

            if (mat.shader != shader)
            {
                mat.shader = shader;
            }

            SetDefaults(mat, p);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static Material FindMaterialByName(string materialName)
        {
            string[] guids = AssetDatabase.FindAssets(materialName + " t:Material");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat != null && mat.name == materialName)
                {
                    return mat;
                }
            }
            return null;
        }

        static void SetDefaults(Material m, Preset p)
        {
            if (m == null)
            {
                return;
            }

            SetC(m, "_Color", Color.white);
            SetF(m, "_UseUIAlphaClip", 0f);

            switch (p)
            {
                case Preset.DefaultEx:
                    SetF(m, "_Brightness", 1f);
                    SetF(m, "_Saturation", 1f);
                    SetF(m, "_Contrast", 1f);
                    SetF(m, "_Alpha", 1f);
                    break;

                case Preset.Gray:
                    SetF(m, "_GrayAmount", 1f);
                    SetF(m, "_Brightness", 1f);
                    break;

                case Preset.BlackToAlpha:
                    SetF(m, "_Cutoff", 0.02f);
                    SetF(m, "_Feather", 0.15f);
                    SetF(m, "_Intensity", 1f);
                    break;

                case Preset.Additive:
                    SetF(m, "_Intensity", 1f);
                    SetF(m, "_Alpha", 1f);
                    break;

                case Preset.FlowLight:
                    SetC(m, "_FlowColor", Color.white);
                    SetF(m, "_FlowStrength", 1.5f);
                    SetF(m, "_FlowWidth", 0.12f);
                    SetF(m, "_FlowSoftness", 0.08f);
                    SetF(m, "_FlowSpeed", 1f);
                    SetF(m, "_Angle", 0.785f);
                    break;

                case Preset.Outline:
                    SetC(m, "_OutlineColor", Color.black);
                    SetF(m, "_OutlineSize", 1f);
                    SetF(m, "_OutlineSoftness", 0.2f);
                    break;

                case Preset.Glow:
                    SetC(m, "_GlowColor", new Color(1f, 0.8f, 0.25f, 1f));
                    SetF(m, "_GlowSize", 4f);
                    SetF(m, "_GlowStrength", 1.2f);
                    break;

                case Preset.RoundedCorner:
                    SetF(m, "_Radius", 0.12f);
                    SetF(m, "_Softness", 0.02f);
                    break;

                case Preset.Dissolve:
                    SetF(m, "_Dissolve", 0f);
                    SetF(m, "_EdgeWidth", 0.06f);
                    SetC(m, "_EdgeColor", new Color(1f, 0.7f, 0.1f, 1f));
                    SetF(m, "_NoiseScale", 35f);
                    break;

                case Preset.ProgressMask:
                    SetF(m, "_FillAmount", 1f);
                    SetF(m, "_FillOrigin", 0f);
                    SetF(m, "_Softness", 0.01f);
                    SetF(m, "_Radial", 0f);
                    break;

                case Preset.Blur:
                    SetF(m, "_BlurSize", 2f);
                    SetF(m, "_Alpha", 1f);
                    break;

                case Preset.NoiseEdge:
                    SetC(m, "_EdgeColor", new Color(1f, 0.5f, 0.05f, 1f));
                    SetF(m, "_EdgeStrength", 1f);
                    SetF(m, "_NoiseScale", 25f);
                    SetF(m, "_Speed", 1f);
                    SetF(m, "_EdgeWidth", 0.1f);
                    break;

                case Preset.HueShift:
                    SetF(m, "_HueShift", 0f);
                    SetF(m, "_Saturation", 1f);
                    SetF(m, "_Value", 1f);
                    break;

                case Preset.GradientOverlay:
                    SetC(m, "_TopColor", Color.white);
                    SetC(m, "_BottomColor", new Color(0.6f, 0.6f, 0.6f, 1f));
                    SetF(m, "_Blend", 0.5f);
                    SetF(m, "_Alpha", 1f);
                    break;
            }
        }

        static void SetF(Material m, string propertyName, float value)
        {
            if (m.HasProperty(propertyName))
            {
                m.SetFloat(propertyName, value);
            }
        }

        static void SetC(Material m, string propertyName, Color value)
        {
            if (m.HasProperty(propertyName))
            {
                m.SetColor(propertyName, value);
            }
        }

        static void EnsureMaterialRoot()
        {
            EnsureFolder("Assets", "UIShaderPack");
            EnsureFolder("Assets/UIShaderPack", "Materials");
        }

        static void EnsureFolder(string parentPath, string folderName)
        {
            string folderPath = parentPath + "/" + folderName;
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder(parentPath, folderName);
            }
        }

        static Type FindType(string fullTypeName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(fullTypeName);
                if (type != null)
                {
                    return type;
                }
            }
            return null;
        }
    }
}
#endif
