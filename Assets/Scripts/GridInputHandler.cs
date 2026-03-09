using UnityEngine;
using UnityEngine.EventSystems;

public class GridInputHandler : MonoBehaviour, IPointerClickHandler
{
    private Board board;

    public void Initialize(Board board)
    {
        this.board = board;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (board == null) return;

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            board.HandleRightClick();
        }
    }
}
