using UnityEngine;

namespace Poolrooms
{
    [CreateAssetMenu(menuName="Poolrooms/Apariencia 3D")]
    public sealed class PoolroomsAvatarDefinition : ScriptableObject
    {
        public GameObject model;
        [Range(.5f,2.5f)] public float height=1.75f;
        public float forwardCorrection;
        [Tooltip("Usa las animaciones comunes. Desactiva solo para un modelo estático de diagnóstico.")]
        public bool sharedHumanoid=true;
        public RuntimeAnimatorController controller; // Legacy serialized field; common animation owns the skeleton.
        [TextArea] public string attribution;
    }
}
