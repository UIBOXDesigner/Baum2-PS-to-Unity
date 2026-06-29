using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;
using UnityEngine.UI;

namespace Baum2.Editor
{
    public sealed class PrefabCreator
    {
        private static readonly string[] Versions = { "0.6.0", "0.6.1" };
        private readonly string spriteRootPath;
        private readonly string fontRootPath;
        private readonly string assetPath;

        public PrefabCreator(string spriteRootPath, string fontRootPath, string assetPath)
        {
            this.spriteRootPath = spriteRootPath;
            this.fontRootPath = fontRootPath;
            this.assetPath = assetPath;
        }

       public GameObject Create()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
            }

            var text = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath).text;
            var json = MiniJSON.Json.Deserialize(text) as Dictionary<string, object>;
            var info = json.GetDic("info");
            Validation(info);

            var canvas = info.GetDic("canvas");
            var imageSize = canvas.GetDic("image");
            var canvasSize = canvas.GetDic("size");
            var baseSize = canvas.GetDic("base");
            var renderer = new Renderer(spriteRootPath, fontRootPath, imageSize.GetVector2("w", "h"), canvasSize.GetVector2("w", "h"), baseSize.GetVector2("x", "y"));
            
            // 1. 生成原始的嵌套节点
            var rootElement = ElementFactory.Generate(json.GetDic("root"), null);
            var root = rootElement.Render(renderer);

            // ==========================================
            // 【新增：消除冗余的同名父级节点】
            // 如果 Root 下只有一个子节点，且名字和 Root 一样（说明是我们在 PS 里获取的最外层组）
            // 就把这个子节点“提拔”为真正的根节点，并删掉原来的外壳。
            // ==========================================
            if (root.transform.childCount == 1)
            {
                Transform singleChild = root.transform.GetChild(0);
                if (singleChild.name == root.name)
                {
                    var realRoot = singleChild.gameObject;
                    realRoot.transform.SetParent(null); // 解除父子关系
                    GameObject.DestroyImmediate(root);  // 销毁多余的旧壳子
                    root = realRoot;                    // 将真实节点指派为 root 供后续挂载
                }
            }
            // ==========================================

            // 给真正的根节点挂载基础 UI 组件
            root.AddComponent<Canvas>();
            root.AddComponent<GraphicRaycaster>();

            // (这里是你之前可能修改过的自定义组件挂载逻辑)
            // root.AddComponent<MyCustomUIView>(); 

            Postprocess(root);

            // 递归归一化所有GameObject名称（安全兜底，确保无遗漏）
            NormalizeHierarchyNames(root.transform);

            // (这部分是默认的 Cache，如果你不需要了可以注释掉)
            // var cache = root.AddComponent<Cache>();
            // cache.CreateCache(root.transform);

            return root;
        }

        /// <summary>
        /// 递归遍历所有子节点，对仍使用原始PS图层名的GameObject进行归一化。
        /// 这是兜底逻辑，正常路径已在 Element.CreateUIGameObject 中完成归一化。
        /// </summary>
        private static void NormalizeHierarchyNames(Transform t)
        {
            if (t == null) return;

            // 尝试归一化当前节点名
            string normalized = BaumNameNormalizer.Normalize(t.name);
            if (normalized != t.name)
            {
                t.name = normalized;
            }

            foreach (Transform child in t)
            {
                NormalizeHierarchyNames(child);
            }
        }

        private void Postprocess(GameObject go)
        {
            var methods = Assembly
                .GetExecutingAssembly()
                .GetTypes()
                .Where(x => x.IsSubclassOf(typeof(BaumPostprocessor)))
                .Select(x => x.GetMethod("OnPostprocessPrefab"));
            foreach (var method in methods)
            {
                method.Invoke(null, new object[] { go });
            }
        }

        public void Validation(Dictionary<string, object> info)
        {
            var version = info.Get("version");
            if (!Versions.Contains(version)) throw new Exception(string.Format("version {0} is not supported", version));
        }
    }

    public class Renderer
    {
        private readonly string spriteRootPath;
        private readonly string fontRootPath;
        private readonly Vector2 imageSize;
        public Vector2 CanvasSize { get; private set; }
        private readonly Vector2 basePosition;

        public Renderer(string spriteRootPath, string fontRootPath, Vector2 imageSize, Vector2 canvasSize, Vector2 basePosition)
        {
            this.spriteRootPath = spriteRootPath;
            this.fontRootPath = fontRootPath;
            this.imageSize = imageSize;
            CanvasSize = canvasSize;
            this.basePosition = basePosition;
        }

        public Sprite GetSprite(string spriteName)
        {
            var fullPath = Path.Combine(spriteRootPath, spriteName) + ".png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(fullPath);

            // Unity 默认可能把 Photoshop 导出的 PNG 当作 Default Texture，
            // LoadAssetAtPath<Sprite> 会返回 null。这里自动修正为 Sprite 后重新导入。
            if (sprite == null)
            {
                var importer = AssetImporter.GetAtPath(fullPath) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.alphaIsTransparency = true;
                    importer.mipmapEnabled = false;
                    importer.SaveAndReimport();
                    sprite = AssetDatabase.LoadAssetAtPath<Sprite>(fullPath);
                }
            }

            Assert.IsNotNull(sprite, string.Format("[Baum2] sprite \"{0}\" is not found or cannot be imported as Sprite. fullPath:{1}", spriteName, fullPath));
            return sprite;
        }

        public Font GetFont(string fontName)
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(Path.Combine(fontRootPath, fontName) + ".ttf");
            if (font == null) font = AssetDatabase.LoadAssetAtPath<Font>(Path.Combine(fontRootPath, fontName) + ".otf");
            Assert.IsNotNull(font, string.Format("[Baum2] font \"{0}\" is not found", fontName));
            return font;
        }

        public Vector2 CalcPosition(Vector2 position, Vector2 size)
        {
            return CalcPosition(position + size / 2.0f);
        }

        private Vector2 CalcPosition(Vector2 position)
        {
            var tmp = position - basePosition;
            tmp.y *= -1.0f;
            return tmp;
        }

        public Vector2[] GetFourCorners()
        {
            var corners = new Vector2[4];
            corners[0] = CalcPosition(Vector2.zero) + (imageSize - CanvasSize) / 2.0f;
            corners[2] = CalcPosition(imageSize) - (imageSize - CanvasSize) / 2.0f;
            return corners;
        }
    }

    public class Area
    {
        public bool Empty { get; private set; }
        public Vector2 Min { get; private set; }
        public Vector2 Max { get; private set; }
        public Vector2 Avg { get { return (Min + Max) / 2.0f; } }
        public Vector2 Center { get { return (Min + Max) / 2.0f; } }
        public float Width { get { return Mathf.Abs(Max.x - Min.x); } }
        public float Height { get { return Mathf.Abs(Max.y - Min.y); } }
        public Vector2 Size { get { return new Vector2(Width, Height); } }

        public Area()
        {
            Empty = true;
        }

        public Area(Vector2 min, Vector2 max)
        {
            Min = min;
            Max = max;
            Empty = false;
        }

        public static Area FromPositionAndSize(Vector2 position, Vector2 size)
        {
            return new Area(position, position + size);
        }

        public static Area None()
        {
            return new Area();
        }

        public void Merge(Area other)
        {
            if (other.Empty) return;
            if (Empty)
            {
                Min = other.Min;
                Max = other.Max;
                Empty = false;
                return;
            }

            if (other.Min.x < Min.x) Min = new Vector2(other.Min.x, Min.y);
            if (other.Min.y < Min.y) Min = new Vector2(Min.x, other.Min.y);
            if (other.Max.x > Max.x) Max = new Vector2(other.Max.x, Max.y);
            if (other.Max.y > Max.y) Max = new Vector2(Max.x, other.Max.y);
        }
    }

    public static class JsonExtensions
    {
        public static string Get(this Dictionary<string, object> json, string key)
        {
            if (json == null || !json.ContainsKey(key) || json[key] == null) return null;
            return Convert.ToString(json[key], CultureInfo.InvariantCulture);
        }

        public static float GetFloat(this Dictionary<string, object> json, string key)
        {
            if (json == null || !json.ContainsKey(key) || json[key] == null) return 0f;
            return Convert.ToSingle(json[key], CultureInfo.InvariantCulture);
        }

        public static int GetInt(this Dictionary<string, object> json, string key)
        {
            return Mathf.RoundToInt(json.GetFloat(key));
        }

        public static T Get<T>(this Dictionary<string, object> json, string key) where T : class
        {
            if (json == null || !json.ContainsKey(key)) return null;
            return json[key] as T;
        }

        public static Dictionary<string, object> GetDic(this Dictionary<string, object> json, string key)
        {
            if (json == null || !json.ContainsKey(key)) return null;
            return json[key] as Dictionary<string, object>;
        }

        public static Vector2 GetVector2(this Dictionary<string, object> json, string keyX, string keyY)
        {
            return new Vector2(json.GetFloat(keyX), json.GetFloat(keyY));
        }
    }
}
