#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UIEquipmentGlow;

namespace UIEquipmentGlow.EditorTools
{
    public static class UIEquipmentGlowEditorTools
    {
        private const string Root = "Assets/UIEquipmentGlow";
        private const string MaterialDir = Root + "/Materials";
        private const string TextureDir = Root + "/Textures";

        [MenuItem("Tools/UI Equipment Glow/1. Generate Default Materials")]
        public static void GenerateDefaultMaterials()
        {
            EnsureFolder(MaterialDir);

            CreateOuterGlowMaterial("M_UI_Glow_Rare", new Color32(58, 167, 255, 255), 0.65f, 3f, 0.8f, 0.12f);
            CreateOuterGlowMaterial("M_UI_Glow_Epic", new Color32(180, 92, 255, 255), 1.05f, 4f, 1.1f, 0.22f);
            CreateOuterGlowMaterial("M_UI_Glow_Legendary", new Color32(255, 157, 40, 255), 1.55f, 5f, 1.35f, 0.32f);
            CreateOuterGlowMaterial("M_UI_Glow_Mythic", new Color32(255, 61, 61, 255), 2.1f, 6f, 1.55f, 0.42f);

            CreateFrameShineMaterial("M_UI_FrameShine_Gold", new Color32(255, 245, 175, 255), 1.8f, 0.1f, 0.65f, 0.785f);
            CreateFrameShineMaterial("M_UI_FrameShine_Red", new Color32(255, 210, 210, 255), 2.25f, 0.13f, 0.75f, 0.785f);
            CreateInnerRarityMaterial("M_UI_InnerRarity_Default", new Color32(255, 157, 40, 255), 0.55f, 0.75f);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("UI Equipment Glow default materials generated under Assets/UIEquipmentGlow/Materials.");
        }

        [MenuItem("Tools/UI Equipment Glow/2. Create Demo Item Cell")]
        public static void CreateDemoItemCell()
        {
            GenerateDefaultMaterials();
            PrepareDemoTexturesAsSprites();

            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasObj.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
            }

            Sprite bgSprite = LoadSprite(TextureDir + "/item_bg.png");
            Sprite frameSprite = LoadSprite(TextureDir + "/frame_mask.png");
            Sprite glowSprite = LoadSprite(TextureDir + "/glow_frame.png");
            Sprite sparkSprite = LoadSprite(TextureDir + "/spark_dot.png");

            GameObject root = new GameObject("Demo_Legendary_EquipmentCell", typeof(RectTransform));
            root.transform.SetParent(canvas.transform, false);
            RectTransform rootRt = root.GetComponent<RectTransform>();
            rootRt.sizeDelta = new Vector2(160, 160);
            rootRt.anchoredPosition = Vector2.zero;

            Image bg = CreateImage(root.transform, "Bg", bgSprite, new Vector2(126, 126), new Color32(42, 30, 24, 255));
            Image glow = CreateImage(root.transform, "Glow", glowSprite, new Vector2(166, 166), Color.white);
            Image frame = CreateImage(root.transform, "Frame", frameSprite, new Vector2(146, 146), Color.white);
            Image icon = CreateImage(root.transform, "Icon_Placeholder", bgSprite, new Vector2(86, 86), new Color32(210, 180, 120, 255));
            Image shine = CreateImage(root.transform, "Shine", frameSprite, new Vector2(146, 146), Color.white);
            GameObject sparkFXRoot = CreateSparkFX(root.transform, sparkSprite);

            Material glowMat = AssetDatabase.LoadAssetAtPath<Material>(MaterialDir + "/M_UI_Glow_Legendary.mat");
            Material shineMat = AssetDatabase.LoadAssetAtPath<Material>(MaterialDir + "/M_UI_FrameShine_Gold.mat");
            Material innerMat = AssetDatabase.LoadAssetAtPath<Material>(MaterialDir + "/M_UI_InnerRarity_Default.mat");
            glow.material = glowMat;
            shine.material = shineMat;
            bg.material = innerMat;

            UIEquipmentFrameEffect effect = root.AddComponent<UIEquipmentFrameEffect>();
            effect.backgroundImage = bg;
            effect.glowImage = glow;
            effect.frameImage = frame;
            effect.shineImage = shine;
            effect.pulseTarget = glow.rectTransform;
            effect.particleFXRoot = sparkFXRoot;
            effect.autoUseGlowAsPulseTarget = true;
            effect.outerGlowMaterial = glowMat;
            effect.frameShineMaterial = shineMat;
            effect.innerRarityMaterial = innerMat;
            effect.ApplyQuality(UIEquipmentQuality.Legendary);

            root.AddComponent<UIEquipmentFrameQualityBinder>();

