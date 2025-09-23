using UnityEngine;

public class SoundEffectsController : MonoBehaviour
{
    [Header("Card Deal Sound Effects")]
    [SerializeField] private AudioClip cardDealSound;
    
    [Header("Audio Source")]
    [SerializeField] private AudioSource audioSource;
    
    [Header("Pitch Control")]
    [SerializeField] private float basePitch = 1.0f;
    [SerializeField] private float pitchIncrement = 0.1f;
    [SerializeField] private float maxPitch = 2.0f;
    
    private float currentPitch;
    
    private void Awake()
    {
        // If no AudioSource is assigned, create one
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Configure audio source for sound effects
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        
        // Initialize pitch
        currentPitch = basePitch;
    }
    
    /// <summary>
    /// Plays the card deal sound effect with current pitch
    /// </summary>
    public void PlayCardDealSound()
    {
        if (cardDealSound != null && audioSource != null)
        {
            // Set the pitch for this sound
            audioSource.pitch = currentPitch;
            audioSource.PlayOneShot(cardDealSound);
            
            // Increment pitch for next card (but don't exceed max)
            currentPitch = Mathf.Min(currentPitch + pitchIncrement, maxPitch);
            
            Debug.Log($"[SoundEffectsController] Playing card deal sound with pitch: {audioSource.pitch}");
        }
        else
        {
            if (cardDealSound == null)
                Debug.LogWarning("[SoundEffectsController] Card deal sound is not assigned!");
            if (audioSource == null)
                Debug.LogWarning("[SoundEffectsController] AudioSource is not assigned!");
        }
    }
    
    /// <summary>
    /// Plays a custom sound effect
    /// </summary>
    /// <param name="soundClip">The audio clip to play</param>
    public void PlaySoundEffect(AudioClip soundClip)
    {
        if (soundClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(soundClip);
            Debug.Log($"[SoundEffectsController] Playing sound: {soundClip.name}");
        }
        else
        {
            if (soundClip == null)
                Debug.LogWarning("[SoundEffectsController] Sound clip is null!");
            if (audioSource == null)
                Debug.LogWarning("[SoundEffectsController] AudioSource is not assigned!");
        }
    }
    
    /// <summary>
    /// Sets the volume for sound effects
    /// </summary>
    /// <param name="volume">Volume level (0.0 to 1.0)</param>
    public void SetVolume(float volume)
    {
        if (audioSource != null)
        {
            audioSource.volume = Mathf.Clamp01(volume);
        }
    }
    
    /// <summary>
    /// Stops all currently playing sounds
    /// </summary>
    public void StopAllSounds()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }
    
    /// <summary>
    /// Resets the pitch to base value for a new dealing round
    /// </summary>
    public void ResetPitch()
    {
        currentPitch = basePitch;
        Debug.Log($"[SoundEffectsController] Pitch reset to base pitch: {basePitch}");
    }
}
