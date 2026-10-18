using UnityEngine;

namespace Poolrooms
{
    [CreateAssetMenu(menuName="Poolrooms/Apariencia 3D")]
    public sealed class PoolroomsAvatarDefinition : ScriptableObject
    {
        public GameObject model;
        [Range(.5f,2.5f)] public float height=1.75f;
        public float forwardCorrection;
        public RuntimeAnimatorController controller;
        [TextArea] public string attribution;
    }
}
