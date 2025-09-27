using UnityEngine;

public class SoundMaster : MonoBehaviour
{
    public static SoundMaster Instance { get; private set; }

    [Header("Master Volume Controls")]
    [SerializeField] private float masterVolume = 1f;
    [SerializeField] private float soundEffectsVolume = 1f;
    [SerializeField] private float musicVolume = 1f;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Load saved volume settings
            LoadVolumeSettings();
            
            if (enableDebugLogs)
                Debug.Log("[SoundMaster] Initialized with master volume system");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Loads volume settings from PlayerPrefs
    /// </summary>
    private void LoadVolumeSettings()
    {
        masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        soundEffectsVolume = PlayerPrefs.GetFloat("SoundEffectsVolume", 1f);
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
        
        if (enableDebugLogs)
        {
            Debug.Log($"[SoundMaster] Loaded volume settings - Master: {masterVolume:F2}, SFX: {soundEffectsVolume:F2}, Music: {musicVolume:F2}");
        }
    }

    /// <summary>
    /// Sets the master volume (affects all sounds)
    /// </summary>
    /// <param name="volume">Volume level (0.0 to 1.0)</param>
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
        PlayerPrefs.Save();
        
        if (enableDebugLogs)
            Debug.Log($"[SoundMaster] Master volume set to: {masterVolume:F2}");
    }

    /// <summary>
    /// Sets the sound effects volume
    /// </summary>
    /// <param name="volume">Volume level (0.0 to 1.0)</param>
    public void SetSoundEffectsVolume(float volume)
    {
        soundEffectsVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat("SoundEffectsVolume", soundEffectsVolume);
        PlayerPrefs.Save();
        
        // Update all sound effects controllers
        UpdateAllSoundEffectsControllers();
        
        if (enableDebugLogs)
            Debug.Log($"[SoundMaster] Sound effects volume set to: {soundEffectsVolume:F2}");
    }

    /// <summary>
    /// Sets the music volume
    /// </summary>
    /// <param name="volume">Volume level (0.0 to 1.0)</param>
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
        PlayerPrefs.Save();
        
        if (enableDebugLogs)
            Debug.Log($"[SoundMaster] Music volume set to: {musicVolume:F2}");
    }

    /// <summary>
    /// Gets the effective volume for sound effects (master * sound effects)
    /// </summary>
    /// <returns>Effective volume (0.0 to 1.0)</returns>
    public float GetEffectiveSoundEffectsVolume()
    {
        return masterVolume * soundEffectsVolume;
    }

    /// <summary>
    /// Gets the effective volume for music (master * music)
    /// </summary>
    /// <returns>Effective volume (0.0 to 1.0)</returns>
    public float GetEffectiveMusicVolume()
    {
        return masterVolume * musicVolume;
    }

    /// <summary>
    /// Gets the current master volume
    /// </summary>
    /// <returns>Master volume (0.0 to 1.0)</returns>
    public float GetMasterVolume()
    {
        return masterVolume;
    }

    /// <summary>
    /// Gets the current sound effects volume
    /// </summary>
    /// <returns>Sound effects volume (0.0 to 1.0)</returns>
    public float GetSoundEffectsVolume()
    {
        return soundEffectsVolume;
    }

    /// <summary>
    /// Gets the current music volume
    /// </summary>
    /// <returns>Music volume (0.0 to 1.0)</returns>
    public float GetMusicVolume()
    {
        return musicVolume;
    }

    /// <summary>
    /// Updates all SoundEffectsController instances with the current effective volume
    /// </summary>
    private void UpdateAllSoundEffectsControllers()
    {
        SoundEffectsController[] controllers = FindObjectsOfType<SoundEffectsController>();
        float effectiveVolume = GetEffectiveSoundEffectsVolume();
        
        foreach (var controller in controllers)
        {
            if (controller != null)
            {
                controller.SetVolume(effectiveVolume);
            }
        }
        
        if (enableDebugLogs)
            Debug.Log($"[SoundMaster] Updated {controllers.Length} SoundEffectsController instances with effective volume: {effectiveVolume:F2}");
    }

    /// <summary>
    /// Forces update of all sound effects controllers (useful for testing)
    /// </summary>
    [ContextMenu("Force Update All Sound Controllers")]
    public void ForceUpdateAllSoundControllers()
    {
        UpdateAllSoundEffectsControllers();
    }

    /// <summary>
    /// Resets all volumes to default values
    /// </summary>
    [ContextMenu("Reset All Volumes")]
    public void ResetAllVolumes()
    {
        SetMasterVolume(1f);
        SetSoundEffectsVolume(1f);
        SetMusicVolume(1f);
        
        if (enableDebugLogs)
            Debug.Log("[SoundMaster] All volumes reset to default (100%)");
    }

    /// <summary>
    /// Prints current volume settings to console
    /// </summary>
    [ContextMenu("Print Volume Settings")]
    public void PrintVolumeSettings()
    {
        Debug.Log($"[SoundMaster] Current Volume Settings:");
        Debug.Log($"  Master Volume: {masterVolume:F2} ({masterVolume * 100:F0}%)");
        Debug.Log($"  Sound Effects Volume: {soundEffectsVolume:F2} ({soundEffectsVolume * 100:F0}%)");
        Debug.Log($"  Music Volume: {musicVolume:F2} ({musicVolume * 100:F0}%)");
        Debug.Log($"  Effective SFX Volume: {GetEffectiveSoundEffectsVolume():F2} ({GetEffectiveSoundEffectsVolume() * 100:F0}%)");
        Debug.Log($"  Effective Music Volume: {GetEffectiveMusicVolume():F2} ({GetEffectiveMusicVolume() * 100:F0}%)");
    }
}
