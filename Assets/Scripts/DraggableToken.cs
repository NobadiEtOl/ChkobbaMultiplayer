using UnityEngine;

public class DraggableToken : MonoBehaviour
{
    private Vector3 originalPosition;
    private bool isDragging = false;
    private Vector3 offset;

    void Start()
    {
        // Save the original position
        originalPosition = transform.position;

        // Ensure there's a Collider2D for mouse events
        if (GetComponent<Collider2D>() == null)
        {
            gameObject.AddComponent<BoxCollider2D>();
        }
    }

    void OnMouseDown()
    {
        isDragging = true;
        // Calculate offset between mouse and token position
        offset = transform.position - GetMouseWorldPosition();
    }

    void OnMouseDrag()
    {
        if (isDragging)
        {
            transform.position = GetMouseWorldPosition() + offset;
        }
    }

    void OnMouseUp()
    {
        isDragging = false;
        // Snap back to original position
        transform.position = originalPosition;
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePoint = Input.mousePosition;
        mousePoint.z = Camera.main.WorldToScreenPoint(transform.position).z;
        return Camera.main.ScreenToWorldPoint(mousePoint);
    }
}
