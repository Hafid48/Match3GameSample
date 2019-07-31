using UnityEngine;

namespace Match3Sample.Gameplay.Effects
{
    public class EffectDestroyer : MonoBehaviour
    {
        public float lifeTime = .3f;

        // Use this for initialization
        void Start()
        {
            Destroy(gameObject, lifeTime);
        }
    }
}
