//#if UNITY_EDITOR
using UnityEngine;
using Match3Sample.Gameplay.Player;

namespace Match3Sample.Helper
{
    [ExecuteInEditMode]
    public class MirrorPlayer : MonoBehaviour
    {
        /*
        [SerializeField]
        private PlayerController targetController = null;
        [SerializeField]
        private TransformComponent position = null, rotation = null, scale = null;
        private Transform myTransform;
        private PlayerController playerController;

        void OnEnable()
        {
            if (Application.isPlaying)
            {
                enabled = false;
                return;
            }
            myTransform = transform;
            playerController = GetComponent<PlayerController>();
            Mirror();
        }

        void Mirror()
        {
            if (Application.isPlaying || playerController == null || targetController == null)
                return;
            if (targetController.transform.position != myTransform.position)
            {
                Vector3 newPosition = targetController.transform.position;
                if (position.affectX)
                    newPosition.x = myTransform.position.x;
                if (position.affectY)
                    newPosition.y = myTransform.position.y;
                if (position.affectZ)
                    newPosition.z = myTransform.position.z;
                targetController.transform.position = newPosition;
            }
            if (targetController.transform.rotation != myTransform.rotation)
            {
                Vector3 newRotation = targetController.transform.eulerAngles;
                if (rotation.affectX)
                    newRotation.x = myTransform.eulerAngles.x;
                if (rotation.affectY)
                    newRotation.y = myTransform.eulerAngles.y;
                if (rotation.affectZ)
                    newRotation.z = myTransform.eulerAngles.z;
                targetController.transform.eulerAngles = newRotation;
            }
            if (targetController.transform.localScale != myTransform.localScale)
            {
                Vector3 newScale = targetController.transform.localScale;
                if (scale.affectX)
                    newScale.x = myTransform.localScale.x;
                if (scale.affectY)
                    newScale.y = myTransform.localScale.y;
                if (scale.affectZ)
                    newScale.z = myTransform.localScale.z;
                targetController.transform.localScale = newScale;
            }
            if (playerController && targetController)
            {
                targetController.IsPlayerController = false;
                targetController.CharacterSettings = playerController.CharacterSettings;
                targetController.Projectile = playerController.Projectile;
                if (playerController.ProjectileSpawn && targetController.ProjectileSpawn)
                    targetController.ProjectileSpawn.position = playerController.ProjectileSpawn.position;
            }
        }
        */
    }

    [System.Serializable]
    public class TransformComponent
    {
        public bool affectX = true;
        public bool affectY = true;
        public bool affectZ = true;
    }
    //#endif
}
