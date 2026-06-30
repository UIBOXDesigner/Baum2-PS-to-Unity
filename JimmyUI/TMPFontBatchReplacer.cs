using UnityEngine;
using UnityEditor;
using TMPro;
using System.Collections.Generic;
using System;

namespace JimmyUI.Editor
{
    public class TMPFontBatchReplacer : EditorWindow
    {
        private sealed class TMPUsageInfo
        {
            public string PrefabPath;
            public string NodePath;
            public string ComponentType;
            public string CurrentFontName;
        }

        private DefaultAsset folderAsset;
        private TMP_FontAsset targetFont;
        private readonly List<TMPUsageInfo> usageList = new List<TMPUsageInfo>();
        private Vector2 scrollPos;
        private int scannedPrefabCount;

        [MenuItem("Tools/荒野大赌客/UI与本地化/TMP 字体批量替换")]
        public static void ShowWindow()
        {
            GetWindow<TMPFontBatchReplacer>("TMP Font Replacer");
        }

        private void OnGUI()
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField("TMP Font Batch Replacer (Folder)", EditorStyles.boldLabel);
            GUILayout.Space(5);

            folderAsset = (DefaultAsset)EditorGUILayout.ObjectField("Target Folder", folderAsset, typeof(DefaultAsset), false);
            targetFont = (TMP_FontAsset)EditorGUILayout.ObjectField("Target Font (New)", targetFont, typeof(TMP_FontAsset), false);

            string folderPath = GetFolderPath(folderAsset);
            bool validFolder = !string.IsNullOrEmpty(folderPath);
            if (!validFolder)
            {
                EditorGUILayout.HelpBox("Please drag a valid folder asset (inside Assets).", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox($"Scan Scope: {folderPath}", MessageType.Info);
            }

            GUILayout.Space(20);

            using (new EditorGUI.DisabledScope(!validFolder))
            {
                if (GUILayout.Button("Scan Prefabs in Folder", GUILayout.Height(32)))
                {
                    ScanFolder(folderPath);
                }
            }

            using (new EditorGUI.DisabledScope(!validFolder || targetFont == null))
            {
                if (GUILayout.Button("Replace Font in Scanned Folder", GUILayout.Height(40)))
                {
                    ReplaceInFolder(folderPath);
                    ScanFolder(folderPath);
                }
            }

            GUILayout.Space(10);
            DrawResultList();
        }

        private void DrawResultList()
        {
            EditorGUILayout.LabelField($"Scanned Prefabs: {scannedPrefabCount}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Found TMP Components: {usageList.Count}", EditorStyles.boldLabel);
            GUILayout.Space(6);

            if (usageList.Count == 0)
            {
                EditorGUILayout.HelpBox("No TMP components found. Click 'Scan Prefabs in Folder' first.", MessageType.None);
                return;
            }

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            for (int i = 0; i < usageList.Count; i++)
            {
                TMPUsageInfo info = usageList[i];
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField($"[{i + 1}] {info.PrefabPath}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Node: {info.NodePath}");
                EditorGUILayout.LabelField($"Type: {info.ComponentType}");
                EditorGUILayout.LabelField($"Current Font: {info.CurrentFontName}");
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndScrollView();
        }

        private static string GetFolderPath(DefaultAsset asset)
        {
            if (asset == null)
            {
                return string.Empty;
            }

            string path = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(path) || !AssetDatabase.IsValidFolder(path))
            {
                return string.Empty;
            }

            return path;
        }

        private void ScanFolder(string folderPath)
        {
            usageList.Clear();
            scannedPrefabCount = 0;

            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });
            scannedPrefabCount = prefabGuids.Length;

            try
            {
                for (int i = 0; i < prefabGuids.Length; i++)
                {
                    string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                    GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
                    try
                    {
                        TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
                        for (int j = 0; j < texts.Length; j++)
                        {
                            TMP_Text text = texts[j];
                            usageList.Add(new TMPUsageInfo
                            {
                                PrefabPath = prefabPath,
                                NodePath = BuildNodePath(text.transform, root.transform),
                                ComponentType = text.GetType().Name,
                                CurrentFontName = text.font != null ? text.font.name : "(None)"
                            });
                        }
                    }
                    finally
                    {
                        PrefabUtility.UnloadPrefabContents(root);
                    }
                }
            }
            catch (Exception exception)
            {
                TEngine.Log.Error($"[TMPFontBatchReplacer] TMP 字体引用扫描失败。异常={exception.Message}");
                EditorUtility.DisplayDialog("Scan Failed", exception.Message, "OK");
                return;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            TEngine.Log.Info($"[TMPFontBatchReplacer] TMP 字体引用扫描完成。Prefab数量={scannedPrefabCount}，TMP数量={usageList.Count}");
        }

        private void ReplaceInFolder(string folderPath)
        {
            if (targetFont == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select a target font.", "OK");
                return;
            }

            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });
            int changedComponentCount = 0;
            int changedPrefabCount = 0;

            try
            {
                for (int i = 0; i < prefabGuids.Length; i++)
                {
                    string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                    float progress = prefabGuids.Length > 0 ? (i + 1f) / prefabGuids.Length : 1f;
                    EditorUtility.DisplayProgressBar("Replacing TMP Font", prefabPath, progress);

                    bool prefabChanged = false;
                    GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
                    try
                    {
                        TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
                        for (int j = 0; j < texts.Length; j++)
                        {
                            TMP_Text text = texts[j];
                            if (text.font == targetFont)
                            {
                                continue;
                            }

                            text.font = targetFont;
                            EditorUtility.SetDirty(text);
                            prefabChanged = true;
                            changedComponentCount++;
                        }

                        if (prefabChanged)
                        {
                            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                            changedPrefabCount++;
                        }
                    }
                    finally
                    {
                        PrefabUtility.UnloadPrefabContents(root);
                    }
                }
            }
            catch (Exception exception)
            {
                TEngine.Log.Error($"[TMPFontBatchReplacer] TMP 字体批量替换失败。异常={exception.Message}");
                EditorUtility.DisplayDialog("Replace Failed", exception.Message, "OK");
                return;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            TEngine.Log.Info($"[TMPFontBatchReplacer] TMP 字体批量替换完成。修改Prefab={changedPrefabCount}，修改TMP={changedComponentCount}");
            EditorUtility.DisplayDialog("Done", $"Changed Prefabs: {changedPrefabCount}\nChanged TMP Components: {changedComponentCount}", "OK");
        }

        private static string BuildNodePath(Transform current, Transform root)
        {
            if (current == null)
            {
                return string.Empty;
            }

            if (current == root)
            {
                return current.name;
            }

            Stack<string> stack = new Stack<string>();
            Transform cursor = current;
            while (cursor != null)
            {
                stack.Push(cursor.name);
                if (cursor == root)
                {
                    break;
                }

                cursor = cursor.parent;
            }

            return string.Join("/", stack);
        }
    }
}
