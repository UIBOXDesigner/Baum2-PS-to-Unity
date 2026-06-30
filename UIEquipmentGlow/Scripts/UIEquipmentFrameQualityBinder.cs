using UnityEngine;

namespace UIEquipmentGlow
{
    [DisallowMultipleComponent]
    public class UIEquipmentFrameQualityBinder : MonoBehaviour
    {
        [SerializeField] private UIEquipmentFrameEffect effect;

        private void Reset()
        {
            effect = GetComponent<UIEquipmentFrameEffect>();
        }

        public void SetNormal() => SetQuality(UIEquipmentQuality.Normal);
        public void SetRare() => SetQuality(UIEquipmentQuality.Rare);
        public void SetEpic() => SetQuality(UIEquipmentQuality.Epic);
        public void SetLegendary() => SetQuality(UIEquipmentQuality.Legendary);
        public void SetMythic() => SetQuality(UIEquipmentQuality.Mythic);

        public void SetQuality(UIEquipmentQuality quality)
        {
            if (effect == null)
                effect = GetComponent<UIEquipmentFrameEffect>();

            if (effect != null)
                effect.ApplyQuality(quality);
        }
    }
}
