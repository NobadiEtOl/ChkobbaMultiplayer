using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarpetScript : MonoBehaviour
{
    void Start()
    {
        FitSpriteToScreen();
    }

    void FitSpriteToScreen()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null) return;

        // Get sprite size in pixels
        float spriteWidth = sr.sprite.rect.width;
        //Debug.Log("Sprite Width: " + spriteWidth);
        float spriteHeight = sr.sprite.rect.height;

        // Get world size of the sprite (before scaling)
        float spritePixelsPerUnit = sr.sprite.pixelsPerUnit;
        float spriteWorldWidth = spriteWidth / spritePixelsPerUnit;

        // Get screen aspect ratio
        float screenAspect = (float)Screen.width / (float)Screen.height;

        // Get camera
        Camera cam = Camera.main;
        if (cam == null) return;

        // Get world width visible by camera at z=0
        float worldScreenHeight = cam.orthographicSize * 2f;
        //Debug.Log("World Screen Height: " + worldScreenHeight);
        float worldScreenWidth = worldScreenHeight * screenAspect;
        //Debug.Log("World Screen Width: " + worldScreenWidth);

        // Calculate scale to fit the screen width
        float scale = worldScreenWidth / spriteWorldWidth;

        // Use the same scale for both x and y to preserve aspect ratio
        transform.localScale = new Vector3(scale, scale, 1f);
    }
}