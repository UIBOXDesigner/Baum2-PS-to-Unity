using UnityEngine;
using UnityEngine.UI;

namespace UIEquipmentGlow
{
    [DisallowMultipleComponent]
    public class UIEquipmentSparkFX : MonoBehaviour
    {
        [Header("Targets")]
        public RectTransform[] sparks;

        [Header("Motion")]
        public bool useUnscaledTime = true;
        [Range(-180f, 180f)] public float rotateSpeed = 18f;
        [Range(0f, 30f)] public float floatAmount = 3f;
        [Range(0f, 6f)] public float twinkleSpeed = 1.6f;
        [Range(0f, 1f)] public float alphaMin = 0.15f;
        [Range(0f, 1f)] public float alphaMax = 0.95f;
        [Range(0f, 1f)] public float scalePulseAmount = 0.20f;

        [Header("Color")]
        public Color sparkColor = new Color(1f, 0.78f, 0.25f, 1f);

        private Vector2[] basePositions;
        private Vector3[] baseScales;
        private Graphic[] graphics;
        private bool cached;

        private void Reset()
        {
            CollectChildren();
        }

        private void OnEnable()
        {
            Cache();
        }

        private void OnValidate()
        {
            if (sparks == null || sparks.Length == 0)
                CollectChildren();
        }

        private void Update()
        {
            if (!cached)
                Cache();

            if (sparks == null || sparks.Length == 0)
                return;

            float time = useUnscaledTime ? Time.unscaledTime : Time.time;
            float rotation = time * rotateSpeed;

            for (int i = 0; i < sparks.Length; i++)
            {
                RectTransform spark = sparks[i];
                if (spark == null)
                    continue;

                float phase = i * 0.73f;
                Vector2 pos = i < basePositions.Length ? basePositions[i] : spark.anchoredPosition;
                Vector2 dir = pos.sqrMagnitude > 0.001f ? pos.normalized : Vector2.up;
                float offset = Mathf.Sin(time * twinkleSpeed * Mathf.PI * 2f + phase) * floatAmount;
                spark.anchoredPosition = Rotate(pos + dir * offset, rotation);

                float wave = Mathf.Sin(time * twinkleSpeed * Mathf.PI * 2f + phase) * 0.5f + 0.5f;
                float scale = 1f + wave * scalePulseAmount;
                Vector3 baseScale = i < baseScales.Length ? baseScales[i] : Vector3.one;
                spark.localScale = baseScale * scale;

                if (graphics != null && i < graphics.Length && graphics[i] != null)
                {
                    Color c = sparkColor;
                    c.a = Mathf.Lerp(alphaMin, alphaMax, wave);
                    graphics[i].color = c;
                }
            }
        }

        public void CollectChildren()
        {
            int count = transform.childCount;
            sparks = new RectTransform[count];
            for (int i = 0; i < count; i++)
                sparks[i] = transform.GetChild(i) as RectTransform;
            cached = false;
        }

        private void Cache()
        {
            if (sparks == null || sparks.Length == 0)
                CollectChildren();

            int count = sparks != null ? sparks.Length : 0;
            basePositions = new Vector2[count];
            baseScales = new Vector3[count];
            graphics = new Graphic[count];

            for (int i = 0; i < count; i++)
            {
                RectTransform spark = sparks[i];
                if (spark == null)
                    continue;

                basePositions[i] = spark.anchoredPosition;
                baseScales[i] = spark.localScale;
                graphics[i] = spark.GetComponent<Graphic>();
            }

            cached = true;
        }

        private static Vector2 Rotate(Vector2 value, float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            float sin = Mathf.Sin(rad);
            float cos = Mathf.Cos(rad);
            return new Vector2(value.x * cos - value.y * sin, value.x * sin + value.y * cos);
        }
    }
}
