using System;
using UnityEngine;
using UnityEngine.UI;

namespace UIEquipmentGlow
{
    public enum UIEquipmentQuality
    {
        Normal = 0,
        Rare = 1,
        Epic = 2,
        Legendary = 3,
        Mythic = 4
    }

    [Serializable]
    public class UIEquipmentRarityPreset
    {
        public UIEquipmentQuality quality = UIEquipmentQuality.Normal;
        public Color color = Color.white;

        [Header("Layer Switch")]
        public bool showGlow;
        public bool showShine;
        public bool showParticle;
        public bool useScalePulse;

        [Header("Outer Glow")]
        [Range(0f, 5f)] public float glowIntensity = 1f;
        [Range(0f, 16f)] public float glowSize = 3f;
        [Range(0f, 10f)] public float pulseSpeed = 1f;
        [Range(0f, 1f)] public float pulseAmount = 0.2f;

        [Header("Frame Shine")]
        [Range(0f, 5f)] public float shineIntensity = 1f;
        [Range(0.01f, 0.5f)] public float shineWidth = 0.12f;
        [Range(-5f, 5f)] public float shineSpeed = 0.65f;
        [Range(-3.1416f, 3.1416f)] public float shineAngle = 0.785f;

        [Header("Runtime Pulse")]
        [Tooltip("Scale breathing amplitude. Recommended: Legendary 0.008-0.012, Mythic 0.012-0.018.")]
        [Range(0f, 0.05f)] public float scalePulseAmount = 0.010f;

        public UIEquipmentRarityPreset() { }

