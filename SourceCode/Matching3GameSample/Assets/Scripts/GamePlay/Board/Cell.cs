using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Match3Sample.UI;
using Match3Sample.Gameplay.Player.Stats;
using Match3Sample.Audio;
using Random = UnityEngine.Random;

namespace Match3Sample.Gameplay.Board
{
    public class Cell : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        #region Properties
        public Transform Transform { get; private set; }
        public BoxCollider Collider { get; private set; }
        public PreMatchedCellsInfo[] PreMatchedInfo { get; private set; } = new PreMatchedCellsInfo[0];
        public CellType Type
        {
            get
            {
                return type;
            }
            set
            {
                type = value;
                name = value.ToString();
                SetSprite();
            }
        }
        public CellState State
        {
            get
            {
                return state;
            }
            set
            {
                state = value;
                SetSprite();
            }
        }
        public int RowIndex { get; private set; }
        public int ColumnIndex { get; private set; }
        public bool IsPowerup { get; set; }
        public bool IsEmpty { get { return Type == CellType.Empty; } }
        public bool IsMoving { get; set; }
        public bool IsSwapping { get; set; }
        public bool IsReadyToMove { get; set; }
        public bool IsMatching { get; set; }
        public bool IsReady { get { return !IsMoving && !IsEmpty; } }
        #endregion

        #region Fields
        private Image myImage;
        private Tweener tweener;
        private CellType type;
        private CellState state;
        private Vector2 firstTouchPosition, lastTouchPosition;
        private float currentFlickerTime;
        [SerializeField]
        private static int cellTypesCount;
        private bool canFlicker;
        private bool isAnimatingPowerup;
        #endregion

        #region Cell Click & Swipe
        void Update()
        {
            if (IsPowerup && !isAnimatingPowerup)
                StartCoroutine(AnimateClearPowerup(BoardManager.Instance.PowerupSwitchTime));
            else if (canFlicker)
            {
                currentFlickerTime += Time.deltaTime;
                if (currentFlickerTime >= BoardManager.Instance.CellFlickerTime || !BoardManager.Instance.ContainsHint(this))
                    Flicker(false);
                else
                {
                    float time = Time.time % BoardManager.Instance.CellFlickerDuration;
                    if (time > 1f)
                        time = BoardManager.Instance.CellFlickerDuration - time;
                    myImage.color = Color.Lerp(Color.white, BoardManager.Instance.CellFlickerTargetColor, time);
                }
            }
        }

        public void OnPointerClick(PointerEventData data)
        {
            ///if the board is moving or shuffling then we don't want the player to interact with the cells
            if (!BoardManager.Instance.CanInteract  || IsSwapping || IsMatching)
                return;
            if (IsPowerup)
            {
                IsPowerup = false;
                BoardManager.Instance.ClearAllCellsOfType(Type);
            }
            else
            {
                Cell lastSelectedCell = BoardManager.Instance.LastSelectedCell;
                if (lastSelectedCell == null)
                    BoardManager.Instance.Select(this);
                else
                {
                    if (lastSelectedCell == this) // we are clicking on the same cell so deselect the cell
                        BoardManager.Instance.Deselect();
                    else // this is a different cell then what we previously selected
                    {
                        if (lastSelectedCell.CanSwap(this))
                        {
                            BoardManager.Instance.Swap(lastSelectedCell, this, true);
                            BoardManager.Instance.Deselect();
                        }
                        else // we clicked on non neighbour cell so deselect the last selected cell and select the new one
                        {
                            BoardManager.Instance.Deselect();
                            BoardManager.Instance.Select(this);
                        }
                    }
                }
            }
        }

        public void OnPointerDown(PointerEventData data)
        {
            ///if the board is moving or shuffling then we don't want the player to interact with the cells
            if (!BoardManager.Instance.CanInteract || IsSwapping || IsPowerup || IsMatching)
                return;
            lastTouchPosition = data.position;
            if (BoardManager.Instance.LastSelectedCell == null)
            {
                if (canFlicker)
                    Flicker(false);
                // we are not using the Select Method because we don't want this to interfere with the click event
                // this is just for visual
                State = CellState.Selected;
            }
        }

