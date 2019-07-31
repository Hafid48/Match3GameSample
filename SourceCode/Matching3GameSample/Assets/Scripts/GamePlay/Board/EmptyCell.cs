using UnityEngine;
using UnityEngine.UI;
using Match3Sample.UI;

namespace Match3Sample.Gameplay.Board
{
    public class EmptyCell : MonoBehaviour
    {
        public BoxCollider Collider { get; private set; }
        public Vector3 AvailablePosition { get; private set; }
        public bool IsOccupied { get; private set; }

        public void Initialize(int r, int c)
        {
            Collider = GetComponent<BoxCollider>();
            transform.localScale = Vector3.one;
            RectTransform rectTransform = transform as RectTransform;
            rectTransform.sizeDelta = new Vector2(BoardManager.Instance.CellWidth, BoardManager.Instance.CellHeight);
            AvailablePosition = new Vector3(BoardManager.Instance.GridOffset.x + r * BoardManager.Instance.CellWidth, BoardManager.Instance.GridOffset.y + (c + 1) * BoardManager.Instance.CellHeight);
            transform.localPosition = AvailablePosition;
            GUIMaster.Instance.SetAnchorsToCorners(rectTransform);
        }

        public static Vector3 GetUnoccupiedPosition(int cordinateX)
        {
            for (int c = 0; c < BoardManager.Instance.Columns; c++)
            {
                EmptyCell emptyCell = BoardManager.Instance.EmptyCells[cordinateX, c];
                if (!emptyCell.IsOccupied)
                {
                    emptyCell.IsOccupied = true;
                    return emptyCell.AvailablePosition;
                }
            }
            return Vector3.zero;
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.tag == "Cell")
                IsOccupied = true;
        }

        void OnTriggerStay(Collider other)
        {
            if (other.tag == "Cell")
                IsOccupied = true;
        }

        void OnTriggerExit(Collider other)
        {
            if (other.tag == "Cell")
                IsOccupied = false;
        }
    }
}
