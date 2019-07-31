using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using DG.Tweening.Core;
using Match3Sample.UI;
using Match3Sample.Audio;

namespace Match3Sample.Gameplay.Board
{
    public class BoardManager : MonoBehaviour
    {
        public Transform gridTransform;
        #region Properties
        public static BoardManager Instance { get; set; }
        public Tweener Tweener { get; private set; }
        public Vector2 GridOffset { get; private set; }
        public Cell[,] Cells { get; private set; }
        public EmptyCell[,] EmptyCells { get; private set; }
        public List<Cell> MovingCells { get; private set; } = new List<Cell>();
        public Cell[] Hints { get; private set; }
        public Cell LastSelectedCell { get; private set; }
        public LayerMask CellLayerMask { get { return cellLayerMask; } }
        public float CellWidth { get; private set; }
        public float CellHeight { get; private set; }
        public float RaycastDistance { get; private set; }
        public float PowerupSwitchTime { get { return powerupSwitchTime; } }
        public float DelayBeforeMatch { get { return delayBeforeMatch; } }
        public float CellFallDuation { get { return cellFallDuration; } }
        public Ease CellFallEaseType { get { return cellFallEaseType; } }
        public float CellFlickerTime { get { return cellFlickerTime; } set { cellFlickerTime = value; } }
        public float CellFlickerDuration { get { return cellFlickerDuration; } set { cellFlickerDuration = value; } }
        public Color CellFlickerTargetColor { get { return cellFlickerTargetColor; } }
        public int Rows { get { return rows; } }
        public int Columns { get { return columns; } }
        public bool CanInteract { get; set; }
        #endregion :

        #region Fields
        [System.NonSerialized]
        public List<Cell> matchingCells = new List<Cell>();
        [SerializeField]
        private LayerMask cellLayerMask = -1;
        [SerializeField]
        private int rows = 8, columns = 8;
        [SerializeField, Range(0, 100)]
        private int powerupChance = 25; // the higher the number the heigher chance the board will generate powerups.
        [SerializeField, Range(.001f, 1f)]
        private float powerupSwitchTime = .05f;
        [SerializeField]
        private float gridSpawnPositionY = 700f, gridRespawnPositionY = 300f;
        [SerializeField]
        private float gridSpawnDuration = 1, gridRespawnDuration = .5f; // how long does it takes to get from grid spawn position to grid end position.
        [SerializeField]
        private Ease gridFallEaseType = Ease.Linear; // this is used along with the boardFullDuration to control the animation ease motion.
        [Range(0, 1f)]
        private float swapDuration = .2f; // how long does the animation takes to complete a swap between two cells.
        [SerializeField]
        private Ease swapEaseType = Ease.OutQuad; // this is used along with the swapDuration to control the animation ease motion.
        [SerializeField]
        private float delayBeforeFall = .2f; // when we have a match, this controls how long does it waits to destroy the cells and handle matches functionalities.
        [SerializeField]
        private float cellFallDuration = .2f; // how long the animation for a cell takes to move one step down, this is used as a base to calculate the exact time needed to reach the cell's destination.
        [SerializeField]
        private Ease cellFallEaseType = Ease.OutQuart; // this is used along with the cellFullDuration to control the animation ease motion.
        [SerializeField]
        private float delayBeforeMatch = .025f;
        [SerializeField]
        private float delayBeforeShuffle = .6f; // when we have no available moves, how long does it wait to shuffle the board.
        [SerializeField]
        private float delayBeforeEndingRound = .25f;
        [SerializeField]
        private Color cellFlickerTargetColor = Color.gray;
        [SerializeField]
        private float cellFlickerDuration = 2f;
        [SerializeField]
        private float cellFlickerTime = 7f;
        private RectTransform grid;
        private RectTransform emptyCells;
        private float gridEndPositionY;
        private bool isBoardReady;
        #endregion

        #region General Setup
        void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
            grid = transform.Find("Grid") as RectTransform;
            emptyCells = transform.Find("EmptyCells") as RectTransform;
            gridEndPositionY = grid.localPosition.y;
        }

        void OnEnable()
        {
            GUIMaster.ScreenChanged += new GUIMaster.ScreenChangeHandler(OnScreenChanged);
        }