        public void OnPointerUp(PointerEventData data)
        {
            ///if the board is moving or shuffling then we don't want the player to interact with the cells
            if (!BoardManager.Instance.CanInteract || IsSwapping || IsPowerup || IsMatching)
                return;
            lastTouchPosition = data.position;
            State = CellState.Normal;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            ///if the board is moving or shuffling then we don't want the player to interact with the cells
            if (!BoardManager.Instance.CanInteract || IsSwapping || IsPowerup || IsMatching)
                return;
            firstTouchPosition = lastTouchPosition = eventData.position;
            if (BoardManager.Instance.LastSelectedCell != null)
                BoardManager.Instance.Deselect();
            State = CellState.Selected;
        }

        public void OnDrag(PointerEventData eventData)
        {
            ///if the board is moving or shuffling then we don't want the player to interact with the cells
            if (!BoardManager.Instance.CanInteract || IsSwapping || IsPowerup || IsMatching)
                return;
            lastTouchPosition = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            ///if the board is moving or shuffling then we don't want the player to interact with the cells
            if (!BoardManager.Instance.CanInteract || IsSwapping || IsPowerup || IsMatching)
                return;
            lastTouchPosition = eventData.position;
            float swipeDirectionX = Mathf.Abs(lastTouchPosition.x - firstTouchPosition.x);
            float swipeDirectionY = Mathf.Abs(lastTouchPosition.y - firstTouchPosition.y);
            if (swipeDirectionX > swipeDirectionY)
            {
                if (lastTouchPosition.x > firstTouchPosition.x) // swapping right
                {
                    Cell rightNeighbour = FindRightNeighbour(BoardManager.Instance.Cells);
                    if (CanSwap(rightNeighbour))
                        BoardManager.Instance.Swap(this, rightNeighbour, true);
                }
                else // swapping left
                {
                    Cell leftNeighbour = FindLeftNeighbour(BoardManager.Instance.Cells);
                    if (CanSwap(leftNeighbour))
                        BoardManager.Instance.Swap(this, leftNeighbour, true);
                }
            }
            else if (swipeDirectionY > swipeDirectionX)
            {
                if (lastTouchPosition.y > firstTouchPosition.y) // swapping above
                {
                    Cell aboveNeighbour = FindAboveNeighbour(BoardManager.Instance.Cells);
                    if (CanSwap(aboveNeighbour))
                        BoardManager.Instance.Swap(this, aboveNeighbour, true);
                }
                else // swapping below
                {
                    Cell belowNeighbour = FindBelowNeighbour(BoardManager.Instance.Cells);
                    if (CanSwap(belowNeighbour))
                        BoardManager.Instance.Swap(this, belowNeighbour, true);
                }
            }
            if (BoardManager.Instance.LastSelectedCell != null)
                BoardManager.Instance.Deselect();
            State = CellState.Normal;
        }


        #endregion

        #region Cell Setup
        public void Initialize(CellType cellType, int r, int c)
        {
            Transform = transform;
            Collider = GetComponent<BoxCollider>();
            myImage = GetComponent<Image>();
            // this is 2019 unity bug where the color is changed on the editor but the change doesn't follow to the game
            myImage.type = Image.Type.Filled;
            myImage.fillAmount = 1f;
            Transform.localScale = Vector3.one;
            RectTransform rectTransform = Transform as RectTransform;
            rectTransform.sizeDelta = new Vector2(BoardManager.Instance.CellWidth, BoardManager.Instance.CellHeight);
            Type = cellType;
            RowIndex = r;
            ColumnIndex = c;
            Transform.localPosition = new Vector3(BoardManager.Instance.GridOffset.x + r * BoardManager.Instance.CellWidth, BoardManager.Instance.GridOffset.y + -c * BoardManager.Instance.CellHeight, 0);
            cellTypesCount = Enum.GetNames(typeof(CellType)).Length;
            GUIMaster.Instance.SetAnchorsToCorners(rectTransform);
        }
        #endregion

