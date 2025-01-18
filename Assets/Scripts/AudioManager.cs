using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance; // Singleton instance

    [Header("Audio Clips")]
    public List<AudioClip> audioClips; // Assign audio clips from the Inspector

    private Dictionary<int, AudioSource> audioSourceMap = new Dictionary<int, AudioSource>(); // Map to manage AudioSources by index

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keep the AudioManager across scenes
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    public void PlayAudio(int index, float volume = 1f, bool loop = false)
    {
        if (index >= 0 && index < audioClips.Count)
        {
            if (audioSourceMap.ContainsKey(index))
            {
                Debug.LogWarning($"Audio clip at index {index} is already playing!");
                return;
            }

            AudioSource newAudioSource = gameObject.AddComponent<AudioSource>();
            newAudioSource.clip = audioClips[index];
            newAudioSource.volume = volume;
            newAudioSource.loop = loop;
            newAudioSource.Play();

            audioSourceMap[index] = newAudioSource;

            if (!loop)
            {
                Destroy(newAudioSource, audioClips[index].length); // Automatically destroy non-looping AudioSources
                audioSourceMap.Remove(index);
            }
        }
        else
        {
            Debug.LogWarning($"Audio clip index {index} is out of range!");
        }
    }

    public void StopAudioByIndex(int index)
    {
        if (audioSourceMap.TryGetValue(index, out AudioSource audioSource))
        {
            audioSource.Stop();
            Destroy(audioSource);
            audioSourceMap.Remove(index);
        }
    }

    public void StopAllAudio()
    {
        foreach (var audioSource in audioSourceMap.Values)
        {
            if (audioSource != null)
            {
                audioSource.Stop();
                Destroy(audioSource);
            }
        }
        audioSourceMap.Clear();
    }

    /// <summary>
    /// Adjusts the volume of a playing audio clip by index.
    /// </summary>
    /// <param name="index">Index of the audio clip.</param>
    /// <param name="volume">New volume (0 to 1).</param>
    public void AdjustVolume(int index, float volume)
    {
        if (audioSourceMap.TryGetValue(index, out AudioSource audioSource))
        {
            audioSource.volume = Mathf.Clamp01(volume); // Ensure volume is between 0 and 1
        }
        else
        {
            Debug.LogWarning($"No audio clip playing at index {index} to adjust volume.");
        }
    }

    public float GetAudioSourceVolume(int index)
    {
        if (audioSourceMap.TryGetValue(index, out AudioSource audioSource))
        {
            return audioSource.volume;
        }
        return 1f; // Default to max volume if no AudioSource found
    }
}
