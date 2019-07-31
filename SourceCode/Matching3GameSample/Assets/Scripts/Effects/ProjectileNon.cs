using UnityEngine;
using System.Collections;
using DG.Tweening;
using Match3Sample.Gameplay.Player;

namespace Match3Sample.Gameplay.Effects
{
    public class ProjectileNon : MonoBehaviour
    {
        public PlayerController PlayerController { get; set; }
        [SerializeField]
        private Transform explosion = null;
        [SerializeField]
        private float duration = 1f;
        private Transform myTransform;

        private void Awake()
        {
            myTransform = transform;
        }
        // Use this for initialization
        void Start()
        {
            StartCoroutine(MoveTowardsTarget());
        }

        // Update is called once per frame
        void Update()
        {
            if (!GameMaster.Instance.IsReadyToBattle)
                return;
            myTransform.LookAt(PlayerController.OpponentController.ProjectileHitSpot);
        }

        private IEnumerator MoveTowardsTarget()
        {
            Vector3 projectileHitSpot = PlayerController.OpponentController.ProjectileHitSpot.position;
            if (PlayerController.IsPlayerController)
                projectileHitSpot.x *= -1f;
            myTransform.DOMove(projectileHitSpot, duration);
            yield return new WaitForSeconds(duration);
            Instantiate(explosion, myTransform.position, Quaternion.identity);
            PlayerController.OpponentController.GotHit = true;
            Destroy(gameObject);
        }

        /*
        private void DetectCollision()
        {
            if (!GameMaster.Instance.isReadyToBattle)
            {
                Destroy(gameObject);
                return;
            }
            float distance = Vector3.Distance (opponentController.projectileHitSpotPosition, myTransform.position);
            if (distance <= minCollisionDistance) 
            {
                Instantiate (explosion, myTransform.position, Quaternion.identity);
                opponentController.gotHit = true;
                Destroy (gameObject);
            }
        }
    /*
        /*
        void OnParticleCollision(GameObject other)
        {
            Instantiate (explosion, myTransform.position, Quaternion.identity);
            PlayerController controller = other.GetComponent<PlayerController> ();
            //if (controller.PlayerStats.AttackPoints <= 0)
                controller.gotHit = true;
            Destroy (myTransform.gameObject);
        }
        */
    }
}