        #region Cell Movement
        public void DisableAll()
        {
            Cell neighbor = null;
            int index = ColumnIndex - 1;
            while (true)
            {
                neighbor = FindAboveNeighbour(BoardManager.Instance.Cells, index);
                if (neighbor == null)
                    break;
                neighbor.IsSwapping = true;
                index--;
            }
        }
        /// <summary>
        /// This method is used to make sure the neighbor is ready to move 
        /// along with all the cells on the column this neighbor is on.
        /// </summary>
        /// <param name="neighbor"></param>
        /// <returns></returns>
        private bool IsColumnFree()
        {
            for (int c = 0; c < BoardManager.Instance.Columns; c++)
            {
                Cell cell = BoardManager.Instance.Cells[RowIndex, c];
                if (cell.IsMoving)
                    return false;
            }
            return true;
        }

        public void TransferTo(Vector2 newPosition)
        {
            Transform.localPosition = newPosition;
        }


        public void StopFollowingMe()
        {
            Cell neighbor = null;
            IsSwapping = false;
            IsMoving = false;
            int index = -1;
            while (true)
            {
                neighbor = FindBelowNeighbour(BoardManager.Instance.Cells, index);
                if (neighbor == null || neighbor == this)
                    break;
                neighbor.State = CellState.Normal;
                neighbor.IsSwapping = false;
                neighbor.IsMoving = false;
                index++;
            }
        }
        /// <summary>
        /// First set a destination to move to then check if the destination that we want to move to is not our current position
        /// so if it is then cancel moving because we are not going to move anyways.
        /// Second Calculate the duration that takes to move from our position to the destination.
        /// Third check if we are already moving, if so then we don't want to move again so we will just override the destination
        /// and if we are not then just move to destination.
        /// </summary>
        public void MoveDown()
        {
            Vector3 destination = new Vector3(BoardManager.Instance.GridOffset.x + RowIndex * BoardManager.Instance.CellWidth, BoardManager.Instance.GridOffset.y + ColumnIndex * -BoardManager.Instance.CellHeight);
            float distance = destination.y - Transform.localPosition.y;
            float moveSteps = Mathf.Abs(distance / BoardManager.Instance.CellHeight);
            if (IsMoving)
            {
                ResetAndAnimateTo(destination, BoardManager.Instance.CellFallDuation * moveSteps);
            }
            else
            {
                IsSwapping = true;
                IsMoving = true;
                tweener = AnimateTo(destination, BoardManager.Instance.CellFallDuation * moveSteps, BoardManager.Instance.CellFallEaseType, true);
            }
            IsReadyToMove = false;
            StartCoroutine(OnMoveCompleted());
        }

        /// <summary>
        /// Wait for the animation to be completed then falg this cell as ready soon afterwards
        /// Check if the column that we are in is free to move, if it is then run throught all the cells
        /// on our column and find all the possible matches for them and handle the matches if there is any.
        /// </summary>
        /// <returns></returns>
        private IEnumerator OnMoveCompleted()
        {
            State = CellState.Normal;
            BoardManager.Instance.MovingCells.Add(this);
            yield return tweener.WaitForCompletion();
            IsMoving = false;
            IsSwapping = false;
            if (IsColumnFree())
            {
                StopFollowingMe();
                StartCoroutine(FindMatches());
            }
            BoardManager.Instance.FindHints();
            if (GUIMaster.Instance.HasUsedSpecialty && GameMaster.Instance.PlayerController.PlayerStats.CharacterType == CharacterType.Zeus)
                BoardManager.Instance.FlickerHints();
        }

        #endregion

        #region Cell Animation


