using UnityEngine;
using System.Collections;
using Match3Sample.UI;
using Match3Sample.Gameplay.Effects;
using Match3Sample.Gameplay.Player.Stats;

namespace Match3Sample.Gameplay.Player
{
    public class PlayerController : MonoBehaviour
    {
        #region Properties
        public Animation Animation { get { return myAnimation; } set { myAnimation = value; } }
        public PlayerStats PlayerStats { get; set; }
        public CharacterSettings CharacterSettings {  get { return characterSettings; } set { characterSettings = value; } }
        public PlayerController OpponentController { get; set; }
        public Transform Projectile {  get { return projectile; } set { projectile = value; } }
        public Transform ProjectileHitSpot { get { return projectileHitSpot; } }
        public Transform ProjectileSpawn { get { return projectileSpawn; } }
        public bool IsPlayerController { get { return isPlayerController; } set { IsPlayerController = value; } }
        public bool GotHit { get; set; }
        #endregion

        #region Fields
        [SerializeField]
        private Animation myAnimation = null;
        [SerializeField]
        private CharacterSettings characterSettings = null;
        [SerializeField]
        private Transform projectile = null;
        [SerializeField]
        private Transform projectileHitSpot = null;
        [SerializeField]
        private Transform projectileSpawn = null;
        [SerializeField]
        private bool isPlayerController = false;
        [SerializeField]
        private bool debug = false;
        private float countDownAmount, countDownDelay;
        private int particleReleased;
        private bool isAlreadyCountingDown;
        private bool isAlreadyBattling;
        private bool isDead;
        private bool isDraw;
        #endregion

        #region Setup
        void Start()
        {
            if (myAnimation == null)
                myAnimation = GetComponent<Animation>();
        }

        public void Update()
        {
            if (!GameMaster.Instance.IsReadyToBattle)
                return;
            if (!isAlreadyCountingDown && PlayerStats.AttackPoints > 0 && PlayerStats.HealthPoints > 0 && OpponentController.PlayerStats.HealthPoints > 0)
                StartCoroutine(CountDown());
            if (!isAlreadyBattling)
                StartCoroutine(StartBattle());
        }

        #endregion

        #region Debug
        void OnDrawGizmos()
        {
            if (!debug)
                return;
            if (!projectileHitSpot || !projectileSpawn)
                return;
            Gizmos.color = Color.red + Color.yellow;
            Gizmos.DrawWireCube(projectileHitSpot.position, new Vector3(5f, 5f, 0));
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(projectileSpawn.position, 2.5f);
        }
        #endregion

        #region Battle
        public void CheckForDraw()
        {
            isDraw = PlayerStats.AttackPoints >= OpponentController.PlayerStats.DefencePoints + OpponentController.PlayerStats.HealthPoints && PlayerStats.DefencePoints == OpponentController.PlayerStats.DefencePoints;
        }

