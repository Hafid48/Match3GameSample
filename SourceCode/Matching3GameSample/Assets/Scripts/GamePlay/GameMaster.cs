using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using DG.Tweening;
using Match3Sample.Gameplay.Board;
using Match3Sample.Gameplay.Player;
using Match3Sample.Gameplay.Player.Stats;
using Match3Sample.UI;
using Match3Sample.Audio;

namespace Match3Sample.Gameplay
{
    public class GameMaster : MonoBehaviour
    {
        #region Properties
        public static GameMaster Instance { get; set; }
        public static PlayMode playMode = PlayMode.PlayerVSAI;
        public PlayerController PlayerController { get; private set; }
        public PlayerController OpponentController { get; private set; }
        public float RoundTimer
        {
            get { return roundTimer; }
            set
            {
                if (value <= 2 && value > 0)
                    AudioMaster.Instance.FadeOutMusic();
                else if (value < 0)
                {
                    value = 0;
                    AudioMaster.Instance.StopMusic();
                    BoardManager.Instance.CanInteract = false;
                    canUpdateTimer = false;
                }
                roundTimer = value;
                GUIMaster.Instance.UpdateRoundTimerText(roundTimer);
            }
        }
        public float ActiveChainTimer
        {
            get { return activeChainTimer; }
            set
            {
                if (value < 0)
                {
                    value = activeChainTime;
                    AddPointsToPlayer();
                    startActiveChain = false;
                }
                activeChainTimer = value;
            }
        }
        public int BattleDuration { get { return battleDuration; } }
        public int CurrentRound { get; private set; } = 1;
        public int MatchesPoints { get; set; }
        public bool IsReadyToBattle { get; set; }
        #endregion

        #region Fields
        [SerializeField]
        private CharacterType playerCharacterType = CharacterType.Thor;
        [SerializeField]
        private CharacterType opponentCharacterType = CharacterType.Chaac;
        [SerializeField]
        private int battleDuration = 5; // how long does the fight animation last for
        [SerializeField]
        private int battleRounds = 5;
        [SerializeField]
        private int roundTime = 30;
        [SerializeField]
        private float activeChainTime = 2f;
        [SerializeField]
        private CellRule[] cellRules = new CellRule[8];
        private PlayerStats playerStats = new PlayerStats();
        private PlayerStats opponentStats = new PlayerStats();
        private int matchesAttackPoints, matchesDefencePoints;
        private int activeChain;
        private float activeChainTimer;
        private float roundTimer;
        private bool startActiveChain;
        private bool canUpdateTimer;
        private bool isAlreadyCheckingForNextRound;
        #endregion

        #region Game Setup
        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        void Start()
        {
            playerStats.CharacterType = playerCharacterType; ///////chage player character
            opponentStats.CharacterType = opponentCharacterType; //////change opponent character
            playMode = PlayMode.PlayerVSAI;
            activeChain = -1;
            activeChainTimer = activeChainTime;
            GameObject playerCharacter = GameObject.Find("Players").transform.Find(playerStats.CharacterType.ToString()).gameObject;
            GameObject opponentCharacter = GameObject.Find("Opponents").transform.Find(opponentStats.CharacterType.ToString()).gameObject;
            playerCharacter.SetActive(true);
            opponentCharacter.SetActive(true);
            PlayerController = playerCharacter.GetComponentInChildren<PlayerController>();
            OpponentController = opponentCharacter.GetComponentInChildren<PlayerController>();
            PlayerController.PlayerStats = playerStats;
            PlayerController.PlayerStats.Reset();
            OpponentController.PlayerStats = opponentStats;
            OpponentController.PlayerStats.Reset();
            PlayerController.OpponentController = OpponentController;
            OpponentController.OpponentController = PlayerController;
            GUIMaster.Instance.SetupPlayerUI(playerStats);
            ResetRoundTimer();
            StartCoroutine(BoardManager.Instance.InitializeBoard());
        }

        void Update()
        {
            if (startActiveChain)
                ActiveChainTimer -= Time.deltaTime;
            if (IsReadyToBattle && !isAlreadyCheckingForNextRound && PlayerController.PlayerStats.HealthPoints > 0 && OpponentController.PlayerStats.HealthPoints > 0)
                StartCoroutine(CheckForNextRound());
            if (canUpdateTimer)
                RoundTimer -= Time.deltaTime;
        }
        #endregion

        #region Update Time & ActiveChain
        public void StartRoundTimer()
        {
            canUpdateTimer = true;
        }

        public void ResetRoundTimer()
        {
            RoundTimer = roundTime;
        }

        public void PauseActiveChain(bool pause)
        {
            startActiveChain = !pause;
            if (pause)
                ActiveChainTimer = activeChainTime;
        }

        public void HandleActiveChain()
        {
            if (!startActiveChain)
            {
                activeChain = -1;
                startActiveChain = true;
            }
            ActiveChainTimer = activeChainTime;
            if (startActiveChain)
            {
                activeChain++;
                if (activeChain > 1)
                    GUIMaster.Instance.SetActiveChainText(activeChain);
            }
        }
        #endregion

