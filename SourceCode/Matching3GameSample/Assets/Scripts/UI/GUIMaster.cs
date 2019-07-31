using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using DG.Tweening;
using Match3Sample.Gameplay;
using Match3Sample.Gameplay.Board;
using Match3Sample.Gameplay.Player.Stats;

namespace Match3Sample.UI
{
    public class GUIMaster : MonoBehaviour
    {
        #region Properties
        public static GUIMaster Instance { get; private set; }
        public bool HasUsedSpecialty { get; private set; }
        public CellType LuckyFaceType { get; private set; }
        #endregion

        #region Delagate & Events
        public delegate void ScreenChangeHandler();
        public static event ScreenChangeHandler ScreenChanged;
        #endregion

        #region Fields
        [SerializeField]
        private GameObject roundAnnouncement = null;
        [SerializeField]
        private Image attackImage = null;
        [SerializeField]
        private Image defenceImage = null;
        [SerializeField]
        private Image specialImage = null;
        [SerializeField]
        private Image luckyFaceImage = null;
        [SerializeField]
        private Text shuffleCountText = null;
        [SerializeField]
        private Text clueCountText = null;
        [SerializeField]
        private Text attackCountText = null;
        [SerializeField]
        private Text defenceCountText = null;
        [SerializeField]
        private Text specialtyCountText = null;
        [SerializeField]
        private Slider playerHealthSlider = null;
        [SerializeField]
        private Slider opponentHealthSlider = null;
        [SerializeField]
        private Text roundText = null;
        [SerializeField]
        private Text roundTimerText = null;
        [SerializeField]
        private Text activeChainText = null;
        [SerializeField]
        private Text attackPointsText = null;
        [SerializeField]
        private Text defencePointsText = null;
        [SerializeField]
        private float activeChainFadeOutSpeed = 0.01f;
        [SerializeField]
        private float activeChainMoveSpeed = 7f;
        private Text roundAnnouncementText;
        private Transform activeChainTextTransform;
        private Vector3 activeChainTextStartPosition;
        private float roundTimer;
        private int lastScreenWidth, lastScreenHeight;
        private static Sprite[] battleTurnsSprites;
        private static Image turnsImage;
        private Sprite[] normalCellSprites = new Sprite[8];
        private Sprite[] selectedCellSprites = new Sprite[8];
        private bool hasUsedAttack;
        private bool hasUsedDefence;
        #endregion

        #region UI Setup
        void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
            roundAnnouncementText = roundAnnouncement.transform.GetChild(0).GetComponent<Text>();
        }

        void Start()
        {
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
            activeChainTextTransform = activeChainText.transform;
            activeChainTextStartPosition = activeChainTextTransform.position;
            LuckyFaceType = CellType.Thor;
        }

        void Update()
        {
            if (Instance == null)
                return;
            DetectScreenChanges();
            FadeActiveChangeText();
        }

        public void SetupPlayerUI(PlayerStats playerStats)
        {
            shuffleCountText.text = playerStats.ShuffleCount.ToString();
            clueCountText.text = playerStats.HintsCount.ToString();
            attackCountText.text = playerStats.AttackCount.ToString();
            defenceCountText.text = playerStats.DefenceCount.ToString();
            specialtyCountText.text = playerStats.SpecialtyCount.ToString();
            attackImage.sprite = Resources.Load<Sprite>("ThundergodPowerupIcons/" + playerStats.CharacterType.ToString() + "Atk");
            defenceImage.sprite = Resources.Load<Sprite>("ThundergodPowerupIcons/" + playerStats.CharacterType.ToString() + "Def");
            specialImage.sprite = Resources.Load<Sprite>("ThundergodPowerupIcons/" + playerStats.CharacterType.ToString() + "Spec");
            normalCellSprites = Resources.LoadAll<Sprite>("ThundergodNormalFaceIcons");
            selectedCellSprites = Resources.LoadAll<Sprite>("ThundergodSelectedFaceIcons");
        }

        public IEnumerator ResetUI()
        {
            UpdateAttackPointsText(0);
            UpdateDefencePointsText(0);
            roundAnnouncement.SetActive(false);
            HasUsedSpecialty = false;
            hasUsedDefence = false;
            hasUsedAttack = false;
            yield return StartCoroutine(UpdateRoundText());
        }

        public void GenerateLuckyFace()
        {
            luckyFaceImage.sprite = GetSprite(LuckyFaceType = Cell.GetRandomType(), CellState.Normal);
        }

        #endregion

        #region Text Updates
        public IEnumerator UpdateRoundText()
        {
            roundText.enabled = true;
            roundText.text = "Round " + GameMaster.Instance.CurrentRound;
            yield return new WaitForSeconds(2f);
            roundText.enabled = false;
        }