        private IEnumerator AnimateClearPowerup(float switchDuration)
        {
            int currentTypeIndex = (int)Type;
            isAnimatingPowerup = true;
            yield return new WaitForSeconds(switchDuration);
            currentTypeIndex++;
            if (currentTypeIndex >= cellTypesCount)
                currentTypeIndex = 1;
            Type = (CellType)currentTypeIndex;
            isAnimatingPowerup = false;
        }

        public void Flicker(bool flicker)
        {
            canFlicker = flicker;
            if (!flicker)
                myImage.color = Color.white;
            currentFlickerTime = 0;
        }

        /// <summary>
        /// This is a helper method to animate a cell from its current position to a destination.
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="duration"></param>
        /// <param name="easeType"></param>
        /// <param name="autoKill"></param>
        /// <returns></returns>
        public Tweener AnimateTo(Vector3 destination, float duration, Ease easeType, bool autoKill)
        {
            tweener = Transform.DOLocalMove(destination, duration).SetAutoKill(autoKill);
            return tweener;
        }

        /// <summary>
        /// This is called when we are already moving just to ovveride the cell's destination.
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="duration"></param>
        public void ResetAndAnimateTo(Vector3 destination, float duration)
        {
            tweener.ChangeEndValue(destination, duration);
        }
        #endregion

        #region Cell Swap

        /// <summary>
        /// this is a helper method to swap this cell with another cell
        /// taking into account the array that we are swapping from.
        /// </summary>
        /// <param name="cells"></param>
        /// <param name="newSelectedCell"></param>
        public void Swap(Cell[,] cells, ref Cell newSelectedCell)
        {
            // get refrence to follow cells cordinates
            int lastSelectedRowIndex = RowIndex;
            int lastSelectedColumnIndex = ColumnIndex;
            int newSelectedRowIndex = newSelectedCell.RowIndex;
            int newSelectedColumnIndex = newSelectedCell.ColumnIndex;
            // update the array by swapping both of its elements
            cells[lastSelectedRowIndex, lastSelectedColumnIndex] = newSelectedCell;
            cells[newSelectedRowIndex, newSelectedColumnIndex] = this;
            // swap last and new selected cells cordinates
            RowIndex = newSelectedRowIndex;
            ColumnIndex = newSelectedColumnIndex;
            newSelectedCell.RowIndex = lastSelectedRowIndex;
            newSelectedCell.ColumnIndex = lastSelectedColumnIndex;
        }

        /// <summary>
        /// This method is used to make sure the selected neighbor is a valid neighbor and that neighbor is ready to move
        /// and also this neighbor is indeed a neighbor of this cell and also check the column the neighbor is on and make sure
        /// that the column is free to move.
        /// </summary>
        /// <param name="neighbor"></param>
        private void CheckSwap(Cell neighbor)
        {
            if (CanSwap(neighbor))
                BoardManager.Instance.Swap(this, neighbor, true);
            else
            {
                State = CellState.Normal;
                SetSprite();
            }
        }

        public bool CanSwap(Cell neighbor)
        {
            if (neighbor != null && !neighbor.IsPowerup && !neighbor.IsMatching && neighbor.IsReady && !neighbor.IsSwapping && IsNeighbor(neighbor)
                && !IsPowerup && !IsSwapping && !IsMatching && IsReady)
                return true;
            return false;
        }

        public void Shuffle()
        {
            State = CellState.Normal;
            Type = GetRandomType();
        }

        public void ChangeTo(CellType cellType)
        {
            State = CellState.Normal;
            Type = cellType;
        }

        public IEnumerator ChangeTo(CellType cellType, float duration)
        {
            Quaternion startRotation = Transform.rotation;
            Transform.Rotate(Vector3.up, 180f);
            Quaternion endRotation = Transform.rotation;
            Transform.rotation = startRotation;
            float rate = 180f / duration;
            for (float i = 0.0f; i < 180f; i += Time.deltaTime * rate)
            {
                yield return null;
                Transform.Rotate(Vector3.up, rate * Time.deltaTime);
            }
            Transform.rotation = endRotation;
            Transform.eulerAngles = new Vector3(Transform.eulerAngles.x, 0, Transform.eulerAngles.z);
            State = CellState.Normal;
            Type = cellType;
            yield return null;
        }

