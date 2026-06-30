using UnityEngine;

namespace UIEquipmentGlow
{
    public class UIEquipmentFrameRuntimeExample : MonoBehaviour
    {
        public UIEquipmentFrameEffect effect;

        [Tooltip("0 Normal, 1 Rare, 2 Epic, 3 Legendary, 4 Mythic")]
        [Range(0, 4)] public int testQuality = 3;

        private void Reset()
        {
            effect = GetComponent<UIEquipmentFrameEffect>();
        }

        [ContextMenu("Apply Test Quality")]
        public void ApplyTestQuality()
        {
            ApplyFromItemQuality(testQuality);
        }

        public void ApplyFromItemQuality(int itemQuality)
        {
            if (effect == null)
                effect = GetComponent<UIEquipmentFrameEffect>();

            if (effect == null)
                return;

            itemQuality = Mathf.Clamp(itemQuality, 0, 4);
            effect.ApplyQuality((UIEquipmentQuality)itemQuality);
        }
    }
}