        public void UpdateRoundTimerText(float time)
        {
            roundTimerText.text = ((int)time).ToString();
        }

        public void SetRoundAnnouncementText(string text)
        {
            if (string.IsNullOrEmpty(text))
                roundAnnouncement.SetActive(false);
            else
            {
                roundAnnouncement.SetActive(true);
                roundAnnouncementText.text = text;
            }
        }

        public void UpdateAttackPointsText(int attackPoints)
        {
            attackPointsText.text = attackPoints.ToString();
        }

        public void UpdateDefencePointsText(int defencePoints)
        {
            defencePointsText.text = defencePoints.ToString();
        }
        #endregion

        #region Health Bar Update
        public void UpdatePlayerHealthSliderText(float healthPoints)
        {
            playerHealthSlider.value = healthPoints;
        }

        public void UpdateOpponentHealthSliderText(float healthPoints)
        {
            opponentHealthSlider.value = healthPoints;
        }
        #endregion

        #region Active Chain
        public void SetActiveChainText(int activeChain)
        {
            activeChainTextTransform.position = activeChainTextStartPosition;
            activeChainText.color = new Color(activeChainText.color.r, activeChainText.color.g, activeChainText.color.b, 1f);
            activeChainText.enabled = true;
            activeChainText.text = "Combo X " + activeChain;
        }

        private void FadeActiveChangeText()
        {
            if (activeChainText.enabled)
            {
                activeChainTextTransform.position += Vector3.up * activeChainMoveSpeed * Time.deltaTime;
                activeChainText.color = new Color(activeChainText.color.r, activeChainText.color.g, activeChainText.color.b, activeChainText.color.a - activeChainFadeOutSpeed);
                if (activeChainText.color.a <= 0)
                    activeChainText.enabled = false;
            }
        }

        #endregion

        #region Click Events Handler

        public void OnShuffleClicked()
        {
            if (!BoardManager.Instance.CanInteract || GameMaster.Instance.PlayerController.PlayerStats.ShuffleCount <= 0)
                return;
            BoardManager.Instance.StopAllCoroutines();
            BoardManager.Instance.Tweener.Kill(true);
            StartCoroutine(BoardManager.Instance.ShuffleBoard(0, false, false));
            shuffleCountText.text = (--GameMaster.Instance.PlayerController.PlayerStats.ShuffleCount).ToString();
        }

        public void OnHintClicked()
        {
            if (!BoardManager.Instance.CanInteract)
                return;
            if (GameMaster.Instance.PlayerController.PlayerStats.HintsCount > 0)
            {
                clueCountText.text = (--GameMaster.Instance.PlayerController.PlayerStats.HintsCount).ToString();
                BoardManager.Instance.FlickerHints();
                /*
                for (int p = 0; p < hint.PreMatchedCells.Length; p++)
                {
                    //PreMatchedCell preMatchedCell = hint.PreMatchedCells[p];
                    //preMatchedCell.Neighbor.Image.color = Color.green;
                    //foreach (Cell cell in preMatchedCell.Cells)
                        //cell.Image.color = Color.green;
                    //hint.Image.color = Color.green;
                    //preMatchedCell.Neighbor.Sprite.color = Color.green;
                    //print(hint.name + " is switching " + preMatchedCell.Direction + " with " + preMatchedCell.Neighbor.name);
                    //return;
                }
                */
            }
        }

        public void OnAttackClicked()
        {
            if (!BoardManager.Instance.CanInteract || GameMaster.Instance.PlayerController.PlayerStats.AttackCount <= 0 || hasUsedAttack)
                return;
            hasUsedAttack = true;
            GameMaster.Instance.PlayerController.PlayerStats.AttackPoints += (int)(GameMaster.Instance.PlayerController.PlayerStats.AttackPoints * .2f);
            attackPointsText.text = GameMaster.Instance.PlayerController.PlayerStats.AttackPoints.ToString();
            attackCountText.text = (--GameMaster.Instance.PlayerController.PlayerStats.AttackCount).ToString();
        }

        public void OnDefenceClicked()
        {
            if (!BoardManager.Instance.CanInteract || GameMaster.Instance.PlayerController.PlayerStats.DefenceCount <= 0 || hasUsedDefence)
                return;
            hasUsedDefence = true;
            GameMaster.Instance.PlayerController.PlayerStats.DefencePoints += (int)(GameMaster.Instance.PlayerController.PlayerStats.DefencePoints * .2f);
            defencePointsText.text = GameMaster.Instance.PlayerController.PlayerStats.DefencePoints.ToString();
            defenceCountText.text = (--GameMaster.Instance.PlayerController.PlayerStats.DefenceCount).ToString();
        }

