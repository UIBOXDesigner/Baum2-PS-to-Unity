using UnityEngine;
using UnityEngine.UI;

namespace Game.UIShaderLibrary
{
    public class UIShaderRuntimeMaterial : MonoBehaviour
    {
        public Graphic targetGraphic;
        public Material sourceMaterial;
        private Material runtimeMaterial;
        public Material RuntimeMaterial => runtimeMaterial;
        void Awake()
        {
            if(targetGraphic==null) targetGraphic=GetComponent<Graphic>();
            if(sourceMaterial!=null) runtimeMaterial=Instantiate(sourceMaterial);
            if(targetGraphic!=null && runtimeMaterial!=null) targetGraphic.material=runtimeMaterial;
        }
        public void SetFloat(string propertyName, float value)
        {
            if(runtimeMaterial!=null && runtimeMaterial.HasProperty(propertyName)) runtimeMaterial.SetFloat(propertyName,value);
        }
        public void SetColor(string propertyName, Color value)
        {
            if(runtimeMaterial!=null && runtimeMaterial.HasProperty(propertyName)) runtimeMaterial.SetColor(propertyName,value);
        }
        void OnDestroy()
        {
            if(runtimeMaterial!=null) Destroy(runtimeMaterial);
        }
    }
}