        public void HandleRule(ref CellType lastCellType)
        {
            CellRule matchedCellRule = GameMaster.Instance.FindCellRule(Type);
            Instantiate(matchedCellRule.matchEffect, new Vector3(Transform.position.x, Transform.position.y, matchedCellRule.matchEffect.transform.position.z), Quaternion.identity);
            if (lastCellType != Type)
            {
                GameMaster.Instance.CalculateMatchingPoints(matchedCellRule);
                AudioMaster.Instance.PlaySFX(matchedCellRule.matchClips[Random.Range(0, matchedCellRule.matchClips.Length)]);
                lastCellType = Type;
            }
        }

        public void SetSprite()
        {
            if (IsEmpty)
                myImage.enabled = false;
            else
            {
                myImage.enabled = true;
                myImage.sprite = GUIMaster.Instance.GetSprite(Type, State);
            }
        }

        /// <summary>
        /// ignore the two types {0=Empty}
        /// and return a random type starting between the range of {1-EnumCount} 
        /// </summary>
        /// <returns></returns>
        public static CellType GetRandomType()
        {
            return (CellType)Random.Range(1, cellTypesCount);
        }
        #endregion

        #region Find Matches
        private IEnumerator FindMatches()
        {
            IsMatching = true;
            yield return new WaitForSeconds(BoardManager.Instance.DelayBeforeMatch);
            IsMatching = false;
            Cell neighbor = null;
            int index = -1;
            while (true)
            {
                neighbor = FindBelowNeighbour(BoardManager.Instance.Cells, index);
                if (neighbor == null)
                    break;
                if (!neighbor.IsEmpty && !neighbor.IsPowerup)
                    GameMaster.Instance.MatchesPoints += neighbor.GetAllMatchedCells(BoardManager.Instance.Cells, ref BoardManager.Instance.matchingCells);
                neighbor.IsMatching = false;
                index++;
            }
            for (int i = BoardManager.Instance.MovingCells.Count - 1; i >= 0; i--)
            {
                Cell cell = BoardManager.Instance.MovingCells[i];
                cell.IsMatching = false;
                if (!cell.IsEmpty && !cell.IsPowerup)
                    GameMaster.Instance.MatchesPoints = cell.GetAllMatchedCells(BoardManager.Instance.Cells, ref BoardManager.Instance.matchingCells);
                BoardManager.Instance.MovingCells.RemoveAt(i);
            }
            if (BoardManager.Instance.matchingCells.Count > 0)
                BoardManager.Instance.HandleMatches(BoardManager.Instance.matchingCells.ToArray());
        }
        /// <summary>
        /// This is a very usefull method for getting all the matches for this cell.
        /// </summary>
        /// <param name="cells"></param>
        public int GetAllMatchedCells(Cell[,] cells, ref List<Cell> matchingCells)
        {
            List<Cell> tempCells = new List<Cell>();
            Cell neighbor = null;
            int index = RowIndex;
            int totalMatchedCount = 0;
            bool hasHorizontalMatch = false, hasVerticalMatch = false;
            while (true)
            {
                neighbor = FindRightNeighbour(cells, index);
                if (!IsMatch(ref tempCells, ref neighbor))
                    break;
                index++;
                hasHorizontalMatch = true;
            }
            index = RowIndex;
            while (true)
            {
                neighbor = FindLeftNeighbour(cells, index);
                if (!IsMatch(ref tempCells, ref neighbor))
                    break;
                index--;
                hasHorizontalMatch = true;
            }
            CheckMatches(ref tempCells, ref matchingCells, ref totalMatchedCount);
            index = ColumnIndex;
            while (true)
            {
                neighbor = FindAboveNeighbour(cells, index);
                if (!IsMatch(ref tempCells, ref neighbor))
                    break;
                index--;
                hasVerticalMatch = true;
            }
            index = ColumnIndex;
            while (true)
            {
                neighbor = FindBelowNeighbour(cells, index);
                if (!IsMatch(ref tempCells, ref neighbor))
                    break;
                index++;
                hasVerticalMatch = true;
            }
            CheckMatches(ref tempCells, ref matchingCells, ref totalMatchedCount);
            if (totalMatchedCount >= 2)
            {
                if (hasHorizontalMatch)
                    totalMatchedCount++;
                if (hasVerticalMatch)
                    totalMatchedCount++;
            }
            return totalMatchedCount;
        }

