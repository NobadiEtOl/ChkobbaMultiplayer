using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CardDrag : MonoBehaviour
{
    private Rigidbody rb;
    private bool dragging = false;
    private Vector3 localClickOffset;
    private Vector3 originalRotation; // Original rotation of the card

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false; // Ensure the card doesn't fall due to gravity
        rb.angularDrag = 5f; // Add some angular drag for smoother rotation
    }

    void OnMouseDown()
    {
        originalRotation = transform.rotation.eulerAngles; // Store the original rotation
        dragging = true;

        // Convert click point to local offset for realistic pivot behavior
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = rb.position.z; // Maintain the card's Z position
        localClickOffset = mouseWorld - rb.position;
    }

    void OnMouseUp()
    {
        dragging = false;

        // Stop movement and rotation when the mouse is released
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        transform.rotation = Quaternion.Euler(originalRotation); // Reset to original rotation
    }

    void FixedUpdate()
    {
        if (!dragging) return;

        // Get the current mouse position in world space
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = rb.position.z; // Maintain the card's Z position

        // Calculate the world grab point (where the card was clicked)
        Vector3 worldGrabPoint = rb.position + localClickOffset;

        // Calculate the force to move the grab point toward the mouse
        Vector3 force = (mouseWorld - worldGrabPoint) * 0.00001f;

        // Apply the force at the grab point to move the card
        rb.AddForceAtPosition(force, worldGrabPoint);

        // Add damping to reduce excessive spinning
        rb.angularVelocity *= 0.9f;
    }
}