        public UIEquipmentRarityPreset(
            UIEquipmentQuality quality,
            Color color,
            bool showGlow,
            bool showShine,
            bool showParticle,
            bool useScalePulse,
            float glowIntensity,
            float glowSize,
            float pulseSpeed,
            float pulseAmount,
            float shineIntensity,
            float shineWidth,
            float shineSpeed,
            float scalePulseAmount)
        {
            this.quality = quality;
            this.color = color;
            this.showGlow = showGlow;
            this.showShine = showShine;
            this.showParticle = showParticle;
            this.useScalePulse = useScalePulse;
            this.glowIntensity = glowIntensity;
            this.glowSize = glowSize;
            this.pulseSpeed = pulseSpeed;
            this.pulseAmount = pulseAmount;
            this.shineIntensity = shineIntensity;
            this.shineWidth = shineWidth;
            this.shineSpeed = shineSpeed;
            this.scalePulseAmount = scalePulseAmount;
        }
    }

    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class UIEquipmentFrameEffect : MonoBehaviour
    {
        private const int CurrentTuningVersion = 2;

        private static readonly int GlowColorId = Shader.PropertyToID("_GlowColor");
        private static readonly int GlowIntensityId = Shader.PropertyToID("_GlowIntensity");
        private static readonly int GlowSizeId = Shader.PropertyToID("_GlowSize");
        private static readonly int PulseSpeedId = Shader.PropertyToID("_PulseSpeed");
        private static readonly int PulseAmountId = Shader.PropertyToID("_PulseAmount");
        private static readonly int ShineColorId = Shader.PropertyToID("_ShineColor");
        private static readonly int ShineIntensityId = Shader.PropertyToID("_ShineIntensity");
        private static readonly int ShineWidthId = Shader.PropertyToID("_ShineWidth");
        private static readonly int ShineSpeedId = Shader.PropertyToID("_ShineSpeed");
        private static readonly int ShineAngleId = Shader.PropertyToID("_ShineAngle");
        private static readonly int RarityColorId = Shader.PropertyToID("_RarityColor");
        private static readonly int RarityStrengthId = Shader.PropertyToID("_RarityStrength");

        [Header("Quality")]
        [SerializeField] private UIEquipmentQuality quality = UIEquipmentQuality.Legendary;
        [SerializeField] private UIEquipmentRarityPreset[] presets = CreateDefaultPresets();
        [SerializeField, HideInInspector] private int tuningVersion = 0;

        [Header("UI Layers")]
        public Image backgroundImage;
        public Image frameImage;
        public Image glowImage;
        public Image shineImage;
        [Tooltip("Only this layer is scaled by the optional runtime pulse. Keep it on Glow, not on the item root.")]
        public RectTransform pulseTarget;
        public ParticleSystem particleFX;
        [Tooltip("Optional root object for UI-only particle/spark effects. Used when no ParticleSystem is required.")]
        public GameObject particleFXRoot;
        public bool autoUseGlowAsPulseTarget = true;

        [Header("Materials")]
        public Material outerGlowMaterial;
        public Material frameShineMaterial;
        public Material innerRarityMaterial;
        public bool createRuntimeMaterialInstances = true;

        [Header("Runtime")]
        public bool useUnscaledTime = true;
        public bool applyOnEnable = true;

        private Material glowRuntimeMaterial;
        private Material shineRuntimeMaterial;
        private Material innerRuntimeMaterial;
        private bool ownsGlowRuntimeMaterial;
        private bool ownsShineRuntimeMaterial;
        private bool ownsInnerRuntimeMaterial;
        private Vector3 baseScale = Vector3.one;
        private UIEquipmentQuality lastQuality;
        private bool initialized;

        public UIEquipmentQuality Quality
        {
            get => quality;
            set => ApplyQuality(value, true);
        }

        public static UIEquipmentRarityPreset[] CreateDefaultPresets()
        {
            return new[]
            {
                new UIEquipmentRarityPreset(UIEquipmentQuality.Normal,    new Color32(180, 180, 180, 255), false, false, false, false, 0f,   0f,  0f,   0f,   0f,  0.12f, 0.65f, 0f),
                new UIEquipmentRarityPreset(UIEquipmentQuality.Rare,      new Color32( 58, 167, 255, 255), true,  false, false, false, 0.65f,3f,  0.8f, 0.10f, 0f,  0.12f, 0.65f, 0f),
                new UIEquipmentRarityPreset(UIEquipmentQuality.Epic,      new Color32(180,  92, 255, 255), true,  false, false, true,  0.95f,4f,  0.9f, 0.14f, 0f,  0.12f, 0.65f, 0.008f),
                new UIEquipmentRarityPreset(UIEquipmentQuality.Legendary, new Color32(255, 157,  40, 255), true,  true,  false, true,  1.30f,5f,  0.95f,0.16f, 1.35f,0.10f, 0.65f, 0.010f),
                new UIEquipmentRarityPreset(UIEquipmentQuality.Mythic,    new Color32(255,  61,  61, 255), true,  true,  true,  true,  1.65f,6f,  1.05f,0.22f, 1.85f,0.13f, 0.75f, 0.014f),
            };
        }

        private void Reset()
        {
            Image[] images = GetComponentsInChildren<Image>(true);
            foreach (Image image in images)
            {
                string n = image.name.ToLowerInvariant();
                if (backgroundImage == null && (n.Contains("bg") || n.Contains("background"))) backgroundImage = image;
                if (frameImage == null && n.Contains("frame")) frameImage = image;
                if (glowImage == null && n.Contains("glow")) glowImage = image;
                if (shineImage == null && n.Contains("shine")) shineImage = image;
            }

            particleFX = GetComponentInChildren<ParticleSystem>(true);
            Transform fx = transform.Find("ParticleFX_GoldSpark");
            if (particleFXRoot == null && fx != null)
                particleFXRoot = fx.gameObject;
            ResolvePulseTarget();
            presets = CreateDefaultPresets();
        }

        private void OnEnable()
        {
            UpgradePresetTuningIfNeeded();
            ResolvePulseTarget();
            baseScale = pulseTarget != null ? pulseTarget.localScale : Vector3.one;
            initialized = false;

            if (applyOnEnable)
                ApplyQuality(quality, true);
        }

        private void OnValidate()
        {
            if (presets == null || presets.Length == 0)
                presets = CreateDefaultPresets();

            UpgradePresetTuningIfNeeded();
            ResolvePulseTarget();

            if (!isActiveAndEnabled)
                return;

            ApplyQuality(quality, true);
        }

        private void Update()
        {
            if (!initialized || lastQuality != quality)
                ApplyQuality(quality, true);

            UIEquipmentRarityPreset preset = GetPreset(quality);
            if (preset == null || !preset.useScalePulse || pulseTarget == null)
                return;

            float time = useUnscaledTime ? Time.unscaledTime : Time.time;
            float scale = 1f + Mathf.Sin(time * Mathf.PI * 2f * Mathf.Max(0.01f, preset.pulseSpeed)) * preset.scalePulseAmount;
            pulseTarget.localScale = baseScale * scale;
        }

        public void ApplyQuality(UIEquipmentQuality newQuality)
        {
            ApplyQuality(newQuality, true);
        }

        public void ApplyQuality(UIEquipmentQuality newQuality, bool updateMaterial)
        {
            quality = newQuality;
            UIEquipmentRarityPreset preset = GetPreset(newQuality);
            if (preset == null)
                return;

            EnsureMaterials();

            if (frameImage != null)
                frameImage.color = preset.color;

            if (glowImage != null)
            {
                glowImage.enabled = preset.showGlow;
                glowImage.color = Color.white;
            }

            if (shineImage != null)
            {
                shineImage.enabled = preset.showShine;
                shineImage.color = Color.white;
            }

            ApplyParticleState(preset.showParticle);

            if (updateMaterial)
                ApplyMaterialParams(preset);

            if (!preset.useScalePulse && pulseTarget != null)
                pulseTarget.localScale = baseScale;

            lastQuality = quality;
            initialized = true;
        }

        public void RefreshMaterials()
        {
            ReleaseRuntimeMaterials();
            initialized = false;
            ApplyQuality(quality, true);
        }

        private void UpgradePresetTuningIfNeeded()
        {
            if (tuningVersion >= CurrentTuningVersion)
                return;

            if (presets == null || presets.Length == 0)
                presets = CreateDefaultPresets();

            for (int i = 0; i < presets.Length; i++)
            {
                UIEquipmentRarityPreset preset = presets[i];
                if (preset == null)
                    continue;

                switch (preset.quality)
                {
                    case UIEquipmentQuality.Rare:
                        preset.pulseAmount = Mathf.Min(preset.pulseAmount, 0.10f);
                        preset.scalePulseAmount = 0f;
                        break;
                    case UIEquipmentQuality.Epic:
                        preset.glowIntensity = Mathf.Min(preset.glowIntensity, 0.95f);
                        preset.pulseSpeed = Mathf.Min(preset.pulseSpeed, 0.9f);
                        preset.pulseAmount = Mathf.Min(preset.pulseAmount, 0.14f);
                        preset.scalePulseAmount = Mathf.Min(preset.scalePulseAmount, 0.008f);
                        break;
                    case UIEquipmentQuality.Legendary:
                        preset.glowIntensity = Mathf.Min(preset.glowIntensity, 1.30f);
                        preset.pulseSpeed = Mathf.Min(preset.pulseSpeed, 0.95f);
                        preset.pulseAmount = Mathf.Min(preset.pulseAmount, 0.16f);
                        preset.shineIntensity = Mathf.Min(preset.shineIntensity, 1.35f);
                        preset.scalePulseAmount = Mathf.Min(preset.scalePulseAmount, 0.010f);
                        break;
                    case UIEquipmentQuality.Mythic:
                        preset.glowIntensity = Mathf.Min(preset.glowIntensity, 1.65f);
                        preset.pulseSpeed = Mathf.Min(preset.pulseSpeed, 1.05f);
                        preset.pulseAmount = Mathf.Min(preset.pulseAmount, 0.22f);
                        preset.shineIntensity = Mathf.Min(preset.shineIntensity, 1.85f);
                        preset.scalePulseAmount = Mathf.Min(preset.scalePulseAmount, 0.014f);
                        break;
                }
            }

            tuningVersion = CurrentTuningVersion;
        }

        private UIEquipmentRarityPreset GetPreset(UIEquipmentQuality targetQuality)
        {
            if (presets == null)
                return null;

            for (int i = 0; i < presets.Length; i++)
            {
                if (presets[i] != null && presets[i].quality == targetQuality)
                    return presets[i];
            }

            return null;
        }

        private void ResolvePulseTarget()
        {
            if (!autoUseGlowAsPulseTarget)
            {
                if (pulseTarget == null)
                    pulseTarget = transform as RectTransform;
                return;
            }

            RectTransform rootRt = transform as RectTransform;
            if (glowImage != null && (pulseTarget == null || pulseTarget == rootRt))
                pulseTarget = glowImage.rectTransform;

            if (pulseTarget == null)
                pulseTarget = rootRt;
        }

        private void ApplyParticleState(bool visible)
        {
            if (particleFXRoot != null && particleFXRoot.activeSelf != visible)
                particleFXRoot.SetActive(visible);

            if (particleFX != null)
            {
                GameObject fxObj = particleFX.gameObject;
                if (fxObj.activeSelf != visible)
                    fxObj.SetActive(visible);

                if (Application.isPlaying)
                {
                    if (visible && !particleFX.isPlaying) particleFX.Play(true);
                    if (!visible && particleFX.isPlaying) particleFX.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }
        }

        private void EnsureMaterials()
        {
            if (glowImage != null && glowRuntimeMaterial == null)
                glowRuntimeMaterial = AssignMaterial(glowImage, outerGlowMaterial, "UI/Equipment/Outer Glow", out ownsGlowRuntimeMaterial);

            if (shineImage != null && shineRuntimeMaterial == null)
                shineRuntimeMaterial = AssignMaterial(shineImage, frameShineMaterial, "UI/Equipment/Frame Shine", out ownsShineRuntimeMaterial);

            if (backgroundImage != null && innerRarityMaterial != null && innerRuntimeMaterial == null)
                innerRuntimeMaterial = AssignMaterial(backgroundImage, innerRarityMaterial, "UI/Equipment/Inner Rarity Tint", out ownsInnerRuntimeMaterial);
        }

        private Material AssignMaterial(Image image, Material source, string shaderName, out bool ownsMaterial)
        {
            ownsMaterial = false;
            if (image == null)
                return null;

            Material assigned = null;
            if (source != null)
            {
                if (createRuntimeMaterialInstances)
                {
                    assigned = new Material(source)
                    {
                        name = source.name + "_Instance",
                        hideFlags = HideFlags.DontSave
                    };
                    ownsMaterial = true;
                }
                else
                {
                    assigned = source;
                }
            }
            else
            {
                Shader shader = Shader.Find(shaderName);
                if (shader != null)
                {
                    assigned = new Material(shader)
                    {
                        name = shaderName.Replace('/', '_') + "_Runtime",
                        hideFlags = HideFlags.DontSave
                    };
                    ownsMaterial = true;
                }
            }

            if (assigned == null)
                return null;

            image.material = assigned;
            return assigned;
        }

        private void ApplyMaterialParams(UIEquipmentRarityPreset preset)
        {
            if (glowRuntimeMaterial != null)
            {
                Color glowColor = preset.color;
                glowColor.a = 1f;
                glowRuntimeMaterial.SetColor(GlowColorId, glowColor);
                glowRuntimeMaterial.SetFloat(GlowIntensityId, preset.glowIntensity);
                glowRuntimeMaterial.SetFloat(GlowSizeId, preset.glowSize);
                glowRuntimeMaterial.SetFloat(PulseSpeedId, preset.pulseSpeed);
                glowRuntimeMaterial.SetFloat(PulseAmountId, preset.pulseAmount);
            }

            if (shineRuntimeMaterial != null)
            {
                Color shineColor = Color.Lerp(Color.white, preset.color, 0.35f);
                shineColor.a = 1f;
                shineRuntimeMaterial.SetColor(ShineColorId, shineColor);
                shineRuntimeMaterial.SetFloat(ShineIntensityId, preset.shineIntensity);
                shineRuntimeMaterial.SetFloat(ShineWidthId, preset.shineWidth);
                shineRuntimeMaterial.SetFloat(ShineSpeedId, preset.shineSpeed);
                shineRuntimeMaterial.SetFloat(ShineAngleId, preset.shineAngle);
            }

            if (innerRuntimeMaterial != null)
            {
                innerRuntimeMaterial.SetColor(RarityColorId, preset.color);
                innerRuntimeMaterial.SetFloat(RarityStrengthId, preset.showGlow ? 0.65f : 0.15f);
                innerRuntimeMaterial.SetFloat(PulseSpeedId, preset.pulseSpeed);
                innerRuntimeMaterial.SetFloat(PulseAmountId, preset.pulseAmount * 0.35f);
            }
        }

        private void OnDisable()
        {
            if (pulseTarget != null)
                pulseTarget.localScale = baseScale;
        }

        private void OnDestroy()
        {
            ReleaseRuntimeMaterials();
        }

        private void ReleaseRuntimeMaterials()
        {
            if (ownsGlowRuntimeMaterial) DestroyMaterial(glowRuntimeMaterial);
            if (ownsShineRuntimeMaterial) DestroyMaterial(shineRuntimeMaterial);
            if (ownsInnerRuntimeMaterial) DestroyMaterial(innerRuntimeMaterial);
            glowRuntimeMaterial = null;
            shineRuntimeMaterial = null;
            innerRuntimeMaterial = null;
            ownsGlowRuntimeMaterial = false;
            ownsShineRuntimeMaterial = false;
            ownsInnerRuntimeMaterial = false;
        }

        private static void DestroyMaterial(Material material)
        {
            if (material == null)
                return;

            if (Application.isPlaying)
                Destroy(material);
            else
                DestroyImmediate(material);
        }
    }
}
