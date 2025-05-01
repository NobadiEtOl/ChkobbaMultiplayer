using UnityEngine;
using UnityEngine.UI;

public class MusicSlider : MonoBehaviour
{
    [SerializeField] private Slider volumeSlider; // Reference to the slider
    [SerializeField] private int audioClipIndex; // Index of the audio clip to adjust

    private void Start()
    {
        if (volumeSlider != null)
        {
            // Add listener to call AdjustVolume when the slider value changes
            volumeSlider.onValueChanged.AddListener(HandleSliderValueChanged);

            // Optionally, initialize the slider's value with the current volume
            volumeSlider.value = 0.04f;
        }
    }

    private void HandleSliderValueChanged(float value)
    {
        // Call the AudioManager's AdjustVolume method
        AudioManager.Instance.AdjustVolume(audioClipIndex, value);
    }
}
