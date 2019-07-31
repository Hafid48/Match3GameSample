using UnityEngine;

namespace Match3Sample.Helper
{
    public class BackPieceAnimator : MonoBehaviour
    {
        public Vector3 axis = Vector3.up;
        public float rotateSpeed = 20f;
        [SerializeField]
        private Animation myAnimation = null;
        private Transform myTransform;

        void Awake()
        {
            myTransform = transform;
        }

        // Update is called once per frame
        void Update()
        {
            if (myAnimation)
            {
                if (myAnimation.IsPlaying("Death") || myAnimation.IsPlaying("Defeat"))
                    Destroy(this);
            }
            myTransform.Rotate(axis * rotateSpeed * Time.deltaTime);
            //myTransform.RotateAround (axis, rotateSpeed * Time.deltaTime);
        }
    }
}