        #region Round & Battle Check
        private IEnumerator CheckForNextRound()
        {
            isAlreadyCheckingForNextRound = true;
            if (PlayerController.PlayerStats.AttackPoints <= 0 && OpponentController.PlayerStats.AttackPoints <= 0)
            {
                PlayerController.Animation.CrossFade("Idle");
                OpponentController.Animation.CrossFade("Idle");
                IsReadyToBattle = false;
                yield return new WaitForSeconds(2f);
                CurrentRound++;
                if (CurrentRound > battleRounds)
                {

                    if (PlayerController.PlayerStats.HealthPoints > OpponentController.PlayerStats.HealthPoints)
                    {
                        PlayerController.Animation.CrossFade("Victory");
                        OpponentController.Animation.CrossFade("Defeat");
                        GUIMaster.Instance.SetRoundAnnouncementText("You Win!");
                        yield return new WaitForSeconds(Mathf.Max(PlayerController.Animation["Victory"].length, OpponentController.Animation["Defeat"].length));
                    }
                    else if (PlayerController.PlayerStats.HealthPoints < OpponentController.PlayerStats.HealthPoints)
                    {
                        PlayerController.Animation.CrossFade("Defeat");
                        OpponentController.Animation.CrossFade("Victory");
                        GUIMaster.Instance.SetRoundAnnouncementText("You Lose!");
                        yield return new WaitForSeconds(Mathf.Max(PlayerController.Animation["Defeat"].length, OpponentController.Animation["Victory"].length));
                    }
                    else // draw
                    {
                        PlayerController.Animation.CrossFade("Defeat");
                        OpponentController.Animation.CrossFade("Defeat");
                        GUIMaster.Instance.SetRoundAnnouncementText("Draw!");
                        yield return new WaitForSeconds(Mathf.Max(PlayerController.Animation["Defeat"].length, OpponentController.Animation["Defeat"].length));
                    }
                    SignalEndBattle();
                }
                else
                {
                    PlayerController.PlayerStats.AttackPoints = PlayerController.PlayerStats.DefencePoints = 0;
                    OpponentController.PlayerStats.AttackPoints = OpponentController.PlayerStats.DefencePoints = 0;
                    ResetRoundTimer();
                    GUIMaster.Instance.ResetUI();
                    yield return StartCoroutine(GUIMaster.Instance.ResetUI());
                    BoardManager.Instance.CanInteract = true;
                    BoardManager.Instance.StopAllCoroutines();
                    BoardManager.Instance.Tweener.Kill();
                    StartCoroutine(BoardManager.Instance.ShuffleBoard(0, true, false));
                }
            }
            isAlreadyCheckingForNextRound = false;
        }

        public IEnumerator OnRoundEnded()
        {
            if (GUIMaster.Instance.HasUsedSpecialty)
            {
                switch (PlayerController.PlayerStats.CharacterType)
                {
                    case CharacterType.Thor:
                        FindCellRule(CellType.Thor).pointsWorth = 1;
                        break;
                    case CharacterType.Zeus:
                        BoardManager.Instance.CellFlickerTime = 2;
                        break;
                    case CharacterType.Chaac:
                        PlayerController.PlayerStats.AttackPoints += (int)(PlayerController.PlayerStats.AttackPoints * .25f);
                        PlayerController.PlayerStats.DefencePoints += (int)(PlayerController.PlayerStats.DefencePoints * .25f);
                        GUIMaster.Instance.UpdateAttackPointsText(PlayerController.PlayerStats.AttackPoints);
                        GUIMaster.Instance.UpdateDefencePointsText(PlayerController.PlayerStats.DefencePoints);
                        break;
                    case CharacterType.Odin:
                        OpponentController.PlayerStats.AttackPoints = (int)(OpponentController.PlayerStats.AttackPoints * .3f);
                        break;
                }
            }
            AddPointsToPlayer();
            yield return BoardManager.Instance.Tweener.WaitForCompletion();
            GUIMaster.Instance.SetRoundAnnouncementText("Round Over!");
            yield return new WaitForSeconds(.75f);
            GUIMaster.Instance.SetRoundAnnouncementText("Getting Opponent's Score...");
            print("Round Ended");
            switch (playMode)
            {
                case PlayMode.PlayerVSPlayer:

                    break;
                case PlayMode.PlayerVSAI:
                    yield return new WaitForSeconds(Random.Range(.25f, 1.25f));
                    int halfAttackPoints = PlayerController.PlayerStats.AttackPoints / 2;
                    int halfDefencePoints = PlayerController.PlayerStats.DefencePoints / 2;
                    OpponentController.PlayerStats.AttackPoints = Mathf.Max(Random.Range(0, 25), PlayerController.PlayerStats.AttackPoints + Random.Range(-halfAttackPoints, halfAttackPoints));
                    OpponentController.PlayerStats.DefencePoints = Mathf.Max(Random.Range(0, 25), PlayerController.PlayerStats.DefencePoints + Random.Range(-halfDefencePoints, halfDefencePoints));
                    PlayerController.CalculateCountDown();
                    OpponentController.CalculateCountDown();
                    PlayerController.CheckForDraw();
                    OpponentController.CheckForDraw();
                    GUIMaster.Instance.SetRoundAnnouncementText("Fight!");
                    IsReadyToBattle = true;
                    break;
            }
        }