        /// <summary>
        /// This is checking if there is an actual match it is used within "GetAllMatchedCells" function.
        /// </summary>
        /// <param name="tempCells"></param>
        /// <param name="neighbor"></param>
        /// <returns></returns>
        private bool IsMatch(ref List<Cell> tempCells, ref Cell neighbor)
        {
            // if the next cell is empty or it is a powerup then break;
            if (neighbor == null || neighbor.IsPowerup || !neighbor.IsReady)
                return false;
            if (Type != neighbor.Type || tempCells.Contains(neighbor) || BoardManager.Instance.matchingCells.Contains(neighbor))
                return false;
            //if there is a match between our current cell and the next cell then add it to our list;
            tempCells.Add(neighbor);
            return true;
        }

        private void CheckMatches(ref List<Cell> tempCells, ref List<Cell> matchingCells, ref int totalMatchedCount)
        {
            if (tempCells.Count >= 2)
            {
                matchingCells.AddRange(tempCells);
                totalMatchedCount += tempCells.Count;
                if (!matchingCells.Contains(this))
                    matchingCells.Add(this);
            }
            tempCells.Clear();
        }
        /// <summary>
        /// This is a method for getting all the matches of our cell before even moving it?
        /// it is used for example in finding a hint or when to check is there is any available moves or not.
        /// </summary>
        /// <param name="cells"></param>
        /// <returns></returns>
        public bool GetAllPreMatchedCells(Cell[,] cells)
        {
            List<Cell> matchingCells = new List<Cell>();
            // move the current cell to right then find all matches from all directions(Right, Left, Up, Down) from that spot
            // move the current cell to left then find all matches from all directions(Right, Left, Up, Down) from that spot
            // move the current cell up then find all matches from all directions(Right, Left, Up, Down) from that spot
            // move the current cell down then find all matches from all directions(Right, Left, Up, Down) from that spot
            List<PreMatchedCellsInfo> preMatchedCells = new List<PreMatchedCellsInfo>();
            Cell rightNeighbour = FindRightNeighbour(cells);
            if (rightNeighbour != null && !rightNeighbour.IsEmpty && !rightNeighbour.IsPowerup)
            {
                // we are one step to the right
                Swap(cells, ref rightNeighbour);
                GetAllMatchedCells(cells, ref matchingCells);
                List<Cell> preMatchedRightCells = matchingCells;
                if (preMatchedRightCells.Count > 0)
                    preMatchedCells.Add(new PreMatchedCellsInfo(preMatchedRightCells.ToArray(), rightNeighbour, PreMatchedCellsInfo.MatchDirection.Right));
                // swap it back to its original spot
                Swap(cells, ref rightNeighbour);
            }
            Cell leftNeighbour = FindLeftNeighbour(cells);
            if (leftNeighbour != null && !leftNeighbour.IsEmpty && !leftNeighbour.IsPowerup)
            {
                // we are one step to the left
                Swap(cells, ref leftNeighbour);
                //Board.Instance.MatchingCells.Clear();
                matchingCells.Clear();
                GetAllMatchedCells(cells, ref matchingCells);
                List<Cell> preMatchedLeftCells = matchingCells;
                if (preMatchedLeftCells.Count > 0)
                    preMatchedCells.Add(new PreMatchedCellsInfo(preMatchedLeftCells.ToArray(), leftNeighbour, PreMatchedCellsInfo.MatchDirection.Left));
                // swap it back to its original spot
                Swap(cells, ref leftNeighbour);
            }
            Cell aboveNeighbor = FindAboveNeighbour(cells);
            if (aboveNeighbor != null && !aboveNeighbor.IsEmpty && !aboveNeighbor.IsPowerup)
            {
                // we are one step up
                Swap(cells, ref aboveNeighbor);
                //Board.Instance.MatchingCells.Clear();
                matchingCells.Clear();
                GetAllMatchedCells(cells, ref matchingCells);
                List<Cell> preMatchedUpCells = matchingCells;
                if (preMatchedUpCells.Count > 0)
                    preMatchedCells.Add(new PreMatchedCellsInfo(preMatchedUpCells.ToArray(), aboveNeighbor, PreMatchedCellsInfo.MatchDirection.Up));
                // swap it back to its original spot
                Swap(cells, ref aboveNeighbor);
            }
            Cell belowNeighbor = FindBelowNeighbour(cells);
            if (belowNeighbor != null && !belowNeighbor.IsEmpty && !belowNeighbor.IsPowerup)
            {
                // we are one step down
                Swap(cells, ref belowNeighbor);
                //Board.Instance.MatchingCells.Clear();
                matchingCells.Clear();
                GetAllMatchedCells(cells, ref matchingCells);
                List<Cell> preMatchedDownCells = matchingCells;
                if (preMatchedDownCells.Count > 0)
                    preMatchedCells.Add(new PreMatchedCellsInfo(preMatchedDownCells.ToArray(), belowNeighbor, PreMatchedCellsInfo.MatchDirection.Down));
                // swap it back to its original spot
                Swap(cells, ref belowNeighbor);
            }
            PreMatchedInfo = preMatchedCells.ToArray();
            return preMatchedCells.Count > 0;
        }
        #endregion

