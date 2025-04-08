using UnityEngine;
using UnityEngine.EventSystems;

public class SimpleDragScroll : MonoBehaviour, IDragHandler, IBeginDragHandler
{
    private RectTransform rectTransform;
    private Vector2 lastMousePosition;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        lastMousePosition = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 currentMousePosition = eventData.position;
        Vector2 difference = currentMousePosition - lastMousePosition;

        rectTransform.anchoredPosition += new Vector2(0, difference.y);

        lastMousePosition = currentMousePosition;
    }
}