        public void SignalEndBattle()
        {
            SceneManager.LoadScene("TheGame");
        }
        #endregion

        #region Calculate & Add Points
        public void AddPointsToPlayer(CellRule matchedCellRule)
        {
            if (matchedCellRule.cellFamily == CellFamily.Attacker)
            {
                PlayerController.PlayerStats.AttackPoints += matchedCellRule.pointsWorth;
                if (matchedCellRule.cellType == GUIMaster.Instance.LuckyFaceType)
                    PlayerController.PlayerStats.DefencePoints += matchedCellRule.pointsWorth;
            }
            else
            {
                PlayerController.PlayerStats.DefencePoints += matchedCellRule.pointsWorth;
                if (matchedCellRule.cellType == GUIMaster.Instance.LuckyFaceType)
                    PlayerController.PlayerStats.AttackPoints += matchedCellRule.pointsWorth;
            }
            GUIMaster.Instance.UpdateAttackPointsText(PlayerController.PlayerStats.AttackPoints);
            GUIMaster.Instance.UpdateDefencePointsText(PlayerController.PlayerStats.DefencePoints);
        }

        private void AddPointsToPlayer()
        {
            if (activeChain > 1)
            {
                matchesAttackPoints *= activeChain;
                matchesDefencePoints *= activeChain;
            }
            //Add points to the player stats
            PlayerController.PlayerStats.AttackPoints += matchesAttackPoints;
            PlayerController.PlayerStats.DefencePoints += matchesDefencePoints;
            MatchesPoints = matchesAttackPoints = matchesDefencePoints = 0;
            GUIMaster.Instance.UpdateAttackPointsText(PlayerController.PlayerStats.AttackPoints);
            GUIMaster.Instance.UpdateDefencePointsText(PlayerController.PlayerStats.DefencePoints);
        }

        public void CalculateMatchingPoints(CellRule matchedCellRule)
        {
            if (GUIMaster.Instance.HasUsedSpecialty && PlayerController.PlayerStats.CharacterType == CharacterType.Raijin)
            {
                int multiplyerFactor = matchedCellRule.cellType == GUIMaster.Instance.LuckyFaceType ? 2 : 1;
                matchesAttackPoints += MatchesPoints * matchedCellRule.pointsWorth * multiplyerFactor;
                matchesDefencePoints += MatchesPoints * matchedCellRule.pointsWorth * multiplyerFactor;
            }
            else
            {
                bool hasPerunSpecialtyActivated = GUIMaster.Instance.HasUsedSpecialty && PlayerController.PlayerStats.CharacterType == CharacterType.Perun;
                if (matchedCellRule.cellFamily == CellFamily.Attacker && !hasPerunSpecialtyActivated)
                {
                    matchesAttackPoints += MatchesPoints * matchedCellRule.pointsWorth;
                    if (matchedCellRule.cellType == GUIMaster.Instance.LuckyFaceType)
                        matchesDefencePoints += MatchesPoints * matchedCellRule.pointsWorth;
                }
                else
                {
                    matchesDefencePoints += MatchesPoints * matchedCellRule.pointsWorth;
                    if (matchedCellRule.cellType == GUIMaster.Instance.LuckyFaceType)
                        matchesAttackPoints += MatchesPoints * matchedCellRule.pointsWorth;
                }
            }
            MatchesPoints = 0;
        }
        #endregion

        #region Helpers
        /// <summary>
        /// A helper method that return the Absolute value of a vector2
        /// </summary>
        /// <param name="vector2"></param>
        /// <returns></returns>
        public Vector2 Abs(Vector2 vector2)
        {
            return new Vector2(Mathf.Abs(vector2.x), Mathf.Abs(vector2.y));
        }

        public CellRule FindCellRule(CellType cellType)
        {
            for (int r = 0; r < cellRules.Length; r++)
            {
                CellRule cellRule = cellRules[r];
                if (cellRule.cellType == cellType)
                    return cellRule;
            }
            return cellRules[0];
        }
        #endregion
    }

    #region Enumerations
    public enum PlayMode
    {
        PlayerVSPlayer,
        PlayerVSAI
    }

    public enum CellFamily
    {
        Attacker,
        Defender
    }
    #endregion

    #region Classes
    [System.Serializable]
    public class CellRule
    {
        public CellType cellType;
        public CellFamily cellFamily;
        public int pointsWorth;
        public GameObject matchEffect;
        public AudioClip[] matchClips;
    }

    [System.Serializable]
    public class CharacterSettings
    {
        public bool useAttackTiming = false;
        [Range(0, 1)]
        public float attackTiming = 1f;
        public bool useVictoryDelay = false;
        public float victoryDelay = 1f;
        public bool useDeathTiming = false;
        [Range(0, 1)]
        public float deathTiming = 1f;
    }
    #endregion
}