        #region Find Cell Neighbours
        /// <summary>
        /// Checks if this neighbor is indeed a neighbor of our by checking this cell neighbors
        /// and compare them to this neighbor
        /// </summary>
        /// <param name="cell"></param>
        /// <returns></returns>
        public bool IsNeighbor(Cell cell)
        {
            // use raycast as it it is more reliable than actually getting neighbour by index
            if (FindRightNeighbourWithRaycast() == cell)
                return true;
            if (FindLeftNeighbourWithRaycast() == cell)
                return true;
            if (FindAboveNeighbourWithRaycast() == cell)
                return true;
            if (FindBelowNeighbourWithRaycast() == cell)
                return true;
            return false;
        }

        public Cell FindRightNeighbourFromPosition(Cell[,] cells)
        {
            foreach (Cell cell in cells)
            {
                if (Mathf.Approximately(Transform.localPosition.y, cell.Transform.localPosition.y) &&
                    Mathf.Approximately(Transform.localPosition.x + BoardManager.Instance.CellHeight, cell.Transform.localPosition.x))
                    return cell;
            }
            return null;
        }

        public Cell FindLeftNeighbourFromPosition(Cell[,] cells)
        {
            foreach (Cell cell in cells)
            {
                if (Mathf.Approximately(Transform.localPosition.y, cell.Transform.localPosition.y) &&
                    Mathf.Approximately(Transform.localPosition.x - BoardManager.Instance.CellHeight, cell.Transform.localPosition.x))
                    return cell;
            }
            return null;
        }

        public Cell FindAboveNeighbourFromPosition(Cell[,] cells)
        {
            foreach (Cell cell in cells)
            {
                if (Mathf.Approximately(Transform.localPosition.x, cell.Transform.localPosition.x) &&
                    Mathf.Approximately(Transform.localPosition.y + BoardManager.Instance.CellHeight, cell.Transform.localPosition.y))
                    return cell;
            }
            return null;
        }

