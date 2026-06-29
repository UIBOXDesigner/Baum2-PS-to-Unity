using System;
using System.Collections.Generic;
using System.Linq;
using TMPro; // <-- 【修复1】新增了对 TMP 的引用
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace Baum2.Editor
{
    public static class ElementFactory
    {
        public static readonly Dictionary<string, Func<Dictionary<string, object>, Element, Element>> Generator = new Dictionary<string, Func<Dictionary<string, object>, Element, Element>>()
        {
            { "Root", (d, p) => new RootElement(d, p) },
            { "Image", (d, p) => new ImageElement(d, p) },
            { "Mask", (d, p) => new MaskElement(d, p) },
            { "Group", (d, p) => new GroupElement(d, p) },
            { "Panel", (d, p) => new PanelElement(d, p) },
            { "Text", (d, p) => new TextElement(d, p) },
            { "Button", (d, p) => new ButtonElement(d, p) },
            { "List", (d, p) => new ListElement(d, p) },
            { "ScrollRect", (d, p) => new ScrollRectElement(d, p) },
            { "Slider", (d, p) => new SliderElement(d, p) },
            { "Scrollbar", (d, p) => new ScrollbarElement(d, p) },
            { "Toggle", (d, p) => new ToggleElement(d, p) },
            { "ToggleGroup", (d, p) => new ToggleGroupElement(d, p) },
            { "InputField", (d, p) => new InputFieldElement(d, p) },
            { "Dropdown", (d, p) => new DropdownElement(d, p) },
            { "GridLayoutGroup", (d, p) => new GridLayoutGroupElement(d, p) },
            { "VerticalLayoutGroup", (d, p) => new VerticalLayoutGroupElement(d, p) },
            { "HorizontalLayoutGroup", (d, p) => new HorizontalLayoutGroupElement(d, p) },
            { "CanvasGroup", (d, p) => new CanvasGroupElement(d, p) },
        };

        public static Element Generate(Dictionary<string, object> json, Element parent)
        {
            var type = json.Get("type");
            Assert.IsTrue(Generator.ContainsKey(type), "[Baum2] Unknown type: " + type);
            return Generator[type](json, parent);
        }
    }

    public abstract class Element
    {
        public string name;
        public string nodeName;
        protected string pivot;
        protected bool stretchX;
        protected bool stretchY;
        protected Element parent;

        // 【修复2】找回了丢失的旋转和缩放解析
        protected float rotation = 0f;
        protected Vector2 scale = Vector2.one;

        public abstract GameObject Render(Renderer renderer);
        public abstract Area CalcArea();

        protected Element(Dictionary<string, object> json, Element parent)
        {
            this.parent = parent;
            name = json.Get("name");
            nodeName = BaumNameNormalizer.Normalize(name);
            if (json.ContainsKey("pivot")) pivot = json.Get("pivot");
            if (json.ContainsKey("stretchxy") || json.ContainsKey("stretchx") || (parent != null ? parent.stretchX : false)) stretchX = true;
            if (json.ContainsKey("stretchxy") || json.ContainsKey("stretchy") || (parent != null ? parent.stretchY : false)) stretchY = true;

            // 解析旋转与缩放数值
            if (json.ContainsKey("rotation")) rotation = Convert.ToSingle(json["rotation"]);
            if (json.ContainsKey("scale")) 
            {
                float s = Convert.ToSingle(json["scale"]);
                scale = new Vector2(s, s);
            }
            if (json.ContainsKey("scaleX")) scale.x = Convert.ToSingle(json["scaleX"]);
            if (json.ContainsKey("scaleY")) scale.y = Convert.ToSingle(json["scaleY"]);
        }

        protected GameObject CreateUIGameObject(Renderer renderer)
        {
            var go = new GameObject(nodeName);
            go.AddComponent<RectTransform>();
            return go;
        }

        protected void SetPivot(GameObject root, Renderer renderer)
        {
            if (string.IsNullOrEmpty(pivot)) pivot = "none";

            var rect = root.GetComponent<RectTransform>();
            var pivotMin = rect.anchorMin;
            var pivotMax = rect.anchorMax;
            var sizeDelta = rect.sizeDelta;

            if (pivot.Contains("bottom"))
            {
                pivotMin.y = 0.0f;
                pivotMax.y = 0.0f;
                sizeDelta.y = CalcArea().Height;
            }
            else if (pivot.Contains("top"))
            {
                pivotMin.y = 1.0f;
                pivotMax.y = 1.0f;
                sizeDelta.y = CalcArea().Height;
            }
            else if (pivot.Contains("middle"))
            {
                pivotMin.y = 0.5f;
                pivotMax.y = 0.5f;
                sizeDelta.y = CalcArea().Height;
            }
            if (pivot.Contains("left"))
            {
                pivotMin.x = 0.0f;
                pivotMax.x = 0.0f;
                sizeDelta.x = CalcArea().Width;
            }
            else if (pivot.Contains("right"))
            {
                pivotMin.x = 1.0f;
                pivotMax.x = 1.0f;
                sizeDelta.x = CalcArea().Width;
            }
            else if (pivot.Contains("center"))
            {
                pivotMin.x = 0.5f;
                pivotMax.x = 0.5f;
                sizeDelta.x = CalcArea().Width;
            }

            rect.anchorMin = pivotMin;
            rect.anchorMax = pivotMax;
            rect.sizeDelta = sizeDelta;

            // 【修复3】应用旋转与缩放
            rect.localRotation = Quaternion.Euler(0, 0, rotation);
            rect.localScale = new Vector3(scale.x, scale.y, 1f);
        }

        protected void SetStretch(GameObject root, Renderer renderer)
        {
            if (!stretchX && !stretchY) return;

            var parentSize = parent != null ? parent.CalcArea().Size : renderer.CanvasSize;
            var rect = root.GetComponent<RectTransform>();
            var pivotPosMin = new Vector2(0.5f, 0.5f);
            var pivotPosMax = new Vector2(0.5f, 0.5f);
            var sizeDelta = rect.sizeDelta;

            if (stretchX)
            {
                pivotPosMin.x = 0.0f;
                pivotPosMax.x = 1.0f;
                sizeDelta.x = CalcArea().Width - parentSize.x;
            }

            if (stretchY)
            {
                pivotPosMin.y = 0.0f;
                pivotPosMax.y = 1.0f;
                sizeDelta.y = CalcArea().Height - parentSize.y;
            }

            rect.anchorMin = pivotPosMin;
            rect.anchorMax = pivotPosMax;
            rect.sizeDelta = sizeDelta;
        }
    }

    public class GroupElement : Element
    {
        protected readonly List<Element> elements;
        private Area areaCache;

        public GroupElement(Dictionary<string, object> json, Element parent, bool resetStretch = false) : base(json, parent)
        {
            elements = new List<Element>();
            var jsonElements = json.Get<List<object>>("elements") ?? new List<object>();
            foreach (var jsonElement in jsonElements)
            {
                var x = stretchX;
                var y = stretchY;
                if (resetStretch)
                {
                    stretchX = false;
                    stretchY = false;
                }
                elements.Add(ElementFactory.Generate(jsonElement as Dictionary<string, object>, this));
                stretchX = x;
                stretchY = y;
            }
            elements.Reverse();
            areaCache = CalcAreaInternal();
        }

        public override GameObject Render(Renderer renderer)
        {
            var go = CreateSelf(renderer);

            RenderChildren(renderer, go);

            SetStretch(go, renderer);
            SetPivot(go, renderer);
            return go;
        }

        protected virtual GameObject CreateSelf(Renderer renderer)
        {
            var go = CreateUIGameObject(renderer);

            var rect = go.GetComponent<RectTransform>();
            var area = CalcArea();
            
            // 【修复4】空组安全渲染
            if (area.Empty)
            {
                rect.sizeDelta = Vector2.zero;
                rect.anchoredPosition = Vector2.zero;
            }
            else
            {
                rect.sizeDelta = area.Size;
                rect.anchoredPosition = renderer.CalcPosition(area.Min, area.Size);
            }

            SetMaskImage(renderer, go);
            return go;
        }

        protected void SetMaskImage(Renderer renderer, GameObject go)
        {
            var maskSource = elements.Find(x => x is MaskElement);
            if (maskSource == null) return;

            elements.Remove(maskSource);
            var maskImage = go.AddComponent<Image>();
            maskImage.raycastTarget = false;

            var dummyMaskImage = maskSource.Render(renderer);
            dummyMaskImage.transform.SetParent(go.transform);
            dummyMaskImage.GetComponent<Image>().CopyTo(maskImage);
            GameObject.DestroyImmediate(dummyMaskImage);

            var mask = go.AddComponent<Mask>();
            mask.showMaskGraphic = false;
        }

        protected void RenderChildren(Renderer renderer, GameObject root, Action<GameObject, Element> callback = null)
        {
            foreach (var element in elements)
            {
                var go = element.Render(renderer);
                var rectTransform = go.GetComponent<RectTransform>();
                var sizeDelta = rectTransform.sizeDelta;
                
                // 【修复5】保存缩放，防止 Unity 洗掉我们的缩放指令
                var localScale = rectTransform.localScale; 
                
                go.transform.SetParent(root.transform, true);
                rectTransform.sizeDelta = sizeDelta;
                rectTransform.localScale = localScale; 
                
                if (callback != null) callback(go, element);
            }
        }

        private Area CalcAreaInternal()
        {
            var area = Area.None();
            foreach (var element in elements) area.Merge(element.CalcArea());
            return area;
        }

        public override Area CalcArea()
        {
            return areaCache;
        }
    }

    public class PanelElement : GroupElement
    {
        public PanelElement(Dictionary<string, object> json, Element parent) : base(json, parent)
        {
        }
    }

    public sealed class ScrollRectElement : GroupElement
    {
        private string scroll;

        public ScrollRectElement(Dictionary<string, object> json, Element parent) : base(json, parent, true)
        {
            if (json.ContainsKey("scroll")) scroll = json.Get("scroll");
        }

        public override GameObject Render(Renderer renderer)
        {
            var go = CreateSelf(renderer);
            var content = new GameObject("Content");
            content.AddComponent<RectTransform>();
            content.transform.SetParent(go.transform, false);

            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;
            contentRect.localScale = Vector3.one;

            RenderChildren(renderer, content);

            var image = go.GetComponent<Image>();
            if (image == null)
            {
                image = go.AddComponent<Image>();
                image.color = new Color(0f, 0f, 0f, 0f);
                image.raycastTarget = true;
            }
            if (go.GetComponent<RectMask2D>() == null) go.AddComponent<RectMask2D>();

            var scrollRect = go.AddComponent<ScrollRect>();
            scrollRect.content = contentRect;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.viewport = go.GetComponent<RectTransform>();
            if (scroll == "horizontal")
            {
                scrollRect.horizontal = true;
                scrollRect.vertical = false;
            }
            else
            {
                scrollRect.horizontal = false;
                scrollRect.vertical = true;
            }

            SetStretch(go, renderer);
            SetPivot(go, renderer);
            return go;
        }
    }

    public sealed class ToggleGroupElement : GroupElement
    {
        public ToggleGroupElement(Dictionary<string, object> json, Element parent) : base(json, parent)
        {
        }

        public override GameObject Render(Renderer renderer)
        {
            var go = base.Render(renderer);
            if (go.GetComponent<ToggleGroup>() == null) go.AddComponent<ToggleGroup>();
            return go;
        }
    }

    public sealed class CanvasGroupElement : GroupElement
    {
        public CanvasGroupElement(Dictionary<string, object> json, Element parent) : base(json, parent)
        {
        }

        public override GameObject Render(Renderer renderer)
        {
            var go = base.Render(renderer);
            if (go.GetComponent<CanvasGroup>() == null) go.AddComponent<CanvasGroup>();
            return go;
        }
    }

    public sealed class InputFieldElement : GroupElement
    {
        public InputFieldElement(Dictionary<string, object> json, Element parent) : base(json, parent)
        {
        }

        public override GameObject Render(Renderer renderer)
        {
            var go = base.Render(renderer);
            var input = go.AddComponent<TMP_InputField>();
            var texts = go.GetComponentsInChildren<TextMeshProUGUI>(true);
            if (texts.Length > 0) input.textComponent = texts[0];
            if (texts.Length > 1) input.placeholder = texts[1];
            return go;
        }
    }

    public sealed class DropdownElement : GroupElement
    {
        public DropdownElement(Dictionary<string, object> json, Element parent) : base(json, parent)
        {
        }

        public override GameObject Render(Renderer renderer)
        {
            var go = base.Render(renderer);
            if (go.GetComponent<TMP_Dropdown>() == null) go.AddComponent<TMP_Dropdown>();
            return go;
        }
    }

    public sealed class GridLayoutGroupElement : GroupElement
    {
        public GridLayoutGroupElement(Dictionary<string, object> json, Element parent) : base(json, parent)
        {
        }

        public override GameObject Render(Renderer renderer)
        {
            var go = base.Render(renderer);
            var grid = go.AddComponent<UnityEngine.UI.GridLayoutGroup>();
            grid.childAlignment = TextAnchor.MiddleCenter;
            grid.enabled = false; // 保留 PS 像素布局，不在导入瞬间重排子节点。
            return go;
        }
    }

    public sealed class VerticalLayoutGroupElement : GroupElement
    {
        public VerticalLayoutGroupElement(Dictionary<string, object> json, Element parent) : base(json, parent)
        {
        }

        public override GameObject Render(Renderer renderer)
        {
            var go = base.Render(renderer);
            var layout = go.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.enabled = false; // 保留 PS 像素布局，需要动态排版时再手动开启。
            return go;
        }
    }

    public sealed class HorizontalLayoutGroupElement : GroupElement
    {
        public HorizontalLayoutGroupElement(Dictionary<string, object> json, Element parent) : base(json, parent)
        {
        }

        public override GameObject Render(Renderer renderer)
        {
            var go = base.Render(renderer);
            var layout = go.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            layout.enabled = false; // 保留 PS 像素布局，需要动态排版时再手动开启。
            return go;
        }
    }

    public class RootElement : GroupElement
    {
        private Vector2 sizeDelta;

        public RootElement(Dictionary<string, object> json, Element parent) : base(json, parent)
        {
        }

        protected override GameObject CreateSelf(Renderer renderer)
        {
            var go = CreateUIGameObject(renderer);

            var rect = go.GetComponent<RectTransform>();
            sizeDelta = renderer.CanvasSize;
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = Vector2.zero;

            SetMaskImage(renderer, go);

            SetStretch(go, renderer);
            SetPivot(go, renderer);
            return go;
        }

        public override Area CalcArea()
        {
            return new Area(-sizeDelta / 2.0f, sizeDelta / 2.0f);
        }
    }

    public class ImageElement : Element
    {
        private string spriteName;
        private Vector2 canvasPosition;
        private Vector2 sizeDelta;
        private float opacity;

        public ImageElement(Dictionary<string, object> json, Element parent) : base(json, parent)
        {
            spriteName = json.Get("image");
            canvasPosition = json.GetVector2("x", "y");
            sizeDelta = json.GetVector2("w", "h");
            opacity = json.GetFloat("opacity");
        }

        public override GameObject Render(Renderer renderer)
        {
            var go = CreateUIGameObject(renderer);

            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = renderer.CalcPosition(canvasPosition, sizeDelta);
            rect.sizeDelta = sizeDelta;

            var image = go.AddComponent<Image>();
            image.sprite = renderer.GetSprite(spriteName);
            image.type = Image.Type.Sliced;
            image.color = new Color(1.0f, 1.0f, 1.0f, opacity / 100.0f);

            SetStretch(go, renderer);
            SetPivot(go, renderer);

            return go;
        }

        public override Area CalcArea()
        {
            return Area.FromPositionAndSize(canvasPosition, sizeDelta);
        }
    }

    public sealed class MaskElement : ImageElement
    {
        public MaskElement(Dictionary<string, object> json, Element parent) : base(json, parent)
        {
        }
    }

    public sealed class TextElement : Element
    {
        private string message;
        private string font;
        private float fontSize;
        private string align;
        private float virtualHeight;
        private Color fontColor;
        private Vector2 canvasPosition;
        private Vector2 sizeDelta;
        private bool enableStroke;
        private int strokeSize;
        private Color strokeColor;
        private string type;

        private static Color SafeHexToColor(string value, Color fallback)
        {
            if (string.IsNullOrEmpty(value)) return fallback;

            value = value.Trim().TrimStart('#');
            if (value.Length == 3)
            {
                value = new string(new[] { value[0], value[0], value[1], value[1], value[2], value[2] });
            }
            else if (value.Length < 6)
            {
                value = value.PadLeft(6, '0');
            }
            else if (value.Length > 6)
            {
                value = value.Substring(value.Length - 6);
            }

            try
            {
                return EditorUtil.HexToColor(value);
            }
            catch
            {
                return fallback;
            }
        }

        public TextElement(Dictionary<string, object> json, Element parent) : base(json, parent)
        {
            message = json.Get("text");
            font = json.Get("font");
            fontSize = json.GetFloat("size");
            align = json.Get("align");
            type = json.Get("textType");
            if (json.ContainsKey("strokeSize"))
            {
                enableStroke = true;
                strokeSize = json.GetInt("strokeSize");
                
                var strokeStr = json.Get("strokeColor");
                strokeColor = SafeHexToColor(strokeStr, Color.black);
            }
            
            // 【修复6】增加颜色兜底，防止出现 null 引发报错崩溃
            var colorStr = json.Get("color");
            fontColor = SafeHexToColor(colorStr, Color.black);
            
            sizeDelta = json.GetVector2("w", "h");
            canvasPosition = json.GetVector2("x", "y");
            virtualHeight = json.GetFloat("vh");
        }

        public override GameObject Render(Renderer renderer)
        {
            var go = CreateUIGameObject(renderer);

            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = renderer.CalcPosition(canvasPosition, sizeDelta);
            rect.sizeDelta = sizeDelta;

            // 【修复7】正式替换为 TextMeshProUGUI
            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = message;

            // 【修复8】加载 TMP 字体资产
            string fontPath = "Assets/AssetRaw/Fonts/SourceHanSerif-Bold SDF.asset"; 
            var customFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
            if (customFont != null)
            {
                text.font = customFont;
            }
            else
            {
                Debug.LogWarning("[Baum2] 找不到指定的 TMP 字体文件，请检查路径: " + fontPath);
            }

            text.fontSize = Mathf.RoundToInt(fontSize);
            text.color = fontColor;

            // 【修复9】TMP 特有的溢出和换行属性转换
            bool middle = true;
            if (type == "point")
            {
                text.overflowMode = TextOverflowModes.Overflow;
                text.enableWordWrapping = false;
                middle = true;
            }
            else if (type == "paragraph")
            {
                text.overflowMode = TextOverflowModes.Overflow;
                text.enableWordWrapping = true;
                middle = !message.Contains("\n");
            }

            var fixedPos = rect.anchoredPosition;
            switch (align)
            {
                case "left":
                    text.alignment = middle ? TextAlignmentOptions.Left : TextAlignmentOptions.TopLeft;
                    rect.pivot = new Vector2(0.0f, 0.5f);
                    fixedPos.x -= sizeDelta.x / 2.0f;
                    break;
                case "center":
                    text.alignment = middle ? TextAlignmentOptions.Center : TextAlignmentOptions.Top;
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    break;
                case "right":
                    text.alignment = middle ? TextAlignmentOptions.Right : TextAlignmentOptions.TopRight;
                    rect.pivot = new Vector2(1.0f, 0.5f);
                    fixedPos.x += sizeDelta.x / 2.0f;
                    break;
            }
            rect.anchoredPosition = fixedPos;

            var d = rect.sizeDelta;
            d.y = virtualHeight;
            rect.sizeDelta = d;

            if (enableStroke)
            {
                var outline = go.AddComponent<Outline>();
                outline.effectColor = strokeColor;
                outline.effectDistance = new Vector2(strokeSize / 2.0f, -strokeSize / 2.0f);
                outline.useGraphicAlpha = false;
            }

            SetStretch(go, renderer);
            SetPivot(go, renderer);
            return go;
        }

        public override Area CalcArea()
        {
            return Area.FromPositionAndSize(canvasPosition, sizeDelta);
        }
    }

    public sealed class ButtonElement : GroupElement
    {
        public ButtonElement(Dictionary<string, object> json, Element parent) : base(json, parent)
        {
        }

        public override GameObject Render(Renderer renderer)
        {
            var go = CreateSelf(renderer);

            Graphic lastImage = null;
            RenderChildren(renderer, go, (g, element) =>
            {
                if (lastImage == null && element is ImageElement) lastImage = g.GetComponent<Image>();
            });

            var button = go.AddComponent<Button>();
            if (lastImage != null)
            {
                button.targetGraphic = lastImage;
            }

            SetStretch(go, renderer);
            SetPivot(go, renderer);
            return go;
        }
    }

    public sealed class ListElement : GroupElement
    {
        private string scroll;

        public ListElement(Dictionary<string, object> json, Element parent) : base(json, parent, true)
        {
            if (json.ContainsKey("scroll")) scroll = json.Get("scroll");
        }

        public override GameObject Render(Renderer renderer)
        {
            var go = CreateSelf(renderer);
            var content = new GameObject("Content");
            content.AddComponent<RectTransform>();
            content.transform.SetParent(go.transform);

            SetupScroll(go, content);
            SetMaskImage(renderer, go, content);

            var items = CreateItems(renderer, go);
            SetupList(go, items, content);

            SetStretch(go, renderer);
            SetPivot(go, renderer);
            return go;
        }

        private void SetupScroll(GameObject go, GameObject content)
        {
            var scrollRect = go.AddComponent<ScrollRect>();
            scrollRect.content = content.GetComponent<RectTransform>();

            var layoutGroup = content.AddComponent<ListLayoutGroup>();
            if (scroll == "vertical")
            {
                scrollRect.vertical = true;
                scrollRect.horizontal = false;
                layoutGroup.Scroll = Scroll.Vertical;
            }
            else if (scroll == "horizontal")
            {
                scrollRect.vertical = false;
                scrollRect.horizontal = true;
                layoutGroup.Scroll = Scroll.Horizontal;
            }
        }

        private void SetMaskImage(Renderer renderer, GameObject go, GameObject content)
        {
            var maskImage = go.AddComponent<Image>();

            var dummyMaskImage = CreateDummyMaskImage(renderer);
            dummyMaskImage.transform.SetParent(go.transform);
            go.GetComponent<RectTransform>().CopyTo(content.GetComponent<RectTransform>());
            content.GetComponent<RectTransform>().localPosition = Vector3.zero;
            dummyMaskImage.GetComponent<Image>().CopyTo(maskImage);
            GameObject.DestroyImmediate(dummyMaskImage);

            maskImage.color = new Color(0.0f, 0.0f, 0.0f, 0.0f);
            go.AddComponent<RectMask2D>();
        }

        private GameObject CreateDummyMaskImage(Renderer renderer)
        {
            var maskElement = elements.Find(x => (x is ImageElement && x.name.Equals("Area", StringComparison.OrdinalIgnoreCase)));
            if (maskElement == null) throw new Exception(string.Format("{0} Area not found", name));
            elements.Remove(maskElement);

            var maskImage = maskElement.Render(renderer);
            maskImage.SetActive(false);
            return maskImage;
        }

        private List<GameObject> CreateItems(Renderer renderer, GameObject go)
        {
            var items = new List<GameObject>();
            foreach (var element in elements)
            {
                var item = element as GroupElement;
                if (item == null) throw new Exception(string.Format("{0}'s element {1} is not group", name, element.name));

                var itemObject = item.Render(renderer);
                itemObject.transform.SetParent(go.transform);

                var rect = itemObject.GetComponent<RectTransform>();
                var originalPosition = rect.anchoredPosition;
                if (scroll == "vertical")
                {
                    rect.anchorMin = new Vector2(0.5f, 1.0f);
                    rect.anchorMax = new Vector2(0.5f, 1.0f);
                    rect.anchoredPosition = new Vector2(originalPosition.x, -rect.rect.height / 2f);
                }
                else if (scroll == "horizontal")
                {
                    rect.anchorMin = new Vector2(0.0f, 0.5f);
                    rect.anchorMax = new Vector2(0.0f, 0.5f);
                    rect.anchoredPosition = new Vector2(rect.rect.width / 2f, originalPosition.y);
                }

                items.Add(itemObject);
            }
            return items;
        }

        private void SetupList(GameObject go, List<GameObject> itemSources, GameObject content)
        {
            var list = go.AddComponent<List>();
            list.ItemSources = itemSources;
            list.LayoutGroup = content.GetComponent<ListLayoutGroup>();
        }
    }

    public sealed class SliderElement : GroupElement
    {
        public SliderElement(Dictionary<string, object> json, Element parent) : base(json, parent)
        {
        }

        public override GameObject Render(Renderer renderer)
        {
            var go = CreateSelf(renderer);

            RectTransform fillRect = null;
            RenderChildren(renderer, go, (g, element) =>
            {
                var image = element as ImageElement;
                if (fillRect != null || image == null) return;

                g.GetComponent<Image>().raycastTarget = false;
                if (element.name.Equals("Fill", StringComparison.OrdinalIgnoreCase)) fillRect = g.GetComponent<RectTransform>();
            });

            var slider = go.AddComponent<Slider>();
            slider.transition = Selectable.Transition.None;
            slider.interactable = false;
            if (fillRect != null)
            {
                fillRect.localScale = Vector2.zero;
                fillRect.anchorMin = Vector2.zero;
                fillRect.anchorMax = Vector2.one;
                fillRect.anchoredPosition = Vector2.zero;
                fillRect.sizeDelta = Vector2.zero;
                fillRect.localScale = Vector3.one;
                slider.fillRect = fillRect;
            }

            SetStretch(go, renderer);
            SetPivot(go, renderer);
            return go;
        }
    }

    public sealed class ScrollbarElement : GroupElement
    {
        public ScrollbarElement(Dictionary<string, object> json, Element parent) : base(json, parent)
        {
        }

        public override GameObject Render(Renderer renderer)
        {
            var go = CreateSelf(renderer);

            RectTransform handleRect = null;
            RenderChildren(renderer, go, (g, element) =>
            {
                var image = element as ImageElement;
                if (handleRect != null || image == null) return;
                if (element.name.Equals("Handle", StringComparison.OrdinalIgnoreCase)) handleRect = g.GetComponent<RectTransform>();
                g.GetComponent<Image>().raycastTarget = false;
            });

            var scrollbar = go.AddComponent<Scrollbar>();
            var handleImage = handleRect == null ? null : handleRect.GetComponent<Image>();
            if (handleImage != null)
            {
                handleRect.anchoredPosition = Vector2.zero;
                handleRect.anchorMin = new Vector2(0.0f, 0.0f);
                handleRect.anchorMax = new Vector2(1.0f, 0.0f);

                scrollbar.direction = Scrollbar.Direction.BottomToTop;
                scrollbar.value = 1.0f;
                scrollbar.targetGraphic = handleImage;
                scrollbar.handleRect = handleRect;

                handleRect.sizeDelta = Vector2.zero;
            }

            SetStretch(go, renderer);
            SetPivot(go, renderer);
            return go;
        }
    }

    public sealed class ToggleElement : GroupElement
    {
        public ToggleElement(Dictionary<string, object> json, Element parent) : base(json, parent)
        {
        }

        public override GameObject Render(Renderer renderer)
        {
            var go = CreateSelf(renderer);

            Graphic lastImage = null;
            Graphic checkImage = null;
            RenderChildren(renderer, go, (g, element) =>
            {
                var image = element as ImageElement;
                if (image == null) return;
                if (lastImage == null) lastImage = g.GetComponent<Image>();
                if (element.name.Contains("Check") || element.name.Contains("check")) checkImage = g.GetComponent<Image>();
            });

            var toggle = go.AddComponent<Toggle>();
            toggle.targetGraphic = lastImage;
            toggle.graphic = checkImage;

            SetStretch(go, renderer);
            SetPivot(go, renderer);
            return go;
        }
    }

    public sealed class NullElement : Element
    {
        public NullElement(Dictionary<string, object> json, Element parent) : base(json, parent)
        {
        }

        public override GameObject Render(Renderer renderer)
        {
            var go = CreateUIGameObject(renderer);
            SetStretch(go, renderer);
            SetPivot(go, renderer);
            return go;
        }

        public override Area CalcArea()
        {
            return Area.None();
        }
    }
}