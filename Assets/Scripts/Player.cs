using Unity.Netcode;
using UnityEngine;

public class Player : NetworkBehaviour
{
    public float moveSpeed = 5f;

    // Handle movement on the client side
    void Update()
    {
        if (IsOwner) // Ensure the player moves only if they own the object
        {
            float moveX = Input.GetAxis("Horizontal");
            float moveY = Input.GetAxis("Vertical");

            Vector3 move = new Vector3(moveX, 0, moveY) * moveSpeed * Time.deltaTime;
            transform.Translate(move);
        }
    }
}