        public Cell FindDownNeighbourFromPosition(Cell[,] cells)
        {
            foreach (Cell cell in cells)
            {
                if (Mathf.Approximately(Transform.localPosition.x, cell.Transform.localPosition.x) &&
                    Mathf.Approximately(Transform.localPosition.y - BoardManager.Instance.CellHeight, cell.Transform.localPosition.y))
                    return cell;
            }
            return null;
        }

        public Cell FindRightNeighbour(Cell[,] cells)
        {
            return FindRightNeighbour(cells, RowIndex);
        }

        public Cell FindRightNeighbourWithRaycast()
        {
            RaycastHit hit;
            if (Physics.Raycast(Transform.position, Vector3.right, out hit, BoardManager.Instance.RaycastDistance, BoardManager.Instance.CellLayerMask))
                return hit.transform.GetComponent<Cell>();
            return null;
        }

        public Cell FindRightNeighbour(Cell[,] cells, int index)
        {
            int nextCellIndex = index + 1;
            if (nextCellIndex >= BoardManager.Instance.Rows)
                return null;
            return cells[nextCellIndex, ColumnIndex];
        }

        public Cell FindLeftNeighbour(Cell[,] cells)
        {
            return FindLeftNeighbour(cells, RowIndex);
        }

        public Cell FindLeftNeighbourWithRaycast()
        {
            RaycastHit hit;
            if (Physics.Raycast(Transform.position, -Vector3.right, out hit, BoardManager.Instance.RaycastDistance, BoardManager.Instance.CellLayerMask))
                return hit.transform.GetComponent<Cell>();
            return null;
        }

        public Cell FindLeftNeighbour(Cell[,] cells, int index)
        {
            int nextCellIndex = index - 1;
            if (nextCellIndex <= -1)
                return null;
            return cells[nextCellIndex, ColumnIndex];
        }

        public Cell FindAboveNeighbour(Cell[,] cells, int index)
        {
            int nextCellIndex = index - 1;
            if (nextCellIndex <= -1)
                return null;
            return cells[RowIndex, nextCellIndex];
        }

        public Cell FindAboveNeighbourWithRaycast()
        {
            RaycastHit hit;
            if (Physics.Raycast(Transform.position, Vector3.up, out hit, BoardManager.Instance.RaycastDistance, BoardManager.Instance.CellLayerMask))
                return hit.transform.GetComponent<Cell>();
            return null;
        }

        public Cell FindAboveNeighbour(Cell[,] cells)
        {
            return FindAboveNeighbour(cells, ColumnIndex);
        }

        public Cell FindBelowNeighbour(Cell[,] cells, int index)
        {
            int nextCellIndex = index + 1;
            if (nextCellIndex >= BoardManager.Instance.Columns)
                return null;
            return cells[RowIndex, nextCellIndex];
        }

        public Cell FindBelowNeighbourWithRaycast()
        {
            RaycastHit hit;
            if (Physics.Raycast(Transform.position, -Vector3.up, out hit, BoardManager.Instance.RaycastDistance, BoardManager.Instance.CellLayerMask))
                return hit.transform.GetComponent<Cell>();
            return null;
        }

        public Cell FindBelowNeighbour(Cell[,] cells)
        {
            return FindBelowNeighbour(cells, ColumnIndex);
        }
        #endregion
    }

    #region Enumerations
    public enum CellType
    {
        Empty,
        Chaac,
        Indra,
        LeiGong,
        Odin,
        Perun,
        Thor,
        Zeus,
        Raijin
    }

    public enum CellState
    {
        Normal,
        Selected
    }
    #endregion

    #region Structs
    public struct PreMatchedCellsInfo
    {
        public Cell[] Cells { get; private set; }
        public Cell Neighbour { get; private set; }
        public MatchDirection Direction { get; private set; }

        public PreMatchedCellsInfo(Cell[] cells, Cell neighbour, MatchDirection direction)
        {
            Cells = cells;
            Neighbour = neighbour;
            Direction = direction;
        }

        public enum MatchDirection
        {
            Right,
            Left,
            Up,
            Down
        }
    }
    #endregion
}



