using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ColorLoop : MonoBehaviour
{
    [Header("Color Loop Settings")]
    [SerializeField] private float loopSpeed = 1f; // Speed of the hue loop
    [SerializeField] private float saturation = 1f; // Saturation value (0-1)
    [SerializeField] private float brightness = 1f; // Brightness/Value (0-1)
    
    private Text textComponent;
    private float currentHue = 0f;

    // Start is called before the first frame update
    void Start()
    {
        // Get the Text component attached to this GameObject
        textComponent = GetComponent<Text>();
        
        if (textComponent == null)
        {
            Debug.LogError("ColorLoop: No Text component found on " + gameObject.name);
            enabled = false; // Disable this script if no Text component is found
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (textComponent != null)
        {
            // Increment the hue value based on time and speed
            currentHue += loopSpeed * Time.deltaTime * 360f;
            
            // Keep hue in the 0-360 range by using modulo
            currentHue = currentHue % 360f;
            
            // Convert HSV to RGB and apply to text component
            Color newColor = Color.HSVToRGB(currentHue / 360f, saturation, brightness);
            
            // Preserve the alpha channel from the original color
            newColor.a = textComponent.color.a;
            
            textComponent.color = newColor;
        }
    }
}