        public void OnSpecialtyClicked()
        {
            if (!BoardManager.Instance.CanInteract || GameMaster.Instance.PlayerController.PlayerStats.SpecialtyCount <= 0 || HasUsedSpecialty)
                return;
            specialtyCountText.text = (--GameMaster.Instance.PlayerController.PlayerStats.SpecialtyCount).ToString();
            HasUsedSpecialty = true;
            switch (GameMaster.Instance.PlayerController.PlayerStats.CharacterType)
            {
                case CharacterType.Thor: GameMaster.Instance.FindCellRule(CellType.Thor).pointsWorth = 4; break;
                case CharacterType.Zeus:
                    BoardManager.Instance.CellFlickerTime = 30;
                    BoardManager.Instance.FlickerHints();
                    break;
                case CharacterType.LeiGong:
                    if (GameMaster.Instance.RoundTimer > 0)
                        GameMaster.Instance.RoundTimer += 5;
                    break;
                case CharacterType.Indra:
                    StartCoroutine(YieldForIndra());
                    break;
            }
        }

        #endregion

        #region Helpers
        private IEnumerator YieldForIndra()
        {
            BoardManager.Instance.CanInteract = false;
            CellType[] cellTypes = new CellType[]
            {
            CellType.Chaac,
            CellType.LeiGong,
            CellType.Odin,
            CellType.Perun,
            CellType.Thor,
            CellType.Zeus,
            CellType.Raijin
            };
            CellType randomCellType = cellTypes[Random.Range(0, cellTypes.Length)];
            for (int r = 0; r < BoardManager.Instance.Rows; r++)
            {
                for (int c = 0; c < BoardManager.Instance.Columns; c++)
                {
                    Cell cell = BoardManager.Instance.Cells[r, c];
                    if (cell.Type == randomCellType)
                        StartCoroutine(cell.ChangeTo(CellType.Indra, .5f));
                }
            }
            yield return new WaitForSeconds(.5f);
            for (int r = 0; r < BoardManager.Instance.Rows; r++)
            {
                for (int c = 0; c < BoardManager.Instance.Columns; c++)
                {
                    Cell cell = BoardManager.Instance.Cells[r, c];
                    GameMaster.Instance.MatchesPoints += cell.GetAllMatchedCells(BoardManager.Instance.Cells, ref BoardManager.Instance.matchingCells);
                }
            }
            if (BoardManager.Instance.matchingCells.Count > 0)
                BoardManager.Instance.HandleMatches(BoardManager.Instance.matchingCells.ToArray());
            BoardManager.Instance.CanInteract = true;
        }

        public void SetAnchorsToCorners(RectTransform transformRect)
        {
            RectTransform parentTransformRect = transformRect.parent.GetComponent<RectTransform>();
            float minAnchorX = transformRect.anchorMin.x + transformRect.offsetMin.x / parentTransformRect.rect.width;
            float minAnchorY = transformRect.anchorMin.y + transformRect.offsetMin.y / parentTransformRect.rect.height;
            float maxAnchorX = transformRect.anchorMax.x + transformRect.offsetMax.x / parentTransformRect.rect.width;
            float maxAnchorY = transformRect.anchorMax.y + transformRect.offsetMax.y / parentTransformRect.rect.height;
            Vector2 minAnchor = new Vector2(minAnchorX, minAnchorY);
            Vector2 maxAnchor = new Vector2(maxAnchorX, maxAnchorY);
            transformRect.anchorMin = minAnchor;
            transformRect.anchorMax = maxAnchor;
            transformRect.offsetMin = transformRect.offsetMax = Vector2.zero;
        }

        private void DetectScreenChanges()
        {
            int newScreenWidth = Screen.width;
            int newScreenHeight = Screen.height;
            if (lastScreenWidth != newScreenWidth || lastScreenHeight != newScreenHeight)
            {
                lastScreenWidth = newScreenWidth;
                lastScreenHeight = newScreenHeight;
                ScreenChanged?.Invoke();
            }
        }

        public Sprite GetSprite(CellType cellType, CellState cellState)
        {
            if (cellType == CellType.Empty)
                return null;
            string spriteName = cellType.ToString() + cellState.ToString();
            if (cellState == CellState.Normal)
            {
                for (int i = 0; i < normalCellSprites.Length; i++)
                {
                    if (normalCellSprites[i].name == spriteName)
                        return normalCellSprites[i];
                }
            }
            if (cellState == CellState.Selected)
            {
                for (int i = 0; i < selectedCellSprites.Length; i++)
                {
                    if (selectedCellSprites[i].name == spriteName)
                        return selectedCellSprites[i];
                }
            }
            return null;
        }
        #endregion
    }
}

