using UnityEngine;

namespace Match3Sample.Helper
{
    [ExecuteInEditMode]
    public class CameraMirror : MonoBehaviour
    {
        private Camera myCamera;

        void OnEnable()
        {
            Awake();
        }

        void Awake()
        {
            if (myCamera == null)
                myCamera = GetComponent<Camera>();
        }

        void OnPreCull()
        {
            myCamera.ResetWorldToCameraMatrix();
            myCamera.ResetProjectionMatrix();
            myCamera.projectionMatrix = myCamera.projectionMatrix * Matrix4x4.Scale(new Vector3(-1, 1, 1));
        }

        void OnPreRender()
        {
            GL.invertCulling = true;
        }

        void OnPostRender()
        {
            GL.invertCulling = false;
        }
    }
}
