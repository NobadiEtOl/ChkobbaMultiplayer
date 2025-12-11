using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VintageRobo_ScreenController : MonoBehaviour
{
    [Header("Sprite Renderer")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Emotion Animation Frames")]
    [SerializeField] private Sprite[] smileFrames;
    [SerializeField] private Sprite[] frownFrames;
    [SerializeField] private Sprite[] neutralFrames;
    [SerializeField] private Sprite[] angryFrames;
    [SerializeField] private Sprite[] talkingFrames;

    [Header("Animation Settings")]
    [SerializeField] private float frameRate = 24f; // Frames per second
    [SerializeField] private bool randomizeFrames = true;

    [Header("Current State")]
    [SerializeField] private EmotionState currentState = EmotionState.Neutral;

    private Coroutine animationCoroutine;

    public enum EmotionState
    {
        Smile,
        Frown,
        Neutral,
        Angry,
        Talking
    }

    private void Awake()
    {
        // Auto-find SpriteRenderer if not assigned
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    private void Start()
    {
        SetState(currentState);
    }

    public void SetState(EmotionState newState)
    {
        currentState = newState;
        
        // Stop current animation
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }

        // Start new animation
        animationCoroutine = StartCoroutine(PlayAnimation(GetFramesForState(newState)));
    }

    private Sprite[] GetFramesForState(EmotionState state)
    {
        switch (state)
        {
            case EmotionState.Smile:
                return smileFrames;
            case EmotionState.Frown:
                return frownFrames;
            case EmotionState.Neutral:
                return neutralFrames;
            case EmotionState.Angry:
                return angryFrames;
            case EmotionState.Talking:
                return talkingFrames;
            default:
                return neutralFrames;
        }
    }

    private IEnumerator PlayAnimation(Sprite[] frames)
    {
        if (frames == null || frames.Length == 0)
        {
            Debug.LogWarning($"No frames assigned for state: {currentState}");
            yield break;
        }

        Debug.Log($"Starting animation for state: {currentState} with {frames.Length} frames");

        while (true)
        {
            // Get random frame order for this loop
            List<int> frameIndices = new List<int>();
            for (int i = 0; i < frames.Length; i++)
            {
                frameIndices.Add(i);
            }
            
            ShuffleList(frameIndices);
            
            //Debug.Log($"Playing animation loop for state: {currentState}");
            
            // Play all frames in random order using custom frame rate
            float frameTime = 1f / frameRate;
            foreach (int index in frameIndices)
            {
                if (frames[index] != null)
                {
                    spriteRenderer.sprite = frames[index];
                    //Debug.Log($"Displaying frame {index} for state: {currentState}");
                }
                else
                {
                    Debug.LogWarning($"Frame {index} is null for state: {currentState}");
                }
                yield return new WaitForSeconds(frameTime);
            }
            
            //Debug.Log($"Completed one loop for state: {currentState}, looping again...");
        }
    }

    private void ShuffleList(List<int> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            int temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    // Public methods for state changes
    public void ShowSmile() => SetState(EmotionState.Smile);
    public void ShowFrown() => SetState(EmotionState.Frown);
    public void ShowNeutral() => SetState(EmotionState.Neutral);
    public void ShowAngry() => SetState(EmotionState.Angry);
    public void StartTalking() => SetState(EmotionState.Talking);

    // Editor helper methods
    #if UNITY_EDITOR
    [ContextMenu("Preview Smile")]
    private void PreviewSmile() => ShowSmile();

    [ContextMenu("Preview Frown")]
    private void PreviewFrown() => ShowFrown();

    [ContextMenu("Preview Neutral")]
    private void PreviewNeutral() => ShowNeutral();

    [ContextMenu("Preview Angry")]
    private void PreviewAngry() => ShowAngry();

    [ContextMenu("Preview Talking")]
    private void PreviewTalking() => StartTalking();
    #endif
}