            Selection.activeGameObject = root;
            EditorUtility.SetDirty(root);
            Debug.Log("Demo equipment cell created in current scene. Glow layer is the pulse target. Switch Quality to Mythic to preview the included UI spark FX.");
        }

        private static GameObject CreateSparkFX(Transform parent, Sprite sparkSprite)
        {
            GameObject root = new GameObject("ParticleFX_GoldSpark", typeof(RectTransform), typeof(UIEquipmentSparkFX));
            root.transform.SetParent(parent, false);
            RectTransform rt = root.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(184, 184);

            const int count = 14;
            RectTransform[] sparks = new RectTransform[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)count;
                float angle = t * Mathf.PI * 2f;
                float radius = 70f + ((i % 3) - 1) * 8f;
                Vector2 pos = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                float size = 4f + (i % 4) * 1.4f;

                Image spark = CreateImage(root.transform, "Spark_" + i.ToString("00"), sparkSprite, new Vector2(size, size), new Color32(255, 205, 78, 210));
                spark.rectTransform.anchoredPosition = pos;
                sparks[i] = spark.rectTransform;
            }

            UIEquipmentSparkFX fx = root.GetComponent<UIEquipmentSparkFX>();
            fx.sparks = sparks;
            fx.sparkColor = new Color32(255, 205, 78, 255);
            fx.rotateSpeed = 16f;
            fx.floatAmount = 3f;
            fx.twinkleSpeed = 1.15f;
            fx.alphaMin = 0.05f;
            fx.alphaMax = 0.85f;
            fx.scalePulseAmount = 0.20f;
            root.SetActive(false);
            return root;
        }

        private static Image CreateImage(Transform parent, string name, Sprite sprite, Vector2 size, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = size;

            Image image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            image.preserveAspect = true;
            return image;
        }

        private static void CreateOuterGlowMaterial(string name, Color color, float intensity, float size, float pulseSpeed, float pulseAmount)
        {
            Shader shader = Shader.Find("UI/Equipment/Outer Glow");
            if (shader == null)
            {
                Debug.LogError("Shader not found: UI/Equipment/Outer Glow. Reimport the package first.");
                return;
            }

            Material mat = LoadOrCreateMaterial(name, shader);
            mat.SetColor("_GlowColor", color);
            mat.SetFloat("_GlowIntensity", intensity);
            mat.SetFloat("_GlowSize", size);
            mat.SetFloat("_PulseSpeed", pulseSpeed);
            mat.SetFloat("_PulseAmount", pulseAmount);
            EditorUtility.SetDirty(mat);
        }

        private static void CreateFrameShineMaterial(string name, Color color, float intensity, float width, float speed, float angle)
        {
            Shader shader = Shader.Find("UI/Equipment/Frame Shine");
            if (shader == null)
            {
                Debug.LogError("Shader not found: UI/Equipment/Frame Shine. Reimport the package first.");
                return;
            }

            Material mat = LoadOrCreateMaterial(name, shader);
            mat.SetColor("_ShineColor", color);
            mat.SetFloat("_ShineIntensity", intensity);
            mat.SetFloat("_ShineWidth", width);
            mat.SetFloat("_ShineSpeed", speed);
            mat.SetFloat("_ShineAngle", angle);
            EditorUtility.SetDirty(mat);
        }

        private static void CreateInnerRarityMaterial(string name, Color color, float strength, float vignette)
        {
            Shader shader = Shader.Find("UI/Equipment/Inner Rarity Tint");
            if (shader == null)
            {
                Debug.LogError("Shader not found: UI/Equipment/Inner Rarity Tint. Reimport the package first.");
                return;
            }

            Material mat = LoadOrCreateMaterial(name, shader);
            mat.SetColor("_RarityColor", color);
            mat.SetFloat("_RarityStrength", strength);
            mat.SetFloat("_VignetteStrength", vignette);
            EditorUtility.SetDirty(mat);
        }

        private static Material LoadOrCreateMaterial(string name, Shader shader)
        {
            EnsureFolder(MaterialDir);
            string path = MaterialDir + "/" + name + ".mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.shader = shader;
            }
            return mat;
        }

        private static void PrepareDemoTexturesAsSprites()
        {
            PrepareTexture(TextureDir + "/item_bg.png");
            PrepareTexture(TextureDir + "/frame_mask.png");
            PrepareTexture(TextureDir + "/glow_frame.png");
            PrepareTexture(TextureDir + "/spark_dot.png");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void PrepareTexture(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }

        private static Sprite LoadSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null)
                return sprite;

            PrepareTexture(path);
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void EnsureFolder(string folderPath)
        {
            string[] parts = folderPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