        void OnDisable()
        {
            GUIMaster.ScreenChanged -= new GUIMaster.ScreenChangeHandler(OnScreenChanged); ;
        }

        void Update()
        {
            if (Instance == null || !isBoardReady)
                return;
            if (!IsAnyCellSwapping())
            {
                if (GameMaster.Instance.RoundTimer <= 0f)
                    StartCoroutine(EndRound());
                if (!IsThereAnyPowerup())
                {
                    if (Hints.Length == 0)
                        StartCoroutine(ShuffleBoard(delayBeforeShuffle, false, true));
                }
            }
        }

        #endregion

        #region Grid Setup
        public void CalculateRaycastDistance()
        {
            RaycastDistance = Vector3.Distance(Cells[0, 0].Transform.position, Cells[0, 1].Transform.position);
        }

        public void CalculateGrid()
        {
            CellWidth = grid.rect.width / rows;
            CellHeight = grid.rect.height / columns;
            GridOffset = new Vector2(grid.rect.xMin + CellWidth * .5f, grid.rect.yMax - CellHeight * .5f);
        }

        private void UpdateCellSize(RectTransform gridRectTransform)
        {
            if (Cells == null || EmptyCells == null)
                return;
            CellWidth = gridRectTransform.rect.width / rows;
            CellHeight = gridRectTransform.rect.height / columns;
            Vector3 cellSize = new Vector3(CellWidth, CellHeight, .1f);
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < columns; c++)
                {
                    Cells[r, c].Collider.size = cellSize;
                    EmptyCells[r, c].Collider.size = cellSize - new Vector3(1, 1, 0);
                    GUIMaster.Instance.SetAnchorsToCorners(Cells[r, c].Transform as RectTransform);
                    GUIMaster.Instance.SetAnchorsToCorners(EmptyCells[r, c].transform as RectTransform);
                }
            }
        }

        void OnScreenChanged()
        {
            UpdateCellSize(grid);
            CalculateRaycastDistance();
        }

        #endregion

        #region Board Setup

        public IEnumerator InitializeBoard()
        {
            yield return StartCoroutine(GUIMaster.Instance.UpdateRoundText());
            CalculateGrid();
            CreateBoard();
            VerifyBoard();
            FindHints();
            matchingCells.Clear();
            CalculateRaycastDistance();
            GUIMaster.Instance.GenerateLuckyFace();
            AnimateBoard(gridSpawnPositionY, gridEndPositionY, gridSpawnDuration, OnBoardAnimationCompleted(true, false));
        }

        /// <summary>
        /// Generate a random board for the first time and intialize it.
        /// </summary>
        private void CreateBoard()
        {
            Cells = new Cell[rows, columns];
            EmptyCells = new EmptyCell[rows, columns];
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < columns; c++)
                {
                    Cells[r, c] = GenerateCell(r, c, Cell.GetRandomType());
                    EmptyCells[r, c] = GenerateEmptyCell(r, c);
                }
            }
            SetRandomPowerups();
        }

        /// <summary>
        /// Create a random cell and intialize its parameters.
        /// </summary>
        /// <param name="rowIndex"></param>
        /// <param name="columnIndex"></param>
        /// <returns></returns>
        private Cell GenerateCell(int rowIndex, int columnIndex, CellType cellType)
        {
            GameObject cellGameObject = new GameObject("Image", typeof(RectTransform));
            cellGameObject.tag = "Cell";
            cellGameObject.layer = LayerMask.NameToLayer("Cell");
            cellGameObject.AddComponent<Image>();
            BoxCollider cellCollider = cellGameObject.AddComponent<BoxCollider>();
            cellCollider.isTrigger = false;
            cellCollider.size = new Vector3(CellWidth - 1, CellHeight - 1, .1f);
            Rigidbody cellRigidbody = cellGameObject.AddComponent<Rigidbody>();
            cellRigidbody.useGravity = false;
            cellRigidbody.isKinematic = true;
            Cell randomCell = cellGameObject.AddComponent<Cell>();
            cellGameObject.transform.SetParent(grid.transform);
            randomCell.Initialize(cellType, rowIndex, columnIndex);
            return randomCell;
        }

        private EmptyCell GenerateEmptyCell(int rowIndex, int columnIndex)
        {
            GameObject emptyCellGameObject = new GameObject("X:" + rowIndex + " Y:" + columnIndex, typeof(RectTransform));
            BoxCollider emptyCellCollider = emptyCellGameObject.AddComponent<BoxCollider>();
            emptyCellCollider.isTrigger = true;
            emptyCellCollider.size = new Vector3(CellWidth, CellHeight, .1f);
            Rigidbody emptyCellRigidbody = emptyCellGameObject.AddComponent<Rigidbody>();
            emptyCellRigidbody.useGravity = false;
            emptyCellRigidbody.isKinematic = true;
            EmptyCell emptyCell = emptyCellGameObject.AddComponent<EmptyCell>();
            emptyCellGameObject.transform.SetParent(emptyCells.transform);
            emptyCell.Initialize(rowIndex, columnIndex);
            return emptyCell;
        }

        /// <summary>
        /// Create a random powerups based on the powerChance variable.
        /// </summary>
        private void SetRandomPowerups()
        {
            if (powerupChance == 0)
                return;
            int powerupProbability = Random.Range(1, 101);
            if (powerupProbability <= powerupChance)
            {
                int randomRow = Random.Range(0, rows);
                int randomColumn = Random.Range(0, columns);
                Cell randomCell = Cells[randomRow, randomColumn];
                randomCell.IsPowerup = true;
                randomCell.SetSprite();
            }
        }

        /// <summary>
        /// Shuffle an already initialized board.
        /// </summary>
        private void ShuffleBoard()
        {
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < columns; c++)
                {
                    Cell currentCell = Cells[r, c];
                    currentCell.IsPowerup = false;
                    currentCell.Shuffle();
                }
            }
            SetRandomPowerups();
        }

        /// <summary>
        /// This check if there is any available moves, if not it shuffles the board.
        /// </summary>
        /// <returns></returns>
        public IEnumerator ShuffleBoard(float delayBeforeShuffle, bool resetMusic, bool addShuffleBonusPoints)
        {
            ResetBoard();
            yield return new WaitForSeconds(delayBeforeShuffle);
            ShuffleBoard();
            VerifyBoard();
            FindHints();
            AnimateBoard(gridRespawnPositionY, gridEndPositionY, gridRespawnDuration, OnBoardAnimationCompleted(resetMusic, addShuffleBonusPoints));
        }

        /// <summary>
        /// Verify the generated cells and make sure there is no matched cells before player's interaction
        /// and also make sure there there is at least one match after shuffeling.
        /// </summary>
        private void VerifyBoard()
        {
            while (true)
            {
                // if there is some matches then randomize this cell to something different, keep doing that untill we are sure there is no matches at all.
                if (!HasAnyMatches())
                    break;
                ShuffleBoard();
            }
            // make sure there is at least one match if not then reshuffle the baord and verify it once more
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < columns; c++)
                {
                    Cell currentCell = Cells[r, c];
                    currentCell.GetAllPreMatchedCells(Cells);
                    if (currentCell.PreMatchedInfo.Length > 0)
                        return;
                }
            }
            ShuffleBoard();
            VerifyBoard();
        }

        /// <summary>
        /// Reset the board so that we unhover any selected cell that the player has did if there is any.
        /// </summary>
        public void ResetBoard()
        {
            GameMaster.Instance.PauseActiveChain(true);
            CanInteract = false;
            isBoardReady = false;
            matchingCells.Clear();
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < columns; c++)
                {
                    Cell currentCell = Cells[r, c];
                    currentCell.IsSwapping = false;
                    currentCell.IsReadyToMove = false;
                    currentCell.IsMoving = false;
                    currentCell.IsMatching = false;
                    currentCell.State = CellState.Normal;
                    currentCell.Flicker(false);
                    currentCell.SetSprite();
                }
            }
        }
        #endregion

        #region Board Animation
        /// <summary>
        /// This is called at startup to animate the board from gridSpawnPosition to gridEndPosition
        /// </summary>
        public void AnimateBoard(float startPositionY, float endPositionY, float duration, IEnumerator endFunctionCall)
        {
            grid.localPosition = new Vector3(grid.localPosition.x, startPositionY, grid.localPosition.z);
            Tweener = grid.DOLocalMoveY(endPositionY, duration).SetEase(gridFallEaseType);
            if (endFunctionCall == null)
                return;
            StartCoroutine(endFunctionCall);
        }

        /// <summary>
        /// Wait untill the board animation has completed and do not allow any interaction with the board
        /// untill the animation is completed.
        /// </summary>
        /// <param name="tweener"></param>
        /// <returns></returns>
        private IEnumerator OnBoardAnimationCompleted(bool resetMusic, bool addShuffleBonusPoints)
        {
            yield return Tweener.WaitForCompletion();
            if (resetMusic)
                AudioMaster.Instance.PlayMusic(.75f);
            if (addShuffleBonusPoints)
            {
                GameMaster.Instance.PlayerController.PlayerStats.AttackPoints += 50;
                GameMaster.Instance.PlayerController.PlayerStats.DefencePoints += 50;
                GUIMaster.Instance.UpdateAttackPointsText(GameMaster.Instance.PlayerController.PlayerStats.AttackPoints);
                GUIMaster.Instance.UpdateDefencePointsText(GameMaster.Instance.PlayerController.PlayerStats.DefencePoints);
            }
            GameMaster.Instance.PauseActiveChain(false);
            GameMaster.Instance.StartRoundTimer();
            CanInteract = true;
            isBoardReady = true;
        }
        #endregion

        #region Board Check
        private bool HasAnyMatches()
        {
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < columns; c++)
                {
                    Cell cell = Cells[r, c];
                    if (cell.IsPowerup)
                        continue;
                    matchingCells.Clear();
                    cell.GetAllMatchedCells(Cells, ref matchingCells);
                    if (matchingCells.Count > 0)
                        return true;
                }
            }
            return false;
        }

        public bool IsAnyCellSwapping()
        {
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < columns; c++)
                {
                    Cell cell = Cells[r, c];
                    if (cell.IsSwapping || cell.IsMoving || cell.IsMatching)
                        return true;
                }
            }
            return false;
        }

        public bool IsThereAnyPowerup()
        {
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < columns; c++)
                {
                    Cell cell = Cells[r, c];
                    if (cell.IsPowerup)
                        return true;
                }
            }
            return false;
        }

        public void ClearAllCellsOfType(CellType cellType)
        {
            AudioMaster.Instance.PlayPowerupSFX();
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < columns; c++)
                {
                    Cell cell = Cells[r, c];
                    if (cell.Type != cellType || cell.IsSwapping)
                        continue;
                    CellRule cellRule = GameMaster.Instance.FindCellRule(cell.Type);
                    GameObject matchEffect = Instantiate(cellRule.matchEffect, new Vector3(cell.Transform.position.x, cell.Transform.position.y, cellRule.matchEffect.transform.position.z), Quaternion.identity);
                    Destroy(matchEffect, .2f);
                    GameMaster.Instance.AddPointsToPlayer(cellRule);
                    cell.Type = CellType.Empty;
                    StartCoroutine(FillEmptyCells(0));
                }
            }
        }

        private IEnumerator EndRound()
        {
            ResetBoard();
            yield return new WaitForSeconds(delayBeforeEndingRound);
            AnimateBoard(gridEndPositionY, gridSpawnPositionY, gridSpawnDuration, GameMaster.Instance.OnRoundEnded());
        }

        #endregion

        #region Cell Selection & Swap

        public void Select(Cell cell)
        {
            LastSelectedCell = cell;
            LastSelectedCell.State = CellState.Selected;
        }

        public void Deselect()
        {
            LastSelectedCell.State = CellState.Normal;
            LastSelectedCell = null;
        }

        /// <summary>
        /// Animate the lastSelectedCell and the newSelectedCell towards each other (SWAP)
        /// </summary>
        /// <param name="lastSelectedCell"></param>
        /// <param name="newSelectedCell"></param>
        public void Swap(Cell lastSelectedCell, Cell newSelectedCell, bool checkForMatches)
        {
            if (!lastSelectedCell.IsReady || !newSelectedCell.IsReady || !lastSelectedCell.IsNeighbor(newSelectedCell))
            {
                lastSelectedCell.IsSwapping = false;
                newSelectedCell.IsSwapping = false;
                Cell[] matchedCells;
                if (HasMatches(lastSelectedCell, newSelectedCell, out matchedCells, out bool hasLastSelectedMatches, out bool hasNewSelectedMatches))
                    HandleMatches(matchedCells);
                return;
            }
            lastSelectedCell.Swap(BoardManager.Instance.Cells, ref newSelectedCell);
            Vector3 lastSelectedCellPosition = lastSelectedCell.Transform.localPosition;
            Vector3 newSelectedCellPosition = newSelectedCell.Transform.localPosition;
            lastSelectedCell.IsSwapping = true;
            newSelectedCell.IsSwapping = true;
            // swap the last and the new selected cells positions and wait for animation completion
            Tweener lastCellTweener = lastSelectedCell.AnimateTo(newSelectedCellPosition, BoardManager.Instance.swapDuration, BoardManager.Instance.swapEaseType, true);
            Tweener newCellTweener = newSelectedCell.AnimateTo(lastSelectedCellPosition, BoardManager.Instance.swapDuration, BoardManager.Instance.swapEaseType, true);
            if (checkForMatches)
                StartCoroutine(OnSwapCompleted(lastCellTweener, newCellTweener, lastSelectedCell, newSelectedCell));
        }

        /// <summary>
        /// Wait for the animation to be completed then swap the elements and check each element
        /// if it has a match, if one of them has matches then handle these matches otherwise just reverse the animation back.
        /// </summary>
        private IEnumerator OnSwapCompleted(Tweener lastCellTweener, Tweener newCellTweener, Cell lastSelectedCell, Cell newSelectedCell)
        {
            yield return new WaitForSeconds(swapDuration);
            Cell[] matchedCells;
            if (!HasMatches(lastSelectedCell, newSelectedCell, out matchedCells, out bool hasLastSelectedMatches, out bool hasNewSelectedMatches))
            {
                if (!newSelectedCell.IsMatching)
                {
                    // if there is no matches at all then reverse the movement, swap the cells elements and reset cells's cordinates
                    Swap(lastSelectedCell, newSelectedCell, false);
                    yield return new WaitForSeconds(swapDuration);
                    if (HasMatches(lastSelectedCell, newSelectedCell, out matchedCells, out hasLastSelectedMatches, out hasNewSelectedMatches))
                        HandleMatches(matchedCells);
                }
                lastSelectedCell.State = CellState.Normal;
                newSelectedCell.State = CellState.Normal;
                lastSelectedCell.IsSwapping = false;
                newSelectedCell.IsSwapping = false;
            }
            else
            {
                lastSelectedCell.State = CellState.Normal;
                lastSelectedCell.IsSwapping = false;
                newSelectedCell.State = CellState.Normal;
                newSelectedCell.IsSwapping = false;
                HandleMatches(matchedCells);
            }
        }

        /// <summary>
        /// This checks the lastSelectedCell and the newSelectedCell if any of them has matches
        /// and return the matched cells result it also returns if the lastSelectedCell and the newSelectedCell
        /// had matches or not.
        /// </summary>
        public bool HasMatches(Cell lastSelectedCell, Cell newSelectedCell, out Cell[] matchedCells, out bool hasLastSelectedMatches, out bool hasNewSelectedMatches)
        {
            int lastMatchedCount = 0;
            BoardManager.Instance.matchingCells.Clear();
            int totalMatchedCount = lastSelectedCell.GetAllMatchedCells(Cells, ref matchingCells);
            lastMatchedCount = matchingCells.Count;
            hasLastSelectedMatches = lastMatchedCount > 0;
            totalMatchedCount += newSelectedCell.GetAllMatchedCells(Cells, ref matchingCells);
            int newMatchedCount = matchingCells.Count - lastMatchedCount;
            hasNewSelectedMatches = newMatchedCount > 0;
            matchedCells = matchingCells.ToArray();
            GameMaster.Instance.MatchesPoints += totalMatchedCount;
            return matchedCells.Length > 0;
        }

        /// <summary>
        /// This method is used to mark the matchedCells as empty in order to be processed by "FillEmptyCells" function.
        /// </summary>
        /// <param name="matchedCells"></param>
        public void HandleMatches(Cell[] matchedCells)
        {
            GameMaster.Instance.HandleActiveChain();
            CellType lastCellType = CellType.Empty;
            for (int i = 0; i < matchedCells.Length; i++)
            {
                Cell matchedCell = matchedCells[i];
                matchedCell.DisableAll();
                matchedCell.HandleRule(ref lastCellType);
                matchedCell.State = CellState.Normal;
                matchedCell.Type = CellType.Empty;
                matchedCell.IsMatching = true;
                matchingCells.Remove(matchedCell);
            }
            StartCoroutine(FillEmptyCells(delayBeforeFall));
        }

        /// <summary>
        /// First this method waits for the fall delay soon afterward it will start looking from the bottom
        /// if it found an empty cell which are the cells that marked by the "HandleMatches" function then
        /// it goes from that emptyCell CordinateY and keep swapping th current cell with its above neighbor
        /// untill all the column is swapped.
        /// Second we start looking again from the borrom of the board for empty cells when we find any
        /// we start from the top of that column till that empty cell and we shuffle them all and transfer them above
        /// Finally we run throught all the board and move them down.
        /// </summary>
        /// <returns></returns>
        public IEnumerator FillEmptyCells(float delayBeforeFall)
        {
            yield return new WaitForSeconds(delayBeforeFall);
            for (int r = 0; r < rows; r++)
            {
                for (int c = columns - 1; c >= 0; c--)
                {
                    Cell thisCell = Cells[r, c];
                    if (!thisCell.IsEmpty)
                        continue;
                    int c1;
                    int c2 = c;
                    for (c1 = c; c1 >= 0; c1--)
                    {
                        Cell neighbor = Cells[r, c1];
                        if (neighbor.IsEmpty)
                            continue;
                        Cell otherCell = Cells[r, c2];
                        neighbor.Swap(Cells, ref otherCell);
                        neighbor.IsReadyToMove = true;
                        c2--;
                    }
                    for (c1 = c; c1 >= 0; c1--)
                    {
                        Cell neighbor = Cells[r, c1];
                        if (!neighbor.IsEmpty)
                            continue;
                        neighbor.TransferTo(EmptyCell.GetUnoccupiedPosition(r));
                        neighbor.Shuffle();
                        Cell otherCell = Cells[r, c2];
                        neighbor.Swap(Cells, ref otherCell);
                        neighbor.IsSwapping = false;
                        neighbor.IsReadyToMove = true;
                        c2--;
                    }
                    for (c1 = c; c1 >= 0; c1--)
                    {
                        Cell neighbor = Cells[r, c1];
                        if (neighbor.IsReadyToMove)
                            neighbor.MoveDown();
                    }
                }
            }
        }
        #endregion

        #region Hints
        /// <summary>
        /// This method is used to find hints.
        /// </summary>
        public void FindHints()
        {
            Cell[,] cells = new Cell[rows, columns];
            List<Cell> hints = new List<Cell>();
            for (int r = 0; r < BoardManager.Instance.rows; r++)
            {
                for (int c = 0; c < BoardManager.Instance.columns; c++)
                {
                    System.Array.Copy(BoardManager.Instance.Cells, cells, cells.Length);
                    Cell cell = cells[r, c];
                    if (cell.IsPowerup)
                    {
                        hints.Add(cell);
                        continue;
                    }
                    if (!cell.IsReady || cell.IsSwapping)
                        continue;
                    if (cell.GetAllPreMatchedCells(cells))
                        hints.Add(cell);
                }
            }
            Hints = hints.ToArray();
        }

        public void FlickerHints()
        {
            for (int h = 0; h < Hints.Length; h++)
            {
                Cell hint = Hints[h];
                hint.Flicker(true);
            }
        }

        public bool ContainsHint(Cell cell)
        {
            foreach (Cell hint in Hints)
            {
                if (hint == cell)
                    return true;
            }
            return false;
        }
        #endregion
    }
}
