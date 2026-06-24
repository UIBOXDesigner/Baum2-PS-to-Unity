using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class ImageReplacerWindow : EditorWindow
{
    private Sprite targetSprite;
    private Sprite replacementSprite;
    private Vector2 scrollPosition;
    private Vector2 batchTargetScrollPosition;

    // 批量模式：从 Project / 资源管理器中多选的旧图片
    private readonly List<Sprite> batchTargetSprites = new List<Sprite>();

    // 用于存储每个 Prefab 中具体的引用细节
    private class ReferenceInfo
    {
        public GameObject Prefab;
        public string PrefabPath;
        public List<ComponentDetail> Details = new List<ComponentDetail>();
        public bool IsExpanded = true; // 控制 UI 上的折叠状态
    }

    private class ComponentDetail
    {
        public string HierarchyPath;
        public string ComponentName;
        public Sprite MatchedSprite;
    }

    private List<ReferenceInfo> referenceInfos = new List<ReferenceInfo>();

    [MenuItem("Tools/图片引用替换工具 (Image Replacer)")]
    public static void ShowWindow()
    {
        GetWindow<ImageReplacerWindow>("图片替换工具");
    }

    private void OnGUI()
    {
        GUILayout.Label("1. 选择要查找的旧图片", EditorStyles.boldLabel);
        targetSprite = (Sprite)EditorGUILayout.ObjectField("目标图片 (旧 / 单图)", targetSprite, typeof(Sprite), false);

        if (GUILayout.Button("精确查找单张图片引用细节"))
        {
            FindDetailedReferences(GetSingleTargetSprites());
        }

        GUILayout.Space(8);
        EditorGUILayout.HelpBox("批量模式：先在 Project / 资源管理器中多选旧图片，再点击下方按钮读取。之后会查找所有引用这些旧图片的 Prefab，并可统一替换成一张新图片。", MessageType.Info);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("读取当前选中的多张旧图片"))
        {
            LoadBatchTargetSpritesFromSelection();
        }

        GUI.enabled = batchTargetSprites.Count > 0;
        if (GUILayout.Button("清空多选旧图片", GUILayout.Width(120)))
        {
            batchTargetSprites.Clear();
            referenceInfos.Clear();
        }
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        GUILayout.Label($"已读取旧图片数量: {batchTargetSprites.Count}", EditorStyles.miniBoldLabel);
        if (batchTargetSprites.Count > 0)
        {
            batchTargetScrollPosition = EditorGUILayout.BeginScrollView(batchTargetScrollPosition, "box", GUILayout.Height(90));
            foreach (var sprite in batchTargetSprites)
            {
                EditorGUILayout.ObjectField(sprite, typeof(Sprite), false);
            }
            EditorGUILayout.EndScrollView();
        }

        GUI.enabled = batchTargetSprites.Count > 0;
        if (GUILayout.Button("批量精确查找这些图片的引用细节"))
        {
            FindDetailedReferences(batchTargetSprites);
        }
        GUI.enabled = true;

        GUILayout.Space(10);
        GUILayout.Label($"受影响的 Prefab 数量: {referenceInfos.Count}", EditorStyles.boldLabel);

        // 显示详细的树状列表
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, "box", GUILayout.Height(250));
        foreach (var info in referenceInfos)
        {
            EditorGUILayout.BeginVertical("helpbox");

            // Prefab 标题行带折叠开关
            info.IsExpanded = EditorGUILayout.Foldout(info.IsExpanded, $"Prefab: {info.Prefab.name} ({info.Details.Count} 处引用)", true, EditorStyles.foldoutHeader);

            if (info.IsExpanded)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.ObjectField("资产文件", info.Prefab, typeof(GameObject), false);

                // 列出该 Prefab 内所有引用此图的完整路径、组件名和命中的旧图片名
                foreach (var detail in info.Details)
                {
                    string spriteName = detail.MatchedSprite != null ? detail.MatchedSprite.name : "Missing Sprite";
                    EditorGUILayout.LabelField($"[{detail.ComponentName}] [{spriteName}]", detail.HierarchyPath, EditorStyles.wordWrappedMiniLabel);
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndScrollView();

        GUILayout.Space(10);
        GUILayout.Label("2. 选择新图片并执行替换", EditorStyles.boldLabel);
        replacementSprite = (Sprite)EditorGUILayout.ObjectField("替换图片 (新 / 统一替换成这张)", replacementSprite, typeof(Sprite), false);

        GUI.enabled = referenceInfos.Count > 0 && replacementSprite != null;
        if (GUILayout.Button("一键替换所有列出的引用", GUILayout.Height(30)))
        {
            ReplaceImages();
        }
        GUI.enabled = true;
    }

    private List<Sprite> GetSingleTargetSprites()
    {
        if (targetSprite == null)
        {
            return new List<Sprite>();
        }

        return new List<Sprite> { targetSprite };
    }

    private void LoadBatchTargetSpritesFromSelection()
    {
        batchTargetSprites.Clear();
        referenceInfos.Clear();

        foreach (UnityEngine.Object selectedObject in Selection.objects)
        {
            AddSpritesFromObject(selectedObject, batchTargetSprites);
        }

        // 去重，避免重复扫描同一张 Sprite
        var distinctSprites = batchTargetSprites
            .Where(sprite => sprite != null)
            .GroupBy(GetSpriteUniqueKey)
            .Select(group => group.First())
            .ToList();

        batchTargetSprites.Clear();
        batchTargetSprites.AddRange(distinctSprites);

        if (batchTargetSprites.Count == 0)
        {
            Debug.LogWarning("当前 Project / 资源管理器选中内容中没有可用的 Sprite。请确认图片 Texture Type 为 Sprite。");
        }
        else
        {
            Debug.Log($"已读取 {batchTargetSprites.Count} 张旧图片，可执行批量查找。 ");
        }
    }

    private void AddSpritesFromObject(UnityEngine.Object sourceObject, List<Sprite> output)
    {
        if (sourceObject == null) return;

        if (sourceObject is Sprite sprite)
        {
            output.Add(sprite);
            return;
        }

        string assetPath = AssetDatabase.GetAssetPath(sourceObject);
        if (string.IsNullOrEmpty(assetPath)) return;

        // 支持在 Project 中选中 Texture2D。若该贴图导入为 Sprite，则读取该资源路径下的所有 Sprite 子资源。
        UnityEngine.Object[] allAssetsAtPath = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        foreach (UnityEngine.Object asset in allAssetsAtPath)
        {
            if (asset is Sprite childSprite)
            {
                output.Add(childSprite);
            }
        }
    }

    private string GetSpriteUniqueKey(Sprite sprite)
    {
        if (sprite == null) return string.Empty;

        string path = AssetDatabase.GetAssetPath(sprite);
        return $"{path}|{sprite.name}|{sprite.GetInstanceID()}";
    }

    private void FindDetailedReferences(List<Sprite> spritesToFind)
    {
        referenceInfos.Clear();

        if (spritesToFind == null || spritesToFind.Count == 0)
        {
            Debug.LogWarning("请先选择目标图片！");
            return;
        }

        List<Sprite> validSprites = spritesToFind.Where(sprite => sprite != null).Distinct().ToList();
        if (validSprites.Count == 0)
        {
            Debug.LogWarning("目标图片为空，请重新选择。 ");
            return;
        }

        HashSet<Sprite> targetSpriteSet = new HashSet<Sprite>(validSprites);
        HashSet<string> targetPaths = new HashSet<string>(validSprites.Select(AssetDatabase.GetAssetPath).Where(path => !string.IsNullOrEmpty(path)));
        string[] allPrefabGuids = AssetDatabase.FindAssets("t:Prefab");

        // 显示进度条
        int total = allPrefabGuids.Length;
        int current = 0;

        try
        {
            foreach (string guid in allPrefabGuids)
            {
                current++;
                string prefabPath = AssetDatabase.GUIDToAssetPath(guid);

                // 1. 先用 GetDependencies 快速过滤，排除完全不相关的 Prefab
                string[] dependencies = AssetDatabase.GetDependencies(prefabPath, false);
                if (!dependencies.Any(targetPaths.Contains)) continue;

                EditorUtility.DisplayProgressBar("深度查找中", $"正在分析: {prefabPath}", (float)current / total);

                GameObject contents = null;
                try
                {
                    // 2. 加载确认为依赖这些图片的 Prefab 内容，进行深度遍历
                    contents = PrefabUtility.LoadPrefabContents(prefabPath);
                    ReferenceInfo newInfo = new ReferenceInfo
                    {
                        Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath),
                        PrefabPath = prefabPath
                    };

                    // 检查 UI Image
                    Image[] uiImages = contents.GetComponentsInChildren<Image>(true);
                    foreach (Image img in uiImages)
                    {
                        if (img.sprite != null && targetSpriteSet.Contains(img.sprite))
                        {
                            newInfo.Details.Add(new ComponentDetail
                            {
                                HierarchyPath = GetGameObjectPath(img.transform),
                                ComponentName = "Image (UGUI)",
                                MatchedSprite = img.sprite
                            });
                        }
                    }

                    // 检查 SpriteRenderer
                    SpriteRenderer[] spriteRenderers = contents.GetComponentsInChildren<SpriteRenderer>(true);
                    foreach (SpriteRenderer sr in spriteRenderers)
                    {
                        if (sr.sprite != null && targetSpriteSet.Contains(sr.sprite))
                        {
                            newInfo.Details.Add(new ComponentDetail
                            {
                                HierarchyPath = GetGameObjectPath(sr.transform),
                                ComponentName = "SpriteRenderer",
                                MatchedSprite = sr.sprite
                            });
                        }
                    }

                    // 如果确实在组件中找到了（防止只是材质或其他间接引用），则添加到列表
                    if (newInfo.Details.Count > 0)
                    {
                        referenceInfos.Add(newInfo);
                    }
                }
                finally
                {
                    if (contents != null)
                    {
                        PrefabUtility.UnloadPrefabContents(contents);
                    }
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        if (referenceInfos.Count == 0)
        {
            Debug.Log("未找到直接引用目标 Sprite 的 UI Image 或 SpriteRenderer 控件。 ");
        }
        else
        {
            int totalReferences = referenceInfos.Sum(info => info.Details.Count);
            Debug.Log($"查找完成！共找到 {referenceInfos.Count} 个 Prefab，{totalReferences} 处直接引用。 ");
        }
    }

    private void ReplaceImages()
    {
        if (replacementSprite == null)
        {
            Debug.LogWarning("请先选择替换图片！");
            return;
        }

        if (referenceInfos.Count == 0)
        {
            Debug.LogWarning("没有可替换的引用列表，请先执行查找。 ");
            return;
        }

        bool confirmed = EditorUtility.DisplayDialog(
            "确认替换图片引用",
            $"将把当前列表中命中的所有旧图片引用统一替换为：{replacementSprite.name}\n\n受影响 Prefab：{referenceInfos.Count} 个\n命中引用：{referenceInfos.Sum(info => info.Details.Count)} 处\n\n建议替换前确认工程已提交版本管理。",
            "确认替换",
            "取消");

        if (!confirmed)
        {
            return;
        }

        int totalReplaced = 0;
        int modifiedPrefabCount = 0;

        try
        {
            for (int index = 0; index < referenceInfos.Count; index++)
            {
                ReferenceInfo info = referenceInfos[index];
                EditorUtility.DisplayProgressBar("替换图片引用中", $"正在处理: {info.PrefabPath}", (float)(index + 1) / referenceInfos.Count);

                HashSet<Sprite> oldSpritesInPrefab = new HashSet<Sprite>(info.Details.Select(detail => detail.MatchedSprite).Where(sprite => sprite != null));
                if (oldSpritesInPrefab.Count == 0) continue;

                GameObject contents = null;
                bool isModified = false;
                int replacedInThisPrefab = 0;

                try
                {
                    contents = PrefabUtility.LoadPrefabContents(info.PrefabPath);

                    Image[] uiImages = contents.GetComponentsInChildren<Image>(true);
                    foreach (Image img in uiImages)
                    {
                        if (img.sprite != null && oldSpritesInPrefab.Contains(img.sprite))
                        {
                            img.sprite = replacementSprite;
                            EditorUtility.SetDirty(img);
                            isModified = true;
                            replacedInThisPrefab++;
                            totalReplaced++;
                        }
                    }

                    SpriteRenderer[] spriteRenderers = contents.GetComponentsInChildren<SpriteRenderer>(true);
                    foreach (SpriteRenderer sr in spriteRenderers)
                    {
                        if (sr.sprite != null && oldSpritesInPrefab.Contains(sr.sprite))
                        {
                            sr.sprite = replacementSprite;
                            EditorUtility.SetDirty(sr);
                            isModified = true;
                            replacedInThisPrefab++;
                            totalReplaced++;
                        }
                    }

                    if (isModified)
                    {
                        PrefabUtility.SaveAsPrefabAsset(contents, info.PrefabPath);
                        modifiedPrefabCount++;
                        Debug.Log($"已替换 Prefab：{info.PrefabPath}，更新 {replacedInThisPrefab} 处引用。 ");
                    }
                }
                finally
                {
                    if (contents != null)
                    {
                        PrefabUtility.UnloadPrefabContents(contents);
                    }
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        Debug.Log($"替换完成！共修改了 {modifiedPrefabCount} 个 Prefab，更新了 {totalReplaced} 处控件引用。 ");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        referenceInfos.Clear();
    }

    // 辅助方法：获取节点在 Prefab 中的相对路径
    private string GetGameObjectPath(Transform transform)
    {
        string path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }
        // 移除最外层 Root 节点的 "(Clone)" 后缀以保证显示的美观性
        return path.Replace("(Clone)", "");
    }
}