        private IEnumerator StartBattle()
        {
            isAlreadyBattling = true;
            if (isDead)
            {
                GameMaster.Instance.IsReadyToBattle = false;
                if (isDraw)
                {
                    myAnimation.CrossFade("Death");
                    OpponentController.myAnimation.CrossFade("Death");
                    yield return new WaitForSeconds(Mathf.Max(myAnimation["Death"].length, OpponentController.myAnimation["Death"].length));
                    Debug.Log("Draw!");
                    GUIMaster.Instance.SetRoundAnnouncementText("Draw!");
                }
                else
                {
                    myAnimation.CrossFade("Death");
                    if (OpponentController.myAnimation.IsPlaying("Attack"))
                        OpponentController.myAnimation.CrossFade("Idle");
                    yield return new WaitForSeconds(characterSettings.useDeathTiming ? characterSettings.deathTiming * myAnimation["Death"].length : myAnimation["Death"].length);
                    OpponentController.myAnimation.CrossFade("Victory");
                    if (OpponentController.isPlayerController)
                        GUIMaster.Instance.SetRoundAnnouncementText("You Win!");
                    else
                        GUIMaster.Instance.SetRoundAnnouncementText("You Lose!");
                    yield return new WaitForSeconds(OpponentController.characterSettings.useVictoryDelay ? OpponentController.characterSettings.victoryDelay + OpponentController.myAnimation["Victory"].length : OpponentController.myAnimation["Victory"].length);
                }
                GameMaster.Instance.SignalEndBattle();
            }
            else if (GotHit)
            {
                GotHit = false;
                if (PlayerStats.AttackPoints <= 0)
                {
                    myAnimation.CrossFade("Hit");
                    yield return new WaitForSeconds(myAnimation["Hit"].length);
                }
            }
            else if (PlayerStats.AttackPoints > 0 && PlayerStats.HealthPoints > 0 && GameMaster.Instance.IsReadyToBattle && !isDead)
            {
                myAnimation.CrossFade("Attack");
                yield return new WaitForSeconds(characterSettings.useAttackTiming ? characterSettings.attackTiming * myAnimation["Attack"].length : myAnimation["Attack"].length);
                if (PlayerStats.AttackPoints > 0 && PlayerStats.HealthPoints > 0 && GameMaster.Instance.IsReadyToBattle && !isDead)
                {
                    Vector3 projectileSpawnPosition = projectileSpawn.position;
                    if (!isPlayerController)
                        projectileSpawnPosition.x *= -1f;
                    Transform projectileClone = Instantiate(projectile, projectileSpawnPosition, Quaternion.identity) as Transform;
                    ProjectileNon projectileNon = projectileClone.GetComponent<ProjectileNon>();
                    projectileNon.PlayerController = this;
                }
                if (characterSettings.useAttackTiming)
                    yield return new WaitForSeconds(myAnimation["Attack"].length - characterSettings.attackTiming * myAnimation["Attack"].length);
            }
            else
                myAnimation.CrossFade("Idle");
            isAlreadyBattling = false;
        }

        public void CalculateCountDown()
        {
            //int maxAttackPoints = Mathf.Max (PlayerStats.AttackPoints, opponentController.PlayerStats.AttackPoints);
            countDownAmount = Mathf.Max(1, ((float)PlayerStats.AttackPoints) / GameMaster.Instance.BattleDuration);
            countDownDelay = (1f / countDownAmount);
        }

        private IEnumerator CountDown()
        {
            isAlreadyCountingDown = true;
            float attackPointsAfterDecrease = Mathf.Max(0, PlayerStats.AttackPoints - countDownAmount);
            while (PlayerStats.AttackPoints > attackPointsAfterDecrease)
            {
                if (OpponentController.PlayerStats.HealthPoints <= 0 || PlayerStats.HealthPoints <= 0 || !GameMaster.Instance.IsReadyToBattle)
                    break;
                PlayerStats.AttackPoints--;
                if (isPlayerController)
                    GUIMaster.Instance.UpdateAttackPointsText(PlayerStats.AttackPoints);
                if (OpponentController.PlayerStats.DefencePoints > 0)
                {
                    OpponentController.PlayerStats.DefencePoints--;
                    if (OpponentController.isPlayerController)
                        GUIMaster.Instance.UpdateDefencePointsText(OpponentController.PlayerStats.DefencePoints);
                }
                else if (OpponentController.PlayerStats.HealthPoints > 0)
                {
                    OpponentController.PlayerStats.HealthPoints--;
                    if (OpponentController.isPlayerController)
                        GUIMaster.Instance.UpdatePlayerHealthSliderText(OpponentController.PlayerStats.HealthPoints / 200f);
                    else
                        GUIMaster.Instance.UpdateOpponentHealthSliderText(OpponentController.PlayerStats.HealthPoints / 200f);
                }
                yield return new WaitForSeconds(countDownDelay);
                //ield return new WaitForSeconds(GameMaster.Ins);		//}
            }
            if (OpponentController.PlayerStats.HealthPoints <= 0)
                OpponentController.isDead = true;
            //yield return new WaitForSeconds (countDownDelay);
            isAlreadyCountingDown = false;
        }
        #endregion
    }
}
